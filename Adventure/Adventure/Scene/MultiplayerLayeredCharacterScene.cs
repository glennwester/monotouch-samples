using System;
using MonoTouch.SpriteKit;
using System.Collections.Generic;
using System.Drawing;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.CoreText;
using MonoTouch.ObjCRuntime;
using MonoTouch.GameController;
using MonoTouch.CoreGraphics;
using System.Threading;

namespace Adventure
{
	public abstract class MultiplayerLayeredCharacterScene : SKScene
	{
		private const float MIN_TIME_INTERVAL = 1f / 60f;
		private const int NUM_PLAYERS = 4; //kNumPlayers
		private const int MIN_HERO_TO_EDGE_DISTANCE = 256;	// minimum distance between hero and edge of camera before moving camera

		private const float HERO_PROJECTILE_SPEED = 480f;
		private const float HERO_PROJECTILE_LIFETIME = 1f;
		private const float HERO_PROJECTILE_FADE_OUT_TIME = 0.6f;

		private List<Player> _players;				// array of player objects or NSNull for no player
		protected Player DefaultPlayer { get; private set; }				// player '1' controlled by keyboard/touch
		private SKNode _world;						// root node to which all game renderables are attached

		List<SKNode> layers;

		protected PointF DefaultSpawnPoint { get; set; }			// the point at which heroes are spawned
		protected bool WorldMovedForUpdate { get; private set; }			// indicates the world moved before or during the current update
		public List<HeroCharacter> Heroes { get; private set; }			// all heroes in the game

		private List<SKSpriteNode> hudAvatars;				// keep track of the various nodes for the HUD
		private List<SKLabelNode> hudLabels;				// - there are always 'kNumPlayers' instances in each array
		private List<SKLabelNode> hudScores;
		private List<List<SKSpriteNode>> hudLifeHeartArrays;		// an array of NSArrays of life hearts

		private double lastUpdateTimeInterval;

		private NSObject _didConnectObserver;
		private NSObject _didDisconnectObserver;

		private bool _isAssetsLoaded;

		// Overridden by subclasses to provide an emitter used to indicate when a new hero is spawned.
		protected abstract SKEmitterNode SharedSpawnEmitter { get; }

		public MultiplayerLayeredCharacterScene (SizeF size)
			: base (size)
		{
		}

		public virtual void Initialize()
		{
			Heroes = new List<HeroCharacter> ();
			_players = new List<Player> (NUM_PLAYERS);
			DefaultPlayer = new Player ();
			_players.Add (DefaultPlayer);
			for (int i = 1; i < NUM_PLAYERS; i++)
				_players.Add (null);

			_world = new SKNode () {
				Name = "world"
			};
			layers = new List<SKNode> ((int)WorldLayer.Count);
			for (int i = 0; i < (int)WorldLayer.Count; i++) {
				var layer = new SKNode {
					ZPosition = i - (int)WorldLayer.Count
				};
				_world.AddChild (layer);
				layers.Add (layer);
			}
			AddChild (_world);

			buildHUD ();
			updateHUDFor (DefaultPlayer, HUDState.Local);
		}

		// Overridden by subclasses to update the scene - called once per frame.
		public abstract void UpdateWithTimeSinceLastUpdate (double timeSinceLast);

		// All sprites in the scene should be added through this method to ensure they are placed in the correct world layer.
		public void AddNode (SKNode node, WorldLayer layer)
		{
			SKNode layerNode = layers [(int)layer];
			layerNode.AddChild (node);
		}

		#region Shared Assets

		// Overridden by subclasses to load scene-specific assets.
		protected abstract void LoadSceneAssets ();

		// Overridden by subclasses to release assets used only by this scene.
		public abstract void ReleaseSceneAssets ();

		// Start loading all the shared assets for the scene in the background. This method calls LoadSceneAssets
		// in background thread, then calls the callback handler on the main thread.
		public void LoadSceneAssetsWithCompletionHandler (NSAction callback)
		{
			if (_isAssetsLoaded)
				return;

			ThreadPool.QueueUserWorkItem (_ => {
				LoadSceneAssets ();
				_isAssetsLoaded = true;

				if (callback != null)
					InvokeOnMainThread (callback);
			});
		}

		#endregion

		#region Heroes and players

