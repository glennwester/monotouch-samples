﻿using System;
using System.Drawing;

using MonoTouch.UIKit;
using MonoTouch.Foundation;

namespace HelloGoodbye
{
	public class AAPLAgeSlider : UISlider
	{
		public override string AccessibilityValue {
			get {
				NSNumber number = NSNumber.FromFloat (Value);
				string str = NSNumberFormatter.LocalizedStringFromNumbernumberStyle (number, NSNumberFormatterStyle.Decimal);
				return str;
			}
			set {
				base.AccessibilityValue = value;
			}
		}

		public AAPLAgeSlider()
		{
			TintColor = AAPLStyleUtilities.ForegroundColor;
			MinValue = 18;
			MaxValue = 120;
		}

		public override void AccessibilityIncrement ()
		{
			Value += 1;
			SendActionForControlEvents (UIControlEvent.ValueChanged);
		}

		public override void AccessibilityDecrement ()
		{
			Value -= 1;
			SendActionForControlEvents (UIControlEvent.ValueChanged);
		}
	}
}