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
			listboxShoppinglist.ItemsSource = GetListFromOnline();

			//string appPath = RegistryInterop.GetAppPathFromRegistry("ShoppingList");
			//if (appPath != null && appPath == Environment.GetCommandLineArgs()[0])//This application is installed
			RegistryInterop.AssociateUrlProtocolHandler(
				cUriStartString,
				"Shopping List with Firepuma",
				"\"" + Environment.GetCommandLineArgs()[0] + "\" " + cUriProtocolHandlerCommandlineArgument + " \"%1\"");
		}

		private ObservableCollection<ShoppinglistItem> GetListFromOnline()
		{
			string username, apikey, appsecret_shoppinglist;
			if (!GetShoppinglist_ApikeyAndAppsecret(out username, out apikey, out appsecret_shoppinglist))
				return null;
			else
			{
				string username_Hex = EncodeAndDecodeInterop.EncodeStringHex(username, OnError);

				string apikey_EncryptWithAppsecret = PhpInterop.PhpEncryption.SimpleTripleDesEncrypt(apikey, appsecret_shoppinglist);
				string apikey_Hex = EncodeAndDecodeInterop.EncodeStringHex(apikey_EncryptWithAppsecret, OnError);

				int mustChange;
				//TODO: Must eventually change the URL to firepuma.com
				var s = PhpInterop.PostPHP(
					null,
					string.Format("http://localhost:8081/shoppinglist/desktop_getall/{0}/{1}", username_Hex, apikey_Hex),
					"");

				string decrypted = PhpInterop.PhpEncryption.SimpleTripleDesDecrypt(s, appsecret_shoppinglist);

				JSON.SetDefaultJsonInstanceSettings();

				return new ObservableCollection<ShoppinglistItem>(
					ShoppinglistItem.ProcessFromWebArrayList(JSON.Instance.Parse(decrypted) as ArrayList, OnError));
			}
		}

		private void OnError(string error)
		{
			UserMessages.ShowErrorMessage("ERROR: " + error);
		}

		private bool GetShoppinglist_ApikeyAndAppsecret(out string outUsername, out string outApiKey, out string outAppSecret)
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

		public IntPtr Handle
		{
			get { return new WindowInteropHelper(this).Handle; }
		}

		private void listboxShoppinglist_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			listboxShoppinglist.SelectedItem = null;
		}
	}
}
