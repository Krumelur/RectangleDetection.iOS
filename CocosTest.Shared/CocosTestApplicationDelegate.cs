using CocosSharp;

namespace CocosTest
{
	public class CocosTestApplicationDelegate : CCApplicationDelegate
	{
		public const float DESIGN_WIDTH = 1024;
		public const float DESIGN_HEIGHT = 768;

		public override void ApplicationDidEnterBackground (CCApplication application)
		{
			// stop all of the animation actions that are running.
			application.Paused = true;
		}

		public override void ApplicationWillEnterForeground (CCApplication application)
		{
			application.Paused = false;
		}

		public override void ApplicationDidFinishLaunching (CCApplication application, CCWindow mainWindow)
		{
			mainWindow.SetDesignResolutionSize (DESIGN_WIDTH, DESIGN_HEIGHT, CCSceneResolutionPolicy.ShowAll);
			application.PreferMultiSampling = false;
			application.ContentRootDirectory = "content";

			if (DESIGN_WIDTH < mainWindow.WindowSizeInPixels.Width)
			{
				Util.Log("Using HD textures (design width = {0}, pixel width = {1}.", DESIGN_WIDTH, mainWindow.WindowSizeInPixels.Width);
				application.ContentSearchPaths.Add("memory_hd");
				
				// Without changing the texel to pixel ration, the HD textures would be too big because design resolution is 1024x768.
				CCSprite.DefaultTexelToContentSizeRatio = 2.0f;
			}
			else
			{
				Util.Log("Using LD textures (design width = {0}, pixel width = {1}.", DESIGN_WIDTH, mainWindow.WindowSizeInPixels.Width);
				application.ContentSearchPaths.Add("memory_ld");
				CCSprite.DefaultTexelToContentSizeRatio = 1.0f;
			}

			// Fonts don't need to be registered, but it reduces loading time if they are. 
			// Don't forget to mark added fonts as "BundleResource"!
			CCSpriteFontCache.RegisterFont("arial", 12, 16, 18, 20, 22, 24, 26, 28, 30, 32, 34, 38, 50, 64);

			// Preload sound files.
			SoundEffects.PreloadSounds();

			//CCScene scene = StartScreenLayer.CreateScene(mainWindow);
			CCScene scene = MemoryGameLayer.CreateScene(mainWindow);
			mainWindow.RunWithScene (scene);
		}

	}
}