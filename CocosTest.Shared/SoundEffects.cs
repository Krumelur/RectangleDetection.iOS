using CocosDenshion;

namespace CocosTest
{
    public static class SoundEffects
    {
		/// <summary>
		/// Available sound effects. Gets mapped to the fxNames array.
		/// </summary>
	    public enum FX
	    {
			MemoryMatchPair = 0,
			MemoryIncorrectPair = 1
	    }

		static readonly string[] fxNames =
		{
			"sounds/magic_wand.mp3",
			"sounds/wrong.mp3"
		};

		/// <summary>
		/// Preloads all sound files.
		/// </summary>
		public static void PreloadSounds()
	    {
		    foreach (var s in fxNames)
		    {
				CCSimpleAudioEngine.SharedEngine.PreloadEffect(s);
		    }
	    }

		/// <summary>
		/// Plays a sound effect.
		/// </summary>
		/// <param name="fx">effect to play</param>
	    public static void PlayFx(FX fx)
	    {
		    CCSimpleAudioEngine.SharedEngine.PlayEffect(fxNames[(int)fx]);
	    }
    }
}
