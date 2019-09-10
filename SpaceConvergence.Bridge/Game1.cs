using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.IO;
using LRCEngine;
using System;

namespace SpaceConvergence
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        JSONTable data;
        _Dictionary<string, ConvergeCardSpec> allCards;
        public static UIContainer ui;

        public static List<KeyValuePair<ConvergeObject, ConvergeZone>> zoneChanges = new List<KeyValuePair<ConvergeObject, ConvergeZone>>();

        public static ConvergePlayer activePlayer;
        public static Random rand = new Random();
        InputState inputState = new InputState();

        ConvergePlayer self;
        ConvergePlayer opponent;
        public static List<ConvergeObject> inPlayList = new List<ConvergeObject>();

        public static SpriteFont font;
        public static Texture2D powerbg;
        public static Texture2D woundbg;
        public static Texture2D shieldbg;
        public static Texture2D tappedicon;
        public static Texture2D[] resourceTextures;
        public static Texture2D abilityHighlight;
        public static Texture2D targetArrow;
        public static Texture2D targetBeam;
        public static Texture2D badTargetArrow;
        public static Texture2D badTargetBeam;
        public static Texture2D attackBeam;

        public static RichImage mouseOverGlow;
        public static RichImage cardFrame;
        public static RichImage whiteFrame;
        public static RichImage blueFrame;
        public static RichImage blackFrame;
        public static RichImage redFrame;
        public static RichImage greenFrame;
        public static RichImage goldFrame;

        UIButton endTurnButton;
        bool endTurnPressed;
        public static int countdown { get; private set; }
        public static bool ticking;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
            data = new JSONTable("Content/data.json");

            mouseOverGlow = new RichImage(data.getJSON("mouseOverGlow"), Content);
            cardFrame = new RichImage(data.getJSON("cardFrame"), Content);
            whiteFrame = new RichImage(data.getJSON("whiteFrame"), Content);
            blueFrame = new RichImage(data.getJSON("blueFrame"), Content);
            blackFrame = new RichImage(data.getJSON("blackFrame"), Content);
            redFrame = new RichImage(data.getJSON("redFrame"), Content);
            greenFrame = new RichImage(data.getJSON("greenFrame"), Content);
            goldFrame = new RichImage(data.getJSON("goldFrame"), Content);

            font = Content.Load<SpriteFont>("Arial");
            shieldbg = Content.Load<Texture2D>("shieldbg");
            powerbg = Content.Load<Texture2D>("powerbg");
            woundbg = Content.Load<Texture2D>("woundbg");
            tappedicon = Content.Load<Texture2D>("tapped");
            abilityHighlight = Content.Load<Texture2D>("abilityHighlight");
            targetArrow = Content.Load<Texture2D>("targetArrow");
            targetBeam = Content.Load<Texture2D>("targetBeam");
            badTargetArrow = Content.Load<Texture2D>("badTargetArrow");
            badTargetBeam = Content.Load<Texture2D>("badTargetBeam");
            attackBeam = Content.Load<Texture2D>("attackBeam");

            resourceTextures = new Texture2D[]
            {
                Content.Load<Texture2D>("total_resources"),
                Content.Load<Texture2D>("white_hats"),
                Content.Load<Texture2D>("blue_science"),
                Content.Load<Texture2D>("black_hats"),
                Content.Load<Texture2D>("red_munitions"),
                Content.Load<Texture2D>("green_seeds"),
            };

            ui = new UIContainer();

            self = new ConvergePlayer(data.getJSON("self"), Content);
            opponent = new ConvergePlayer(data.getJSON("opponent"), Content);
            self.opponent = opponent;
            opponent.opponent = self;

            ui.Add(new ConvergeUIObject(self.homeBase));
            ui.Add(new ConvergeUIObject(opponent.homeBase));

            UIButtonStyle defaultStyle = UIButton.GetDefaultStyle(Content);
            endTurnButton = new UIButton("End Turn", new Rectangle(600, 400, 80, 40), defaultStyle, EndTurn_onPress);
            ui.Add(endTurnButton);

            JSONTable allCardsTemplate = data.getJSON("cards");
            foreach (string cardName in allCardsTemplate.Keys)
            {
                ConvergeCardSpec newSpec = new ConvergeCardSpec();
                ConvergeCardSpec.allCards.Add(cardName, newSpec);

                newSpec.Init(allCardsTemplate.getJSON(cardName), Content);
            }

            foreach(string cardName in data.getArray("mydeck").asStrings())
            {
                //ConvergeObject handCard =
                new ConvergeObject(ConvergeCardSpec.allCards[cardName], self.laboratory);
                //ui.Add(new ConvergeUIObject(handCard));
            }

            foreach (string cardName in data.getArray("oppdeck").asStrings())
            {
                //ConvergeObject handCard =
                new ConvergeObject(ConvergeCardSpec.allCards[cardName], opponent.laboratory);
                //ui.Add(new ConvergeUIObject(handCard));
            }

            UpdateZoneChanges();

            self.BeginGame();
            opponent.BeginGame();

            activePlayer = self;
            self.BeginMyTurn();
            opponent.numLandsPlayed = 1; // can't play a land in your first response phase
        }

        public void EndTurn_onPress()
        {
            endTurnPressed = true;
        }

        public void EndTurn_startTimer()
        {
            activePlayer.SufferWounds();
            UpdateZoneChanges();
            activePlayer.EndMyTurn();
            activePlayer = activePlayer.opponent;
            endTurnButton.visible = false;
            countdown = 30;
        }

        public void EndTurn_timerExpired()
        {
            activePlayer.SufferWounds();
            UpdateZoneChanges();
//            activePlayer = activePlayer.opponent;
            activePlayer.BeginMyTurn();

            foreach (ConvergeObject obj in Game1.inPlayList)
            {
                obj.BeginAnyTurn(activePlayer);
            }

            endTurnButton.visible = true;
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            inputState.Update();

            ticking = (countdown > 0);

            inputState.hoveringElement = ui.GetMouseHover(inputState.MousePos);

            UpdateZoneChanges();
            ui.Update(inputState);

            if(endTurnPressed)
            {
                EndTurn_startTimer();
                endTurnPressed = false;
            }

            if (ticking)
            {
                countdown--;
                if(countdown == 0)
                {
                    EndTurn_timerExpired();
                }
            }

            base.Update(gameTime);
        }

        void UpdateZoneChanges()
        {
            bool didAnything = zoneChanges.Count > 0;
            for(int Idx = 0; Idx < zoneChanges.Count; ++Idx)
            {
                KeyValuePair<ConvergeObject, ConvergeZone> kv = zoneChanges[Idx];
                ConvergeObject obj = kv.Key;
                ConvergeZone newZone = kv.Value;
                ConvergeZone oldZone = obj.zone;

                if (newZone.zoneId == ConvergeZoneId.DiscardPile && TriggerSystem.HasTriggers(ConvergeTriggerType.Discarded))
                {
                    TriggerSystem.CheckTriggers(ConvergeTriggerType.Discarded, new TriggerData(newZone.owner, null, obj, 0));
                }

                newZone.Add(obj);

                if (newZone.inPlay && (oldZone == null || !oldZone.inPlay) && TriggerSystem.HasTriggers(ConvergeTriggerType.EnterPlay))
                {
                    TriggerSystem.CheckTriggers(ConvergeTriggerType.EnterPlay, new TriggerData(newZone.owner, obj, null, 0));
                }

                if (obj.ui == null && !newZone.isHidden && obj.zone == newZone)
                {
                    obj.ui = new ConvergeUIObject(obj);
                    ui.Add(obj.ui);
                }
                else if (obj.ui != null && newZone.isHidden && obj.zone == newZone)
                {
                    ui.Remove(obj.ui);
                    obj.ui = null;
                }
            }
            zoneChanges.Clear();
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here
            spriteBatch.Begin();
            ui.Draw(spriteBatch);
            if(countdown > 0)
            {
                spriteBatch.DrawString(font, "" + countdown, new Vector2(620, 420), Color.Black);
            }

            if(inputState.hoveringElement is ConvergeUIObject)
            {
                ((ConvergeUIObject)inputState.hoveringElement).DrawTooltip(spriteBatch);
            }
            else if (inputState.hoveringElement is ConvergeUIAbility)
            {
                ((ConvergeUIAbility)inputState.hoveringElement).DrawTooltip(spriteBatch);
            }
            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
