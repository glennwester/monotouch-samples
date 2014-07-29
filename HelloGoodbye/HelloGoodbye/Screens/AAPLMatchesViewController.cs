﻿using System;
using System.Collections.Generic;

using MonoTouch.UIKit;
using MonoTouch.Foundation;
using MonoTouch.CoreGraphics;
using MonoTouch.ObjCRuntime;

namespace HelloGoodbye
{
	public class AAPLMatchesViewController : AAPLPhotoBackgroundViewController
	{
		private const float AAPLHelloGoodbyeVerticalMargin = 5f;
		private const float AAPLSwipeAnimationDuration = 0.5f;
		private const float AAPLZoomAnimationDuration = 0.3f;
		private const float AAPLFadeAnimationDuration = 0.3f;

		private AAPLCardView _cardView;
		private UIView _swipeInstructionsView;
		private UIView _allMatchesViewedExplanatoryView;

		private NSLayoutConstraint[] _cardViewVerticalConstraints;

		private List<AAPLPerson> _matches;
		private int _currentMatchIndex;

		private AAPLPerson CurrentMatch {
			get {
				return _currentMatchIndex < _matches.Count ? _matches [_currentMatchIndex] : null;
			}
		}

		public AAPLMatchesViewController ()
		{
			string path = NSBundle.MainBundle.PathForResource ("matches", "plist");
			NSArray serializedMatches = NSArray.FromFile (path);

			var matches = new List<AAPLPerson> ();

			for (int i = 0; i < serializedMatches.Count; i++) {
				var sMatch = serializedMatches.GetItem<NSDictionary> (i);
				AAPLPerson match = AAPLPerson.PersonFromDictionary (sMatch);
				matches.Add (match);
			}

			Title = "Matches".LocalizedString("Title of the matches page");
			_matches = matches;

			BackgroundImage = UIImage.FromBundle ("dessert.jpg");
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();

			UIView containerView = View;
			var constraints = new List<NSLayoutConstraint> ();

			// Show instructions for how to say hello and goodbye
			_swipeInstructionsView = AddSwipeInstructionsToContainerView (containerView, constraints);

			// Add a dummy view to center the card between the explanatory view and the bottom layout guide
			UIView dummyView = AddDummyViewToContainerView (containerView, _swipeInstructionsView, BottomLayoutGuide, constraints);

			// Create and add the card
			AAPLCardView cardView = AddCardViewToView (containerView);

			// Define the vertical positioning of the card
			// These constraints will be removed when the card animates off screen
			_cardViewVerticalConstraints = new NSLayoutConstraint[] {
				NSLayoutConstraint.Create (cardView, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, dummyView, NSLayoutAttribute.CenterY, 1f, 0f),
				NSLayoutConstraint.Create (cardView, NSLayoutAttribute.Top, NSLayoutRelation.GreaterThanOrEqual, _swipeInstructionsView, NSLayoutAttribute.Bottom, 1f, AAPLHelloGoodbyeVerticalMargin)
			};
			constraints.AddRange (_cardViewVerticalConstraints);

			// Ensure that the card is centered horizontally within the container view, and doesn't exceed its width
			constraints.AddRange (new NSLayoutConstraint[] {
				NSLayoutConstraint.Create (cardView, NSLayoutAttribute.CenterX, NSLayoutRelation.Equal, containerView, NSLayoutAttribute.CenterX, 1f, 0f),
				NSLayoutConstraint.Create (cardView, NSLayoutAttribute.Left, NSLayoutRelation.GreaterThanOrEqual, containerView, NSLayoutAttribute.Left, 1f, 0f),
				NSLayoutConstraint.Create (cardView, NSLayoutAttribute.Right, NSLayoutRelation.LessThanOrEqual, containerView, NSLayoutAttribute.Right, 1f, 0f),
			});

			// When the matches run out, we'll show this message
			_allMatchesViewedExplanatoryView = AddAllMatchesViewExplanatoryViewToContainerView (containerView, constraints);
			containerView.AddConstraints (constraints.ToArray());
		}

		private UIView AddDummyViewToContainerView(UIView containerView, INativeObject topItem, INativeObject bottomItem, List<NSLayoutConstraint> constraints)
		{
			UIView dummyView = new UIView{
				TranslatesAutoresizingMaskIntoConstraints = false
			};
			containerView.AddSubview (dummyView);

			// The horizontal layout of the dummy view does not matter, but for completeness, we give it a width of 0 and center it horizontally.
			constraints.AddRange (new NSLayoutConstraint[] {
				NSLayoutConstraint.Create (dummyView, NSLayoutAttribute.Width, NSLayoutRelation.Equal, null, NSLayoutAttribute.NoAttribute, 0f, 0f),
				NSLayoutConstraint.Create (dummyView, NSLayoutAttribute.CenterX, NSLayoutRelation.Equal, containerView, NSLayoutAttribute.CenterX, 1f, 0f),
				NSLayoutConstraint.Create (dummyView, NSLayoutAttribute.Top, NSLayoutRelation.Equal, topItem, NSLayoutAttribute.Bottom, 1f, 0f),
				NSLayoutConstraint.Create (dummyView, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, bottomItem, NSLayoutAttribute.Top, 1f, 0f)
			});

			return dummyView;
		}

