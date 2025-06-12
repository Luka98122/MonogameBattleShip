using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices.Marshalling;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MonogameBattleShip
{
    public class Ship
    {
        public int Id;
        public int Length;
        public int X;
        public int Y;
        public int Rotation;
        public bool BeingDragged;
        public int OriginalX;
        public int OriginalY;
    }
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
    public class Slider
    {
        private Texture2D pixel;
        private Rectangle bounds;
        private int handleSize;
        public float value;

        private bool isDragging = false;

        public float Value => value;

        public Slider(GraphicsDevice graphicsDevice, Rectangle bounds, int handleSize = 10)
        {
            this.bounds = bounds;
            this.handleSize = handleSize;
            this.value = 0.5f; // start in the middle

            pixel = new Texture2D(graphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });
        }

        public void Update(MouseState mouse)
        {

            
            var mousePos = new Point(mouse.X, mouse.Y);
            var handleX = (int)(bounds.X + value * bounds.Width);
            var handleRect = new Rectangle(handleX - handleSize / 2, bounds.Y, handleSize, bounds.Height);

            if (mouse.LeftButton == ButtonState.Pressed)
            {
                if (!bounds.Contains(mousePos))
                    return;
                if (!isDragging && handleRect.Contains(mousePos))
                    isDragging = true;

                if (isDragging)
                {
                    float relativeX = MathHelper.Clamp(mouse.X - bounds.X, 0, bounds.Width);
                    value = relativeX / bounds.Width;
                }
            }
            else
            {
                isDragging = false;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            int fillWidth = (int)(value * bounds.Width);

            // Left (red)
            spriteBatch.Draw(pixel, new Rectangle(bounds.X, bounds.Y, fillWidth, bounds.Height), Color.Red);

            // Right (white)
            spriteBatch.Draw(pixel, new Rectangle(bounds.X + fillWidth, bounds.Y, bounds.Width - fillWidth, bounds.Height), Color.White);

            // Slider handle (small white square)
            int handleX = (int)(bounds.X + value * bounds.Width) - handleSize / 2;
            Rectangle handleRect = new Rectangle(handleX, bounds.Y, handleSize, bounds.Height);
            spriteBatch.Draw(pixel, handleRect, Color.Black);
        }
    }
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private SpriteFont font;
        private SpriteFont font2;


        public Button b_vsAi;
        public Button b_vsPlayer;
        public Button b_exit;
        public Button b_EasyAi;
        public Button b_NormalAi;
        public Button b_HardAi;
        public Button b_AiMainMenu;

        public Slider diff_slider;

        public float difficulty = -1f;

        // Rendering sizes
        public Texture2D _whiteTexture;
        public int screen = 0;
        public int ww = 1700;
        public int wh = 956;
        public int board_edge_pix = 640;

        // Boards
        public int[,] board_player;
        public int[,] board_ai;

        public int[,] gueses_player;
        public int[,] gueses_ai;

        public int[,] waterframes_player;
        public int[,] waterframes_ai;
        public int board_x = 140;
        public int board_y = 158;
        public KeyboardState keyboard;
        public int tile_x;
        public int tile_y;
        public int odvoj;
        public int[,] enemyFog;
        public int[,] sunken;


        List<Texture2D> explosions = new List<Texture2D> { };
        public int explosionCounter = 0;
        // Ship placing
        private int ship_length;
        private List<Ship> playerShips;
        private Ship draggedShip = null;
        private int dragOffsetX, dragOffsetY;
        private Button b_startGame;

        private int[] ship_counts = new int[] { 0, 0, 1, 2, 1, 1 };

        // Input stuff
        private Vector2 placePos = new Vector2();
        private MouseState currentMouseState;
        private bool wasHolding = false;
        private bool R_wasHolding = false;
        private int rotation = 0;
        public List<Texture2D> waterImgs = new List<Texture2D> { };
        public int waitForTurn = 0;
        public bool isMyTurn = true;
        // Guesses
        public List<Vector2> guesses = new List<Vector2> { };
        public List<Vector2> guesses_ai = new List<Vector2> { };

        // Textures
        public int waterFrame = 0;
        public int animationSlow = 50;
        public int animationSpeed = 3;
        Dictionary<string, Texture2D> brodovi = new Dictionary<string, Texture2D> { };
        Dictionary<int, List<int>> stats = new Dictionary<int, List<int>> { };
        Texture2D fog = null;
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
        int animdirection = 1;
        public static void dump(int[,] matrix, string path)
        {
            using (StreamWriter writer = new StreamWriter(path))
            {
                int rows = matrix.GetLength(0);
                int cols = matrix.GetLength(1);

                for (int i = 0; i < rows; i++)
                {
                    string[] line = new string[cols];
                    for (int j = 0; j < cols; j++)
                    {
                        line[j] = matrix[i, j].ToString();
                    }
                    writer.WriteLine(string.Join("\t", line));
                }
            }
        }

        public static int[,] GenerateHeatmap(int[,] grid, List<int> shipLengths, int[,] sunken)
        {
            int[,] heatmap = new int[10, 10];

            foreach (int shipLength in shipLengths)
            {

                for (int y = 0; y < 10; y++)
                {
                    for (int x = 0; x <= 10 - shipLength; x++)
                    {
                        bool isValid = true;

                        for (int i = 0; i < shipLength; i++)
                        {
                            if (grid[y, x + i] != 0) 
                            {
                                isValid = false;
                                break;
                            }
                        }

                        if (isValid)
                        {
                            for (int i = 0; i < shipLength; i++)
                            {
                                heatmap[y, x + i]++;
                            }
                        }
                    }
                }
                for (int x = 0; x < 10; x++)
                {
                    for (int y = 0; y <= 10 - shipLength; y++)
                    {
                        bool isValid = true;

                        for (int i = 0; i < shipLength; i++)
                        {
                            if (grid[y + i, x] != 0)
                            {
                                isValid = false;
                                break;
                            }
                        }

                        if (isValid)
                        {
                            for (int i = 0; i < shipLength; i++)
                            {
                                heatmap[y + i, x]++;
                            }
                        }
                    }
                }
            }

            // Step 2: Add bonus to surrounding cells of hits
            int[] dx = { -1, 1, 0, 0 };
            int[] dy = { 0, 0, -1, 1 };

            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 10; x++)
                {
                    if (grid[y, x] == 1) // It's a hit
                    {
                        for (int d = 0; d < 4; d++) // Check up, down, left, right
                        {
                            int nx = x + dx[d];
                            int ny = y + dy[d];
                            if (nx >= 0 && nx < 10 && ny >= 0 && ny < 10 && grid[ny, nx] == 0 && sunken[y,x]!=1)
                            {
                                heatmap[ny, nx] += 40; // Bonus value can be tuned
                            }
                        }
                    }
                }
            }

            return heatmap;
        }

        public Vector2 getBest(int[,] heatmap)
        {
            Vector2 res = new Vector2(0, 0);
            int max = 0;

            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 10; x++)
                {
                    int val = heatmap[y, x];
                    if (val >= max)
                    {
                        res = new Vector2(x, y);
                        max = val;
                    }
                }
            }

            return res;
        }

        

        private List<Ship> ExtractShipsFromBoard(int[,] board)
        {
            List<Ship> ships = new List<Ship>();
            bool[,] visited = new bool[10, 10];

            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 10; x++)
                {
                    if (board[y, x] != 0 && !visited[y, x])
                    {
                        int id = board[y, x];

                        // Horizontal ship
                        if (x < 9 && board[y, x + 1] == id)
                        {
                            int length = 1;
                            visited[y, x] = true;
                            int cx = x + 1;
                            while (cx < 10 && board[y, cx] == id)
                            {
                                visited[y, cx] = true;
                                length++;
                                cx++;
                            }

                            Ship ship = new Ship();
                            ship.Id = id;
                            ship.Length = length;
                            ship.X = x;
                            ship.Y = y;
                            ship.Rotation = 0;
                            ships.Add(ship);
                        }
                        // Vertical ship
                        else
                        {
                            int length = 1;
                            visited[y, x] = true;
                            int cy = y + 1;
                            while (cy < 10 && board[cy, x] == id)
                            {
                                visited[cy, x] = true;
                                length++;
                                cy++;
                            }

                            Ship ship = new Ship();
                            ship.Id = id;
                            ship.Length = length;
                            ship.X = x;
                            ship.Y = y;
                            ship.Rotation = 1;
                            ships.Add(ship);
                        }
                    }
                }
            }
            return ships;
        }

        protected override void Initialize()
        {

            _whiteTexture = new Texture2D(GraphicsDevice, 1, 1);
            _whiteTexture.SetData(new[] { Color.White });
            b_vsAi = new Button(0.4f, 0.1f, 0.2f, 0.1f, "vs AI", _whiteTexture);
            b_vsPlayer = new Button(0.4f, 0.3f, 0.2f, 0.1f, "vs Player", _whiteTexture);
            b_exit = new Button(0.4f, 0.5f, 0.2f, 0.15f, "Exit", _whiteTexture);

            b_EasyAi = new Button(0.4f, 0.1f, 0.2f, 0.1f, "Ez", _whiteTexture);
            b_AiMainMenu = new Button(0.35f, 0.7f, 0.3f, 0.1f, "Main Menu", _whiteTexture);

            board_ai = new int[10, 10];
            board_player = new int[10, 10];

            
            b_startGame = new Button(0.8f, 0.9f, 0.15f, 0.08f, "Start Game", _whiteTexture);

            gueses_ai = new int[10, 10];
            gueses_player = new int[10, 10];
            sunken = new int[10, 10];
            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 10; x++)
                {
                    if ((x + y) % 2 == 0)
                    {
                        board_player[y, x] = 0;
                    }
                }
            }

            board_ai = AIPlace();
            

            waterframes_ai = new int[10, 10];
            waterframes_player = new int[10, 10];
            enemyFog = new int[10, 10];
            Random rn = new Random();
            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 10; x++)
                {
                    waterframes_ai[y, x] = rn.Next(1, 39);
                    waterframes_player[y, x] = rn.Next(1, 39);

                }
            }

            diff_slider = new Slider(GraphicsDevice, new Rectangle(650, 300, 300, 30), 5);
            
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            font = Content.Load<SpriteFont>("DefaultFont");
            font2 = Content.Load<SpriteFont>("Font2");

            fog = Content.Load<Texture2D>("fog");
            for (int i = 0; i < 40; i++)
            {
                string imgName = i.ToString("D4");
                waterImgs.Add(Content.Load<Texture2D>(imgName));
            }

            for (int i = 0; i < 18; i++)
            {
                string imgName = "frame_" + i.ToString("D2") + "_delay-0.1s";
                Texture2D originalImage = Content.Load<Texture2D>(imgName);

                // Get pixel data
                Color[] pixels = new Color[originalImage.Width * originalImage.Height];
                originalImage.GetData(pixels);

                int transparentCount = 0;

                for (int j = 0; j < pixels.Length; j++)
                {
                    if (pixels[j].A == 0)
                    {
                        transparentCount++;
                    }
                    else if (pixels[j].R == 223 && pixels[j].G == 223 && pixels[j].B == 223 && pixels[j].A == 255)
                    {
                        pixels[j] = new Color(0, 0, 0, 0); // Replace with transparent
                        transparentCount++;
                    }
                }

                Console.WriteLine($"Frame {i}: Transparent pixels: {transparentCount}");

                // Create a new texture with modified pixels
                Texture2D modifiedImage = new Texture2D(GraphicsDevice, originalImage.Width, originalImage.Height);
                modifiedImage.SetData(pixels);

                explosions.Add(modifiedImage);
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
            Dictionary<int, int> shipPartsHit = new Dictionary<int, int>
            {
                { 13, 0 },
                { 12, 0 },
                { 23, 0 },
                { 14, 0 },
                { 15, 0 }
            };

           
            explosionCounter += animdirection;
            explosionCounter %= 13*7;
            if (explosionCounter >= 89) 
            {
                animdirection = -1;
                explosionCounter = 88;
            }
            if (explosionCounter <= 30) 
            {
                animdirection = 1;
                explosionCounter = 32;
            }
            keyboard = Keyboard.GetState();
            currentMouseState = Mouse.GetState();
            tile_x = (currentMouseState.X - board_x) / (board_edge_pix / 10);
            tile_y = (currentMouseState.Y - board_y) / (board_edge_pix / 10);
            animationSlow -= 1;
            if (animationSlow == 0)
            {
                animationSlow = animationSpeed;
                for (int y = 0; y < 10; y++)
                {
                    for (int x = 0; x < 10; x++)
                    {
                        if (enemyFog[y, x] != 0)
                        {
                            waterframes_ai[y, x] += 1;
                            waterframes_ai[y, x] %= 40;
                        }
                        waterframes_player[y, x] += 1;
                        waterframes_player[y, x] %= 40;
                    }
                }
            }
            if (keyboard.IsKeyDown(Keys.D2))
            {
                ship_length = 2;
            }
            if (keyboard.IsKeyDown(Keys.D3))
            {
                ship_length = 3;
            }

            if (keyboard.IsKeyDown(Keys.D4))
            {
                ship_length = 4;
            }

            if (keyboard.IsKeyDown(Keys.D5))
            {
                ship_length = 5;
            }
            if (keyboard.IsKeyDown(Keys.Left))
            {
                screen = 200;
                //dump(board_ai, "C:\\users\\luka9\\downloads\\board_ai.txt");
                //dump(board_player, "C:\\users\\luka9\\downloads\\board_pl.txt");
            }
            if (keyboard.IsKeyDown(Keys.Right))
            {
                screen = 300;
            }

            if (keyboard.IsKeyDown(Keys.R))
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
                tile_y = Math.Max(0, Math.Min(tile_y, 9));
                tile_x = Math.Max(Math.Min(tile_x, 10 - ship_length), 0);
            }
            if (rotation == 1)
            {
                tile_x = Math.Max(0, Math.Min(tile_x, 9));
                tile_y = Math.Max(Math.Min(tile_y, 10 - ship_length), 0);
            }

            placePos = new Vector2(tile_x, tile_y);
            if (currentMouseState.LeftButton == ButtonState.Pressed)
            {
                // Main menu
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
                        screen = 400;
                        //Exit();
                    }
                }
                // V.S. AI Sub-Menu
                if (screen == 1)
                {
                    if (b_EasyAi.Update(currentMouseState.X, currentMouseState.Y, ww, wh) && wasHolding == false)
                    {
                        difficulty = 0.0f;
                        screen = 100;
                        wasHolding = true;
                    }

                    if (b_AiMainMenu.Update(currentMouseState.X, currentMouseState.Y, ww, wh) && wasHolding == false)
                    {
                        screen = 0;
                        wasHolding = true;
                    }

                    diff_slider.Update(currentMouseState);
                }
                // Placer
                if (screen == 100)
                {
                    if (tile_x >= 0 && tile_x < 10 && tile_y >= 0 && tile_y < 10 && wasHolding == false && ship_counts[ship_length] > 0)
                    {
                        if (rotation == 0)
                        {
                            bool nasliSmoBrod = false;
                            for (int i = 0; i < ship_length; i++)
                            {
                                if (board_player[tile_y, tile_x + i] != 0)
                                {
                                    nasliSmoBrod = true;
                                    break;
                                }
                            }
                            if (!nasliSmoBrod)
                            {
                                stats[ship_counts[ship_length] * 10 + ship_length] = new List<int> { tile_y, tile_x, rotation }; 
                                for (int i = 0; i < ship_length; i++)
                                {
                                    board_player[tile_y, tile_x + i] = ship_counts[ship_length] * 10 + ship_length;
                                }
                                ship_counts[ship_length] -= 1;
                            }
                        }
                        if (rotation == 1)
                        {
                            bool nasliSmoBrod = false;
                            for (int i = 0; i < ship_length; i++)
                            {
                                if (board_player[tile_y + i, tile_x] != 0)
                                {
                                    nasliSmoBrod = true;
                                    break;
                                }
                            }
                            if (!nasliSmoBrod)
                            {
                                stats[ship_counts[ship_length] * 10 + ship_length] = new List<int> { tile_y, tile_x, rotation };
                                for (int i = 0; i < ship_length; i++)
                                {
                                    board_player[tile_y + i, tile_x] = ship_counts[ship_length] * 10 + ship_length;
                                }
                                ship_counts[ship_length] -= 1;
                            }
                        }

                    }
                }
                // Attack
                if (screen == 200)
                {

                    

                    if (tile_x >= 0 && tile_x < 10 && tile_y >= 0 && tile_y < 10 && isMyTurn)
                    {
                        Vector2 guess = new Vector2((currentMouseState.X - board_x-odvoj) / (board_edge_pix / 10), (currentMouseState.Y - board_y) / (board_edge_pix / 10));
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
                            if (guess.X < 10 && guess.X >= 0 && guess.Y < 10 && guess.Y >= 0 && isMyTurn==true)
                            {
                                if (gueses_player[(int)(guess.Y), (int)(guess.X)] == 0)
                                {
                                    gueses_player[(int)(guess.Y), (int)(guess.X)] = 1;
                                    enemyFog[(int)(guess.Y), (int)(guess.X)] = 2;
                                    if (board_ai[(int)(guess.Y), (int)(guess.X)] == 0)
                                    {
                                        gueses_player[(int)(guess.Y), (int)(guess.X)] = 2;
                                        enemyFog[(int)(guess.Y), (int)(guess.X)] = 1;
                                    }
                                    waitForTurn = 30;
                                    isMyTurn = false;
                                    // Ai Guess
                                }
                            }
                        }
                    }
                }
                if (screen == 400)
                {
                    
                }

                if (screen == 500)
                {

                }
            }
            else
            {
                wasHolding = false;
            }

            if (!isMyTurn)
            {
                waitForTurn -= 1;
            }

            for (int y = 0; y < 10; y++) 
            {
                for (int x = 0; x < 10; x++) 
                {
                    if (board_player[y, x] != 0 && gueses_ai[y, x] == 1) 
                    {
                        shipPartsHit[board_player[y, x]] += 1;  
                    }
                }
            }
            List<int> shiLengths = new List<int> { };

            foreach (int key in shipPartsHit.Keys)
            {
                if (key % 10 != shipPartsHit[key]) {
                    shiLengths.Add(key % 10);
                }
                else
                {
                    for (int y = 0; y < 10; y++)
                    {
                        for (int x = 0; x < 10; x++)
                        {
                            if (board_player[y, x] == key)
                            {
                                sunken[y, x] = 1;
                            }
                        }
                    }
                }
            }

            if (shiLengths.Count == 0)
            {
                screen = 400;
                //Exit();
            }


            if (waitForTurn <= 0 && isMyTurn==false)
            {
                Random nr = new Random();
                while (true)
                {

                    float value = (float)nr.NextDouble();
                    Vector2 guess_ai;
                    if (value < diff_slider.value)
                    {
                        int[,] hm = GenerateHeatmap(gueses_ai, shiLengths, sunken);
                        guess_ai = getBest(hm);
                    }
                    else
                    {
                        guess_ai = new Vector2(nr.Next(0, 10), nr.Next(0, 10));
                    }
                    int gys = (int)(guess_ai.Y);
                    int gxs = (int)(guess_ai.X);

                    if (gueses_ai[gys,gxs] == 0)
                    {
                        if (board_player[gys,gxs] == 0)
                        {
                            gueses_ai[gys, gxs] = 2;
                        }
                        else
                        {
                            gueses_ai[gys, gxs] = 1;
                        }
                        break;
                    }
                }
                
                waitForTurn = 0;
                isMyTurn = true;
            }

            if (keyboard.IsKeyDown(Keys.Escape))
                Exit();

            base.Update(gameTime);


        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Gray); // Red background
            odvoj = (int)(board_edge_pix * 1.2f);
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
                b_AiMainMenu.Draw(_spriteBatch, font, ww, wh);
                diff_slider.Draw(_spriteBatch);
            }

            if (screen == 100)
            {
                for (int y = 0; y < 10; y++)
                {
                    for (int x = 0; x < 10; x++)
                    {
                        _spriteBatch.Draw(waterImgs[waterframes_player[y, x]], new Rectangle(board_x + (x * board_edge_pix) / 10, board_y + (y * board_edge_pix) / 10, board_edge_pix / 10, board_edge_pix / 10), Color.White);
                    }
                }
                for (int y = 0; y < 10; y++)
                {
                    for (int x = 0; x < 10; x++)
                    {
                        if (board_player[y, x] == 0)
                        {
                            //_spriteBatch.Draw(_whiteTexture, new Rectangle(board_x + (x * board_edge_pix) / 10, board_y + (y * board_edge_pix) / 10, board_edge_pix / 10, board_edge_pix / 10), Color.White);
                        }
                        else if (board_player[y, x] > 0)
                        {
                            _spriteBatch.Draw(_whiteTexture, new Rectangle(board_x + (x * board_edge_pix) / 10, board_y + (y * board_edge_pix) / 10, board_edge_pix / 10, board_edge_pix / 10), Color.Green);

                        }
                    }
                }
                // Placement overlay

                if (rotation == 0)
                {
                    _spriteBatch.Draw(_whiteTexture, new Rectangle((int)(placePos.X) * board_edge_pix / 10 + board_x, (int)(placePos.Y) * board_edge_pix / 10 + board_y, ship_length * board_edge_pix / 10, board_edge_pix / 10), Color.Black);
                }
                else
                {
                    _spriteBatch.Draw(_whiteTexture, new Rectangle((int)(placePos.X) * board_edge_pix / 10 + board_x, (int)(placePos.Y) * board_edge_pix / 10 + board_y, board_edge_pix / 10, ship_length * board_edge_pix / 10), Color.Black);
                }

                for (int y = 0; y <= 10; y++)
                {
                    int yPos = board_y + (y * board_edge_pix) / 10;
                    _spriteBatch.Draw(_whiteTexture, new Rectangle(board_x, yPos, board_edge_pix, 2), Color.Black);
                }

                for (int x = 0; x <= 10; x++)
                {
                    int xPos = board_x + (x * board_edge_pix) / 10;
                    _spriteBatch.Draw(_whiteTexture, new Rectangle(xPos, board_y, 2, board_edge_pix), Color.Black);
                }
            }
            if (screen == 200)
            {
                for (int y = 0; y < 10; y++)
                {
                    for (int x = 0; x < 10; x++)
                    {
                        _spriteBatch.Draw(waterImgs[waterframes_player[y,x]], new Rectangle(board_x + (x * board_edge_pix) / 10, board_y + (y * board_edge_pix) / 10, board_edge_pix / 10, board_edge_pix / 10), Color.White);
                    }
                }
                // Make water darker
                foreach (var key in stats.Keys)
                {
                    bool nacrtao = false;
                    for (int y = 0; y < 10; y++)
                    {
                        for (int x = 0; x < 10; x++)
                        {
                            if (stats[key][2] == 1 && board_player[y, x]==key)
                            {
                                _spriteBatch.Draw(brodovi["ship"+board_player[y, x].ToString()], new Rectangle(board_x + (x * board_edge_pix) / 10, board_y + (y * board_edge_pix) / 10, board_edge_pix / 10, board_edge_pix * (key % 10) / 10), Color.White);
                                nacrtao = true;

                            }
                            else if (stats[key][2] == 0 && board_player[y, x] == key) // Horizontal (rotated)
                            {
                                int length = key % 10; // Ship length
                                int tileSize = board_edge_pix / 10;

                                Texture2D texture = brodovi["ship" + board_player[y, x].ToString()];

                                // Position where the ship starts drawing
                                Vector2 position = new Vector2(board_x + ((x+length) * tileSize), board_y + (y * tileSize));

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

            

                




                for (int y = 0; y < 10; y++)
                {
                    for (int x = 0; x < 10; x++)
                    {
                        // 1. Calculate tile position and size
                        int tileX = odvoj + board_x + ((0 + x) * board_edge_pix) / 10;
                        int tileY = board_y + ((0 + y) * board_edge_pix) / 10;
                        int tileSize = board_edge_pix / 10;


                        // 2. Draw water tile
                        _spriteBatch.Draw(
                            waterImgs[waterframes_ai[y, x]],
                            new Rectangle(tileX, tileY, tileSize, tileSize),
                            Color.White
                        );

                        if (enemyFog[y, x] == 0)
                        {
                            for (int leoverlap = 0; leoverlap < 2; leoverlap++)
                            {
                                // Fog from TOP edge (extends downward)
                                _spriteBatch.Draw(
                                    fog,
                                    new Rectangle(tileX, tileY, tileSize, tileSize), // Adjusted X position
                                    null,
                                    Color.White * 0.5f,
                                    0f,
                                    Vector2.Zero,
                                    SpriteEffects.None,
                                    0f
                                );

                                // Fog from RIGHT edge (extends leftward)
                                _spriteBatch.Draw(
                                    fog,
                                    new Rectangle(tileX + tileSize, tileY, tileSize, tileSize), // Adjusted Y position
                                    null,
                                    Color.White * 0.5f,
                                    MathHelper.PiOver2,
                                    Vector2.Zero,
                                    SpriteEffects.None,
                                    0f
                                );

                                // Fog from BOTTOM edge (extends upward)
                                _spriteBatch.Draw(
                                    fog,
                                    new Rectangle(tileX, tileSize, tileSize, tileSize),
                                    null,
                                    Color.White * 0.5f,
                                    MathHelper.Pi,
                                    Vector2.Zero,
                                    SpriteEffects.None,
                                    0f
                                );

                                // Fog from LEFT edge (extends rightward)
                                _spriteBatch.Draw(
                                    fog,
                                    new Rectangle(tileX, tileY+tileSize, tileSize, tileSize),
                                    null,
                                    Color.White * 0.5f,
                                    MathHelper.Pi * 1.5f,
                                    Vector2.Zero,
                                    SpriteEffects.None,
                                    0f
                                );
                            }
                        }

                    }
                }

                for (int y = 0; y < 10; y++)
                {
                    for (int x = 0; x < 10; x++)
                    {
                        if (gueses_ai[y, x] == 2)
                        {
                            _spriteBatch.Draw(_whiteTexture, new Rectangle(board_x + x * board_edge_pix / 10, board_y + y * board_edge_pix / 10, board_edge_pix / 10, board_edge_pix / 10), Color.Aqua);
                        }
                        else if (gueses_ai[y, x] == 1)
                        {
                            _spriteBatch.Draw(explosions[explosionCounter/7], new Rectangle(board_x + x * board_edge_pix / 10, board_y + y * board_edge_pix / 10, board_edge_pix / 10, board_edge_pix / 10), Color.White);
                        }

                    }
                }

                _spriteBatch.Draw(_whiteTexture, new Rectangle(0,0, board_edge_pix*3, 70), Color.Gray);

                for (int y = 0; y <= 10; y++)
                {
                    int yPos = board_y + (y * board_edge_pix) / 10;
                    _spriteBatch.Draw(_whiteTexture, new Rectangle(board_x+odvoj, yPos, board_edge_pix, 2), Color.Black);
                }



                for (int x = 0; x <= 10; x++)
                {
                    int xPos = board_x + (x * board_edge_pix) / 10;
                    _spriteBatch.Draw(_whiteTexture, new Rectangle(xPos+odvoj, board_y, 2, board_edge_pix), Color.Black);
                }
                for (int y = 0; y < 10; y++)
                {
                    for (int x = 0; x < 10; x++)
                    {
                        if (gueses_player[y, x] == 2)
                        {
                            //_spriteBatch.Draw(_whiteTexture, new Rectangle(odvoj+board_x + x * board_edge_pix / 10, board_y + y * board_edge_pix / 10, board_edge_pix / 10, board_edge_pix / 10), Color.Aqua);
                        }
                        else if (gueses_player[y, x] == 1)
                        {
                            _spriteBatch.Draw(explosions[explosionCounter/7], new Rectangle(odvoj+board_x + x * board_edge_pix / 10, board_y + y * board_edge_pix / 10, board_edge_pix / 10, board_edge_pix / 10), Color.White);
                        }

                    }
                }

                for (int y = 0; y <= 10; y++)
                {
                    int yPos = board_y + (y * board_edge_pix) / 10;
                    _spriteBatch.Draw(_whiteTexture, new Rectangle(board_x, yPos, board_edge_pix, 2), Color.Black);
                }

                for (int x = 0; x <= 10; x++)
                {
                    int xPos = board_x + (x * board_edge_pix) / 10;
                    _spriteBatch.Draw(_whiteTexture, new Rectangle(xPos, board_y, 2, board_edge_pix), Color.Black);
                }

                
            }

            if (screen == 400)
            {
                _spriteBatch.DrawString(font2, "Izgubio si", new Vector2(400, 400), Color.White);
            }

            if (screen == 500)
            {
                _spriteBatch.DrawString(font2, "Pobedio si. ( Probaj tezi AI )", new Vector2(400, 400), Color.White);
            }
            //_spriteBatch.Draw(_whiteTexture, new Rectangle(100, 100, 200, 100), Color.White);

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
