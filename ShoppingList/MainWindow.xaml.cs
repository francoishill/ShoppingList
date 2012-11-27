using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SharedClasses;
using System.Windows.Interop;
using System.Collections.ObjectModel;
using System.Collections;

namespace ShoppingList
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, System.Windows.Forms.IWin32Window
	{
		public const string cUriStartString = "shoppinglist";
		public const string cUriProtocolHandlerCommandlineArgument = "uriprotocolhandler";

		public MainWindow()
		{
			InitializeComponent();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			listboxShoppinglist.ItemsSource = ShoppinglistItem.GetListFromOnline(OnError);

			string appPath = RegistryInterop.GetAppPathFromRegistry("ShoppingList");
			if (appPath != null && appPath == Environment.GetCommandLineArgs()[0])//This application is installed
				RegistryInterop.AssociateUrlProtocolHandler(
					cUriStartString,
					"Shopping List with Firepuma",
					"\"" + Environment.GetCommandLineArgs()[0] + "\" " + cUriProtocolHandlerCommandlineArgument + " \"%1\"");
		}

		private void OnError(string error)
		{
			UserMessages.ShowErrorMessage("ERROR: " + error);
		}

		public IntPtr Handle
		{
			get { return new WindowInteropHelper(this).Handle; }
		}

		private void listboxShoppinglist_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			listboxShoppinglist.SelectedItem = null;
		}

		private void buttonAddItemClick(object sender, RoutedEventArgs e)
		{
			string category = InputBoxWPF.Prompt("Please enter the category", "Category");
			string itemname = category == null ? null : InputBoxWPF.Prompt("Please enter the item name", "Item name");

			if (category == null || itemname == null)
				return;

			if (ShoppinglistItem.AddItem(category, itemname, OnError))
			{
				UserMessages.ShowInfoMessage("Item added '" + itemname + "' with category '" + category + "'");
				//We succeeded in adding an item, add it to the list again
			}
		}
	}
}
