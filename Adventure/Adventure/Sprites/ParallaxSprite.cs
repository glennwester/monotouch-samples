﻿using System;
using MonoTouch.SpriteKit;
using MonoTouch.Foundation;
using System.Drawing;

namespace Adventure
{
	public abstract class ParallaxSprite : SKSpriteNode, ICloneable
	{
		private float _parallaxOffset;
		public bool UsesParallaxEffect { get; set; }

		public float VirtualZRotation { get; set; }

		protected ParallaxSprite(IntPtr handle)
			: base(handle)
		{

		}

		protected ParallaxSprite (SKTexture texture)
			: base (texture)
		{
		}

		protected ParallaxSprite (SKNode [] sprites, float offset)
		{
			if (sprites == null)
				throw new ArgumentNullException ("offset");

			UsesParallaxEffect = true;

			// Make sure our z layering is correct for the stack.
			float zOffset = 1f / (float)sprites.Length;

			// All nodes in the stack are direct children, with ordered zPosition.
			float ourZPosition = ZPosition;
			int childNumber = 0;
			foreach (var node in sprites) {
				node.ZPosition = ourZPosition + (zOffset + (zOffset * childNumber));
				AddChild (node);
				childNumber++;
			}

			_parallaxOffset = offset;
		}

		public virtual object Clone ()
		{
			var sprite = (ParallaxSprite)((NSObject)this).Copy ();
			sprite._parallaxOffset = _parallaxOffset;
			sprite.UsesParallaxEffect = UsesParallaxEffect;

			return sprite;
		}

		public override float ZRotation {
			get {
				return base.ZRotation;
			}
			set {
				// Override to apply the zRotation just to the stack nodes, but only if the parallax effect is enabled.
				if (!UsesParallaxEffect) {
					base.ZRotation = value;
					return;
				}

				if (value > 0f) {
					ZRotation = 0f; // never rotate the group node

					// Instead, apply the desired rotation to each node in the stack.
					foreach (var child in Children)
						child.ZRotation = value;

					VirtualZRotation = value;
				}
			}
		}

		public void UpdateOffset ()
		{
			if (!UsesParallaxEffect || Parent == null)
				return;

			PointF scenePos = Scene.ConvertPointFromNode (Position, Parent);

			// Calculate the offset directions relative to the center of the screen.
			// Bias to (-0.5, 0.5) range.
			float offsetX = (-1f + (2f * (scenePos.X / Scene.Size.Width)));
			float offsetY = (-1f + (2f * (scenePos.Y / Scene.Size.Height)));

			float delta = _parallaxOffset / (float)Children.Length;

			int childNumber = 0;
			foreach (var node in Children) {
				node.Position = new PointF (offsetX * delta * childNumber, offsetY * delta * childNumber);
				childNumber++;
			}
		}
	}
}