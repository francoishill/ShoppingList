using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using SharedClasses;

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


		protected override void OnStartup(StartupEventArgs e)
		{
			SingleInstanceApplicationManager<MainWindow>.CheckIfAlreadyRunningElseCreateNew(
				(evt, mainwin) =>
				{
					mainwin.Dispatcher.Invoke((Action)delegate
					{
						var args = evt.CommandLineArgs.ToList();
						args.RemoveAt(0);//Remove the EXE path
						if (args.Count >= 1 && args[0].ToLower() == ShoppingList.MainWindow.cUriProtocolHandlerCommandlineArgument.ToLower())
						{
							string commandFromUri =
								args[1].Substring(ShoppingList.MainWindow.cUriStartString.Length + 1).ToLower();//The +1 is because we must add the ':' character
							if (commandFromUri == "show")
							{
								mainwin.Show();
								mainwin.Topmost = !mainwin.Topmost;
								mainwin.Topmost = !mainwin.Topmost;
								mainwin.Activate();
							}
						}
					});
				},
				(args, mainwin) =>
				{
					AppDomain.CurrentDomain.UnhandledException += (snder, exc) =>
					{
						Exception exception = (Exception)exc.ExceptionObject;
						ShowError("Exception" + (exc.IsTerminating ? ", application will now exit" : "") + ":"
							+ exception.Message + Environment.NewLine + exception.StackTrace);
					};

					ThreadingInterop.PerformVoidFunctionSeperateThread(() =>
					{
						AutoUpdating.CheckForUpdates(null, null);
					},
					false);

					//ApplicationRecoveryAndRestart.RegisterForRecoveryAndRestart(

					mainwindow = mainwin;
					mainwin.Show();
				});
		}
	}
}
