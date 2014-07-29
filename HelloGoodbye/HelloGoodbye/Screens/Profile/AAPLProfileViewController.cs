﻿using System;
using System.Collections.Generic;

using MonoTouch.UIKit;
using MonoTouch.Foundation;
using MonoTouch.CoreText;
using System.Drawing;



namespace HelloGoodbye
{
	public class AAPLProfileViewController : AAPLPhotoBackgroundViewController
	{
		private const float AAPLLabelControlMinimumSpacing = 20;
		private const float AAPLMinimumVerticalSpacingBetweenRows = 20;
		private const float AAPLPreviewTabMinimumWidth = 80;
		private const float AAPLPreviewTabHeight = 30;
		private const float AAPLPreviewTabCornerRadius = 10;
		private const float AAPLPreviewTabHorizontalPadding = 30;
		private const float AAPLCardRevealAnimationDuration = 0.3f;

		private AAPLPerson _person;
		private UILabel _ageValueLabel;
		private UITextField _hobbiesField;
		private UITextField _elevatorPitchField;
		private UIImageView _previewTab;
		private AAPLCardView _cardView;
		private NSLayoutConstraint _cardRevealConstraint;
		private bool _cardWasRevealedBeforePan;

		private bool IsCardRevealed {
			get {
				return _cardRevealConstraint.Constant < 0;
			}
		}

		private float CardHeight {
			get {
				return _cardView.Frame.Height;
			}
		}

		public AAPLProfileViewController ()
		{
			Title = @"Profile".LocalizedString("Title of the profile page");
			BackgroundImage = UIImage.FromBundle ("girl-bg.jpg");

			// Create the model.  If we had a backing service, this model would pull data from the user's account settings.
			_person = new AAPLPerson {
				Photo = UIImage.FromBundle("girl.jpg"),
				Age = 37,
				Hobbies = "Music, swing dance, wine",
				ElevatorPitch = "I can keep a steady beat.",
			};
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();

			UIView containerView = View;
			var constraints = new List<NSLayoutConstraint> ();

			UIView overlayView = AddOverlayViewToView (containerView, constraints);
			UIView[] ageControls = AddAgeControlsToView (overlayView, constraints);
			_hobbiesField = AddTextFieldWithName ("Hobbies".LocalizedString ("The user's hobbies"), _person.Hobbies, overlayView, ageControls, constraints);
			_elevatorPitchField = AddTextFieldWithName ("Elevator Pitch".LocalizedString ("The user's elevator pitch for finding a partner"), _person.ElevatorPitch, overlayView, new UIView[] { _hobbiesField }, constraints);

			AddCardAndPreviewTab (constraints);
			containerView.AddConstraints (constraints.ToArray());
		}

		private UIView AddOverlayViewToView (UIView containerView, List<NSLayoutConstraint> constraints)
		{
			UIView overlayView = new UIView {
				BackgroundColor = AAPLStyleUtilities.OverlayColor,
				TranslatesAutoresizingMaskIntoConstraints = false
			};
			overlayView.Layer.CornerRadius = AAPLStyleUtilities.OverlayCornerRadius;

			containerView.AddSubview (overlayView);

			// Cover the view controller with the overlay, leaving a margin on all sides
			float margin = AAPLStyleUtilities.OverlayMargin;
			constraints.Add(NSLayoutConstraint.Create(overlayView, NSLayoutAttribute.Top, NSLayoutRelation.Equal, TopLayoutGuide, NSLayoutAttribute.Bottom, 1f, margin));
			constraints.Add(NSLayoutConstraint.Create(overlayView, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, BottomLayoutGuide, NSLayoutAttribute.Bottom, 1f,  -margin));
			constraints.Add (NSLayoutConstraint.Create (overlayView, NSLayoutAttribute.Left, NSLayoutRelation.Equal, containerView, NSLayoutAttribute.Left, 1f, margin));
			constraints.Add (NSLayoutConstraint.Create (overlayView, NSLayoutAttribute.Right, NSLayoutRelation.Equal, containerView, NSLayoutAttribute.Right, 1f, -margin));
			return overlayView;
		}

