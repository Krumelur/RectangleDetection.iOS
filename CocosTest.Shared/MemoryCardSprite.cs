using System;
using CocosSharp;

namespace CocosTest
{
	/// <summary>
	/// A memory card imageSprite. Uses a background texture and places the actual card content on top of it.
	/// Card content will be scaled to fit into the background texture.
	/// </summary>
	internal sealed class MemoryCardSprite : CCSprite
	{
		static CCTexture2D backsideTexture;
		static CCTexture2D frontsideTexture;

		public const float MEMORY_CARD_SPRITE_WIDTH = 144;
		public const float MEMORY_CARD_SPRITE_HEIGHT = 144;
		public const float FLIP_ANIM_DURATION_SECONDS = 0.5f;

		const float skewAmount = 10;
		const float zoomAnimDurationSeconds = 0.1f;
		const float zoomFactor = 1.2f;

		static readonly CCSkewBy actionSkew = new CCSkewBy(FLIP_ANIM_DURATION_SECONDS / 2, 0, skewAmount);
		static readonly CCSkewTo actionFlipSkew = new CCSkewTo(0, 0, -skewAmount);
		static readonly CCSkewTo actionResetSkew = new CCSkewTo(FLIP_ANIM_DURATION_SECONDS / 2, 0, 0);
		static readonly CCScaleTo actionZoomCard = new CCScaleTo(zoomAnimDurationSeconds, zoomFactor);
		static readonly CCScaleTo actionReverseZoomCard = new CCScaleTo(zoomAnimDurationSeconds, 1.0f);

		static readonly CCDelayTime actionDelayZoom = new CCDelayTime(zoomAnimDurationSeconds);

		readonly CCCallFunc actionZOrder100;
		readonly CCCallFunc actionZOrder0;

		readonly CCScaleTo actionScaleCard;
		readonly CCScaleTo actionScaleCardReverse;

		public MemoryCardSprite(string filename)
			: base()
		{
			// Load one global texture for all backsides of the cards.
			if (backsideTexture == null)
			{
				backsideTexture = CCTextureCache.SharedTextureCache.AddImage("memory_card_backside.png");
			}

			if (frontsideTexture == null)
			{
				frontsideTexture = CCTextureCache.SharedTextureCache.AddImage("memory_card_background.png");
			}

			this.Texture = frontsideTexture;
			this.AnchorPoint = CCPoint.AnchorMiddle;

			this.imageSprite = new CCSprite(filename)
			{
				AnchorPoint = CCPoint.AnchorMiddle,
				// Add the content to the background imageSprite and center.
				Position = new CCPoint(MEMORY_CARD_SPRITE_WIDTH / 2, MEMORY_CARD_SPRITE_HEIGHT / 2)
			};

			// Scale the card content into the background texture.
			this.imageSprite.Scale = Math.Min(MEMORY_CARD_SPRITE_WIDTH / imageSprite.ContentSize.Width, MEMORY_CARD_SPRITE_HEIGHT / imageSprite.ContentSize.Height);
			this.AddChild(this.imageSprite);


			actionScaleCard = new CCScaleTo(FLIP_ANIM_DURATION_SECONDS / 2, 0, this.ScaleY * zoomFactor);
			actionScaleCardReverse = new CCScaleTo(FLIP_ANIM_DURATION_SECONDS / 2, this.ScaleX * zoomFactor, this.ScaleY * zoomFactor);

			actionZOrder100 = new CCCallFunc(() => this.ZOrder = 100);
			actionZOrder0 = new CCCallFunc(() => this.ZOrder = 0);

			this.typeLabel = new CCLabel(string.Empty, "Arial", 24f) { Color = CCColor3B.Red, Text = this.TypeId.ToString() };
#if DEBUG
			this.AddChild(typeLabel);
#endif
		}

		/// <summary>
		/// Gets if the frontside of the card is currently visible.
		/// </summary>
		public bool IsRevealed
		{
			get;
			private set;
		}

		readonly CCSprite imageSprite;
		readonly CCLabel typeLabel;
		int typeId;

		/// <summary>
		/// Flips the card over.
		/// </summary>
		/// <param name="revealed">TRUE to reveal the card, FALSE to hide frontside</param>
		/// <param name="animate">TRUE to animate</param>
		public void Flip(bool revealed, bool animate)
		{
			this.IsRevealed = revealed;

			if (!animate)
			{
				this.Texture = revealed ? frontsideTexture : backsideTexture;
				this.imageSprite.Visible = revealed;
				return;
			}

			// Use CCOrbitCamera to flip card. Looks great but does not work if sprite is not centered (rotating 90 degrees will not fully hide the sprite).
			//var animateInFirstHalf = new CCOrbitCamera(animDurationSeconds, 1, 0, 0, 90f, 0, 0);
			//var swapTextureIn = new CCCallFunc(() => { card.Texture = new CCTexture2D("memory_card_backside.png"); });
			//var animateInSecondHalf = new CCSpawn(new CCOrbitCamera(animDurationSeconds, 1, 0, 90, 180, 0, 0), new CCScaleTo(animDurationSeconds, 1.5f));
			//var animateOutFirstHalf = new CCSpawn(new CCOrbitCamera(animDurationSeconds, 1, 0, 180, -180, 0, 0), new CCScaleTo(animDurationSeconds, 1f));
			//var swapTextureOut = new CCCallFunc(() => { card.Texture = currentTexture; });
			//var animateOutSecondHalf = new CCSpawn(new CCOrbitCamera(animDurationSeconds, 1, 0, 180, -180, 0, 0), new CCScaleTo(animDurationSeconds, 1f));
			//var sequence = new CCSequence(animateInFirstHalf, new CCDelayTime(2), swapTextureIn, new CCDelayTime(2), animateInSecondHalf, new CCDelayTime(2), animateOutFirstHalf, new CCDelayTime(2), swapTextureOut, new CCDelayTime(2), animateOutSecondHalf);
			//card.RunAction(sequence);


			// Fake flip effect with actionScaleCard and skew.
			this.RunActions(
                actionZOrder100, 
                actionZoomCard, 
                actionScaleCard, 
                new CCCallFunc(() => {
                    this.Texture = revealed ? frontsideTexture : backsideTexture;
                    this.imageSprite.Visible = revealed;
                }), 
                actionScaleCardReverse, 
                actionReverseZoomCard, 
                actionZOrder0);
 
			// flip the skew value after the initial half of the animation for a more credible 3d effect
			this.RunActions(actionDelayZoom, actionSkew, actionFlipSkew, actionResetSkew);
		}
			

		/// <summary>
		/// Every card has a type. There must be two cards of each type in a game.
		/// </summary>
		public int TypeId
		{
			get { return typeId; }
			set
			{
				typeId = value;
#if DEBUG
				this.typeLabel.Text = value.ToString();
#endif
			}
		}

		public override string ToString()
		{
			return string.Format("[MemoryCard: TypeId={0}]", this.TypeId);
		}
	}
}

