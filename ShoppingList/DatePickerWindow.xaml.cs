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
using System.Windows.Shapes;

namespace SharedClasses
{
	/// <summary>
	/// Interaction logic for DatePickerWindow.xaml
	/// </summary>
	public partial class DatePickerWindow : Window
	{
		public DatePickerWindow()
		{
			InitializeComponent();

			dateTimePicker.Value = DateTime.Now.AddMinutes(30);
		}

		public static DateTime? PickDateTime()
		{
			DatePickerWindow win = new DatePickerWindow();
			if (win.ShowDialog() == true)
				return win.dateTimePicker.Value;
			else
				return null;
		}

		private void buttonAccept_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
		}	
	}
}
