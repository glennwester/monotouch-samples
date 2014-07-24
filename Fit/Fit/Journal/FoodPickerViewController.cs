using System;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace Fit
{
	public partial class FoodPickerViewController : UITableViewController
	{
		private readonly NSString CellIdentifier = new NSString ("cell");
		private NSEnergyFormatter energyFormatter;

		private NSEnergyFormatter EnergyFormatter {
			get {
				if (energyFormatter == null) {
					energyFormatter = new NSEnergyFormatter {
						UnitStyle = NSFormattingUnitStyle.Long,
						ForFoodEnergyUse = true
					};

					energyFormatter.NumberFormatter.MaximumFractionDigits = 2;
				}

				return energyFormatter;
			}
		}

		public NSArray FoodItems { get; private set; }

		public FoodItem SelectedFoodItem { get; private set; }

		public FoodPickerViewController (IntPtr handle) : base (handle)
		{
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			FoodItems = NSArray.FromObjects (new object[] {
				FoodItem.Create ("Wheat Bagel", 240000.0),
				FoodItem.Create ("Bran with Raisins", 190000.0),
				FoodItem.Create ("Regular Instant Coffee", 1000.0),
				FoodItem.Create ("Banana", 439320.0),
				FoodItem.Create ("Cranberry Bagel", 416000.0),
				FoodItem.Create ("Oatmeal", 150000.0),
				FoodItem.Create ("Fruits Salad", 60000.0),
				FoodItem.Create ("Fried Sea Bass", 200000.0),
				FoodItem.Create ("Chips", 190000.0),
				FoodItem.Create ("Chicken Taco", 170000.0)
			});
		}

		public override int RowsInSection (UITableView tableview, int section)
		{
			return (int)FoodItems.Count;
		}

		public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
		{
			var cell = TableView.DequeueReusableCell (CellIdentifier, indexPath);
			var foodItem = FoodItems.GetItem<FoodItem> (indexPath.Row);

			cell.TextLabel.Text = foodItem.Name;
			cell.DetailTextLabel.Text = EnergyFormatter.StringFromJoules (foodItem.Joules);
			return cell;
		}

		public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
		{
			NSIndexPath indexPathForSelectedRow = TableView.IndexPathForSelectedRow;
			SelectedFoodItem = FoodItems.GetItem<FoodItem> (indexPathForSelectedRow.Row);
			((JournalViewController)NavigationController.ViewControllers [NavigationController.ViewControllers.Length - 2]).
				AddFoodItem (SelectedFoodItem);
			NavigationController.PopViewControllerAnimated (true);
		}
	}
}