		private AAPLCardView AddCardViewToView(UIView containerView)
		{
			AAPLCardView cardView = new AAPLCardView ();
			cardView.UpdateWithPerson (CurrentMatch);
			cardView.TranslatesAutoresizingMaskIntoConstraints = false;
			_cardView = cardView;
			containerView.AddSubview (cardView);

			UISwipeGestureRecognizer swipeUpRecognizer = new UISwipeGestureRecognizer(HandleSwipeUp);
			swipeUpRecognizer.Direction = UISwipeGestureRecognizerDirection.Up;
			cardView.AddGestureRecognizer (swipeUpRecognizer);

			UISwipeGestureRecognizer swipeDownRecognizer = new UISwipeGestureRecognizer (HandleSwipeDown);
			swipeDownRecognizer.Direction = UISwipeGestureRecognizerDirection.Down;
			cardView.AddGestureRecognizer (swipeDownRecognizer);

			string sayHelloName = "Say hello".LocalizedString (@"Accessibility action to say hello");
			UIAccessibilityCustomAction helloAction = new UIAccessibilityCustomAction (sayHelloName, this, new Selector ("sayHello"));

			string sayGoodbyeName = "Say goodbye".LocalizedString ("Accessibility action to say goodbye");
			UIAccessibilityCustomAction goodbyeAction = new UIAccessibilityCustomAction (sayGoodbyeName, this, new Selector ("sayGoodbye"));

			UIView[] elements = NSArray.FromArray<UIView> ((NSArray)cardView.GetAccessibilityElements ());
			foreach (UIView element in elements)
				element.AccessibilityCustomActions = new UIAccessibilityCustomAction[] { helloAction, goodbyeAction };

			return cardView;
		}

		private void HandleSwipeUp(UISwipeGestureRecognizer gestureRecognizer)
		{
			if (gestureRecognizer.State == UIGestureRecognizerState.Recognized)
				SayHello ();
		}

		private void HandleSwipeDown(UISwipeGestureRecognizer gestureRecognizer)
		{
			if (gestureRecognizer.State == UIGestureRecognizerState.Recognized)
				SayGoodbye ();
		}

		private UIView AddAllMatchesViewExplanatoryViewToContainerView(UIView containerView, List<NSLayoutConstraint> constraints)
		{
			UIView overlayView = AddOverlayViewToContainerView (containerView);

			// Start out hidden
			// This view will become visible once all matches have been viewed
			overlayView.Alpha = 0f;

			UILabel label = AAPLStyleUtilities.CreateStandardLabel ();
			label.Font = AAPLStyleUtilities.LargeFont;
			label.Text = "Stay tuned for more matches!".LocalizedString ("Shown when all matches have been viewed");
			overlayView.AddSubview (label);

			// Center the overlay view
			constraints.Add (NSLayoutConstraint.Create (overlayView, NSLayoutAttribute.CenterX, NSLayoutRelation.Equal, containerView, NSLayoutAttribute.CenterX, 1f, 0f));
			constraints.Add (NSLayoutConstraint.Create (overlayView, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, containerView, NSLayoutAttribute.CenterY, 1f, 0f));

			// Position the label in the overlay view
			constraints.Add (NSLayoutConstraint.Create (label, NSLayoutAttribute.Top, NSLayoutRelation.Equal, overlayView, NSLayoutAttribute.Top, 1f, AAPLStyleUtilities.ContentVerticalMargin));
			constraints.Add (NSLayoutConstraint.Create (label, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, overlayView, NSLayoutAttribute.Bottom, 1f, -1 * AAPLStyleUtilities.ContentVerticalMargin));
			constraints.Add (NSLayoutConstraint.Create (label, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, overlayView, NSLayoutAttribute.Leading, 1f, AAPLStyleUtilities.ContentHorizontalMargin));
			constraints.Add (NSLayoutConstraint.Create (label, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, overlayView, NSLayoutAttribute.Trailing, 1f, -1 * AAPLStyleUtilities.ContentHorizontalMargin));

			return overlayView;
		}

		[Export("sayHello")]
		private bool SayHello()
		{
			AnimateCardsForHello (true);
			return true;
		}

		[Export("sayGoodbye")]
		private bool SayGoodbye()
		{
			AnimateCardsForHello (false);
			return true;
		}

		private void AnimateCardsForHello(bool forHello)
		{
			AnimateCardOffScreenToTop (forHello, () => {
				_currentMatchIndex++;
				AAPLPerson nextMatch = CurrentMatch;
				if (nextMatch != null) {
					// Show the next match's profile in the card
					_cardView.UpdateWithPerson (nextMatch);

					// Ensure that the view's layout is up to date before we animate it
					View.LayoutIfNeeded ();

					if (UIAccessibility.IsReduceMotionEnabled) {
						// Fade the card into view
						FadeCardIntoView ();
					} else {
						// Zoom the new card from a tiny point into full view
						ZoomCardIntoView ();
					}
				} else {
					// Hide the card
					_cardView.Hidden = true;

					// Fade in the "Stay tuned for more matches" blurb
					UIView.Animate (AAPLFadeAnimationDuration, () => {
						_swipeInstructionsView.Alpha = 0f;
						_allMatchesViewedExplanatoryView.Alpha = 1f;
					});
				}

				UIAccessibility.PostNotification (UIAccessibilityPostNotification.LayoutChanged, null);
			});
		}