		protected HeroCharacter AddHeroFor (Player player)
		{
			if (player == null)
				throw new ArgumentNullException ("player", "Player should not be null");

			if (player.Hero != null && !player.Hero.Dying)
				player.Hero.RemoveFromParent ();

			PointF spawnPos = DefaultSpawnPoint;
			HeroCharacter hero = CreateHeroBy (player.HeroType, spawnPos, player);

			if (hero != null) {
				SKEmitterNode emitter = (SKEmitterNode)((NSObject)SharedSpawnEmitter).Copy();
				emitter.Position = spawnPos;
				AddNode (emitter, WorldLayer.AboveCharacter);
				GraphicsUtilities.RunOneShotEmitter (emitter, 0.15f);

				hero.FadeIn (2f);
				hero.AddToScene (this);
				Heroes.Add (hero);
			}
			player.Hero = hero;

			return hero;
		}

		public virtual void HeroWasKilled (HeroCharacter hero)
		{
			if (hero == null)
				throw new ArgumentNullException ("hero");

			Player player = hero.Player;

			Heroes.Remove (hero);

			player.MoveRequested = false;

			if (--player.LivesLeft < 1)
				return; // In a real game, you'd want to end the game when there are no lives left.

			updateHUDAfterHeroDeathFor (hero.Player);

			hero = AddHeroFor (hero.Player);
			CenterWorld (hero);
		}

		#endregion

		#region Utility methods for coordinates

		protected void CenterWorld (Character character)
		{
			CenterWorld (character.Position);
		}

		protected void CenterWorld (PointF position)
		{
			// https://developer.apple.com/library/ios/documentation/GraphicsAnimation/Conceptual/SpriteKit_PG/Nodes/Nodes.html
			_world.Position = new PointF (-position.X + Frame.GetMidX (), -position.Y + Frame.GetMidY ());
			WorldMovedForUpdate = true;
		}

		public abstract float DistanceToWall (Point pos0, Point pos1);

		public abstract bool CanSee (PointF pos0, PointF pos1);

		#endregion

		#region Event Handling - OS X

		// TODO: Event Handling - OS X
//		- (void)handleKeyEvent:(NSEvent *)event keyDown:(BOOL)downOrUp
//		- (void)keyDown:(NSEvent *)event
//		- (void)keyUp:(NSEvent *)event

		#endregion

		#region HUD and Scores

		/* Determines the relevant player from the given projectile, and adds to that player's score. */
		public void AddToScoreAfterEnemyKill (int amount, SKNode projectile)
		{
			UserData userData = new UserData (projectile.UserData);
			Player player = userData.Player;
			player.Score += amount;
			updateHUDFor (player);
		}

		void buildHUD ()
		{
			string[] iconNames = new [] {
				"iconWarrior_blue",
				"iconWarrior_green",
				"iconWarrior_pink",
				"iconWarrior_red"
			};
			UIColor[] colors = new [] { UIColor.Green, UIColor.Blue, UIColor.Yellow, UIColor.Red };
			float hudX = 30;
			float hudY = Frame.Size.Height - 30;
			float hudD = Frame.Size.Width / NUM_PLAYERS;

			hudAvatars = new List<SKSpriteNode> (NUM_PLAYERS);
			hudLabels = new List<SKLabelNode> (NUM_PLAYERS);
			hudScores = new List<SKLabelNode> (NUM_PLAYERS);
			hudLifeHeartArrays = new List<List<SKSpriteNode>> (NUM_PLAYERS);
			var hud = new SKNode ();

			for (int i = 0; i < NUM_PLAYERS; i++) {
				var avatar = new SKSpriteNode (iconNames [i]) {
					Scale = 0.5f,
					Alpha = 0.5f,
				};
				avatar.Position = new PointF (hudX + i * hudD + (avatar.Size.Width * 0.5f),
					Frame.Size.Height - avatar.Size.Height * 0.5f - 8f);
				hudAvatars.Add (avatar);
				hud.AddChild (avatar);

				var label = new SKLabelNode ("Copperplate") {
					Text = "NO PLAYER",
					FontColor = colors [i],
					FontSize = 16,
					HorizontalAlignmentMode = SKLabelHorizontalAlignmentMode.Left,
					Position = new PointF (hudX + i * hudD + avatar.Size.Width, hudY + 10)
				};
				hudLabels.Add (label);
				hud.AddChild (label);

				var score = new SKLabelNode ("Copperplate") {
					Text = "SCORE: 0",
					FontColor = colors [i],
					FontSize = 16,
					HorizontalAlignmentMode = SKLabelHorizontalAlignmentMode.Left,
					Position = new PointF (hudX + i * hudD + avatar.Size.Width, hudY - 40)
				};
				hudScores.Add (score);
				hud.AddChild (score);

				var playerHearts = new List<SKSpriteNode> (Player.StartLives);
				hudLifeHeartArrays.Add (playerHearts);
				for (int j = 0; j < Player.StartLives; j++) {
					var heart = new SKSpriteNode ("lives.png") {
						Scale = 0.4f,
						Alpha = 0.1f
					};
					heart.Position = new PointF (hudX + i * hudD + avatar.Size.Width + 18 + (heart.Size.Width + 5) * j, hudY - 10);
					playerHearts.Add (heart);
					hud.AddChild (heart);
				}
			}

			AddChild (hud);
		}

