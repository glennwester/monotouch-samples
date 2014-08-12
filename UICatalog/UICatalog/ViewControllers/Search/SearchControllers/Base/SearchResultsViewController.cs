﻿using System;

using MonoTouch.UIKit;
using MonoTouch.Foundation;

namespace UICatalog
{
	[Register ("SearchResultsViewController")]
	public class SearchResultsViewController : SearchControllerBaseViewController, IUISearchResultsUpdating
	{
		public SearchResultsViewController (IntPtr handle)
			: base (handle)
		{
		}

		[Export ("updateSearchResultsForSearchController:")]
		public void UpdateSearchResultsForSearchController (UISearchController searchController)
		{
			// UpdateSearchResultsForSearchController is called when the controller is being dismissed
			// to allow those who are using the controller they are search as the results controller a chance to reset their state.
			// No need to update anything if we're being dismissed.
			if (!searchController.Active)
				return;

			ApplyFilter (searchController.SearchBar.Text);
		}
	}
}

