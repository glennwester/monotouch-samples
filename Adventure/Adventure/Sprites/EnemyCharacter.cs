﻿using System;
using System.Drawing;

using MonoTouch.SpriteKit;

namespace Adventure
{
	public abstract class EnemyCharacter : Character
	{
		public ArtificialIntelligence Intelligence { get; protected set; }

		public EnemyCharacter(SKTexture texture, PointF position)
			: base(texture, position)
		{

		}

		public EnemyCharacter (SKNode[] sprites, PointF position, float offset)
			: base (sprites, position, offset)
		{
		}

		public override void UpdateWithTimeSinceLastUpdate (double interval)
		{
			base.UpdateWithTimeSinceLastUpdate (interval);
			Intelligence.UpdateWithTimeSinceLastUpdate (interval);
		}

		public override void AnimationDidComplete (AnimationState animation)
		{
			if (animation != AnimationState.Attack)
				return;

			// Attacking hero should apply same damage as collision with hero, so simply
			// tell the target that we collided with it.
			if(Intelligence.Target != null)
				Intelligence.Target.CollidedWith (PhysicsBody);
		}
	}
}

