﻿using System;
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
using System.Diagnostics.CodeAnalysis;

namespace ShoppingList
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, System.Windows.Forms.IWin32Window
	{
		public const string cUriStartString = "shoppinglist";
		public const string cUriProtocolHandlerCommandlineArgument = "uriprotocolhandler";
		private ObservableCollection<ShoppinglistCategoryWithItems> ItemlistGroupedByCategory = new ObservableCollection<ShoppinglistCategoryWithItems>();

		public MainWindow()
		{
			InitializeComponent();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			var onlineList = ShoppinglistItem.GetListFromOnline(OnError);
			Dictionary<string, List<ShoppinglistItem>> tmpdict = new Dictionary<string, List<ShoppinglistItem>>();
			foreach (var item in onlineList)
			{
				if (!tmpdict.ContainsKey(item.ItemCategory))
					tmpdict.Add(item.ItemCategory, new List<ShoppinglistItem>());
				tmpdict[item.ItemCategory].Add(item);
			}
			foreach (var cat in tmpdict.Keys)
				ItemlistGroupedByCategory.Add(new ShoppinglistCategoryWithItems(
					cat,
					tmpdict[cat][0].CategoryDueDate,
					tmpdict[cat][0].CategoryStopReminding,
					new ObservableCollection<ShoppinglistItem>(tmpdict[cat])));
			treeviewShoppingLists.ItemsSource = ItemlistGroupedByCategory;

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

		private void listboxShoppinglist_SelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			//listboxShoppinglist.SelectedItem = null;
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

		private void textboxItemName_TextChanged_LostFocus(object sender, DependencyPropertyChangedEventArgs e)
		{
			//This should happen when the control lost focus and the value has changed
			//The value has indeed been changed so we need to update it online
			FrameworkElement fe = sender as FrameworkElement;
			if (fe == null) return;
			ShoppinglistItem item = fe.DataContext as ShoppinglistItem;
			if (item == null) return;
			item.ChangeItemName(e.OldValue.ToString(), e.NewValue.ToString(), OnError);
		}

		private void textboxItemCategory_TextChanged_LostFocus(object sender, DependencyPropertyChangedEventArgs e)
		{
			//Item category changed (and we dont have focus anymore), so we will now update it online
			FrameworkElement fe = sender as FrameworkElement;
			if (fe == null) return;
			ShoppinglistCategoryWithItems catitem = fe.DataContext as ShoppinglistCategoryWithItems;
			if (catitem == null) return;
			if (catitem.ChangeCategoryName(e.OldValue.ToString(), e.NewValue.ToString(), OnError))
			{
				for (int i = 0; i < catitem.Items.Count; i++)
					catitem.Items[i].ItemCategory = e.NewValue.ToString();
			}
		}

		private void textboxItemCategory_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if ((e.Key == Key.Tab && Keyboard.Modifiers == ModifierKeys.Shift) || e.Key == Key.Up)
			{
				FrameworkElement fe = sender as FrameworkElement;
				if (fe == null) return;
				ShoppinglistCategoryWithItems catitem = fe.DataContext as ShoppinglistCategoryWithItems;
				if (catitem == null) return;
				e.Handled = true;
				FocusPreviousCategoryTextbox(catitem);
			}
			else if ((e.Key == Key.Tab && Keyboard.Modifiers != ModifierKeys.Shift) || e.Key == Key.Down)
			{
				FrameworkElement fe = sender as FrameworkElement;
				if (fe == null) return;
				ShoppinglistCategoryWithItems catitem = fe.DataContext as ShoppinglistCategoryWithItems;
				if (catitem == null) return;
				e.Handled = true;
				FocusNextCategoryTextbox(catitem);
			}
		}

		private void FocusNextCategoryTextbox(ShoppinglistCategoryWithItems categoryItem)
		{
			int curIndex = ItemlistGroupedByCategory.IndexOf(categoryItem);
			int newIndex = curIndex;
			if (curIndex + 1 < ItemlistGroupedByCategory.Count)
				newIndex = curIndex + 1;
			else
				newIndex = 0;
			FocusCategoryTextbox(ItemlistGroupedByCategory[newIndex]);
		}

		private void FocusPreviousCategoryTextbox(ShoppinglistCategoryWithItems categoryItem)
		{
			int curIndex = ItemlistGroupedByCategory.IndexOf(categoryItem);
			int newIndex = curIndex;
			if (curIndex - 1 >= 0)
				newIndex = curIndex - 1;
			else
				newIndex = ItemlistGroupedByCategory.Count - 1;
			FocusCategoryTextbox(ItemlistGroupedByCategory[newIndex]);
		}

		private void FocusCategoryTextbox(ShoppinglistCategoryWithItems catitem)
		{
			TreeViewItem tvi = treeviewShoppingLists.ItemContainerGenerator.ContainerFromItem(catitem) as TreeViewItem;
			if (tvi == null) return;
			ContentPresenter presenter = tvi.FindVisualChild<ContentPresenter>();
			DataTemplate template = presenter.ContentTemplate;
			var textbox = template.FindName("textboxItemCategory", presenter) as TextblockDoubleclickTextbox;
			textbox.OnGotFocus();
			textbox.textBoxName.SelectAll();
		}

		private void textboxItemName_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if ((e.Key == Key.Tab && Keyboard.Modifiers == ModifierKeys.Shift) || e.Key == Key.Up)
			{
				FrameworkElement fe = sender as FrameworkElement;
				if (fe == null) return;
				ShoppinglistItem item = fe.DataContext as ShoppinglistItem;
				if (item == null) return;
				e.Handled = true;
				FocusPreviousItemTextbox(item);
			}
			else if ((e.Key == Key.Tab && Keyboard.Modifiers != ModifierKeys.Shift) || e.Key == Key.Down)
			{
				FrameworkElement fe = sender as FrameworkElement;
				if (fe == null) return;
				ShoppinglistItem item = fe.DataContext as ShoppinglistItem;
				if (item == null) return;
				e.Handled = true;
				FocusNextItemTextbox(item);
			}
		}

		private void FocusNextItemTextbox(ShoppinglistItem shoppingitem)
		{
			var cats = ItemlistGroupedByCategory.Where(c => c.CategoryName.Equals(shoppingitem.ItemCategory)).ToArray();
			if (cats.Length == 0) return;

			int curIndex = cats[0].Items.IndexOf(shoppingitem);
			int newIndex = curIndex;
			if (curIndex + 1 < cats[0].Items.Count)
				newIndex = curIndex + 1;
			else
				newIndex = 0;

			FocusShoppingitemTextbox(cats[0].Items[newIndex]);
		}

		private void FocusPreviousItemTextbox(ShoppinglistItem shoppingitem)
		{
			var cats = ItemlistGroupedByCategory.Where(c => c.CategoryName.Equals(shoppingitem.ItemCategory)).ToArray();
			if (cats.Length == 0) return;

			int curIndex = cats[0].Items.IndexOf(shoppingitem);
			int newIndex = curIndex;
			if (curIndex - 1 >= 0)
				newIndex = curIndex - 1;
			else
				newIndex = cats[0].Items.Count - 1;

			FocusShoppingitemTextbox(cats[0].Items[newIndex]);
		}

		private void FocusShoppingitemTextbox(ShoppinglistItem item)
		{
			var cats = ItemlistGroupedByCategory.Where(c => c.CategoryName.Equals(item.ItemCategory)).ToArray();
			if (cats.Length == 0) return;

			TreeViewItem tvi = treeviewShoppingLists.ItemContainerGenerator.ContainerFromItem(cats[0]) as TreeViewItem;
			if (tvi == null) return;
			tvi = tvi.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
			ContentPresenter presenter = tvi.FindVisualChild<ContentPresenter>();
			DataTemplate template = presenter.ContentTemplate;
			var textbox = template.FindName("textboxItemName", presenter) as TextblockDoubleclickTextbox;
			textbox.OnGotFocus();
			textbox.textBoxName.SelectAll();
		}

		private void menuitemAddReminder_Click(object sender, RoutedEventArgs e)
		{
			FrameworkElement fe = sender as FrameworkElement;
			if (fe == null) return;
			ShoppinglistCategoryWithItems catitem = fe.DataContext as ShoppinglistCategoryWithItems;
			if (catitem == null) return;
			DateTime? pickedDate = DatePickerWindow.PickDateTime();
			if (pickedDate.HasValue)
				catitem.AddReminder(pickedDate.Value, OnError);
		}

		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			FocusManager.SetFocusedElement(this, null);//If we click outside one of the TextBoxes we just unfocus it all
		}
	}
}