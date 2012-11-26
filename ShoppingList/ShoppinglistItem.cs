using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Globalization;

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
	}
}