		void updateHUDFor (Player player, HUDState state, string message = null)
		{
			int playerIndex = _players.IndexOf (player);

			SKSpriteNode avatar = hudAvatars [playerIndex];
			avatar.RunAction (SKAction.Sequence (new [] {
				SKAction.FadeAlphaTo (1f, 1),
				SKAction.FadeAlphaTo (0.2f, 1),
				SKAction.FadeAlphaTo (1f, 1)
			}));

			SKLabelNode label = hudLabels [playerIndex];
			float heartAlpha = 1f;

			switch (state) {
				case HUDState.Local:
					label.Text = "ME";
					break;

				case HUDState.Connecting:
					heartAlpha = 0.25f;
					label.Text = message ?? "AVAILABLE";
					break;

				case HUDState.Disconnected:
					avatar.Alpha = 0.5f;
					heartAlpha = 0.1f;
					label.Text = "NO PLAYER";
					break;

				case HUDState.Connected:
					label.Text = message ?? "CONNECTED";
					break;

				default:
					throw new NotImplementedException ();
			}

			for (int i = 0; i < player.LivesLeft; i++) {
				SKSpriteNode heart = hudLifeHeartArrays [playerIndex] [i];
				heart.Alpha = heartAlpha;
			}
		}

		void updateHUDFor (Player player)
		{
			int playerIndex = _players.IndexOf (player);
			SKLabelNode label = hudScores [playerIndex];
			label.Text = string.Format ("SCORE: {0}", player.Score);
		}

		void updateHUDAfterHeroDeathFor (Player player) {
			int playerIndex = _players.IndexOf (player);

			// Fade out the relevant heart - one-based livesLeft has already been decremented.
			int heartNumber = player.LivesLeft;

			List<SKSpriteNode> heartArray = hudLifeHeartArrays [playerIndex];
			var heart = heartArray [heartNumber];
			heart.RunAction (SKAction.FadeAlphaTo (0, 3));
		}

		#endregion

		#region Loop Update

		public override void Update (double currentTime)
		{
			// Handle time delta.
			// If we drop below 60fps, we still want everything to move the same distance.
			double timeSinceLast = currentTime - lastUpdateTimeInterval;
			lastUpdateTimeInterval = currentTime;

			// more than a second since last update
			if (timeSinceLast > 1) {
				timeSinceLast = MIN_TIME_INTERVAL;
				WorldMovedForUpdate = true;
			}

			UpdateWithTimeSinceLastUpdate (timeSinceLast);

			var defaultPlayer = DefaultPlayer;
			HeroCharacter hero = Heroes.Count > 0 ? defaultPlayer.Hero : null;

			if (hero != null && !hero.Dying
			    && defaultPlayer.TargetLocation != PointF.Empty) {
				if (defaultPlayer.FireAction)
					hero.FaceTo (defaultPlayer.TargetLocation);

				if (defaultPlayer.MoveRequested) {
					if (defaultPlayer.TargetLocation != hero.Position)
						hero.MoveTowards (defaultPlayer.TargetLocation, timeSinceLast);
					else
						defaultPlayer.MoveRequested = false;
				}
			}

			foreach (var player in _players) {
				if (player == null)
					continue;

				hero = player.Hero;
				if (hero == null || hero.Dying)
					continue;

				// heroMoveDirection is used by game controllers.
				PointF heroMoveDirection = player.HeroMoveDirection;
				if (GraphicsUtilities.Hypotenuse (heroMoveDirection.X, heroMoveDirection.Y) > 0f)
					hero.MoveInDirection (heroMoveDirection, timeSinceLast);
				else {
					if (player.MoveForward)
						hero.Move (MoveDirection.Forward, timeSinceLast);
					else if (player.MoveBack)
						hero.Move (MoveDirection.Back, timeSinceLast);

					if (player.MoveLeft)
						hero.Move (MoveDirection.Left, timeSinceLast);
					else if (player.MoveRight)
						hero.Move (MoveDirection.Right, timeSinceLast);
				}

				if (player.FireAction)
					hero.PerformAttackAction ();
			}
		}

		#endregion