		private UITextField AddTextFieldWithName (string name, string text, UIView overlayView, IEnumerable<UIView> previousRowItems, List<NSLayoutConstraint> constraints)
		{
			UILabel titleLabel = AAPLStyleUtilities.CreateStandardLabel ();
			titleLabel.Text = name;
			overlayView.AddSubview (titleLabel);

			var attributes = new CTStringAttributes ();
			attributes.ForegroundColor = AAPLStyleUtilities.DetailOnOverlayPlaceholderColor.CGColor;

			UITextField valueField = new UITextField {
				WeakDelegate = this,
				Font = AAPLStyleUtilities.StandardFont,
				TextColor = AAPLStyleUtilities.DetailOnOverlayColor,
				Text = text,
				AttributedPlaceholder = new NSAttributedString("Type here...".LocalizedString("Placeholder for profile text fields"), attributes),
			TranslatesAutoresizingMaskIntoConstraints = false
			};
			overlayView.AddSubview (valueField);

			// Ensure sufficient spacing from the row above this one
			foreach (UIView previousRowItem in previousRowItems)
				constraints.Add (NSLayoutConstraint.Create (titleLabel, NSLayoutAttribute.Top, NSLayoutRelation.GreaterThanOrEqual, previousRowItem, NSLayoutAttribute.Bottom, 1f, AAPLMinimumVerticalSpacingBetweenRows));

			// Place the title directly above the value
			constraints.Add (NSLayoutConstraint.Create (valueField, NSLayoutAttribute.Top, NSLayoutRelation.Equal, titleLabel, NSLayoutAttribute.Bottom, 1f, 0f));

			// Position the title and value within the overlay view
			constraints.Add (NSLayoutConstraint.Create (titleLabel, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, overlayView, NSLayoutAttribute.Leading, 1f, AAPLStyleUtilities.ContentHorizontalMargin));
			constraints.Add (NSLayoutConstraint.Create (valueField, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, titleLabel, NSLayoutAttribute.Leading, 1f, 0f));
			constraints.Add (NSLayoutConstraint.Create (valueField, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, overlayView, NSLayoutAttribute.Trailing, 1f, -1 * AAPLStyleUtilities.ContentHorizontalMargin));

			return valueField;
		}

		private UIView[] AddAgeControlsToView(UIView overlayView, List<NSLayoutConstraint> constraints)
		{
			UILabel ageTitleLabel = AAPLStyleUtilities.CreateStandardLabel ();
			ageTitleLabel.Text = "Your age".LocalizedString ("The user's age");
			overlayView.AddSubview (ageTitleLabel);

			AAPLAgeSlider ageSlider = new AAPLAgeSlider {
				Value = _person.Age,
				TranslatesAutoresizingMaskIntoConstraints = false,
			};
			ageSlider.ValueChanged += OnAgeUpdate;
			overlayView.AddSubview (ageSlider);

			// Display the current age next to the slider
			_ageValueLabel = AddAgeValueLabelToView (overlayView);
			UpdateAgeValueLabelFromSlider (ageSlider);

//			// Position the age title and value side by side, within the overlay view
			constraints.Add (NSLayoutConstraint.Create (ageTitleLabel, NSLayoutAttribute.Top, NSLayoutRelation.Equal, overlayView, NSLayoutAttribute.Top, 1f, AAPLStyleUtilities.ContentVerticalMargin));
			constraints.Add (NSLayoutConstraint.Create (ageTitleLabel, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, overlayView, NSLayoutAttribute.Leading, 1f, AAPLStyleUtilities.ContentHorizontalMargin));
			constraints.Add (NSLayoutConstraint.Create (ageSlider, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, ageTitleLabel, NSLayoutAttribute.Trailing, 1f, AAPLLabelControlMinimumSpacing));
			constraints.Add (NSLayoutConstraint.Create (ageSlider, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, ageTitleLabel, NSLayoutAttribute.CenterY, 1f, 0f));
			constraints.Add (NSLayoutConstraint.Create (_ageValueLabel, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, ageSlider, NSLayoutAttribute.Trailing, 1f, AAPLLabelControlMinimumSpacing));
			constraints.Add (NSLayoutConstraint.Create (_ageValueLabel, NSLayoutAttribute.FirstBaseline, NSLayoutRelation.Equal, ageTitleLabel, NSLayoutAttribute.FirstBaseline, 1f, 0f));
			constraints.Add (NSLayoutConstraint.Create (_ageValueLabel, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, overlayView, NSLayoutAttribute.Trailing, 1f, -1 * AAPLStyleUtilities.ContentHorizontalMargin));

			return new UIView[]{ ageTitleLabel, ageSlider, _ageValueLabel };
		}

		private UILabel AddAgeValueLabelToView(UIView overlayView)
		{
			UILabel ageValueLabel = AAPLStyleUtilities.CreateStandardLabel ();
			ageValueLabel.IsAccessibilityElement = false;
			overlayView.AddSubview (ageValueLabel);

			return ageValueLabel;
		}

