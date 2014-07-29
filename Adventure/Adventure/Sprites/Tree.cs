using System;
using MonoTouch.SpriteKit;
using MonoTouch.Foundation;
using System.Drawing;

namespace Adventure
{
	public class Tree : ParallaxSprite
	{
		private const float OpaqueDistance = 400;

		public bool FadeAlpha { get; set; }

		public Tree(IntPtr handle)
			: base(handle)
		{
		}

		public Tree (SKNode [] sprites, float offset)
			: base(sprites, offset)
		{
		}

		public override object Clone ()
		{
			var tree = (Tree)base.Clone ();
			tree.FadeAlpha = FadeAlpha;

			return tree;
		}

		#region Offsets

		public void UpdateAlphaWithScene(MultiplayerLayeredCharacterScene scene)
		{
			if (scene == null)
				throw new ArgumentNullException ("scene");

			if (!FadeAlpha)
				return;

			float closestHeroDistance = float.MaxValue;

			// See if there are any heroes nearby.
			PointF ourPosition = Position;
			foreach (SKNode hero in scene.Heroes) {
				PointF theirPos = hero.Position;
				float distance = GraphicsUtilities.DistanceBetweenPoints(ourPosition, theirPos);
				closestHeroDistance = Math.Min (distance, closestHeroDistance);
			}

			if (closestHeroDistance > OpaqueDistance) {
				// No heroes nearby.
				Alpha = 1;
			} else {
				// Adjust the alpha based on how close the hero is.
				float ratio = closestHeroDistance / OpaqueDistance;
				Alpha = 0.1f + ratio * ratio * 0.9f;
			}
		}

		#endregion

	}
}

