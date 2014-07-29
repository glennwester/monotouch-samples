using System;
using MonoTouch.Foundation;
using System.Drawing;
using MonoTouch.ObjCRuntime;
using MonoTouch.GameController;
using MonoTouch.UIKit;

namespace Adventure
{
	public class Player : NSObject
	{
		public static readonly int StartLives = 3;

		public HeroCharacter Hero { get; set; }
		public HeroType HeroType { get; set; }
		public bool MoveForward { get; private set; }
		public bool MoveLeft { get; private set; }
		public bool MoveRight { get; private set; }
		public bool MoveBack { get; private set; }
		public bool FireAction { get; set; }

		public PointF HeroMoveDirection { get; set; }

		public int LivesLeft { get; set; }
		public int Score  { get; set; }

		public GCController Controller  { get; set; }

		public UITouch MovementTouch { get; set; }		// track whether a touch is move or fire action
		public PointF TargetLocation { get; set; }		// track target location
		public bool MoveRequested { get; set; }			// track whether a move was requested



		public Player ()
		{
			LivesLeft = StartLives;

			Random rnd = new Random ();
			HeroType = rnd.Next (1) == 0 ? HeroType.Warrior : HeroType.Archer;
		}
	}
}

