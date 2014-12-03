using CocosSharp;

namespace CocosTest
{
	public sealed class StartScreenLayer : CCLayerColor
	{
		private StartScreenLayer () : base()
		{
			Color = CCColor3B.Green;
			Opacity = 255;

			this.AddEventListener (new CCEventListenerTouchAllAtOnce
			{ 
				OnTouchesBegan = (touches, ev) => {
					this.Window.DefaultDirector.ReplaceScene (MemoryGameLayer.CreateScene(this.Window));
				}
			}, this);
		}

		protected override void AddedToScene ()
		{
			base.AddedToScene ();

			var label = new CCLabel ("Bildschirm berühren!", "Times New Roman", 44)
			{
				Position = new CCPoint(CocosTestApplicationDelegate.DESIGN_WIDTH / 2, CocosTestApplicationDelegate.DESIGN_HEIGHT / 2),
				Color = CCColor3B.Black,
				IsAntialiased = true,
				HorizontalAlignment = CCTextAlignment.Center,
				VerticalAlignment = CCVerticalTextAlignment.Center,
				IgnoreAnchorPointForPosition = true
			};
			this.AddChild (label);
		}

		public static CCScene CreateScene(CCWindow window)
		{
			var scene = new CCScene (window);
			var layer = new StartScreenLayer ();
			scene.AddChild (layer);
			return scene;
		}
	}
}
