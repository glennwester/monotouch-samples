// This file has been autogenerated from a class added in the UI designer.

using System;

using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace UICatalog
{
	public partial class SearchPresentOverNavigationBarViewController : SearchControllerBaseViewController
	{
		private UISearchController _searchController;
		public SearchPresentOverNavigationBarViewController (IntPtr handle)
			: base (handle)
		{
		}

		[Action("searchButtonClicked:")]
		public void SearchButtonClicked(UIBarButtonItem sender)
		{
			// Create the search results view controller and use it for the UISearchController.
			SearchResultsViewController searchResultsController = (SearchResultsViewController)Storyboard.InstantiateViewController (SearchResultsViewController.StoryboardIdentifier);
			UISearchResultsUpdatingWrapper wrapper = new UISearchResultsUpdatingWrapper (searchResultsController);

			// Create the search controller and make it perform the results updating.
			_searchController = new UISearchController (searchResultsController);
			// TODO: need overload https://trello.com/c/bEtup8us
			_searchController.SearchResultsUpdater = wrapper;
			_searchController.HidesNavigationBarDuringPresentation = false;

			// Present the view controller.
			PresentViewController (_searchController, true, null);
		}
	}
}