		public override void DidSimulatePhysics ()
		{
			base.DidSimulatePhysics ();

			HeroCharacter defaultHero = DefaultPlayer.Hero;

			// Move the world relative to the default player position.
			if (defaultHero != null) {
				PointF heroPosition = defaultHero.Position;
				PointF worldPos = _world.Position;

				var yCoordinate = worldPos.Y + heroPosition.Y;
				if (yCoordinate < MIN_HERO_TO_EDGE_DISTANCE) {
					worldPos.Y = - heroPosition.Y + MIN_HERO_TO_EDGE_DISTANCE;
					WorldMovedForUpdate = true;
				} else if (yCoordinate > Frame.Size.Height - MIN_HERO_TO_EDGE_DISTANCE) {
					worldPos.Y = Frame.Size.Height - heroPosition.Y - MIN_HERO_TO_EDGE_DISTANCE;
					WorldMovedForUpdate = true;
				}

				var xCoordinate = worldPos.X + heroPosition.X;
				if (xCoordinate < MIN_HERO_TO_EDGE_DISTANCE) {
					worldPos.X = - heroPosition.X + MIN_HERO_TO_EDGE_DISTANCE;
					WorldMovedForUpdate = true;
				} else if (xCoordinate > Frame.Size.Width - MIN_HERO_TO_EDGE_DISTANCE) {
					worldPos.X = Frame.Size.Width - heroPosition.X - MIN_HERO_TO_EDGE_DISTANCE;
					WorldMovedForUpdate = true;
				}

				_world.Position = worldPos;
			}

			// Using performSelector:withObject:afterDelay: withg a delay of 0.0 means that the selector call occurs after
			// the current pass through the run loop.
			// This means the property will be cleared after the subclass implementation of didSimluatePhysics completes.
			PerformSelector (new Selector ("clearWorldMoved"), null, 0.0f);
		}

		[Export ("clearWorldMoved")]
		private void ClearWorldMoved ()
		{
			WorldMovedForUpdate = false;
		}

		#region Touch Events

		public override void TouchesBegan (NSSet touches, UIEvent evt)
		{
			if (Heroes.Count < 1)
				return;

			var touch = (UITouch)touches.AnyObject;

			var defaultPlayer = DefaultPlayer;
			if (defaultPlayer.MovementTouch != null)
				return;

			defaultPlayer.TargetLocation = touch.LocationInNode (defaultPlayer.Hero.Parent);

			bool wantsAttack = false;
			var nodes = GetNodesAtPoint (touch.LocationInNode (this));
			foreach (var node in nodes) {
				if (node == null || node.PhysicsBody == null)
					continue;

				if ((node.PhysicsBody.CategoryBitMask & (uint)(ColliderType.Cave | ColliderType.GoblinOrBoss)) > 0)
					wantsAttack = true;
			}

			defaultPlayer.FireAction = wantsAttack;
			defaultPlayer.MoveRequested = !wantsAttack;
			defaultPlayer.MovementTouch = touch;
		}

		public override void TouchesMoved (NSSet touches, UIEvent evt)
		{
			if (Heroes.Count < 1)
				return;

			UITouch touch = DefaultPlayer.MovementTouch;

			if (touch == null)
				return;

			if (touches.Contains (touch)) {
				DefaultPlayer.TargetLocation = touch.LocationInNode (DefaultPlayer.Hero.Parent);
				if (!DefaultPlayer.FireAction)
					DefaultPlayer.MoveRequested = true;
			}
		}

		public override void TouchesEnded (NSSet touches, UIEvent evt)
		{
			if (Heroes.Count < 1)
				return;

			UITouch touch = DefaultPlayer.MovementTouch;
			if (touch != null && touches.Contains (touch)) {
				DefaultPlayer.MovementTouch = null;
				DefaultPlayer.FireAction = false;
			}
		}
		#endregion

		#region Game Controllers
		/* This method should be called when the level is loaded to set up currently-connected game controllers,
  		   and register for the relevant notifications to deal with new connections/disconnections. */
		public void ConfigureGameControllers ()
		{
			// Receive notifications when a controller connects or disconnects.
			_didConnectObserver = GCController.Notifications.ObserveDidConnect(OnGameControllerDidConnect);
			_didDisconnectObserver = GCController.Notifications.ObserveDidDisconnect(OnGameControllerDidDisconnect);

			// Configure all the currently connected game controllers.
			configureConnectedGameControllers ();

			// And start looking for any wireless controllers.
			GCController.StartWirelessControllerDiscovery (() => Console.WriteLine ("Finished finding controllers"));
		}

