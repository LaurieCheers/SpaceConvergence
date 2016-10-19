using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.IO;
using LRCEngine;

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
        List<ConvergeObject> hand;
        UIContainer ui;
        InputState inputState = new InputState();

        ConvergePlayer self;
        ConvergePlayer opponent;

        public static SpriteFont font;
        public static Texture2D powerbg;
        public static Texture2D woundbg;
        public static Texture2D shieldbg;

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

            self = new ConvergePlayer(data.getJSON("self"), Content);
            opponent = new ConvergePlayer(data.getJSON("opponent"), Content);

            ui = new UIContainer();

            ui.Add(new ConvergeUIObject(self.homeBase));
            ui.Add(new ConvergeUIObject(opponent.homeBase));

            foreach(JSONTable cardTemplate in data.getArray("cards").asJSONTables())
            {
                ConvergeObject handCard = new ConvergeObject(new ConvergeCardSpec(cardTemplate, Content), self.hand);
                ui.Add(new ConvergeUIObject(handCard));
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
            ui.Update(inputState);

            base.Update(gameTime);
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
