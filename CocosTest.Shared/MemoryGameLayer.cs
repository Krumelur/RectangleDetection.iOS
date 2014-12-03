using System;
using System.Linq;
using CocosSharp;
using System.Collections.Generic;
using System.Diagnostics;

namespace CocosTest
{
	public sealed class MemoryGameLayer : CCLayer
	{
		public const float CARD_SPACING = 10;

		/// <summary>
		/// Tag to identify particle system upon removal.
		/// </summary>
		const int PARTICLE_SYSTEM_TAG = 20;

		public sealed class RowCol : Tuple<int, int>
		{
			public RowCol(int row, int col)
				: base(row, col)
			{
			}

			public int Row
			{
				get
				{
					return this.Item1;
				}
			}

			public int Column
			{
				get
				{
					return this.Item2;
				}
			}

			public override string ToString()
			{
				return string.Format("[RowCol: Row={0}, Column={1}]", Row, Column);
			}
		}

		/// <summary>
		/// Defines how cards will be layouted.
		/// </summary>
		static Dictionary<int, RowCol> CardLayout = new Dictionary<int, RowCol>
		{
			{4, new RowCol(2, 2)},
			{6, new RowCol(2, 3)},
			{8, new RowCol(2, 4)},
			{10, new RowCol(2, 5)},
			{12, new RowCol(3, 4)},
			{14, new RowCol(3, 5)},
			{16, new RowCol(4, 4)},
			{18, new RowCol(4, 5)},
			{20, new RowCol(4, 5)},
			{22, new RowCol(4, 6)},
			{24, new RowCol(4, 6)},
		};

		RowCol activeCardLayout;

		/// <summary>
		/// The cards aligned in a grid.
		/// </summary>
		MemoryCardSprite[] cardsGrid;

		IList<string> GetCardTextureFiles()
		{
			// http://opengameart.org/content/animals-pack
			return new List<string>
			{
				"default_memory_card_textures/bear.png",
				"default_memory_card_textures/cat.png",
				"default_memory_card_textures/cow.png",
				"default_memory_card_textures/dog.png",
				"default_memory_card_textures/dragon.png",
				"default_memory_card_textures/monkey.png",
				"default_memory_card_textures/pig.png",
				"default_memory_card_textures/rabbit.png",
				"default_memory_card_textures/rat.png"
			};
		}

		private MemoryGameLayer()
		{
			this.Color = new CCColor3B(CCColor4B.Blue);
		}

		/// <summary>
		/// Restarts the game.
		/// </summary>
		void RestartScene()
		{
			var rand = new Random();
			int cardsPlaced = 0;
			var takenCardIndexes = new HashSet<int>();

			var cardTextureFiles = this.GetCardTextureFiles();
			Debug.Assert(cardTextureFiles != null, "Loading memory card texture filenames returned NULL!");

			// Every card will be shown twice.
			this.totalNumCards = cardTextureFiles.Count * 2;

			if (!CardLayout.ContainsKey(this.totalNumCards))
			{
				throw new InvalidOperationException("Total number of cards must be 4, 6, 8, 10, 12, 14, 16, 18, 20, 22 or 24 - " + this.totalNumCards + " is not supported!");
			}

			this.activeCardLayout = CardLayout[this.totalNumCards];

			Util.Log("Creating {0} cards in total. Layout: {1}", this.totalNumCards, this.activeCardLayout);

			// Create a grid of cards.
			this.cardsGrid = new MemoryCardSprite[this.activeCardLayout.Row * this.activeCardLayout.Column];

			// Create a pair of cards for each texture.
			int cardTypeId = 0;

			if (this.allCards != null)
			{
				foreach (var card in allCards)
				{
					card.RemoveFromParent();
				}
			}

			this.allCards = new List<MemoryCardSprite>();
			foreach (var cardTextureFile in cardTextureFiles)
			{
				for (int i = 0; i < 2; i++)
				{
					var card = new MemoryCardSprite(cardTextureFile) { TypeId = cardTypeId };
					this.allCards.Add(card);
				}
				cardTypeId++;
			}

			Util.Log("Shuffling card locations.");
			while (cardsPlaced < this.totalNumCards)
			{
				int randomRowColIndex = rand.Next(0, this.totalNumCards);

				if (takenCardIndexes.Contains(randomRowColIndex))
				{
					continue;
				}

				takenCardIndexes.Add(randomRowColIndex);

				var currentCard = this.allCards[cardsPlaced];
				currentCard.Scale = 1f;

				// Stick cards into the grid.
				this.cardsGrid[randomRowColIndex] = currentCard;
				Util.Log("Card index {0}; {1}", cardsPlaced, currentCard);

				// Also add card to the scene.
				currentCard.Visible = false;
				if (currentCard.Parent == null)
				{
					this.AddChild(currentCard);
				}

				cardsPlaced++;
			}
			Util.Log("Done shuffling cards.");

			this.UpdateCardPositions();
			this.MoveCardsToFinalPosition();
		}