		private void UpdateAgeValueLabelFromSlider(AAPLAgeSlider ageSlider)
		{
			NSNumber number = NSNumber.FromFloat (ageSlider.Value);
			_ageValueLabel.Text = NSNumberFormatter.LocalizedStringFromNumbernumberStyle (number, NSNumberFormatterStyle.Decimal);
		}

		private void AddCardAndPreviewTab(List<NSLayoutConstraint> constraints)
		{
			_previewTab = AddPreviewTab ();
			_previewTab.TranslatesAutoresizingMaskIntoConstraints = false;

			AAPLPreviewLabel previewLabel = AddPreviewLabel ();
			previewLabel.TranslatesAutoresizingMaskIntoConstraints = false;

			AAPLCardView cardView = AddCardView ();
			cardView.TranslatesAutoresizingMaskIntoConstraints = false;

			// Pin the tab to the bottom center of the screen
			_cardRevealConstraint = NSLayoutConstraint.Create (_previewTab, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, View, NSLayoutAttribute.Bottom, 1f, 0f);
			constraints.Add (_cardRevealConstraint);
			constraints.Add(NSLayoutConstraint.Create(_previewTab, NSLayoutAttribute.CenterX, NSLayoutRelation.Equal, View, NSLayoutAttribute.CenterX, 1f, 0f));
			// Center the preview label within the tab
			constraints.Add(NSLayoutConstraint.Create (previewLabel, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, _previewTab, NSLayoutAttribute.Leading, 1f, AAPLPreviewTabHorizontalPadding));
			constraints.Add (NSLayoutConstraint.Create (previewLabel, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, _previewTab, NSLayoutAttribute.Trailing, 1f, -AAPLPreviewTabHorizontalPadding));
			constraints.Add (NSLayoutConstraint.Create (previewLabel, NSLayoutAttribute.CenterY, NSLayoutRelation.Equal, _previewTab, NSLayoutAttribute.CenterY, 1f, 0f));

			// Pin the top of the card to the bottom of the tab
			constraints.Add (NSLayoutConstraint.Create (cardView, NSLayoutAttribute.Top, NSLayoutRelation.Equal, _previewTab, NSLayoutAttribute.Bottom, 1f, 0f));
			constraints.Add (NSLayoutConstraint.Create (cardView, NSLayoutAttribute.CenterX, NSLayoutRelation.Equal, _previewTab, NSLayoutAttribute.CenterX, 1f, 0f));

			// Ensure that the card fits within the view
			constraints.Add (NSLayoutConstraint.Create (_cardView, NSLayoutAttribute.Width, NSLayoutRelation.LessThanOrEqual, View, NSLayoutAttribute.Width, 1f, 0f));
		}

		private UIImageView AddPreviewTab()
		{
			UIImage previewTabBackgroundImage = CreatePreviewTabBackgroundImage ();
			UIImageView previewTab = new UIImageView(previewTabBackgroundImage);
			previewTab.UserInteractionEnabled = true;
			View.AddSubview(previewTab);

			UIPanGestureRecognizer revealGestureRecognizer = new UIPanGestureRecognizer(DidSlidePreviewTab);
			previewTab.AddGestureRecognizer(revealGestureRecognizer);
			return previewTab;
		}

		private UIImage CreatePreviewTabBackgroundImage()
		{
			// The preview tab should be flat on the bottom, and have rounded corners on top.
			var size = new SizeF (AAPLPreviewTabMinimumWidth, AAPLPreviewTabHeight);
			UIGraphics.BeginImageContextWithOptions (size, false, UIScreen.MainScreen.Scale);

			var rect = new RectangleF (0f, 0f, AAPLPreviewTabMinimumWidth, AAPLPreviewTabHeight);
			var radii = new SizeF (AAPLPreviewTabCornerRadius, AAPLPreviewTabCornerRadius);
			var roundedTopCornersRect = UIBezierPath.FromRoundedRect (rect, UIRectCorner.TopLeft | UIRectCorner.TopRight, radii);

			AAPLStyleUtilities.ForegroundColor.SetColor ();
			roundedTopCornersRect.Fill ();
			UIImage previewTabBackgroundImage = UIGraphics.GetImageFromCurrentImageContext ();

			var caps = new UIEdgeInsets (0f, AAPLPreviewTabCornerRadius, 0f, AAPLPreviewTabCornerRadius);
			previewTabBackgroundImage = previewTabBackgroundImage.CreateResizableImage (caps);

			UIGraphics.EndImageContext ();
			return previewTabBackgroundImage;
		}

