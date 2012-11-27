using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Globalization;
using System.Collections.ObjectModel;
using SharedClasses;

namespace ShoppingList
{
	public class ShoppinglistItem
	{
		public int Index { get; set; }
		public string Username { get; set; }
		public DateTime Created { get; set; }
		public string ItemCategory { get; set; }
		public string ItemName { get; set; }

		public ShoppinglistItem(int Index, string Username, DateTime Created, string ItemCategory, string ItemName)
		{
			this.Index = Index;
			this.Username = Username;
			this.Created = Created;
			this.ItemCategory = ItemCategory;
			this.ItemName = ItemName;
		}

		public static List<ShoppinglistItem> ProcessFromWebArrayList(ArrayList arrList, Action<string> onError)
		{
			if (onError == null) onError = delegate { };

			List<ShoppinglistItem> tmplist = new List<ShoppinglistItem>();

			foreach (var obj in arrList)
				if (obj is Dictionary<string, object>)
				{
					Dictionary<string, object> tmpdict = obj as Dictionary<string, object>;
					if (tmpdict.ContainsKey("index")
						&& tmpdict.ContainsKey("username")
						&& tmpdict.ContainsKey("created")
						&& tmpdict.ContainsKey("itemcategory")
						&& tmpdict.ContainsKey("itemname"))
					{
						string tmperr;
						var item = GetShoppinglistItemFromDictionary(tmpdict, out tmperr);
						if (item == null)
							onError("Cannot get shoppinglist item: " + tmperr);
						else
							tmplist.Add(item);
					}
				}

			return tmplist;
		}

		private static ShoppinglistItem GetShoppinglistItemFromDictionary(Dictionary<string, object> dict, out string errorIfFailed)
		{
			int index;
			if (!int.TryParse(dict["index"].ToString(), out index))
			{
				errorIfFailed = "Item INDEX is not a valid integer: " + dict["index"];
				return null;
			}

			DateTime created;
			string dateFormat = "yyyy-MM-dd HH:mm:ss";
			if (!DateTime.TryParseExact(dict["created"].ToString(), dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out created))
			{
				errorIfFailed = "Invalid CREATED is not a valid DateTime: " + dict["created"];
				return null;
			}

			errorIfFailed = null;
			return new ShoppinglistItem(
				index,
				dict["username"].ToString(),
				created,
				dict["itemcategory"].ToString(),
				dict["itemname"].ToString());
		}

		private static bool GetShoppinglist_ApikeyAndAppsecret(out string outUsername, out string outApiKey, out string outAppSecret)
		{
			string appname = "shoppinglist";

			outUsername = SettingsSimple.WebsiteKeys.Instance.Username;
			outApiKey = SettingsSimple.WebsiteKeys.Instance.ApiKey;//Not really used at this stage

			var appsecrets = SettingsSimple.WebsiteKeys.Instance.AppSecrets;
			if (outUsername == null
				|| outApiKey == null
				|| appsecrets == null || !appsecrets.ContainsKey(appname))
			{
				UserMessages.ShowWarningMessage("Please enter the API key and App Secret first for the shopppinglist app.");
				outApiKey = null;
				outAppSecret = null;
				return false;
			}

			outAppSecret = appsecrets[appname];//Used to encrypt/decrypt (by passing username) the data passed for the shoppinglist application
			return true;
		}

		private static ObservableCollection<ShoppinglistItem> createdList = null;
		public static ObservableCollection<ShoppinglistItem> GetListFromOnline(Action<string> onError)
		{
			if (createdList != null)
				return createdList;

			string username, apikey, appsecret_shoppinglist;
			if (!GetShoppinglist_ApikeyAndAppsecret(out username, out apikey, out appsecret_shoppinglist))
				return null;
			else
			{
				string username_Hex = EncodeAndDecodeInterop.EncodeStringHex(username, onError);

				string apikey_EncryptWithAppsecret = PhpInterop.PhpEncryption.SimpleTripleDesEncrypt(apikey, appsecret_shoppinglist);
				string apikey_Hex = EncodeAndDecodeInterop.EncodeStringHex(apikey_EncryptWithAppsecret, onError);

				var s = PhpInterop.PostPHP(
					null,
					string.Format("{0}/shoppinglist/desktop_getall/{1}/{2}", SettingsSimple.HomePcUrls.Instance.WebappsRoot, username_Hex, apikey_Hex),
					"");

				string decrypted = PhpInterop.PhpEncryption.SimpleTripleDesDecrypt(s, appsecret_shoppinglist);

				JSON.SetDefaultJsonInstanceSettings();

				createdList = new ObservableCollection<ShoppinglistItem>(
					ShoppinglistItem.ProcessFromWebArrayList(JSON.Instance.Parse(decrypted) as ArrayList, onError));
				return createdList;
			}
		}

		public static bool AddItem(string category, string itemname, Action<string> onError)
		{
			string username, apikey, appsecret_shoppinglist;
			if (!GetShoppinglist_ApikeyAndAppsecret(out username, out apikey, out appsecret_shoppinglist))
				return false;
			else
			{
				string username_Hex = EncodeAndDecodeInterop.EncodeStringHex(username, onError);

				string apikey_EncryptWithAppsecret = PhpInterop.PhpEncryption.SimpleTripleDesEncrypt(apikey, appsecret_shoppinglist);
				string apikey_Hex = EncodeAndDecodeInterop.EncodeStringHex(apikey_EncryptWithAppsecret, onError);

				string category_Hex = EncodeAndDecodeInterop.EncodeStringHex(category, onError);
				string itemname_Hex = EncodeAndDecodeInterop.EncodeStringHex(itemname, onError);

				var s = PhpInterop.PostPHP(
					null,
					string.Format(
						"{0}/shoppinglist/desktop_additem/{1}/{2}/{3}/{4}",
						SettingsSimple.HomePcUrls.Instance.WebappsRoot,
						username_Hex,
						apikey_Hex,
						category_Hex,
						itemname_Hex
						),
					"");

				string decrypted = PhpInterop.PhpEncryption.SimpleTripleDesDecrypt(s, appsecret_shoppinglist);

				if (decrypted.StartsWith("Item added:", StringComparison.InvariantCultureIgnoreCase))
					return true;
				else
				{
					onError("ERROR adding item: " + decrypted);
					return false;
				}
			}
		}
	}
}
