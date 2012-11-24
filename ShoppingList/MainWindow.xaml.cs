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
			//string appPath = RegistryInterop.GetAppPathFromRegistry("ShoppingList");
			//if (appPath != null && appPath == Environment.GetCommandLineArgs()[0])//This application is installed
				RegistryInterop.AssociateUrlProtocolHandler(
					cUriStartString,
					"Shopping List with Firepuma",
					"\"" + Environment.GetCommandLineArgs()[0] + "\" " + cUriProtocolHandlerCommandlineArgument + " \"%1\"");
		}

		public IntPtr Handle
		{
			get { return new WindowInteropHelper(this).Handle; }
		}
	}
}