		private void configureConnectedGameControllers ()
		{
			if (GCController.Controllers == null)
				return;

			// First deal with the controllers previously set to a player.
			foreach (var controller in GCController.Controllers) {
				int playerIndex = controller.PlayerIndex;
				if (playerIndex == GCController.PlayerIndexUnset)
					continue;

				assignPresetController (controller, playerIndex);
			}

			// Now deal with the unset controllers.
			foreach (var controller in GCController.Controllers) {
				int playerIndex = controller.PlayerIndex;
				if (playerIndex != GCController.PlayerIndexUnset)
					continue;

				assignUnknownController (controller);
			}
		}

		private void OnGameControllerDidConnect (object sender, NSNotificationEventArgs e)
		{
			var controller = (GCController)e.Notification.Object;
			Console.WriteLine ("Connected game controller: {0}", controller);

			int playerIndex = controller.PlayerIndex;
			if (playerIndex == GCController.PlayerIndexUnset)
				assignUnknownController (controller);
			else
				assignPresetController (controller, playerIndex);
		}

		private void OnGameControllerDidDisconnect (object sender, NSNotificationEventArgs e)
		{
			var controller = (GCController)e.Notification.Object;
			foreach (Player player in _players) {
				if (player == null)
					continue;

				if (player.Controller == controller)
					player.Controller = null;
			}

			Console.WriteLine ("Disconnected game controller: {0}", controller);
		}

		private void assignUnknownController (GCController controller)
		{
			for (int playerIndex = 0; playerIndex < NUM_PLAYERS; playerIndex++) {
				Player player = ConnectPlayerFor (playerIndex);
				if (player.Controller != null)
					continue;

				// Found an unlinked player.
				controller.PlayerIndex = playerIndex;
				ConfigureController (controller, player);
				return;
			}
		}

		private void assignPresetController (GCController controller, int playerIndex)
		{
			Player player = ConnectPlayerFor (playerIndex);

			if (player.Controller != null
				&& player.Controller != controller) {
				// Taken by another controller so reassign to another player.
				assignUnknownController (controller);
				return;
			}

			ConfigureController (controller, player);
		}

		private Player ConnectPlayerFor(int playerIndex)
		{
			Player player = _players[playerIndex];
			if (player == null) {
				player = new Player ();
				_players [playerIndex] = player;
				updateHUDFor (player, HUDState.Connected, "CONTROLLER");
			}

			return player;
		}

		private void ConfigureController(GCController controller, Player player)
		{
			Console.WriteLine ("Assigning {0} to player {1} [{2}]", controller.VendorName, player, _players.IndexOf (player));

			// Assign the controller to the player.
			player.Controller = controller;

			GCControllerDirectionPadValueChangedHandler dpadMoveHandler = (GCControllerDirectionPad dpad, float xValue, float yValue) => {
				float length = GraphicsUtilities.Hypotenuse(xValue, yValue);
				if (length > 0f) {
					float invLength = 1f / length;
					player.HeroMoveDirection = new PointF(xValue * invLength, yValue * invLength);
				} else {
					player.HeroMoveDirection = Point.Empty;
				}
			};

			// Use either the dpad or the left thumbstick to move the character.
			controller.ExtendedGamepad.LeftThumbstick.ValueChangedHandler = dpadMoveHandler;
			controller.Gamepad.DPad.ValueChangedHandler = dpadMoveHandler;

			GCControllerButtonValueChanged fireButtonHandler = (GCControllerButtonInput button, float value, bool pressed) => {
				player.FireAction = pressed;
			};

			// TODO: why it is not a property? https://trello.com/c/RcQk2UIB
			controller.Gamepad.ButtonA.SetValueChangedHandler (fireButtonHandler);
			controller.Gamepad.ButtonB.SetValueChangedHandler (fireButtonHandler);

			if (player != DefaultPlayer && player.Hero == null)
				AddHeroFor(player);
		}

		#endregion

		private HeroCharacter CreateHeroBy(HeroType type, PointF position, Player player)
		{
			switch (type) {
				case HeroType.Archer:
					return new Archer (position, player);

				case HeroType.Warrior:
					return new Warrior (position, player);

				default:
					throw new NotImplementedException ();
			}
		}

		protected override void Dispose (bool disposing)
		{
			// unsubscribe
			// For more info visit: http://iosapi.xamarin.com/?link=T%3aMonoTouch.Foundation.NSNotificationCenter
			_didConnectObserver.Dispose ();
			_didDisconnectObserver.Dispose ();

			base.Dispose (disposing);
		}
	}
}

