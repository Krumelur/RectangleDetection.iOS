using CocosSharp;

namespace CocosTest
{
    public static class VisualEffects
    {
		/// <summary>
		/// Creates a starry particle system.
		/// </summary>
		/// <returns>the particle system</returns>
	    public static CCParticleSystemQuad CreateStarParticleEmitter()
	    {
			var emitter = new CCParticleSystemQuad(50);
			emitter.Texture = CCTextureCache.SharedTextureCache.AddImage("particle_stars2.png");
		    emitter.AnchorPoint = CCPoint.AnchorMiddle;
			emitter.Duration = -1;

			// gravity
			emitter.Gravity = CCPoint.Zero;

			// angle
			emitter.Angle = 90;
			emitter.AngleVar = 360;

			// speed of particles
			emitter.Speed = (250);
			emitter.SpeedVar = (20);

			// radial
			emitter.RadialAccel = (-170);
			emitter.RadialAccelVar = (0);

			// tagential
			emitter.TangentialAccel = (30);
			emitter.TangentialAccelVar = (0);

			// emitter position
			emitter.Position = new CCPoint(160, 240);
			emitter.PositionVar = new CCPoint(0, 0);

			// life of particles
			emitter.Life = 4;
			emitter.LifeVar = 1;

			// spin of particles
			emitter.StartSpin = 0;
			emitter.StartSizeVar = 0;
			emitter.EndSpin = 0;
			emitter.EndSpinVar = 0;

			// color of particles
			var startColor = new CCColor4F(0.5f, 0.5f, 0.5f, 1.0f);
			emitter.StartColor = startColor;

			var startColorVar = new CCColor4F(0.5f, 0.5f, 0.5f, 1.0f);
			emitter.StartColorVar = startColorVar;

			var endColor = new CCColor4F(0.1f, 0.1f, 0.1f, 0.2f);
			emitter.EndColor = endColor;

			var endColorVar = new CCColor4F(0.1f, 0.1f, 0.1f, 0.2f);
			emitter.EndColorVar = endColorVar;

			// size, in pixels
			emitter.StartSize = 80.0f;
			emitter.StartSizeVar = 40.0f;
			emitter.EndSize = CCParticleSystem.ParticleStartSizeEqualToEndSize;

			// emits per second
			emitter.EmissionRate = emitter.TotalParticles / emitter.Life;

			// additive
			emitter.BlendAdditive = true;

			return emitter;
	    }
    }
}
