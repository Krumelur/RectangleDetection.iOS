using MonoTouch.Foundation;
using MonoTouch.UIKit;
using CocosSharp;

namespace CocosTest
{
	// The UIApplicationDelegate for the application. This class is responsible for launching the
	// User Interface of the application, as well as listening (and optionally responding) to
	// application events from iOS.
	[Register ("AppDelegate")]
	public partial class AppDelegate : UIApplicationDelegate
	{
		public override void FinishedLaunching (UIApplication app)
		{
			var application = new CCApplication ();
			application.ApplicationDelegate = new CocosTestApplicationDelegate ();
			application.StartGame ();
		}
	}
}

