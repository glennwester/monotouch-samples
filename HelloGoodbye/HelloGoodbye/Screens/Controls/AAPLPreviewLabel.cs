using System;
using MonoTouch.UIKit;
using System.Drawing;

namespace HelloGoodbye
{
	public class AAPLPreviewLabel : UILabel
	{
		public event EventHandler ActivatePreviewLabel;

		public override UIAccessibilityTrait AccessibilityTraits {
			get {
				return base.AccessibilityTraits | UIAccessibilityTrait.Button;
			}
			set {
				base.AccessibilityTraits = value;
			}
		}

		public AAPLPreviewLabel ()
		{
			Text = "Preview".LocalizedString("Name of the card preview tab");
			Font = AAPLStyleUtilities.LargeFont;
			TextColor = AAPLStyleUtilities.PreviewTabLabelColor;
		}

		public override bool AccessibilityActivate ()
		{
			if (ActivatePreviewLabel != null)
				ActivatePreviewLabel (this, EventArgs.Empty);

			return true;
		}
	}
}

