using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using SharedClasses;
using System.IO;
using System.Security.Cryptography;
using Rhino.Licensing;
using StrKeyVal = System.Collections.Generic.Dictionary<string, string>;

namespace ShoppingList
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public static MainWindow mainwindow;

		public static void ShowError(string err)
		{
			System.Windows.Forms.Application.EnableVisualStyles();
			if (mainwindow == null)
				System.Windows.Forms.MessageBox.Show(err, "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
			else
				mainwindow.Dispatcher.Invoke((Action)delegate
				{
					System.Windows.Forms.MessageBox.Show(mainwindow, err, "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
				});
		}

		//private const string publicKeyPath = @"C:\Francois\Other\publicKey.xml";
		//private const string privateKeyPath = @"C:\Francois\Other\privateKey.xml";
		//private const string licensePath = @"C:\Francois\Other\license.xml";		

		protected override void OnStartup(StartupEventArgs e)
		{
			AutoUpdating.CheckForUpdates_ExceptionHandler(null);
			/*Intentially created exception
			int i = 0;
			var j = 1 / i;*/

			Dictionary<string, string> userPrivilages;
			if (!LicensingInterop_Client.Client_ValidateLicense(out userPrivilages, ShowError))
				Environment.Exit(LicensingInterop_Client.cApplicationExitCodeIfLicenseFailedValidation);

			SingleInstanceApplicationManager<MainWindow>.CheckIfAlreadyRunningElseCreateNew(
				(evt, mainwin) =>
				{
					mainwin.Dispatcher.Invoke((Action)delegate
					{
						PerformCommandFromArguments(evt.CommandLineArgs, mainwin);
					});
				},
				(args, mainwin) =>
				{
					//AppDomain.CurrentDomain.UnhandledException += (snder, exc) =>
					//{
					//    Exception exception = (Exception)exc.ExceptionObject;
					//    ShowError("Exception" + (exc.IsTerminating ? ", application will now exit" : "") + ":"
					//        + exception.Message + Environment.NewLine + exception.StackTrace);
					//};

					//AutoUpdating.CheckForUpdates(null, null);

					//ApplicationRecoveryAndRestart.RegisterForRecoveryAndRestart(

					mainwindow = mainwin;
					PerformCommandFromArguments(args, mainwindow);
				});
		}

		private void PerformCommandFromArguments(string[] commandlineArgs, MainWindow mainwin)
		{
			//Just always show it
			mainwin.Show();
			mainwin.Topmost = !mainwin.Topmost;
			mainwin.Topmost = !mainwin.Topmost;
			mainwin.Activate();

			var args = commandlineArgs.ToList();
			args.RemoveAt(0);//Remove the EXE path
			if (args.Count >= 1 && args[0].ToLower() == ShoppingList.MainWindow.cUriProtocolHandlerCommandlineArgument.ToLower())
			{
				string commandFromUri =
								args[1].Substring(ShoppingList.MainWindow.cUriStartString.Length + 1).ToLower();//The +1 is because we must add the ':' character
				if (commandFromUri == "show")
				{
				}
				else if (commandFromUri == "additem")
				{
					UserMessages.ShowInfoMessage("Must add a dialog for a new item here...");
				}
			}
		}
	}
}