		protected override void AddedToScene()
		{
			base.AddedToScene();

			CCMenuItemFont.FontSize = 30;
			var menuItemRestart = new CCMenuItemFont(Util.Localize("Neu starten"), obj => this.RestartScene());
			var menu = new CCMenu(menuItemRestart)
			{
				Position = new CCPoint(CocosTestApplicationDelegate.DESIGN_WIDTH / 2, 20)
			};
			menu.AlignItemsHorizontally();
			this.AddChild(menu);

			// Add a touch listener which allows flipping the cards.
			var touchListener = new CCEventListenerTouchOneByOne
			{
				OnTouchBegan = this.HandleTouch
			};
			this.AddEventListener(touchListener);

			// Two emitters which will be shown if a pair of cards gets removed.
			this.emitter1 = VisualEffects.CreateStarParticleEmitter();
			this.emitter1.Tag = PARTICLE_SYSTEM_TAG;

			this.emitter2 = VisualEffects.CreateStarParticleEmitter();
			this.emitter2.Tag = PARTICLE_SYSTEM_TAG;

			// Actions can be reused, so let's have the ones that are used over and over again.
			this.actionDelayTimeFlipAnim = new CCDelayTime(MemoryCardSprite.FLIP_ANIM_DURATION_SECONDS * 1.2f);
			const float shakeSpeedSeconds = 0.03f;
			const int shakeTimes = 3;
			// Shake animation. Used if a non-matching pair of cards is discovered.
//			this.actionDelayedShake = new CCSequence(
//				actionDelayTimeFlipAnim,
//				new CCCallFuncN(c =>
//				{
//					// Play error sound.
//					SoundEffects.PlayFx(SoundEffects.FX.MemoryIncorrectPair);
//					c.Repeat(shakeTimes, new CCSequence(
//						new CCRotateTo(shakeSpeedSeconds, 5f),
//						new CCRotateTo(shakeSpeedSeconds, 0f),
//						new CCRotateTo(shakeSpeedSeconds, -5f),
//						new CCRotateTo(shakeSpeedSeconds, 0f))
//					);
//				})
//			);

			this.actionDelayedShake = new CCSequence(
				actionDelayTimeFlipAnim,
				new CCShaky3D(0.6f, new CCGridSize(15, 10), 4, true),
				new CCStopGrid()
			);

			this.actionZIndex0 = new CCCallFuncN(c => c.ZOrder = 0);
			this.actionZIndex100 = new CCCallFuncN(c => c.ZOrder = 100);
					


			// Particle and fade out if a matching pair of cards was found.
			this.actionRemoveMatchingCards = new CCSequence(
				actionDelayTimeFlipAnim,
				new CCCallFuncN(c =>
				{
					// Play ferry sound.
					SoundEffects.PlayFx(SoundEffects.FX.MemoryMatchPair);
					// Show stars.
					if (emitter1.Parent == null)
					{
						c.AddChild(emitter1, 10, PARTICLE_SYSTEM_TAG);
					}
					else
					{
						c.AddChild(emitter2, 10, PARTICLE_SYSTEM_TAG);
					}
				}),
				new CCScaleTo(0.5f, 1.4f),
				new CCScaleTo(0.3f, 0f),
				new CCCallFuncN(c =>
				{
					// Remove the emitter first.
					c.GetChildByTag(PARTICLE_SYSTEM_TAG).RemoveFromParent();
					c.RemoveFromParent();
					this.CheckGameWon();
				})
				);

			// Start the scene.
			this.RestartScene();
		}

		bool HandleTouch(CCTouch touch, CCEvent ev)
		{
			// Touches are caught globally. Even if the listener is attached to a specific node, it will still trigger if _any_ screen coordinate
			// is touched (also the ones outside of the node).

			if (this.currentFlippedCards.Count == 2)
			{
				// If two (non-matching) cards are currently revealed, flip them back.
				this.currentFlippedCards.ForEach(c => c.Flip(revealed: false, animate: true));

				// Reset currently revealed cards.
				this.currentFlippedCards.Clear();
				return true;
			}


			var card = this.cardsGrid.FirstOrDefault(c => c != null && c.BoundingBoxTransformedToWorld.ContainsPoint(touch.Location));
			if (card == null || card.NumberOfRunningActions > 0 || (card.IsRevealed && this.currentFlippedCards.Count > 0))
			{
				return true;
			}

			Util.Log("Flipping card {0}. Currently revealed: {1}", card, card.IsRevealed);

			card.Flip(revealed: !card.IsRevealed, animate: true);

			this.currentFlippedCards.Add(card);

			if (this.currentFlippedCards.Count == 2)
			{
				var card1 = this.currentFlippedCards[0];
				var card2 = this.currentFlippedCards[1];

				// If card types match, remove the two cards. Player scores.
				if (card1.TypeId == card2.TypeId)
				{
					// Show a fancy particle emitter as reward.
					this.emitter1.Position = card1.ContentSize.Center;
					this.emitter2.Position = card2.ContentSize.Center;

					// Fade out cards.
					card1.RunAction(this.actionRemoveMatchingCards);
					card2.RunAction(this.actionRemoveMatchingCards);

					// Reset currently revealed cards.
					this.currentFlippedCards.Clear();
				}
				else
				{
					// Show shaky animation if pairs mismatch.
					card1.RunAction(this.actionDelayedShake);
					card2.RunAction(this.actionDelayedShake);
				}
			}

			return true;
		}