		private void FadeCardIntoView()
		{
			_cardView.Alpha = 0f;
			UIView.Animate (AAPLFadeAnimationDuration, () => {
				_cardView.Alpha = 1f;
			});
		}

		private void ZoomCardIntoView()
		{
			_cardView.Transform = CGAffineTransform.MakeScale(0f, 0f);
			UIView.Animate (AAPLZoomAnimationDuration, () => {
				_cardView.Transform = CGAffineTransform.MakeIdentity ();
			});
		}

		private void AnimateCardOffScreenToTop(bool toTop, Action completion)
		{
			NSLayoutConstraint offScreenConstraint = null;
			if (toTop)
				offScreenConstraint = NSLayoutConstraint.Create (_cardView, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, View, NSLayoutAttribute.Top, 1f, 0f);
			else
				offScreenConstraint = NSLayoutConstraint.Create (_cardView, NSLayoutAttribute.Top, NSLayoutRelation.Equal, View, NSLayoutAttribute.Bottom, 1f, 0f);

			View.LayoutIfNeeded ();

			UIView.Animate (AAPLSwipeAnimationDuration, () => {
				// Slide the card off screen
				View.RemoveConstraints (_cardViewVerticalConstraints);
				View.AddConstraint (offScreenConstraint);
				View.LayoutIfNeeded ();
			}, () => {
				// Bring the card back into view
				View.RemoveConstraint (offScreenConstraint);
				View.AddConstraints (_cardViewVerticalConstraints);

				if (completion != null)
					completion ();
			});
		}

		private UIView AddSwipeInstructionsToContainerView(UIView containerView, List<NSLayoutConstraint>constraints)
		{
			UIView overlayView = AddOverlayViewToContainerView (containerView);

			UILabel swipeInstructionsLabel = AAPLStyleUtilities.CreateStandardLabel ();
			swipeInstructionsLabel.Font = AAPLStyleUtilities.LargeFont;
			overlayView.AddSubview (swipeInstructionsLabel);
			swipeInstructionsLabel.Text = "Swipe ↑ to say \"Hello!\"\nSwipe ↓ to say \"Goodbye...\"".LocalizedString ("Instructions for the Matches page");
			swipeInstructionsLabel.AccessibilityLabel = "Swipe up to say \"Hello!\"\nSwipe down to say \"Goodbye\"".LocalizedString ("Accessibility instructions for the Matches page");

			float overlayMargin = AAPLStyleUtilities.OverlayMargin;
			NSLayoutConstraint topMarginConstraint = NSLayoutConstraint.Create(overlayView, NSLayoutAttribute.Top, NSLayoutRelation.Equal, TopLayoutGuide, NSLayoutAttribute.Bottom, 1f, overlayMargin);
			float priority = (int)UILayoutPriority.Required - 1;
			topMarginConstraint.Priority = priority;
			constraints.Add (topMarginConstraint);

			// Position the label inside the overlay view
			constraints.Add (NSLayoutConstraint.Create (swipeInstructionsLabel, NSLayoutAttribute.Top, NSLayoutRelation.Equal, overlayView, NSLayoutAttribute.Top, 1f, AAPLHelloGoodbyeVerticalMargin));
			constraints.Add (NSLayoutConstraint.Create (swipeInstructionsLabel, NSLayoutAttribute.CenterX, NSLayoutRelation.Equal, overlayView, NSLayoutAttribute.CenterX, 1f, 0f));
			constraints.Add (NSLayoutConstraint.Create (overlayView, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, swipeInstructionsLabel, NSLayoutAttribute.Bottom, 1f, AAPLHelloGoodbyeVerticalMargin));

			// Center the overlay view horizontally
			constraints.Add (NSLayoutConstraint.Create (overlayView, NSLayoutAttribute.Left, NSLayoutRelation.Equal, containerView, NSLayoutAttribute.Left, 1f, overlayMargin));
			constraints.Add (NSLayoutConstraint.Create (overlayView, NSLayoutAttribute.Right, NSLayoutRelation.Equal, containerView, NSLayoutAttribute.Right, 1f, -overlayMargin));

			return overlayView;
		}

		private UIView AddOverlayViewToContainerView(UIView containerView)
		{
			UIView overlayView = new UIView {
				BackgroundColor = AAPLStyleUtilities.OverlayColor,
				TranslatesAutoresizingMaskIntoConstraints = false,
			};
			overlayView.Layer.CornerRadius = AAPLStyleUtilities.OverlayCornerRadius;
			containerView.AddSubview(overlayView);

			return overlayView;
		}
	}
}

