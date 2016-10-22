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
        Dictionary<string, ConvergeCardSpec> allCards;
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

        public static RichImage mouseOverGlow;

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
            ui.Add(new UIButton("End Turn", new Rectangle(600, 400, 80, 40), defaultStyle, EndTurn_onPress));

            allCards = new Dictionary<string, ConvergeCardSpec>();
            JSONTable allCardsTemplate = data.getJSON("cards");
            foreach (string cardName in allCardsTemplate.Keys)
            {
                allCards.Add(cardName, new ConvergeCardSpec(allCardsTemplate.getJSON(cardName), Content));
            }

            foreach(string cardName in data.getArray("mydeck").asStrings())
            {
                //ConvergeObject handCard =
                new ConvergeObject(allCards[cardName], self.laboratory);
                //ui.Add(new ConvergeUIObject(handCard));
            }

            foreach (string cardName in data.getArray("oppdeck").asStrings())
            {
                //ConvergeObject handCard =
                new ConvergeObject(allCards[cardName], opponent.laboratory);
                //ui.Add(new ConvergeUIObject(handCard));
            }

            UpdateZoneChanges();

            self.BeginGame();
            opponent.BeginGame();

            activePlayer = self;
            self.BeginMyTurn();
        }

        public void EndTurn_onPress()
        {
            activePlayer.EndMyTurn();
            activePlayer = activePlayer.opponent;
            activePlayer.BeginMyTurn();

            foreach (ConvergeObject obj in Game1.inPlayList)
            {
                obj.BeginAnyTurn(activePlayer);
            }
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

            inputState.hoveringElement = ui.GetMouseHover(inputState.MousePos);

            UpdateZoneChanges();
            ui.Update(inputState);

            base.Update(gameTime);
        }

        void UpdateZoneChanges()
        {
            bool didAnything = zoneChanges.Count > 0;
            foreach (KeyValuePair<ConvergeObject, ConvergeZone> kv in zoneChanges)
            {
                ConvergeObject obj = kv.Key;
                ConvergeZone newZone = kv.Value;
                newZone.Add(obj);

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
            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
