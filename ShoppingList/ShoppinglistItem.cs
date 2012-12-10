using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Globalization;
using System.Collections.ObjectModel;
using SharedClasses;
using System.ComponentModel;

namespace ShoppingList
{
	public class ShoppinglistItem : INotifyPropertyChanged
	{
		public int Index { get; set; }
		public string Username { get; set; }
		public DateTime Created { get; set; }
		public string ItemCategory { get; set; }
		private string _itemname;
		public string ItemName { get { return _itemname; } set { _itemname = value; OnPropertyChanged("ItemName"); } }
		private bool _isbusyuploadingonline;
		public bool IsBusyUploadingOnline { get { return _isbusyuploadingonline; } set { _isbusyuploadingonline = value; OnPropertyChanged("IsBusyUploadingOnline"); } }
		public DateTime? CategoryDueDate;//Only reminders per category at this stage (2012-12-02), not per item
		public bool CategoryStopReminding;//Only reminders per category at this stage (2012-12-02), not per item

		public ShoppinglistItem(int Index, string Username, DateTime Created, string ItemCategory, string ItemName, DateTime? CategoryDueDate, bool CategoryStopReminding)
		{
			this.Index = Index;
			this.Username = Username;
			this.Created = Created;
			this.ItemCategory = ItemCategory;
			this.ItemName = ItemName;
			this.IsBusyUploadingOnline = false;
			this.CategoryDueDate = CategoryDueDate;
			this.CategoryStopReminding = CategoryStopReminding;
		}

		public static string GetCurrentUsername() { return SettingsSimple.WebsiteKeys.Instance.Username; }

		public static string DoShoppinglistTask(string taskname, Action<string> onError, params string[] urlParams)
		{
			string username, apikey, appsecret_shoppinglist;
			if (!GetShoppinglist_ApikeyAndAppsecret(out username, out apikey, out appsecret_shoppinglist))
				return null;
			else
			{
				string username_Hex = EncodeAndDecodeInterop.EncodeStringHex(username, onError);

				string apikey_EncryptWithAppsecret = EncryptionInterop.SimpleTripleDesEncrypt(apikey, appsecret_shoppinglist);
				string apikey_Hex = EncodeAndDecodeInterop.EncodeStringHex(apikey_EncryptWithAppsecret, onError);

				for (int i = 0; i < urlParams.Length; i++)
					urlParams[i] = EncodeAndDecodeInterop.EncodeStringHex(urlParams[i], onError);

				string preformattedString = "{0}/shoppinglist/{1}/{2}/{3}{4}";//{2}=username, {3}=apikey_Hex, {4}=urlParams(if exists)
				string urlparamsString = "";
				for (int i = 0; i < urlParams.Length; i++)
					urlparamsString += "/{" + i + "}";
				urlparamsString = string.Format(urlparamsString, urlParams);

				var s = PhpInterop.PostPHP(
					null,
					string.Format(
						preformattedString,
					//"http://localhost:8081",
						SettingsSimple.HomePcUrls.Instance.WebappsRoot,
						taskname,
						username_Hex,
						apikey_Hex,
						urlparamsString), "");

				try
				{
					string decrypted = EncryptionInterop.SimpleTripleDesDecrypt(s, appsecret_shoppinglist);
					return decrypted;
				}
				catch (Exception exc)
				{
					onError(exc.Message + Environment.NewLine + Environment.NewLine + s);
					return null;
				}
			}
		}

		public bool ChangeItemName(string oldValue, string newValue, Action<string> onError = null)
		{
			if (onError == null) onError = delegate { };

			this.IsBusyUploadingOnline = true;
			string result = DoShoppinglistTask("desktop_changeitemname", onError, this.Index.ToString(), newValue);
			this.IsBusyUploadingOnline = false;
			if (result != null && result.StartsWith("Item changed:", StringComparison.InvariantCultureIgnoreCase))
				return true;
			else
			{
				this.ItemName = null;
				this.ItemName = oldValue;
				return false;
			}
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
						&& tmpdict.ContainsKey("itemname")
						&& tmpdict.ContainsKey("duedate")
						&& tmpdict.ContainsKey("stopreminding"))
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

			DateTime tmpparsedduedate;
			DateTime? duedate = null;
			if (!DateTime.TryParseExact((dict["duedate"] ?? "").ToString(), dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out tmpparsedduedate))
			{
				//errorIfFailed = "Invalid DUEDATE is not a valid DateTime: " + dict["duedate"];
				//return null;
				duedate = null;
			}
			else
				duedate = tmpparsedduedate;

