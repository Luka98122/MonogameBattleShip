using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices.Marshalling;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MonogameBattleShip
{

    public class Button
    {
        public float x;
        public float y;
        public float w;
        public float h;
        public string text;
        public Texture2D tex;

        public Button(float x, float y, float w, float h, string text, Texture2D tex)
        {
            this.x = x;
            this.y = y;
            this.w = w;
            this.h = h;
            this.text = text;
            this.tex = tex;
        }

        public void Draw(SpriteBatch sp, SpriteFont font, int ww, int wh)
        {
            // Draw button background

            int rx = (int)(ww * this.x);
            int ry = (int)(wh * this.y);
            int rw = (int)(ww * this.w);
            int rh = (int)(wh * this.h);

            sp.Draw(this.tex, new Rectangle(rx, ry, rw, rh), Color.White);

            // Measure text
            Vector2 textSize = font.MeasureString(this.text);

            // Center text
            Vector2 textPos = new Vector2(
                rx + (rw - textSize.X) / 2,
                ry + (rh - textSize.Y) / 2
            );

            // Draw text
            sp.DrawString(font, this.text, textPos, Color.Black);
        }

        public bool Update(int mx, int my, int ww, int wh)
        {
            int rx = (int)(ww * this.x);
            int ry = (int)(wh * this.y);
            int x2 = rx + (int)(ww * this.w);
            int y2 = ry + (int)(wh * this.h);
            if (mx > rx && mx < x2 && my > ry && my < y2)
            {
                return true;
            }
            return false;
        }
    }

    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private SpriteFont font;


        public Button b_vsAi;
        public Button b_vsPlayer;
        public Button b_exit;
        public Button b_EasyAi;
        public Button b_NormalAi;
        public Button b_HardAi;
        public Button b_AiMainMenu;

        public int diff = -1;

        public Texture2D _whiteTexture;
        public int screen = 0;
        public Button myBut;
        public List<Button> mybuts;
        public int ww = 1700;
        public int wh = 956;
        public int i_board_edge = 640;


        public int[,] t_player;
        public int[,] t_ai;

        public int[,] t_player2;
        public int[,] t_ai2;
        public int t_x = 140;//(int)(ww / 2) - (int)(i_board_edge * 0.7);
        public int t_y = 158;//(int)(wh * 0.05);
        public KeyboardState ks;// = Keyboard.GetState()
        public int nmx;
        public int nmy;
        // Ship placing
        private int ship_type;
        //public int[] ship_ids = new int[7] {0,0, 12, 13, 23, 14, 15 };
        // 0 1 2 3 4 5
        private int[] ship_counts = new int[] { 0, 0, 1, 2, 1, 1 };
        // number_left*10+length_of_ship
        private Vector2 placePos = new Vector2();
        private MouseState currentMouseState;
        private bool wasHolding = false;
        private bool R_wasHolding = false;
        private int rotation = 0;
        public List<Texture2D> waterImgs = new List<Texture2D> { };
        // Guesses
        public List<Vector2> guesses = new List<Vector2> { };
        public List<Vector2> guesses_ai = new List<Vector2> { };
        public int waterFrame = 0;
        public int animationSlow = 50;
        public int animationSpeed = 3;

        Dictionary<string, Texture2D> brodovi = new Dictionary<string, Texture2D> { };
        Dictionary<int, List<int>> stats = new Dictionary<int, List<int>> { };
        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _graphics.PreferredBackBufferWidth = ww;
            _graphics.PreferredBackBufferHeight = wh;
            _graphics.ApplyChanges();
            Window.AllowUserResizing = false;

        }


        protected override void Initialize()
        {

            _whiteTexture = new Texture2D(GraphicsDevice, 1, 1);
            _whiteTexture.SetData(new[] { Color.White });
            b_vsAi = new Button(0.4f, 0.1f, 0.2f, 0.1f, "vs AI", _whiteTexture);
            b_vsPlayer = new Button(0.4f, 0.3f, 0.2f, 0.1f, "vs Player", _whiteTexture);
            b_exit = new Button(0.4f, 0.5f, 0.2f, 0.15f, "Exit", _whiteTexture);

            b_EasyAi = new Button(0.4f, 0.1f, 0.2f, 0.1f, "Ez", _whiteTexture);
            b_NormalAi = new Button(0.4f, 0.3f, 0.2f, 0.1f, "Normal", _whiteTexture);
            b_HardAi = new Button(0.4f, 0.5f, 0.2f, 0.1f, "Hard", _whiteTexture);
            b_AiMainMenu = new Button(0.35f, 0.7f, 0.3f, 0.1f, "Main Menu", _whiteTexture);

            t_ai = new int[10, 10];
            t_player = new int[10, 10];

            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 10; x++)
                {
                    if ((x + y) % 2 == 0)
                    {
                        t_player[y, x] = 0;
                    }
                }
            }

            int[,] test = AIPlace();
            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 10; x++)
                {
                    Console.WriteLine(test[y, x]);
                }
            }

            t_ai2 = new int[10, 10];
            t_player2 = new int[10, 10];
            Random rn = new Random();
            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 10; x++)
                {
                    t_ai2[y, x] = rn.Next(1, 39);
                    t_player2[y, x] = rn.Next(1, 39);

                }
            }



            //t_ai2 = new int[10, 10];
            //t_player2 = new int[10, 10];
            t_ai = test;

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            mybuts = new List<Button> { };
            font = Content.Load<SpriteFont>("DefaultFont");

            for (int i = 0; i < 40; i++)
            {
                string imgName = i.ToString("D4");
                waterImgs.Add(Content.Load<Texture2D>(imgName));
            }

            brodovi["ship12"] = Content.Load<Texture2D>("ship12");
            brodovi["ship13"] = Content.Load<Texture2D>("ship13");
            brodovi["ship14"] = Content.Load<Texture2D>("ship14");
            brodovi["ship15"] = Content.Load<Texture2D>("ship15");
            brodovi["ship23"] = Content.Load<Texture2D>("ship23");

        }

        private int[,] AIPlace()
        {
            int[,] t_ai_new = new int[10, 10];
            int[] ships = new int[] { 0, 0, 1, 2, 1, 1 };
            int num_ships_left = 5;
            Random rand = new Random();
            while (true)
            {
                if (num_ships_left == 0)
                {
                    break;
                }
                int type_ship = rand.Next(2, 6);
                if (ships[type_ship] == 0)
                {
                    continue;
                }
                int type_shit_r = rand.Next(0, 2);
                int type_shit_x = rand.Next(0, 10);
                int type_shit_y = rand.Next(0, 10);

                if (type_shit_r == 0)
                {
                    bool nasliSmoBrod = false;
                    if (type_shit_x + type_ship > 10)
                    {
                        continue;
                    }
                    for (int i = 0; i < type_ship; i++)
                    {
                        // Dodaj da proveri da li je x+i<10
                        if (t_ai_new[type_shit_y, type_shit_x + i] != 0)
                        {
                            nasliSmoBrod = true;
                            break;
                        }
                    }
                    if (!nasliSmoBrod)
                    {
                        if (type_shit_x + type_ship > 10)
                        {
                            continue;
                        }
                        for (int i = 0; i < type_ship; i++)
                        {
                            t_ai_new[type_shit_y, type_shit_x + i] = ships[type_ship] * 10 + type_ship;
                        }
                        ships[type_ship] -= 1;
                        num_ships_left -= 1;
                    }
                }
                if (type_shit_r == 1)
                {
                    bool nasliSmoBrod = false;
                    if (type_shit_y + type_ship > 10)
                    {
                        continue;
                    }
                    for (int i = 0; i < type_ship; i++)
                    {

                        if (t_ai_new[type_shit_y + i, type_shit_x] != 0)
                        {
                            nasliSmoBrod = true;
                            break;
                        }
                    }
                    if (!nasliSmoBrod)
                    {
                        if (type_shit_y + type_ship > 10)
                        {
                            continue;
                        }
                        for (int i = 0; i < type_ship; i++)
                        {
                            t_ai_new[type_shit_y + i, type_shit_x] = ships[type_ship] * 10 + type_ship;
                        }
                        ships[type_ship] -= 1;
                        num_ships_left -= 1;
                    }
                }
            }

            return t_ai_new;
        }

        protected override void Update(GameTime gameTime)
        {
            ks = Keyboard.GetState();
            currentMouseState = Mouse.GetState();
            nmx = (currentMouseState.X - t_x) / (i_board_edge / 10);
            nmy = (currentMouseState.Y - t_y) / (i_board_edge / 10);
            animationSlow -= 1;
            if (animationSlow == 0)
            {
                animationSlow = animationSpeed;
                for (int y = 0; y < 10; y++)
                {
                    for (int x = 0; x < 10; x++)
                    {


                        t_ai2[y, x] += 1;
                        t_ai2[y, x] %= 40;
                        t_player2[y, x] += 1;
                        t_player2[y, x] %= 40;

                    }
                }
            }
            if (ks.IsKeyDown(Keys.D2))
            {
                ship_type = 2;
            }

            if (ks.IsKeyDown(Keys.D3))
            {
                ship_type = 3;
            }

            if (ks.IsKeyDown(Keys.D4))
            {
                ship_type = 4;
            }

            if (ks.IsKeyDown(Keys.D5))
            {
                ship_type = 5;
            }
            if (ks.IsKeyDown(Keys.Left))
            {
                screen = 200;
            }
            if (ks.IsKeyDown(Keys.Right))
            {
                screen = 300;
            }

            if (ks.IsKeyDown(Keys.R))
            {
                if (R_wasHolding == false)
                {
                    rotation = 1 - rotation;
                    R_wasHolding = true;


                }
            }
            else
            {
                R_wasHolding = false;
            }
            if (rotation == 0)
            {
                nmy = Math.Max(0, Math.Min(nmy, 9));
                nmx = Math.Max(Math.Min(nmx, 10 - ship_type), 0);
            }
            if (rotation == 1)
            {
                nmx = Math.Max(0, Math.Min(nmx, 9));
                nmy = Math.Max(Math.Min(nmy, 10 - ship_type), 0);
            }

            placePos = new Vector2(nmx, nmy);
            if (currentMouseState.LeftButton == ButtonState.Pressed)
            {
                if (screen == 0)
                {
                    if (b_vsAi.Update(currentMouseState.X, currentMouseState.Y, ww, wh))
                    {
                        screen = 1;
                        wasHolding = true;
                    }
                    if (b_vsPlayer.Update(currentMouseState.X, currentMouseState.Y, ww, wh))
                    {
                        screen = 2;
                        wasHolding = true;
                    }
                    if (b_exit.Update(currentMouseState.X, currentMouseState.Y, ww, wh))
                    {
                        Exit();
                    }
                }
                if (screen == 1)
                {
                    if (b_EasyAi.Update(currentMouseState.X, currentMouseState.Y, ww, wh) && wasHolding == false)
                    {
                        diff = 0;
                        screen = 100;
                        wasHolding = true;
                    }
                    if (b_NormalAi.Update(currentMouseState.X, currentMouseState.Y, ww, wh) && wasHolding == false)
                    {
                        screen = 100;
                        diff = 1;
                        wasHolding = true;
                    }
                    if (b_HardAi.Update(currentMouseState.X, currentMouseState.Y, ww, wh) && wasHolding == false)
                    {
                        diff = 2;
                        screen = 100;
                        wasHolding = true;
                    }

                    if (b_AiMainMenu.Update(currentMouseState.X, currentMouseState.Y, ww, wh) && wasHolding == false)
                    {
                        screen = 0;
                        wasHolding = true;
                    }
                }
                if (screen == 100)
                {
                    if (nmx >= 0 && nmx < 10 && nmy >= 0 && nmy < 10 && wasHolding == false && ship_counts[ship_type] > 0)
                    {

                        //t_player[nmy, nmx] = 1;
                        if (rotation == 0)
                        {
                            bool nasliSmoBrod = false;
                            for (int i = 0; i < ship_type; i++)
                            {
                                if (t_player[nmy, nmx + i] != 0)
                                {
                                    nasliSmoBrod = true;
                                    break;
                                }
                            }
                            if (!nasliSmoBrod)
                            {
                                stats[ship_counts[ship_type] * 10 + ship_type] = new List<int> { nmy, nmx, rotation }; 
                                for (int i = 0; i < ship_type; i++)
                                {
                                    t_player[nmy, nmx + i] = ship_counts[ship_type] * 10 + ship_type;
                                }
                                ship_counts[ship_type] -= 1;
                            }
                        }
                        if (rotation == 1)
                        {
                            bool nasliSmoBrod = false;
                            for (int i = 0; i < ship_type; i++)
                            {
                                if (t_player[nmy + i, nmx] != 0)
                                {
                                    nasliSmoBrod = true;
                                    break;
                                }
                            }
                            if (!nasliSmoBrod)
                            {
                                stats[ship_counts[ship_type] * 10 + ship_type] = new List<int> { nmy, nmx, rotation };
                                for (int i = 0; i < ship_type; i++)
                                {
                                    t_player[nmy + i, nmx] = ship_counts[ship_type] * 10 + ship_type;
                                }
                                ship_counts[ship_type] -= 1;
                            }
                        }

                    }

                }
                if (screen == 300)
                {
                    if (nmx >= 0 && nmx < 10 && nmy >= 0 && nmy < 10)
                    {
                        Vector2 guess = new Vector2((currentMouseState.X - t_x) / (i_board_edge / 10), (currentMouseState.Y - t_y) / (i_board_edge / 10));
                        bool nasli = false;

                        for (int i = 0; i < guesses.Count; i++)
                        {
                            if (guesses[i].X == guess.X && guesses[i].Y == guess.Y)
                            {
                                nasli = true;
                            }
                        }

                        if (!nasli)
                        {
                            if (guess.X < 10 && guess.X >= 0 && guess.Y < 10 && guess.Y >= 0)
                            {
                                guesses.Add(guess);

                                // Ai Guess
                                Random nr = new Random();
                                Vector2 guess_ai = new Vector2(nr.Next(0, 10), nr.Next(0, 10));
                                guesses_ai.Add(guess_ai);
                            }
                        }
                    }
                }
            }
            else
            {
                wasHolding = false;
            }


            if (ks.IsKeyDown(Keys.Escape))
                Exit();

            base.Update(gameTime);


        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Gray); // Red background
            int odvoj = (int)(i_board_edge * 1.2f);
            _spriteBatch.Begin();

            if (screen == 0)
            {
                b_vsAi.Draw(_spriteBatch, font, ww, wh);
                b_vsPlayer.Draw(_spriteBatch, font, ww, wh);
                b_exit.Draw(_spriteBatch, font, ww, wh);
            }
            if (screen == 1)
            {
                b_EasyAi.Draw(_spriteBatch, font, ww, wh);
                b_NormalAi.Draw(_spriteBatch, font, ww, wh);
                b_HardAi.Draw(_spriteBatch, font, ww, wh);
                b_AiMainMenu.Draw(_spriteBatch, font, ww, wh);
            }

            if (screen == 100)
            {
                for (int y = 0; y < 10; y++)
                {
                    for (int x = 0; x < 10; x++)
                    {
                        if (t_player[y, x] == 0)
                        {
                            _spriteBatch.Draw(_whiteTexture, new Rectangle(t_x + (x * i_board_edge) / 10, t_y + (y * i_board_edge) / 10, i_board_edge / 10, i_board_edge / 10), Color.White);
                        }
                        else if (t_player[y, x] > 0)
                        {
                            _spriteBatch.Draw(_whiteTexture, new Rectangle(t_x + (x * i_board_edge) / 10, t_y + (y * i_board_edge) / 10, i_board_edge / 10, i_board_edge / 10), Color.Green);

                        }
                    }
                }
                // Placement overlay

                if (rotation == 0)
                {
                    _spriteBatch.Draw(_whiteTexture, new Rectangle((int)(placePos.X) * i_board_edge / 10 + t_x, (int)(placePos.Y) * i_board_edge / 10 + t_y, ship_type * i_board_edge / 10, i_board_edge / 10), Color.Black);
                }
                else
                {
                    _spriteBatch.Draw(_whiteTexture, new Rectangle((int)(placePos.X) * i_board_edge / 10 + t_x, (int)(placePos.Y) * i_board_edge / 10 + t_y, i_board_edge / 10, ship_type * i_board_edge / 10), Color.Black);
                }
            }
            if (screen == 200)
            {
                for (int y = 0; y < 10; y++)
                {
                    for (int x = 0; x < 10; x++)
                    {
                        _spriteBatch.Draw(waterImgs[t_player2[y,x]], new Rectangle(t_x + (x * i_board_edge) / 10, t_y + (y * i_board_edge) / 10, i_board_edge / 10, i_board_edge / 10), Color.White);
                    }
                }
                for (int y = 0; y <= 10; y++)
                {
                    int yPos = t_y + (y * i_board_edge) / 10;
                    _spriteBatch.Draw(_whiteTexture, new Rectangle(t_x, yPos, i_board_edge, 2), Color.Black);
                }

                for (int x = 0; x <= 10; x++)
                {
                    int xPos = t_x + (x * i_board_edge) / 10;
                    _spriteBatch.Draw(_whiteTexture, new Rectangle(xPos, t_y, 2, i_board_edge), Color.Black);
                }
                foreach (var key in stats.Keys)
                {
                    bool nacrtao = false;
                    for (int y = 0; y < 10; y++)
                    {
                        for (int x = 0; x < 10; x++)
                        {
                            if (stats[key][2] == 1 && t_player[y, x]==key)
                            {
                                _spriteBatch.Draw(brodovi["ship"+t_player[y, x].ToString()], new Rectangle(t_x + (x * i_board_edge) / 10, t_y + (y * i_board_edge) / 10, i_board_edge / 10, i_board_edge * (key % 10) / 10), Color.White);
                                nacrtao = true;

                            }
                            else if (stats[key][2] == 0 && t_player[y, x] == key) // Horizontal (rotated)
                            {
                                int length = key % 10; // Ship length
                                int tileSize = i_board_edge / 10;

                                Texture2D texture = brodovi["ship" + t_player[y, x].ToString()];

                                // Position where the ship starts drawing
                                Vector2 position = new Vector2(t_x + ((x+length) * tileSize), t_y + (y * tileSize));

                                // We'll rotate 90° clockwise around the top-left of the ship
                                float rotation = MathHelper.PiOver2;

                                // Draw size in world space after rotation: length * tileSize width, 1 * tileSize height
                                Rectangle destRect = new Rectangle(
                                    (int)position.X,
                                    (int)position.Y,
                                    tileSize,
                                    length * tileSize
                                );

                                // The origin shifts to the top-left corner to avoid weird offsets
                                Vector2 origin = Vector2.Zero;

                                _spriteBatch.Draw(
                                    texture,
                                    destRect,
                                    null,
                                    Color.White,
                                    rotation,
                                    origin,
                                    SpriteEffects.None,
                                    0f
                                );

                                nacrtao = true;
                            }

                            if (nacrtao)
                            {
                                break;
                            }
                        }
                        if (nacrtao)
                        {
                            break;
                        }
                    }
                    
                }

                
                for (int i = 0; i < guesses_ai.Count; i++)
                {
                    if (t_ai[(int)(guesses_ai[i].X), (int)(guesses_ai[i].Y)] == 0)
                    {
                        _spriteBatch.Draw(_whiteTexture, new Rectangle(t_x + (int)(guesses_ai[i].X * i_board_edge / 10), t_y + (int)(guesses_ai[i].Y * i_board_edge / 10), i_board_edge / 10, i_board_edge / 10), Color.Aqua);
                    }
                    else
                    {
                        _spriteBatch.Draw(_whiteTexture, new Rectangle(t_x + (int)(guesses_ai[i].X * i_board_edge / 10), t_y + (int)(guesses_ai[i].Y * i_board_edge / 10), i_board_edge / 10, i_board_edge / 10), Color.Red);
                    }

                }




                for (int y = 0; y < 10; y++)
                {
                    for (int x = 0; x < 10; x++)
                    {
                        _spriteBatch.Draw(waterImgs[t_ai2[y, x]], new Rectangle(odvoj+t_x + (x * i_board_edge) / 10, t_y + (y * i_board_edge) / 10, i_board_edge / 10, i_board_edge / 10), Color.White);
                    }
                }
                for (int y = 0; y <= 10; y++)
                {
                    int yPos = t_y + (y * i_board_edge) / 10;
                    _spriteBatch.Draw(_whiteTexture, new Rectangle(t_x+odvoj, yPos, i_board_edge, 2), Color.Black);
                }

                for (int x = 0; x <= 10; x++)
                {
                    int xPos = t_x + (x * i_board_edge) / 10;
                    _spriteBatch.Draw(_whiteTexture, new Rectangle(xPos+odvoj, t_y, 2, i_board_edge), Color.Black);
                }
                for (int i = 0; i < guesses.Count; i++)
                {
                    if (t_ai[(int)(guesses[i].X), (int)(guesses[i].Y)] == 0)
                    {
                        _spriteBatch.Draw(_whiteTexture, new Rectangle(odvoj + t_x + (int)(guesses[i].X * i_board_edge / 10), t_y + (int)(guesses[i].Y * i_board_edge / 10), i_board_edge / 10, i_board_edge / 10), Color.Aqua);
                    }
                    else
                    {
                        _spriteBatch.Draw(_whiteTexture, new Rectangle(odvoj + t_x + (int)(guesses[i].X * i_board_edge / 10), t_y + (int)(guesses[i].Y * i_board_edge / 10), i_board_edge / 10, i_board_edge / 10), Color.Red);
                    }

                }
            }

            //_spriteBatch.Draw(_whiteTexture, new Rectangle(100, 100, 200, 100), Color.White);

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
