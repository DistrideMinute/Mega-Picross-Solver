using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Diagnostics;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace MegaPicrossSolver
{
    public class Game1 : Game
    {
        #region System vars
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        RenderTarget2D display;

        Thread solver;

        Stopwatch timer;
        #endregion

        #region Inputs
        GamePadState[] nGState;
        GamePadState[] oGState;
        KeyboardState nKState;
        KeyboardState oKState;
        MouseState oMState;
        MouseState nMState;
        #endregion

        #region Sprites
        Texture2D pixel;
        AdvTexture tileSet;
        AdvTexture text;
        AdvTexture hMega;
        AdvTexture vMega;
        #endregion

        #region PlayData
        State gameState;
        State? newState;
        int[,] diagram;
        Hints[] hHints;
        Hints[] vHints;
        int width;
        int height;
        bool solving;
        List<int[,]>[] megaWorks;
        int currentMega;

        int targetH;
        int targetV;
        int solvingVal;
        int semiValid;
        #endregion

        #region Settings
        bool debugMode;
        bool fullScreen;
        int screenStretch;
        bool perfectStretch;
        #endregion

        #region Constants
        const int screenWid = 16 * (20 + 10);
        const int screenHei = 16 * (20 + 10);
        #endregion


        #region Default methods
        #region Init
        public Game1()
        {
            debugMode = false;
            fullScreen = false;
            screenStretch = 2;
            perfectStretch = true;


            graphics = new GraphicsDeviceManager(this);
            UpdateResolution();
            Content.RootDirectory = "Content";
            EnterState(State.SizeSelection);

            timer = new Stopwatch();
        }
        private void UpdateResolution()
        {
            if (!fullScreen)
            {
                graphics.PreferredBackBufferWidth = screenWid * screenStretch;
                graphics.PreferredBackBufferHeight = screenHei * screenStretch;
                graphics.IsFullScreen = fullScreen;
            }
            else
            {
                graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
                graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
                graphics.IsFullScreen = fullScreen;
            }
            graphics.ApplyChanges();
        }

        protected override void Initialize()
        {
            SetupInputs();
            display = new RenderTarget2D(GraphicsDevice, screenWid, screenHei, false, SurfaceFormat.Color, DepthFormat.None, 5, RenderTargetUsage.DiscardContents);

            timer.Start();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            pixel = Content.Load<Texture2D>("pixel");
            tileSet = new AdvTexture(14, 14, Content.Load<Texture2D>("tileSet"));
            text = new AdvTexture(6, 7, Content.Load<Texture2D>("LargeFont"));
            hMega = new AdvTexture(14, 30, Content.Load<Texture2D>("hMega"));
            vMega = new AdvTexture(30, 14, Content.Load<Texture2D>("vMega"));
            // TODO: use this.Content to load your game content here
        }

        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }
        #endregion
        protected override void Update(GameTime gameTime)
        {
            UpdateInputs();
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
            if (IsActive)
            {
                if (Press(Keys.O))
                {
                    debugMode = !debugMode;
                }
                if (Press(Keys.F))
                {
                    fullScreen = !fullScreen;
                    UpdateResolution();
                }

                newState = null;
                switch (gameState)
                {
                    case State.SizeSelection:
                        UpdateSizeSelection();
                        break;
                    case State.HintSelection:
                        UpdateHintSelection();
                        break;
                    case State.Solving:
                        UpdateSolving();
                        break;
                }
                if (newState != null)
                {
                    EnterState((State)newState);
                }
            }

            // TODO: Add your update logic here

            base.Update(gameTime);
        }
        protected override void Draw(GameTime gameTime)
        {
            #region Start
            timer.Stop();
            double timeCalc = timer.ElapsedMilliseconds;
            timer.Restart();

            GraphicsDevice.SetRenderTarget(display);
            GraphicsDevice.Clear(Color.DarkBlue);
            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp);
            #endregion

            #region Main draw
            switch (gameState)
            {
                case State.SizeSelection:
                    DrawSizeSelection();
                    break;
                case State.HintSelection:
                    DrawHintSelection();
                    break;
                case State.Solving:
                    DrawSolving();
                    break;
            }

            #region Debug
            if (debugMode)
            {
                timeCalc = 1000 / timeCalc;
                timeCalc = Math.Round(timeCalc, 0);
                //DrawString(timeCalc.ToString(), 0, 0);
            }
            #endregion
            #endregion

            #region End
            spriteBatch.End();
            #region Draw to screen
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Black);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp);
            double multiplier = System.Math.Min(Window.ClientBounds.Width / (double)screenWid, Window.ClientBounds.Height / (double)screenHei);
            if (perfectStretch)
            {
                multiplier = System.Math.Floor(multiplier);
            }
            spriteBatch.Draw(display, new Rectangle((int)((Window.ClientBounds.Width - multiplier * screenWid) / 2), (int)((Window.ClientBounds.Height - multiplier * screenHei) / 2), (int)(screenWid * multiplier), (int)(screenHei * multiplier)), Color.White);
            spriteBatch.End();


            #endregion
            base.Draw(gameTime);
            #endregion
        }
        #endregion

        #region SizeSelection methods
        private void UpdateSizeSelection()
        {
            if (Press(Buttons.A) || Press(Keys.Enter))
            {
                newState = State.HintSelection;
            }
            int increaseAmount = 1;
            if (Down(Buttons.B) || Down(Keys.LeftShift))
            {
                increaseAmount = 5;
            }
            if (Press(Buttons.LeftThumbstickUp) || Press(Keys.W))
            {
                width += increaseAmount;
                if (width > 20)
                {
                    width = 20;
                }
            }
            if (Press(Buttons.LeftThumbstickDown) || Press(Keys.S))
            {
                width -= increaseAmount;
                if (width < 1)
                {
                    width = 1;
                }
            }
            if (Press(Buttons.RightThumbstickUp) || Press(Keys.Up))
            {
                height += increaseAmount;
                if (height > 20)
                {
                    height = 20;
                }
            }
            if (Press(Buttons.RightThumbstickDown) || Press(Keys.Down))
            {
                height -= increaseAmount;
                if (height < 1)
                {
                    height = 1;
                }
            }

        }
        private void DrawSizeSelection()
        {
            DrawString(width + " x " + height, screenWid / 2, screenHei / 2, true);
            if (debugMode)
            {
                DrawString("Up/Down increases width (Left stick) or height (Right stick)", 0, 0, false);
                DrawString("A continues. Hold B to make increments of 5.", 0, 10, false);
            }
        }
        #endregion
        #region HintSelection methods
        private int[] HSGetHintArray()
        {
            if (targetH < height)
            {
                //v hints
                return vHints[targetH].HintArr;
            }
            else
            {
                //h hints
                return hHints[targetH - height].HintArr;
            }
        }
        private void HSModHintArray(int[] newHint)
        {
            if (targetH < height)
            {
                //v hints
                vHints[targetH].HintArr = newHint;
            }
            else
            {
                //h hints
                hHints[targetH - height].HintArr = newHint;
            }
        }
        private int HSGetHint()
        {
            if (targetH < height)
            {
                //v hints
                return vHints[targetH].HintArr[targetV];
            }
            else
            {
                //h hints
                return hHints[targetH - height].HintArr[targetV];
            }
        }
        private void HSModHint(int newHint)
        {
            int hint = newHint;
            if (newHint < 0)
            {
                hint = -hint;
            }

            hint = hint % 100;

            if (newHint < 0)
            {
                hint = -hint;
            }
            if (targetH < height)
            {
                //v hints
                vHints[targetH].HintArr[targetV] = hint;
            }
            else
            {
                //h hints
                hHints[targetH - height].HintArr[targetV] = hint;
            }
        }
        private void UpdateHintSelection()
        {
            if (Press(Keys.T))
            {
                SaveHints();
            }
            if (Press(Keys.Y))
            {
                LoadHints();
            }
            if (Down(Keys.LeftShift) && HSGetHints().IsFirstMega ==true)
            {
                if (Press(Keys.Back))
                {
                    int hint = HSGetHint();
                    hint = -hint;
                    hint = hint - (hint % 10);
                    hint = hint / 10;

                    hint = -hint;
                    HSModHint(hint);
                }
                if (Press(Keys.D0))
                {
                    int hint = HSGetHint();
                    hint = -hint;
                    hint = hint * 10;
                    hint = -hint;
                    HSModHint(hint);
                }
                if (Press(Keys.D1))
                {
                    int hint = HSGetHint();
                    hint = -hint;
                    hint = hint * 10 + 1;

                    hint = -hint;
                    HSModHint(hint);
                }
                if (Press(Keys.D2))
                {
                    int hint = HSGetHint();
                    hint = -hint;
                    hint = hint * 10 + 2;

                    hint = -hint;
                    HSModHint(hint);

                }
                if (Press(Keys.D3))
                {

                    int hint = HSGetHint();
                    hint = -hint;
                    hint = hint * 10 + 3;

                    hint = -hint;
                    HSModHint(hint);
                }
                if (Press(Keys.D4))
                {

                    int hint = HSGetHint();
                    hint = -hint;
                    hint = hint * 10 + 4;

                    hint = -hint;
                    HSModHint(hint);
                }
                if (Press(Keys.D5))
                {

                    int hint = HSGetHint();
                    hint = -hint;
                    hint = hint * 10 + 5;

                    hint = -hint;
                    HSModHint(hint);
                }
                if (Press(Keys.D6))
                {

                    int hint = HSGetHint();
                    hint = -hint;
                    hint = hint * 10 + 6;

                    hint = -hint;
                    HSModHint(hint);
                }
                if (Press(Keys.D7))
                {

                    int hint = HSGetHint();
                    hint = -hint;
                    hint = hint * 10 + 7;

                    hint = -hint;
                    HSModHint(hint);
                }
                if (Press(Keys.D8))
                {

                    int hint = HSGetHint();
                    hint = -hint;
                    hint = hint * 10 + 8;

                    hint = -hint;
                    HSModHint(hint);
                }
                if (Press(Keys.D9))
                {

                    int hint = HSGetHint();
                    hint = -hint;
                    hint = hint * 10 + 9;

                    hint = -hint;
                    HSModHint(hint);
                }
            }
            else
            {
                if (Press(Keys.Back))
                {
                    int hint = HSGetHint();
                    hint = hint - (hint % 10);
                    hint = hint / 10;

                    HSModHint(hint);
                }
                if (Press(Keys.D0))
                {
                    int hint = HSGetHint();
                    hint = hint * 10;
                    HSModHint(hint);
                }
                if (Press(Keys.D1))
                {
                    int hint = HSGetHint();
                    hint = hint * 10 + 1;

                    HSModHint(hint);
                }
                if (Press(Keys.D2))
                {
                    int hint = HSGetHint();
                    hint = hint * 10 + 2;

                    HSModHint(hint);

                }
                if (Press(Keys.D3))
                {

                    int hint = HSGetHint();
                    hint = hint * 10 + 3;

                    HSModHint(hint);
                }
                if (Press(Keys.D4))
                {

                    int hint = HSGetHint();
                    hint = hint * 10 + 4;

                    HSModHint(hint);
                }
                if (Press(Keys.D5))
                {

                    int hint = HSGetHint();
                    hint = hint * 10 + 5;

                    HSModHint(hint);
                }
                if (Press(Keys.D6))
                {

                    int hint = HSGetHint();
                    hint = hint * 10 + 6;

                    HSModHint(hint);
                }
                if (Press(Keys.D7))
                {

                    int hint = HSGetHint();
                    hint = hint * 10 + 7;

                    HSModHint(hint);
                }
                if (Press(Keys.D8))
                {

                    int hint = HSGetHint();
                    hint = hint * 10 + 8;

                    HSModHint(hint);
                }
                if (Press(Keys.D9))
                {

                    int hint = HSGetHint();
                    hint = hint * 10 + 9;

                    HSModHint(hint);
                }
            }
            if (Press(Keys.Enter))
            {
                int[] hintArray = HSGetHintArray();
                if (hintArray.Length < 10)
                {
                    int[] temp = hintArray;
                    hintArray = new int[hintArray.Length + 1];
                    for (int i = 0; i < temp.Length; i++)
                    {
                        if (i <= targetV)
                        {
                            hintArray[i] = temp[i];
                        }
                        else
                        {
                            hintArray[i + 1] = temp[i];
                        }
                    }
                    targetV++;
                    HSModHintArray(hintArray);
                }

            }
            if (Press(Keys.RightShift))
            {
                int[] hintArray = HSGetHintArray();
                if (hintArray.Length > 1)
                {
                    int[] temp = hintArray;
                    hintArray = new int[hintArray.Length - 1];
                    for (int i = 0; i < hintArray.Length; i++)
                    {
                        if (i < targetV)
                        {
                            hintArray[i] = temp[i];
                        }
                        else
                        {
                            hintArray[i] = temp[i + 1];
                        }
                    }
                    if (targetV > 0)
                    {
                        targetV--;
                    }
                    HSModHintArray(hintArray);
                }

            }
            if (Press(Keys.Left))
            {
                targetH--;
                targetV = 0;
                if (targetH == -1)
                {
                    targetH = hHints.Length + vHints.Length - 1;
                }
                if (HSGetHints().IsFirstMega != null)
                {
                    targetV = HSGetHintArray().Length - 1;
                }
            }
            if (Press(Keys.Right))
            {
                targetH++;
                targetV = 0;
                if (targetH == hHints.Length + vHints.Length)
                {
                    targetH = 0;
                }
                if (HSGetHints().IsFirstMega != null)
                {
                    targetV = HSGetHintArray().Length - 1;
                }
            }
            if (Press(Keys.Up))
            {
                int[] hintArray = HSGetHintArray();
                targetV = (targetV - 1 + hintArray.Length) % hintArray.Length;
            }
            if (Press(Keys.Down))
            {
                int[] hintArray = HSGetHintArray();
                targetV = (targetV + 1) % hintArray.Length;
            }
            if (Press(Buttons.A) || Press(Keys.Space))
            {
                newState = State.Solving;
            }
            if (Press(Buttons.B))
            {
                newState = State.SizeSelection;
            }
            if (Press(Keys.LeftAlt))
            {
                //if next hint is true or nonexistent, do nothing
                bool doNothing = false;
                try
                {
                    doNothing = HSGetHints(1).IsFirstMega == true || HSGetHints(0).IsFirstMega == false;
                }
                catch
                {
                    doNothing = true;
                }

                if (!doNothing)
                {
                    //if next hint is false and this is true, make both a new Hints and set targetV to 0
                    if (HSGetHints().IsFirstMega == true || HSGetHints(1).IsFirstMega == false)
                    {
                        HSSetHints(new Hints());
                        HSSetHints(new Hints(), 1);
                        targetV = 0;
                    }
                    //otherwise, make Hints[i].IsFirstMega=true Hints[i+1].IsFirstMega=false, make Hints[i] and Hints[i+1] .HintArr into new int[width or height]
                    else
                    {
                        Hints hintsA = HSGetHints();
                        Hints hintsB = HSGetHints(1);

                        hintsA.IsFirstMega = true;
                        hintsB.IsFirstMega = false;
                        if (targetH < height)
                        {
                            hintsA.HintArr = new int[10];
                            hintsB.HintArr = new int[10];
                        }
                        else
                        {
                            hintsA.HintArr = new int[10];
                            hintsB.HintArr = new int[10];
                        }
                        HSSetHints(hintsA);
                        HSSetHints(hintsB, 1);
                        targetV = 9;
                    }
                }
            }
        }
        private void DrawHintSelection()
        {
            DrawGrid();
            //draw selected in yellow
            int[] hints = HSGetHintArray();
            if (targetH < height)
            {
                //vHint
                switch (vHints[targetH].IsFirstMega)
                {
                    case null:
                        DrawString(HSGetHint().ToString(), (10 - (hints.Length - targetV)) * 16, 160 + targetH * 16, Color.Yellow);
                        break;
                    case true:
                        if (HSGetHint() < 0)
                        {
                            DrawSprite(hMega, -HSGetHint(), (10 - (hints.Length - targetV)) * 16, 160 + targetH * 16, Color.Yellow);
                        }
                        else if (HSGetHint() > 0)
                        {
                            DrawString(HSGetHint().ToString(), (10 - (hints.Length - targetV)) * 16, 160 + targetH * 16, Color.Yellow);
                        }
                        if (HSGetHint() == 0)
                        {
                            DrawString("-", (10 - (hints.Length - targetV)) * 16, 160 + targetH * 16, Color.Yellow);
                        }
                        break;
                    case false:
                        if (HSGetHint() > 0)
                        {
                            DrawString(HSGetHint().ToString(), (10 - (hints.Length - targetV)) * 16, 160 + targetH * 16, Color.Yellow);
                        }
                        if (HSGetHint() == 0)
                        {
                            DrawString("-", (10 - (hints.Length - targetV)) * 16, 160 + targetH * 16, Color.Yellow);
                        }
                        break;
                }
            }
            else
            {
                //hHint
                switch (hHints[(targetH - height)].IsFirstMega)
                {
                    case null:
                        DrawString(HSGetHint().ToString(), 160 + (targetH - height) * 16, (10 - (hints.Length - targetV)) * 16, Color.Yellow);
                        break;
                    case true:
                        if (HSGetHint() < 0)
                        {
                            DrawSprite(vMega, -HSGetHint(), 160 + (targetH - height) * 16, (10 - (hints.Length - targetV)) * 16, Color.Yellow);
                        }
                        else if (HSGetHint() > 0)
                        {
                            DrawString(HSGetHint().ToString(), 160 + (targetH - height) * 16, (10 - (hints.Length - targetV)) * 16, Color.Yellow);
                        }
                        if (HSGetHint() == 0)
                        {
                            DrawString("-", 160 + (targetH - height) * 16, (10 - (hints.Length - targetV)) * 16, Color.Yellow);
                        }
                        break;
                    case false:
                        if (HSGetHint() > 0)
                        {
                            DrawString(HSGetHint().ToString(), 160 + (targetH - height) * 16, (10 - (hints.Length - targetV)) * 16, Color.Yellow);
                        }
                        if (HSGetHint() == 0)
                        {
                            DrawString("-", 160 + (targetH - height) * 16, (10 - (hints.Length - targetV)) * 16, Color.Yellow);
                        }
                        break;
                }
            }

            if (debugMode)
            {
                DrawString("Press numbers and backspace to enter hint", 0, 0);
                DrawString("Press enter to add another hint", 0, 10);
                DrawString("Press left and right to change row/column", 0, 20);
                DrawString("Press up and down to navigate hints", 0, 30);
                DrawString("Press A to continue and B to go back", 0, 40);
                DrawString("Right shift to remove a hint", 0, 50);
                DrawString("Left alt to make a pair into mega", 0, 60);
                DrawString("Left shift and number for mega hint", 0, 70);
            }
        }
        private Hints HSGetHints(int offset = 0)
        {
            if (targetH < height)
            {
                return vHints[targetH + offset];
            }
            else
            {
                return hHints[targetH + offset - height];
            }
        }
        private void HSSetHints(Hints input, int offset = 0)
        {
            if (targetH < height)
            {
                vHints[targetH + offset] = input;
            }
            else
            {
                hHints[targetH + offset - height] = input;
            }
        }
        #endregion
        #region Solving methods
        private void UpdateSolving()
        {
            if (!solving)
            {
                if (Press(Keys.Up) || Press(Buttons.LeftThumbstickUp))
                {
                    targetV = (targetV - 1 + height) % height;
                }
                if (Press(Keys.Right) || Press(Buttons.LeftThumbstickRight))
                {
                    targetH = (targetH + 1) % width;
                }
                if (Press(Keys.Left) || Press(Buttons.LeftThumbstickLeft))
                {
                    targetH = (targetH - 1 + width) % width;
                }
                if (Press(Keys.Down) || Press(Buttons.LeftThumbstickDown))
                {
                    targetV = (targetV + 1) % height;
                }
                if (Press(Buttons.A) || Press(Keys.A))
                {
                    if (diagram[targetH, targetV] == 1)
                    {
                        diagram[targetH, targetV] = 0;
                    }
                    else
                    {
                        diagram[targetH, targetV] = 1;
                    }
                }
                if (Press(Buttons.B) || Press(Keys.B))
                {
                    if (diagram[targetH, targetV] == 2)
                    {
                        diagram[targetH, targetV] = 0;
                    }
                    else
                    {
                        diagram[targetH, targetV] = 2;
                    }
                }
                if (Press(Keys.Enter) && Down(Keys.LeftAlt))
                {
                    newState = State.SizeSelection;
                }
                if (Press(Keys.Back) && Down(Keys.LeftAlt))
                {
                    newState = State.HintSelection;
                }
            }
            if (Press(Keys.T))
            {
                SaveHints();
            }
            if (Press(Keys.Y))
            {
                LoadHints();
            }

            if (Press(Buttons.X) || Press(Keys.X))
            {
                solving = !solving;
                if (solving)
                {
                    solver = new Thread(new ThreadStart(Solve));
                    solver.Start();
                }
            }
        }
        private void DrawSolving()
        {
            DrawGrid();
            DrawSprite(tileSet, diagram[targetH, targetV], 160 + targetH * 16, 160 + targetV * 16, Color.Gray);
            DrawString(solvingVal.ToString(), 10, 10);

            if (solving)
            {
                DrawString("Solving...", 0, 0);
            }
            if (debugMode)
            {
                DrawString("Left stick or arrows to move", 0, 0);
                DrawString("A to fill, B to empty.", 0, 10);
                DrawString("X to begin autosolve.", 0, 20);
                DrawString("AltEnter to make new. AltBackspace to return to hint.", 0, 30);
            }
        }

        #region Solve
        private void Solve()
        {
            bool doMega = false;
            while (solving)
            {
                currentMega = 0;
                int[,] oldDiagram = (int[,])diagram.Clone();

                for (int h = 0; h < width; h++)
                {
                    solvingVal = 1+h;
                    if (hHints[h].IsFirstMega==null)
                    {
                        int[] row = new int[height];
                        for (int i = 0; i < row.Length; i++)
                        {
                            row[i] = diagram[h, i];
                        }
                        
                        row = SolveRow(row, hHints[h].HintArr);
                        for (int i = 0; i < row.Length; i++)
                        {
                            diagram[h, i] = row[i];
                        }
                    }
                    else if (doMega)
                    {
                        int[,] rows = new int[height, 2];
                        for (int i =0; i < rows.GetLength(0); i++)
                        {
                            rows[i, 0] = diagram[h, i];
                            rows[i, 1] = diagram[h + 1, i];
                        }
                        rows = SolveMegaRow(rows);

                        for (int i = 0; i < rows.GetLength(0); i++)
                        {
                            diagram[h, i] = rows[i, 0];
                            diagram[h + 1, i]= rows[i, 1];
                        }
                        h++;
                        currentMega++;
                    }                    
                }
                for (int v = 0; v < height; v++)
                {
                    solvingVal = (1+v)*10;
                    if (vHints[v].IsFirstMega == null)
                    {
                        int[] row = new int[width];
                        for (int i = 0; i < row.Length; i++)
                        {
                            row[i] = diagram[i, v];
                        }

                        row = SolveRow(row, vHints[v].HintArr);
                        for (int i = 0; i < row.Length; i++)
                        {
                            diagram[i, v] = row[i];
                        }
                    }
                    else if(doMega)
                    {
                        int[,] rows = new int[width, 2];
                        for (int i = 0; i < rows.GetLength(0); i++)
                        {
                            rows[i, 0] = diagram[i, v];
                            rows[i, 1] = diagram[i,v + 1];
                        }
                        rows = SolveMegaRow(rows);

                        for (int i = 0; i < rows.GetLength(0); i++)
                        {
                            diagram[i, v] = rows[i, 0];
                            diagram[i, v + 1] = rows[i, 1];
                        }
                        v++;
                        currentMega++;
                    }
                }
                solvingVal = -1;

                bool changed = false;
                for (int h = 0; h < width; h++)
                {
                    for (int v = 0; v < height; v++)
                    {
                        if (oldDiagram[h, v] != diagram[h, v])
                        {
                            changed = true;
                        }
                    }
                }
                if (!changed)
                {
                    if (doMega)
                    {
                        solving = false;
                    }
                    else
                    {
                        doMega = true;
                    }
                }
                else
                {
                    doMega = false;
                }
                if (debugMode)
                {
                    solving = false;
                }
            }
        }
        private int[] SolveRow(int[] input, int[] hints)
        {
            int[] row = new int[input.Length];
            Array.Copy(input, row, input.Length);

            //check if anywhere must be something

            int[] offsets = new int[hints.Length];
            int[] definites = new int[row.Length]; //0 for unassigned, -1 for not definite, 1 or 2 for definite
            bool done = false;
            int last = hints.Length - 1;
            for (int i = 1; i < offsets.Length; i++)
            {
                offsets[i] = offsets[i - 1] + hints[i - 1] + 1;
            }
            offsets[last]--;
            while (!done)
            {
                //O is filled (1), X is not (2).
                offsets[last]++;
                if (offsets[last] + hints[last] <= row.Length)
                {
                    //test if this uses all already in place
                    int[] test = new int[row.Length];
                    for (int i = 0; i < offsets.Length; i++)
                    {
                        for (int x = 0; x < hints[i]; x++)
                        {
                            test[x + offsets[i]] = 1;
                        }
                    }
                    for (int i = 0; i < test.Length; i++)
                    {
                        if (test[i] == 0)
                        {
                            test[i] = 2;
                        }
                    }
                    bool works = true;
                    for (int i = 0; i < test.Length; i++)
                    {
                        if (row[i] != 0)
                        {
                            if (row[i] != test[i])
                            {
                                works = false;
                                i = test.Length;
                            }
                        }
                    }
                    if (works)
                    {
                        //if so, do definites
                        for (int i = 0; i < test.Length; i++)
                        {
                            switch (definites[i])
                            {
                                case 0:
                                    definites[i] = test[i];
                                    break;
                                case 1:
                                case 2:
                                    if (definites[i] != test[i])
                                    {
                                        definites[i] = -1;
                                    }
                                    break;
                            }
                        }
                    }
                }
                else
                {
                    bool foundValid = false;
                    int toChange = last;
                    while (!foundValid)
                    {
                        toChange--;
                        if (toChange >= 0)
                        {
                            offsets[toChange]++;
                            for (int i = toChange + 1; i < offsets.Length; i++)
                            {
                                offsets[i] = offsets[i - 1] + hints[i - 1] + 1;
                            }
                            if (offsets[last] + hints[last] <= row.Length)
                            {
                                offsets[last]--;
                                foundValid = true;
                            }
                        }
                        else
                        {
                            done = true;
                            foundValid = true;
                        }
                    }
                }
            }
            for (int i = 0; i < definites.Length; i++)
            {
                if (definites[i] == -1)
                {
                    definites[i] = row[i];
                }
            }

            return definites;
        }
        private void GenerateMegaWorks()
        {
            //for each IsFirstMega==true find every possible group
            int megaGroups = 0;
            for (int i = 0; i < width; i++)
            {
                if (hHints[i].IsFirstMega==true)
                {
                    megaGroups++;
                }
            }
            for (int i = 0; i < height; i++)
            {
                if (vHints[i].IsFirstMega == true)
                {
                    megaGroups++;
                }
            }

            megaWorks = new List<int[,]>[megaGroups];
            megaGroups = 0;
            for (int i = 0; i < width; i++)
            {
                if (hHints[i].IsFirstMega == true)
                {
                    megaWorks[megaGroups] = GetMegaGroup(height, hHints[i].HintArr, hHints[i + 1].HintArr);
                    megaGroups++;
                }
            }
            for (int i = 0; i < height; i++)
            {
                if (vHints[i].IsFirstMega == true)
                {
                    megaWorks[megaGroups] = GetMegaGroup(width, vHints[i].HintArr, vHints[i + 1].HintArr);
                    megaGroups++;
                }
            }
        }
        private List<int[,]> GetMegaGroup(int length, int[] hintsA, int[] hintsB)
        {
            semiValid = 0;
            for (int i = 0; i < hintsA.Length; i++)
            {
                if (hintsA[i] <0)
                {
                    hintsB[i] = hintsA[i];
                }
            }

            //make every possible combination, apply the hints to them, and if it is valid then keep it
            List<int[,]> result = new List<int[,]>();
            int[,] trial = new int[length, 2];
            for (int i = 0; i < length; i++)
            {
                trial[i, 0] = 2;
                trial[i, 1] = 2;
            }

            int totalO = 0;
            for (int i = 0; i < hintsA.Length; i++)
            {
                if (hintsA[i] < 0)
                {
                    totalO -= hintsA[i];
                }
                else
                {
                    totalO += hintsA[i] + hintsB[i];
                }
            }
            int totalA = 0;

            bool firstTrial = true;
            bool foundAll = false;
            while (!foundAll)
            {
                if (firstTrial)
                {
                    firstTrial = false;
                }
                else
                {
                    int targetA = length - 1;
                    int targetB = 1;
                    bool found = false;
                    
                    while (!found)
                    {
                        if (trial[targetA,targetB] == 2)
                        {
                            trial[targetA, targetB] = 1;
                            totalA++;
                            found = true;
                        }
                        else
                        {
                            trial[targetA, targetB] = 2;
                            totalA--;
                            targetA--;
                            if (targetA < 0)
                            {
                                targetB--;
                                targetA = length - 1;
                                if (targetB < 0)
                                {
                                    foundAll = true;
                                    found = true;
                                }
                            }
                            if (targetA < 2)
                            {
                                int asfk = 0;
                            }
                        }
                        if (found)
                        {
                            if (totalA != totalO)
                            {
                                found = foundAll;
                                targetA = length - 1;
                                targetB = 1;
                            }
                        }
                    }
                }

                if (!foundAll)
                {
                    if (ValidMega(trial, hintsA, hintsB))
                    {
                        result.Add((int[,])trial.Clone());
                    }
                }
            }

            for (int i = 0; i < result.Count;i++)
            {
                if (result[i] ==null)
                {
                    int niodg = 32;
                }
            }
            return result;
        }
        private int[,] SolveMegaRow(int[,] input)
        {
            //compare each in the appropriate List<int[,]> to see if it matches input, and if not remove it. Then take the common elements of the remaining and fill in the gaps of input with them


            List<int[,]> possible = megaWorks[currentMega];
            for (int i = 0; i < possible.Count; i++)
            {
                if (possible[i] == null)
                {
                    int niodg = 32;
                }
            }

            int[,] definite = new int[input.GetLength(0), 2]; //-1 is conflicted, 1 or 2 is definite
            for (int i = 0; i < possible.Count; i++)
            {
                for (int x = 0; x < input.GetLength(0); x++)
                {
                    for (int y = 0; y < 2; y++)
                    {
                        if (input[x, y] > 0)
                        {
                            if (possible[i][x, y] != input[x, y])
                            {
                                possible.RemoveAt(i);
                                i--;
                                x = input.GetLength(0);
                                y = 2;
                            }
                        }
                    }
                }
            }
            for (int i = 0; i < possible.Count; i++)
            {
                for (int x = 0; x < input.GetLength(0); x++)
                {
                    for (int y = 0; y < 2; y++)
                    {
                        if (input[x, y] == 0)
                        {
                            switch (definite[x,y])
                            {
                                case 0:
                                    definite[x, y] = possible[i][x, y];
                                    break;
                                case 1:
                                case 2:
                                    if (possible[i][x,y] != definite[x,y])
                                    {
                                        definite[x, y] = -1;
                                    }
                                    break;

                            }
                        }
                    }
                }
            }
            megaWorks[currentMega] = possible;

            int[,] output = new int[input.GetLength(0), 2];
            for (int x = 0; x < input.GetLength(0); x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    if (input[x, y] > 0)
                    {
                        output[x, y] = input[x, y];
                    }
                    else
                    {
                        if (definite[x,y] > 0)
                        {
                            output[x, y] = definite[x, y];
                        }
                        else
                        {
                            output[x, y] = 0;
                        }
                    }
                }
            }

            for (int i = 0; i < possible.Count; i++)
            {
                if (possible[i] == null)
                {
                    int niodg = 32;
                }
            }
            return output;
        }
        
        private bool ValidMega(int[,] input, int[] hintsA, int[] hintsB)
        {

            int aTar = 0;
            int bTar = 0;
            int aLoc = 0;
            int bLoc = 0;
            int inLen = input.GetLength(0);
            while (aTar < hintsA.Length || bTar < hintsB.Length)
            {
                if (aTar < hintsA.Length)
                {
                    if (hintsA[aTar] == 0)
                    {
                        aTar++;
                    }
                    else if (hintsA[aTar] > 0)
                    {
                        bool found = false;
                        int amount = 0;
                        while (!found)
                        {
                            if (aLoc >= inLen)
                            {
                                return false;
                            }
                            if (input[aLoc, 0] == 2)
                            {
                                aLoc++;
                            }
                            else
                            {
                                found = true;
                            }
                        }
                        found = false;
                        amount = hintsA[aTar];
                        while (!found)
                        {
                            if (aLoc >= inLen)
                            {
                                return false;
                            }
                            if (input[aLoc, 0] == 1 && input[aLoc, 1] == 2)
                            {
                                amount--;
                                aLoc++;
                            }
                            else
                            {
                                return false;
                            }
                            if (amount == 0)
                            {
                                found = true;
                            }
                        }
                        if (aLoc < inLen)
                        {
                            if (input[aLoc, 0] == 1)
                            {
                                return false;
                            }
                        }                        
                        aTar++;
                    }
                }
                if (bTar < hintsB.Length)
                {
                    if (hintsB[bTar] == 0)
                    {
                        bTar++;
                    }
                    else if (hintsB[bTar] > 0)
                    {
                        bool found = false;
                        int amount = 0;
                        while (!found)
                        {
                            if (bLoc >= inLen)
                            {
                                return false;
                            }
                            if (input[bLoc, 1] == 2)
                            {
                                bLoc++;
                            }
                            else
                            {
                                found = true;
                            }
                        }
                        found = false;
                        amount = hintsB[bTar];
                        while (!found)
                        {
                            if (bLoc >= inLen)
                            {
                                return false;
                            }
                            if (input[bLoc, 1] == 1 && input[bLoc, 0] == 2)
                            {
                                amount--;
                                bLoc++;
                            }
                            else
                            {
                                return false;
                            }
                            if (amount == 0)
                            {
                                found = true;
                            }
                        }
                        if (bLoc < inLen)
                        {
                            if (input[bLoc, 1] == 1)
                            {
                                return false;
                            }
                        } 
                        bTar++;
                    }
                }
                if (aTar == bTar)
                {
                    if ((aTar < hintsA.Length) && (bTar < hintsB.Length))
                    {
                        if (hintsA[aTar] < 0)
                        {
                            //solve mega hint
                            bool containsA = false;
                            bool containsB = false;
                            int remaining = -hintsA[aTar];
                            int strand = -1; //-1 is unassigned (can go to any), 0 is both (can go to any), 1 is A (can go to 0 and 1), 2 is B (can go to 0 and 2)
                                             //catchup lesser of aLoc and bLoc
                            aLoc = Math.Max(aLoc, bLoc);
                            bLoc = Math.Max(aLoc, bLoc);

                            bool done = false;
                            //find anything
                            while (!done)
                            {
                                if (aLoc >= inLen)
                                {
                                    return false;
                                }
                                if (input[aLoc, 0] == 1 || input[aLoc, 1] == 1)
                                {
                                    done = true;
                                }
                                else
                                {
                                    aLoc++;
                                }
                            }

                            done = false;
                            while (!done)
                            {
                                if (aLoc >= inLen)
                                {
                                    return false;
                                }
                                int a = input[aLoc, 0];
                                int b = input[aLoc, 1];
                                if (a == 1)
                                {
                                    containsA = true;
                                }
                                if (b == 1)
                                {
                                    containsB = true;
                                }

                                int newStrand = -1;
                                if (a == 1 && b == 2)
                                {
                                    newStrand = 1;
                                    remaining--;
                                }
                                if (a == 1 && b == 1)
                                {
                                    newStrand = 0;
                                    remaining -= 2;
                                }
                                if (a == 2 && b == 1)
                                {
                                    newStrand = 2;
                                    remaining--;
                                }
                                if (newStrand == -1)
                                {
                                    return false;
                                }

                                switch (strand)
                                {
                                    case -1:
                                    case 0:
                                        strand = newStrand;
                                        break;

                                    case 1:
                                        if (newStrand == 0 || newStrand == 1)
                                        {
                                            strand = newStrand;
                                        }
                                        else
                                        {
                                            return false;
                                        }
                                        break;
                                    case 2:
                                        if (newStrand == 0 || newStrand == 2)
                                        {
                                            strand = newStrand;
                                        }
                                        else
                                        {
                                            return false;
                                        }
                                        break;
                                }
                                aLoc++;

                                if (remaining == 0)
                                {
                                    done = true;
                                }
                                if (remaining < 0)
                                {
                                    return false;
                                }
                            }
                            bLoc = aLoc;

                            //ensure ends are covered in nothing
                            switch (strand)
                            {
                                case 1:
                                    bLoc--;
                                    break;
                                case 2:
                                    aLoc--;
                                    break;
                            }
                            if (aLoc < input.GetLength(0))
                            {
                                if (input[aLoc, 0] != 2)
                                {
                                    return false;
                                }
                            }
                            if (bLoc < input.GetLength(0))
                            {
                                if (input[bLoc, 1] != 2)
                                {
                                    return false;
                                }
                            }

                            aTar++;
                            bTar++;
                            if (!containsA || !containsB)
                            {
                                return false;
                            }
                        }
                    }
                }
            }
            return true;
        }
        #endregion
        #endregion

        #region Util
        #region Save system
        private void SaveHints()
        {
            HintSave savedHints = new HintSave();

            savedHints.vHints = vHints;
            savedHints.hHints = hHints;
            IFormatter formatter = new BinaryFormatter();
            using (Stream stream = new FileStream("gdr.dat", FileMode.Create, FileAccess.Write, FileShare.None))
            {
                formatter.Serialize(stream, savedHints);
                stream.Close();
            }
        }
        private void LoadHints()
        {
            HintSave loadedHints = new HintSave();
            IFormatter formatter = new BinaryFormatter();
            using (Stream stream = new FileStream("gdr.dat", FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                loadedHints = (HintSave)formatter.Deserialize(stream);
                stream.Close();
            }
            vHints = loadedHints.vHints;
            hHints = loadedHints.hHints;
        }
        #endregion
        enum State { SizeSelection, HintSelection, Solving }
        private void EnterState(State state)
        {
            switch (state)
            {
                case State.SizeSelection:
                    width = 10;
                    height = 10;
                    break;
                case State.HintSelection:
                    diagram = new int[width, height];
                    hHints = new Hints[width];
                    for (int i = 0; i < width; i++)
                    {
                        hHints[i] = new Hints();
                    }
                    vHints = new Hints[height];
                    for (int i = 0; i < height; i++)
                    {
                        vHints[i] = new Hints();
                    }
                    targetH = 0;
                    targetV = 0;
                    break;
                case State.Solving:
                    targetH = 0;
                    targetV = 0;
                    solving = false;
                    solvingVal = -1;
                    GenerateMegaWorks();
                    break;
            }
            gameState = state;
        }
        #region Input methods
        //TODO give analog sticks a minimum activation point
        private enum Input { Up, Down, Left, Right, A, X, B, Y, Start }
        private void SetupInputs()
        {
            nGState = new GamePadState[4];
            for (int i = 0; i < 4; i++)
            {
                nGState[i] = GamePad.GetState(i);
            }
            nKState = Keyboard.GetState();
            nMState = Mouse.GetState();
        }
        private void UpdateInputs()
        {
            oKState = nKState;
            oGState = nGState;
            nGState = new GamePadState[4];
            for (int i = 0; i < 4; i++)
            {
                nGState[i] = GamePad.GetState(i);
            }
            nKState = Keyboard.GetState();
            oMState = nMState;
            nMState = Mouse.GetState();
        }
        #region Press
        private bool Press(Buttons button)
        {
            for (int i = 0; i < 4; i++)
            {
                if (nGState[i].IsButtonDown(button) && oGState[i].IsButtonUp(button))
                {
                    return true;
                }
            }
            return false;
        }
        private bool Press(Keys key)
        {
            return (nKState.IsKeyDown(key) && oKState.IsKeyUp(key));
        }
        private bool Press(int button)
        {
            switch (button)
            {
                case 0:
                    return nMState.LeftButton == ButtonState.Pressed && oMState.LeftButton == ButtonState.Released;
                case 1:
                    return nMState.MiddleButton == ButtonState.Pressed && oMState.MiddleButton == ButtonState.Released;
                case 2:
                    return nMState.RightButton == ButtonState.Pressed && oMState.RightButton == ButtonState.Released;
            }
            return false;
        }
        #endregion
        #region Down
        private bool Down(Buttons button, bool old = false)
        {
            if (old)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (oGState[i].IsButtonDown(button))
                    {
                        return true;
                    }
                }
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    if (nGState[i].IsButtonDown(button))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        private bool Down(Keys key, bool old = false)
        {
            if (old)
            {
                return (oKState.IsKeyDown(key));
            }
            else
            {
                return (nKState.IsKeyDown(key));
            }
        }
        private bool Down(int button)
        {
            switch (button)
            {
                case 0:
                    return nMState.LeftButton == ButtonState.Pressed;
                case 1:
                    return nMState.MiddleButton == ButtonState.Pressed;
                case 2:
                    return nMState.RightButton == ButtonState.Pressed;
            }
            return false;
        }
        #endregion
        #region Numbers
        private int MouseX()
        {
            return nMState.X;
        }
        private int MouseY()
        {
            return nMState.Y;
        }
        private int ScrollAmount()
        {
            return nMState.ScrollWheelValue - oMState.ScrollWheelValue;
        }
        #endregion
        #endregion
        
        #region Draw methods
        private void DrawGrid()
        {
            //Draw grid
            for (int h = 0; h < width; h++)
            {
                for (int v = 0; v < height; v++)
                {
                    DrawSprite(tileSet, diagram[h, v], 160 + h * 16, 160 + v * 16);
                }
            }
            //Draw hints
            for (int h = 0; h < width; h++)
            {
                int[] hints = hHints[h].HintArr;
                for (int i = 0; i < hints.Length; i++)
                {
                    switch (hHints[h].IsFirstMega)
                    {
                        case null:
                            DrawString(hints[i].ToString(), 160 + h * 16, (10 - (hints.Length - i)) * 16);
                            break;
                        case true:
                            if (hints[i] < 0)
                            {
                                DrawSprite(vMega, -hints[i], 160 + h * 16, (10 - (hints.Length - i)) * 16);
                            }
                            else if (hints[i] > 0)
                            {
                                DrawString(hints[i].ToString(), 160 + h * 16, (10 - (hints.Length - i)) * 16);
                            }
                            break;
                        case false:
                            if (hints[i] > 0)
                            {
                                DrawString(hints[i].ToString(), 160 + h * 16, (10 - (hints.Length - i)) * 16);
                            }
                            break;
                    }
                }
            }
            for (int v = 0; v < height; v++)
            {
                int[] hints = vHints[v].HintArr;
                for (int i = 0; i < hints.Length; i++)
                {
                    switch(vHints[v].IsFirstMega)
                    {
                        case null:
                            DrawString(hints[i].ToString(), (10 - (hints.Length - i)) * 16, 160 + v * 16);
                            break;
                        case true:
                            if (hints[i] < 0)
                            {
                                DrawSprite(hMega, -hints[i], (10 - (hints.Length - i)) * 16, 160 + v * 16);
                            }
                            else if (hints[i] > 0)
                            {
                                DrawString(hints[i].ToString(), (10 - (hints.Length - i)) * 16, 160 + v * 16);
                            }
                            break;
                        case false:
                            if (hints[i] > 0)
                            {
                                DrawString(hints[i].ToString(), (10 - (hints.Length - i)) * 16, 160 + v * 16);
                            }
                            break;
                    }
                }
            }
        }
        private void DrawColour(Rectangle r, Color colour)
        {
            spriteBatch.Draw(pixel, r, colour);
        }
        private void DrawSprite(AdvTexture texture, int sprite, int x, int y, Color colour)
        {
            spriteBatch.Draw(texture.texture, texture.GetDestRect(x, y), texture.GetSpriteRect(sprite), colour);
        }
        private void DrawSprite(AdvTexture texture, int sprite, int x, int y)
        {
            spriteBatch.Draw(texture.texture, texture.GetDestRect(x, y), texture.GetSpriteRect(sprite), Color.White);
        }
        private void DrawTransparency(Rectangle target, int opacity = 128)
        {
            spriteBatch.Draw(pixel, target, new Color(Color.White, opacity));
        }
        private void DrawTransparency(int x, int y, int width, int height, int opacity = 128)
        {
            DrawTransparency(new Rectangle(x, y, width, height), opacity);
        }
        private void DrawTransparency(int opacity)
        {
            DrawTransparency(0, 0, screenWid, screenHei, opacity);
        }
        private void DrawString(string[] toDraw, int x, int y, Color colour)
        {
            for (int i = 0; i < toDraw.Length; i++)
            {
                DrawString(toDraw[i], x, y + i * (text.spriteHeight + 1));
            }
        }
        private void DrawString(string toDraw, int x, int y, Color colour, bool centre = false)
        {
            if (centre)
            {
                int offset = x - (toDraw.Length * (1 + text.spriteWidth) - 1) / 2;
                for (int i = 0; i < toDraw.Length; i++)
                {
                    DrawSprite(text, toDraw[i], offset + i * (1 + text.spriteWidth), y, colour);
                }
            }
            else
            {
                for (int i = 0; i < toDraw.Length; i++)
                {
                    DrawSprite(text, toDraw[i], x + i * (1 + text.spriteWidth), y, colour);
                }
            }
        }
        private void DrawString(string toDraw, int x, int y, bool centre = false)
        {
            DrawString(toDraw, x, y, Color.White, centre);
        }
        public static int IntAOverB(int a, int b)
        {
            return (a - (a % b)) / b;
        }
        #endregion
        #endregion
    }
    #region Classes
    class AdvTexture
    {
        public Texture2D texture;
        public int spriteWidth;
        public int spriteHeight;
        public int spritesPerRow;
        public int spritesPerCol;
        public int height;
        public int width;
        public AdvTexture(int spriteWidth, int spriteHeight, Texture2D texture)
        {
            this.spriteWidth = spriteWidth;
            this.spriteHeight = spriteHeight;
            this.texture = texture;
            this.height = texture.Height;
            this.width = texture.Width;
            spritesPerRow = Game1.IntAOverB(texture.Width, spriteWidth);
            spritesPerCol = Game1.IntAOverB(texture.Height, spriteHeight);
        }
        public Rectangle GetSpriteRect(int spriteID)
        {
            int x = spriteID % spritesPerRow;
            int y = (spriteID - x) / spritesPerRow;
            return new Rectangle(x * spriteWidth, y * spriteHeight, spriteWidth, spriteHeight);
        }
        public Rectangle GetDestRect(int x, int y)
        {
            return new Rectangle(x, y, spriteWidth, spriteHeight);
        }
    }
    [Serializable]
    public class HintSave
    {
        public Hints[] vHints;
        public Hints[] hHints;
    }
    [Serializable]
    public class Hints
    {
        public bool? IsFirstMega; //null is not mega, true is the left/up mega, false is the right/down mega
        public int[] HintArr;
        public Hints()
        {
            IsFirstMega = null;
            HintArr = new int[1];
        }
    }
    #endregion
}
