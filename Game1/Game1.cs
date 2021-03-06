﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Game1
{
	/// <summary>
	/// This is the main type for your game.
	/// </summary>
	public class Game1 : Game
	{
		GraphicsDeviceManager graphics;
		SpriteBatch spriteBatch;

		Effect distortEffect;

		RenderTarget2D sceneMap;

		Texture2D backgroundTexture;
		Texture2D gradTexture;

		Vector2 spritePos;
		
		Droplet droplet;
		public float waveSpeed = 1.25f;
		public float reflectionStrength = 1.7f;
		public Color reflectionColor = Color.Gray;
		public float refractionStrength = 2.5f;
		public float dropInterval = 1.5f;

		float timer;

		public Game1()
		{
			graphics = new GraphicsDeviceManager(this);

			graphics.IsFullScreen = false;
			graphics.PreferredBackBufferWidth = 800;
			graphics.PreferredBackBufferHeight = 600;
			this.IsMouseVisible = true;

			Content.RootDirectory = "Content";

			droplet = new Droplet();
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
			spriteBatch = new SpriteBatch(graphics.GraphicsDevice);

			base.Initialize();
		}

		/// <summary>
		/// LoadContent will be called once per game and is the place to load
		/// all of your content.
		/// </summary>
		protected override void LoadContent()
		{
			// TODO: use this.Content to load your game content here
			distortEffect = Content.Load<Effect>("Distorter_Ripple");

			backgroundTexture = Content.Load<Texture2D>("background");

			// look up the resolution and format of our main backbuffer
			PresentationParameters pp = GraphicsDevice.PresentationParameters;
			int width = pp.BackBufferWidth;
			int height = pp.BackBufferHeight;
			SurfaceFormat format = pp.BackBufferFormat;
			DepthFormat depthFormat = pp.DepthStencilFormat;

			// create textures for reading back the backbuffer contents
			sceneMap = new RenderTarget2D(GraphicsDevice, width, height, false, format, depthFormat);

			// Build Displacement texture
			Curve waveform = new Curve();

			waveform.Keys.Add(new CurveKey(0.00f, 0.50f, 0, 0));
			waveform.Keys.Add(new CurveKey(0.05f, 1.00f, 0, 0));
			waveform.Keys.Add(new CurveKey(0.15f, 0.10f, 0, 0));
			waveform.Keys.Add(new CurveKey(0.25f, 0.80f, 0, 0));
			waveform.Keys.Add(new CurveKey(0.35f, 0.30f, 0, 0));
			waveform.Keys.Add(new CurveKey(0.45f, 0.60f, 0, 0));
			waveform.Keys.Add(new CurveKey(0.55f, 0.40f, 0, 0));
			waveform.Keys.Add(new CurveKey(0.65f, 0.55f, 0, 0));
			waveform.Keys.Add(new CurveKey(0.75f, 0.46f, 0, 0));
			waveform.Keys.Add(new CurveKey(0.85f, 0.52f, 0, 0));
			waveform.Keys.Add(new CurveKey(0.99f, 0.50f, 0, 0));

			gradTexture = new Texture2D(GraphicsDevice, 256, 1, false, SurfaceFormat.Color);

			Color[] datas = new Color[256];

			for (var i = 0; i < gradTexture.Width; i++)
			{
				var x = 1.0f / gradTexture.Width * i;
				var a = waveform.Evaluate(x);
				datas[i] = new Color(a, a, a, a);
			}

			gradTexture.SetData<Color>(datas);
		}

		/// <summary>
		/// UnloadContent will be called once per game and is the place to unload
		/// game-specific content.
		/// </summary>
		protected override void UnloadContent()
		{
			// TODO: Unload any non ContentManager content here
			spriteBatch = null;
			distortEffect = null;
			gradTexture = null;

			if (sceneMap != null)
			{
				sceneMap.Dispose();
				sceneMap = null;
			}
		}

		/// <summary>
		/// Allows the game to run logic such as updating the world,
		/// checking for collisions, gathering input, and playing audio.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Update(GameTime gameTime)
		{
			var ms = Mouse.GetState();

			spritePos = ms.Position.ToVector2();

			if (dropInterval > 0)
			{
				timer += (float)gameTime.ElapsedGameTime.TotalSeconds;

				while (timer > dropInterval)
				{
					droplet.Reset();
					timer -= dropInterval;
				}
			}

			droplet.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

			base.Update(gameTime);
		}

		/// <summary>
		/// This is called when the game should draw itself.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Draw(GameTime gameTime)
		{
			var gd1 = GraphicsDevice;

			Viewport viewport = GraphicsDevice.Viewport;

			float aspect = viewport.AspectRatio;

			Matrix projection;
			Matrix.CreateOrthographicOffCenter(0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, 0, 0, -1, out projection);
			Matrix _matrix = Matrix.Identity;//matrix use in spriteBatch.Draw

			Matrix.Multiply(ref _matrix, ref projection, out projection);

			this.distortEffect.Parameters["MatrixTransform"].SetValue(projection);
			this.distortEffect.Parameters["GradTexture"].SetValue(this.gradTexture);
			this.distortEffect.Parameters["_Reflection"].SetValue(reflectionColor.ToVector4());
			this.distortEffect.Parameters["_Params1"].SetValue(new Vector4(aspect, 1, 1 / waveSpeed, 0));    // [ aspect, 1, scale, 0 ]
			this.distortEffect.Parameters["_Params2"].SetValue(new Vector4(1, 1 / aspect, refractionStrength, reflectionStrength));    // [ 1, 1/aspect, refraction, reflection ]
			this.distortEffect.Parameters["_Drop1"].SetValue(droplet.MakeShaderParameter(aspect));

			//First render the background image to the sceneMap rendertarget
			GraphicsDevice.SetRenderTarget(sceneMap);
			GraphicsDevice.Clear(Color.CornflowerBlue);
			DrawFullscreenQuad(backgroundTexture, viewport.Width, viewport.Height, null);

			//Render the water ripple on top of the sceneMap rendertarget
			spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, null, DepthStencilState.None, null, distortEffect);

			Vector2 scaleFactor = new Vector2(0.5f, 0.5f);
			Vector2 origin = new Vector2(sceneMap.Width / 2, sceneMap.Height / 2);
			spriteBatch.Draw(sceneMap, spritePos, null, Color.White, 0f, origin, scaleFactor, SpriteEffects.None, 0);

			spriteBatch.End();

			//finally, darw the completed scenemap rendertarget to the screen
			GraphicsDevice.SetRenderTarget(null);
			DrawFullscreenQuad(sceneMap, viewport.Width, viewport.Height, null);

			base.Draw(gameTime);
		}

		/// <summary>
		/// Helper for drawing a texture into the current rendertarget,
		/// using a custom shader to apply postprocessing effects.
		/// </summary>
		void DrawFullscreenQuad(Texture2D texture, int width, int height, Effect effect)
		{
			spriteBatch.Begin(0, BlendState.Opaque, null, null, null, effect);
			spriteBatch.Draw(texture, new Rectangle(0, 0, width, height), Color.White);
			spriteBatch.End();
		}
	}
}