		/// <summary>
		/// Checks if the game is won.
		/// </summary>
		void CheckGameWon()
		{
			bool cardsLeft = this.allCards.Any(c => c.Parent != null);
			if (!cardsLeft)
			{
				this.RestartScene();
			}
		}

		readonly List<MemoryCardSprite> currentFlippedCards = new List<MemoryCardSprite>();
		int totalNumCards;
		List<MemoryCardSprite> allCards;
		CCParticleSystemQuad emitter1;
		CCParticleSystemQuad emitter2;
		CCDelayTime actionDelayTimeFlipAnim;
		CCAction actionDelayedShake;
		CCSequence actionRemoveMatchingCards;
		CCCallFuncN actionZIndex0;
		CCCallFuncN actionZIndex100;

		void UpdateCardPositions()
		{
			float topIndent = (CocosTestApplicationDelegate.DESIGN_HEIGHT - (this.activeCardLayout.Row * (MemoryCardSprite.MEMORY_CARD_SPRITE_HEIGHT + MemoryGameLayer.CARD_SPACING) - MemoryGameLayer.CARD_SPACING)) / 2f;

			for (int row = 0; row < this.activeCardLayout.Row; row++)
			{
				// Get number of unoccupied spots in the row to center everything nicely.
				int unoccupiedColumnsCount = 0;
				int startIndex = row * this.activeCardLayout.Column;
				for (int index = startIndex; index < startIndex + this.activeCardLayout.Column; index++)
				{
					if (this.cardsGrid[index] == null)
					{
						unoccupiedColumnsCount++;
					}
				}
				float rowIndent = (CocosTestApplicationDelegate.DESIGN_WIDTH - (this.activeCardLayout.Column * (MemoryCardSprite.MEMORY_CARD_SPRITE_WIDTH + MemoryGameLayer.CARD_SPACING) - MemoryGameLayer.CARD_SPACING)) / 2f +
					(unoccupiedColumnsCount * (MemoryCardSprite.MEMORY_CARD_SPRITE_WIDTH + MemoryGameLayer.CARD_SPACING)) / 2f;

				for (int col = 0; col < this.activeCardLayout.Column; col++)
				{
					var card = this.cardsGrid[row * this.activeCardLayout.Column + col];
					if (card == null)
					{
						continue;
					}

					card.Visible = false;
					card.Flip(revealed: false, animate: false);
					card.Position = new CCPoint(
						rowIndent + col * (MemoryCardSprite.MEMORY_CARD_SPRITE_WIDTH + MemoryGameLayer.CARD_SPACING) + MemoryCardSprite.MEMORY_CARD_SPRITE_WIDTH / 2,
						CocosTestApplicationDelegate.DESIGN_HEIGHT - topIndent - row * (MemoryCardSprite.MEMORY_CARD_SPRITE_HEIGHT + MemoryGameLayer.CARD_SPACING) - MemoryCardSprite.MEMORY_CARD_SPRITE_HEIGHT / 2);
				}
			}
		}

		/// <summary>
		/// Moves cards in from off screen to their final positions.
		/// </summary>
		void MoveCardsToFinalPosition()
		{
			// Animate cards in.
			float delay = 0f;

			foreach (var card in this.cardsGrid)
			{
				if (card == null)
				{
					continue;
				}

				var finalPosition = card.Position;
				card.Position = new CCPoint(CocosTestApplicationDelegate.DESIGN_WIDTH / 2, CocosTestApplicationDelegate.DESIGN_HEIGHT + MemoryCardSprite.MEMORY_CARD_SPRITE_HEIGHT);
				card.Visible = true;

				// Move in, rotate 360° while moving in and ease out to final position. Delay each card a bit.
				var move = new CCMoveTo(0.5f, finalPosition);
				var ease = new CCEaseOut(move, 0.3f);
				var rotate = new CCRotateBy(ease.Duration, 360);
				card.RunAction(new CCSequence(
					this.actionZIndex100,
					new CCDelayTime(delay),
					new CCSpawn(ease, rotate),
					this.actionZIndex0
				));

				delay += 0.05f;
			}
		}

		public static CCScene CreateScene(CCWindow window)
		{
			var scene = new CCScene(window);
			var layer = new MemoryGameLayer();
			scene.AddChild(layer);
			return scene;
		}
	}
}