		private void DidSlidePreviewTab(UIPanGestureRecognizer gestureRecognizer)
		{
			switch (gestureRecognizer.State) {
				case UIGestureRecognizerState.Began:
					_cardWasRevealedBeforePan = IsCardRevealed;
					break;

				case UIGestureRecognizerState.Changed:
					float cardHeight = CardHeight;
					float cardRevealConstant = gestureRecognizer.TranslationInView (View).Y;
					if (_cardWasRevealedBeforePan) {
						cardRevealConstant += -1 * cardHeight;
					}
						// Never let the card tab move off screen
					cardRevealConstant = Math.Min (0f, cardRevealConstant);
						// Never let the card have a gap below it
					cardRevealConstant = Math.Max (-1 * cardHeight, cardRevealConstant);
					_cardRevealConstraint.Constant = cardRevealConstant;
					break;

				case UIGestureRecognizerState.Ended:
				// Card was closer to the bottom of the screen
					if (_cardRevealConstraint.Constant > (-0.5 * CardHeight))
						DismissCard ();
					else
						RevealCard ();
					break;

				case UIGestureRecognizerState.Cancelled:
					if (_cardWasRevealedBeforePan)
						RevealCard ();
					else
						DismissCard ();
					break;
			}
		}

		private void RevealCard()
		{
			View.LayoutIfNeeded ();

			UIView.Animate (AAPLCardRevealAnimationDuration, () => {
				_cardRevealConstraint.Constant = -1 * CardHeight;
				View.LayoutIfNeeded ();
			}, () => {
				UIAccessibility.PostNotification(UIAccessibilityPostNotification.LayoutChanged, null);
			});
		}

		private void DismissCard()
		{
			View.LayoutIfNeeded ();

			UIView.Animate (AAPLCardRevealAnimationDuration, () => {
				_cardRevealConstraint.Constant = 0f;
				View.LayoutIfNeeded ();
			}, () => {
				UIAccessibility.PostNotification (UIAccessibilityPostNotification.LayoutChanged, null);
			});
		}

		private AAPLPreviewLabel AddPreviewLabel()
		{
			AAPLPreviewLabel previewLabel = new AAPLPreviewLabel();
			previewLabel.ActivatePreviewLabel += DidActivatePreviewLabel;
			View.AddSubview(previewLabel);

			return previewLabel;
		}

		void DidActivatePreviewLabel (object sender, EventArgs e)
		{
			if (IsCardRevealed)
				DismissCard ();
			else
				RevealCard ();
		}

		private AAPLCardView AddCardView()
		{
			AAPLCardView cardView = new AAPLCardView();
			cardView.UpdateWithPerson (_person);
			_cardView = cardView;
			View.AddSubview (cardView);

			return cardView;
		}

		private void OnAgeUpdate (object sender, EventArgs e)
		{
			var ageSlider = (AAPLAgeSlider)sender;
			// Turn the value into a valid age
			int age = (int)Math.Round(ageSlider.Value);
			ageSlider.Value = age;

			// Display the updated age next to the slider
			UpdateAgeValueLabelFromSlider (ageSlider);

			// Update the model
			_person.Age = age;

			// Update the card view with the new data
			_cardView.UpdateWithPerson (_person);
		}

		#region UITextFieldDelegate
		[Export("textFieldDidBeginEditing:")]
		private void TextFieldDidBeginEditing(UITextField textField)
		{
			// Add a Done button so that the user can dismiss the keyboard easily
			NavigationItem.RightBarButtonItem = new UIBarButtonItem (UIBarButtonSystemItem.Done, DoneButtonPressed);
		}

		[Export("textFieldDidEndEditing:")]
		private void TextFieldDidEndEditing(UITextField textField)
		{
			// Remove the Done button
			NavigationItem.RightBarButtonItem = null;

			// Update the model
			if (textField == _hobbiesField)
				_person.Hobbies = textField.Text;
			else if (textField == _elevatorPitchField)
				_person.ElevatorPitch = textField.Text;

			// Update the card view with the new data
			_cardView.UpdateWithPerson(_person);
		}
		#endregion

		private void DoneButtonPressed(object sender, EventArgs args)
		{
			// End editing on whichever text field is first responder
			_hobbiesField.ResignFirstResponder ();
			_elevatorPitchField.ResignFirstResponder ();
		}
	}
}