			errorIfFailed = null;
			return new ShoppinglistItem(
				index,
				dict["username"].ToString(),
				created,
				dict["itemcategory"].ToString(),
				dict["itemname"].ToString(),
				duedate,
				(dict["stopreminding"] ?? "").ToString() == "1");
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

			string result = DoShoppinglistTask("desktop_getall", onError);
			JSON.SetDefaultJsonInstanceSettings();
			createdList = new ObservableCollection<ShoppinglistItem>(
				ShoppinglistItem.ProcessFromWebArrayList(JSON.Instance.Parse(result) as ArrayList, onError));
			return createdList;
		}

		public static bool AddItem(string category, string itemname, Action<string> onError)
		{
			string result = DoShoppinglistTask("desktop_additem", onError, category, itemname);
			if (result.StartsWith("Item added:", StringComparison.InvariantCultureIgnoreCase))
				return true;
			else
			{
				onError("ERROR adding item: " + result);
				return false;
			}
		}

		public event PropertyChangedEventHandler PropertyChanged = delegate { };
		public void OnPropertyChanged(string propertyName) { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }
	}

	public class ShoppinglistCategoryWithItems : INotifyPropertyChanged
	{
		private string _categoryname;
		public string CategoryName { get { return _categoryname; } set { _categoryname = value; OnPropertyChanged("CategoryName"); } }
		private bool _isbusyuploadingonline;
		public bool IsBusyUploadingOnline { get { return _isbusyuploadingonline; } set { _isbusyuploadingonline = value; OnPropertyChanged("IsBusyUploadingOnline"); } }
		public ObservableCollection<ShoppinglistItem> Items { get; set; }
		private DateTime? _duedate;
		public DateTime? DueDate { get { return _duedate; } set { _duedate = value; OnPropertyChanged("DueDate", "HasReminder"); } }
		public bool HasReminder { get { return DueDate.HasValue; } }
		private bool _stopreminding;
		public bool StopReminding { get { return _stopreminding; } set { _stopreminding = value; OnPropertyChanged("StopReminding"); } }

		public ShoppinglistCategoryWithItems(string CategoryName, DateTime? DueDate, bool StopReminding, ObservableCollection<ShoppinglistItem> Items)
		{
			this.CategoryName = CategoryName;
			this.Items = Items;
			this.IsBusyUploadingOnline = false;
			this.DueDate = DueDate;
			this.StopReminding = StopReminding;
		}

		public event PropertyChangedEventHandler PropertyChanged = delegate { };
		public void OnPropertyChanged(params string[] propertyNames) { foreach (var pn in propertyNames) PropertyChanged(this, new PropertyChangedEventArgs(pn)); }

		public bool ChangeCategoryName(string oldValue, string newValue, Action<string> onError = null)
		{
			if (onError == null) onError = delegate { };

			this.IsBusyUploadingOnline = true;
			string result = ShoppinglistItem.DoShoppinglistTask("desktop_changecategoryname", onError, oldValue, newValue);
			this.IsBusyUploadingOnline = false;
			if (result != null && result.StartsWith("Category changed:", StringComparison.InvariantCultureIgnoreCase))
				return true;
			else
			{
				this.CategoryName = null;
				this.CategoryName = oldValue;
				return false;
			}
		}

		public bool AddReminder(DateTime reminderDate, Action<string> onError = null)
		{
			if (onError == null) onError = delegate { };

			this.IsBusyUploadingOnline = true;
			string result = ShoppinglistItem.DoShoppinglistTask("desktop_addreminder", onError, this.CategoryName, reminderDate.ToString("yyyy-MM-dd HH:mm:ss"));
			this.IsBusyUploadingOnline = false;
			if (result != null && result.StartsWith("Reminder added:", StringComparison.InvariantCultureIgnoreCase))
			{
				this.DueDate = reminderDate;
				this.StopReminding = false;
				return true;
			}
			else
			{
				onError("Unable to add reminder for category '" + this.CategoryName + "': " + result);
				return false;
			}
		}

		public bool StopReminder(Action<string> onError = null)
		{
			if (onError == null) onError = delegate { };

			this.IsBusyUploadingOnline = true;
			string result = ShoppinglistItem.DoShoppinglistTask("desktop_stopreminder", onError, this.CategoryName);
			this.IsBusyUploadingOnline = false;
			if (result != null && result.StartsWith("Reminder removed:", StringComparison.InvariantCultureIgnoreCase))
			{
				this.StopReminding = true;
				return true;
			}
			else
			{
				onError("Unable to add reminder for category '" + this.CategoryName + "': " + result);
				return false;
			}
		}
	}
}
