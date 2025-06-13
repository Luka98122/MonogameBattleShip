using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;


namespace MonogameBattleShip
{
    public class Ship
    {
        public int Id;
        public int Length;
        public int X;
        public int Y;
        public int Rotation;
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
            int rx = (int)(ww * this.x);
            int ry = (int)(wh * this.y);
            int rw = (int)(ww * this.w);
            int rh = (int)(wh * this.h);

            sp.Draw(this.tex, new Rectangle(rx, ry, rw, rh), Color.White);

            Vector2 textSize = font.MeasureString(this.text);

            Vector2 textPos = new Vector2(
                rx + (rw - textSize.X) / 2,
                ry + (rh - textSize.Y) / 2
            );

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
            this.value = 0.5f; 

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
            spriteBatch.Draw(pixel, new Rectangle(bounds.X, bounds.Y, fillWidth, bounds.Height), Color.Aqua);

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

        // Textures
        public Texture2D tex_button;
        public Texture2D tex_white;
        public Texture2D tex_VictoryScreen;
        public Texture2D tex_DefeatScreen;
        public Texture2D tex_menu;
        public Texture2D tex_sunkBanner;
        public Texture2D tex_logo;
        public Texture2D x_img;
        Dictionary<string, Texture2D> brodovi = new Dictionary<string, Texture2D> { };

        List<Texture2D> explosions = new List<Texture2D> { };

        // Buttons
        public Button b_vsAi;
        public Button b_vsPlayer;
        public Button b_exit;
        public Button b_startAI;
        public Button b_exitToMenu;
        public Button b_subStart;
        public Slider diff_slider;

        public List<int> notifications = new List<int> { };

        // Magic Numbers
        public int screen = 0;
        public int ww = 1700;
        public int wh = 956;
        public int board_length = 640;
        public int board_x = 140; // Positioning of board.
        public int board_y = 158;
        public int gap_between_boards;

        // Boards
        public int[,] player_layout;
        public int[,] ai_layout;
        public int[,] gueses_player; // matrica pogadjaja koje je igrac napravio 2- miss, 1 -hit, 0 nije probao
        public int[,] gueses_ai;

        public int[,] waterframes_player; // Na kom frameu je svako polje za vodu za animaciju ( od 40)
        public int[,] waterframes_ai;
        public int[,] enemyFog;
        
        public int[,] sunken;
        public int[,] heatmap;

        // Animations
        public int endFade = 0;
        public int explosionCounter = 0;
        public int waterFrame = 0;
        public int animationSlow = 50;
        public int animationSpeed = 3;

        // Runtime

        public bool showHeatmap = false;
        public Vector2 bestMove;
        public KeyboardState keyboard;
        public int tile_x;
        public int tile_y;

        

        // Ship placing
        private List<Ship> playerShips;
        private Ship draggedShip = null;

        public bool wasHoldingEsc = true;
        
        // Input stuff
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

        public bool wasHoldingHeatmap = false;
        public Button heatmapButton;

        public int previousScrollWheel = 0;
        public Dictionary<int, bool> sunkenShips = new Dictionary<int, bool> { { 13, false }, { 12, false }, { 23, false }, { 14, false }, { 15, false } };


        
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

       

        protected override void Initialize()
        {
            //gti
            bestMove = new Vector2(5,5);
            tex_white = new Texture2D(GraphicsDevice, 1, 1);
            tex_white.SetData(new[] { Color.White });
            tex_button = Content.Load<Texture2D>("button");
            notifications = new List<int> { };
            wasHoldingEsc = true;

            // Buttons
            b_vsAi = new Button(0.1f, 0.25f, 0.15f, 0.08f, "V.S. AI", tex_button);
            b_vsPlayer = new Button(0.1f, 0.23f, 0.15f, 0.08f, "vs Player", tex_button);
            b_exit = new Button(0.1f, 0.46f, 0.15f, 0.08f, "Exit", tex_button);
            b_startAI = new Button(0.1f, 0.45f, 0.15f, 0.08f, "Start", tex_button);
            b_exitToMenu = new Button(0.02f, 0.85f, 0.2f, 0.1f, "Main Menu", tex_button);
            b_subStart = new Button(0.5f, 0.4f, 0.1f, 0.08f, "Start", tex_button);
            
            ai_layout = new int[10, 10];
            player_layout = PlaceRandomShips();// new int[10, 10];
            heatmapButton = new Button(0.2f, 0.05f, 0.15f, 0.11f, "Toggle Heatmap", tex_button);
            endFade = 0;
            isMyTurn = true;
            waitForTurn = 0;
            sunkenShips = new Dictionary<int, bool> { { 13, false }, { 12, false }, { 23, false }, { 14, false }, { 15, false } };
            gueses_ai = new int[10, 10];
            gueses_player = new int[10, 10];
            sunken = new int[10, 10];

            ai_layout = PlaceRandomShips();
            

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

            diff_slider = new Slider(GraphicsDevice, new Rectangle((int)(ww*0.1f)-20, (int)(0.32f*wh), 300, 30), 10);
            heatmap = new int[10, 10];
            showHeatmap = false;
            base.Initialize();
        }


        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            font = Content.Load<SpriteFont>("DefaultFont");
            font2 = Content.Load<SpriteFont>("Font2");
            tex_DefeatScreen = Content.Load<Texture2D>("DefeatScreen");
            tex_VictoryScreen = Content.Load<Texture2D>("VictoryScreen");
            tex_sunkBanner = Content.Load<Texture2D>("SunkBanner");
            tex_logo = Content.Load<Texture2D>("Battleship-logo");
            tex_menu = Content.Load<Texture2D>("main1");
            x_img = Content.Load<Texture2D>("x");
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
                        pixels[j] = new Color(0, 0, 0, 0);
                        transparentCount++;
                    }
                }

                Console.WriteLine($"Frame {i}: Transparent pixels: {transparentCount}");

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

            int[] dx = { -1, 1, 0, 0 };
            int[] dy = { 0, 0, -1, 1 };

            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 10; x++)
                {
                    if (grid[y, x] == 1) // hit
                    {
                        for (int d = 0; d < 4; d++) 
                        {
                            int nx = x + dx[d];
                            int ny = y + dy[d];
                            if (nx >= 0 && nx < 10 && ny >= 0 && ny < 10 && grid[ny, nx] == 0 && sunken[y, x] != 1)
                            {
                                heatmap[ny, nx] += 70; 
                            }
                            nx += dx[d];
                            ny += dy[d];
                            if (nx >= 0 && nx < 10 && ny >= 0 && ny < 10 && grid[ny, nx] == 0 && sunken[y, x] != 1)
                            {
                                heatmap[ny, nx] += 40; 
                            }
                        }
                    }
                }
            }

            return heatmap;
        }
        public List<Ship> ExtractShipsFromBoard(int[,] board)
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

        public Vector2 getBestMoveFromHeatmap(int[,] heatmap)
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

        public int[,] PlaceRandomShips()
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
            keyboard = Keyboard.GetState();
            currentMouseState = Mouse.GetState();

            for (int i = 0; i < notifications.Count; i++)
            {
                notifications[i] += 1;
            }

            if (screen == 400)
            {
                if (endFade > 128)
                {
                    if (keyboard.IsKeyDown(Keys.Escape))
                    {
                        screen = 0;
                        Initialize();

                    }
                }
                endFade += 1;
                return;
            }

            if (screen == 500)
            {
                if (endFade > 128)
                {
                    if (keyboard.IsKeyDown(Keys.Escape))
                    {
                        screen = 0;
                        Initialize();

                    }
                }
                endFade += 1;
                return;
            }
            Dictionary<int, int> shipPartsHit = new Dictionary<int, int>
            {
                { 13, 0 },
                { 12, 0 },
                { 23, 0 },
                { 14, 0 },
                { 15, 0 }
            };

            Dictionary<int, int> shipPartsHit2 = new Dictionary<int, int>
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

            if (screen == 100)
            {
                tile_x = (currentMouseState.X - board_x) / (board_length / 10);
            }
            else
            {
                tile_x = (currentMouseState.X - board_x-gap_between_boards) / (board_length / 10);
            }
            tile_y = (currentMouseState.Y - board_y) / (board_length / 10);
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

            if (keyboard.IsKeyDown(Keys.Left))
            {
                screen = 200;
                wasHolding = true;
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
            

            if (currentMouseState.LeftButton == ButtonState.Pressed)
            {
                // Main menu
                if (screen == 0)
                {
                    if (b_vsAi.Update(currentMouseState.X, currentMouseState.Y, ww, wh))
                    {
                        screen = 1;
                        player_layout = PlaceRandomShips();
                        playerShips = ExtractShipsFromBoard(player_layout);
                        wasHolding = true;
                    }

                    if (b_exit.Update(currentMouseState.X, currentMouseState.Y, ww, wh))
                    {
                        //screen = 400;
                        Exit();
                    }
                }
                // V.S. AI Sub-Menu
                if (screen == 1)
                {
                    if (b_startAI.Update(currentMouseState.X, currentMouseState.Y, ww, wh) && wasHolding == false)
                    {
                        screen = 100;
                        wasHolding = true;
                    }

                    if (b_exitToMenu.Update(currentMouseState.X, currentMouseState.Y, ww, wh) && wasHolding == false)
                    {
                        screen = 0;
                        wasHolding = true;
                    }

                    diff_slider.Update(currentMouseState);
                }
                // Placer
                if (screen == 100)
                {
                    if (b_subStart.Update(currentMouseState.X, currentMouseState.Y, ww, wh))
                    {
                        screen = 200;
                        wasHolding = true;
                    }
                    if (tile_x >= 0 && tile_x < 10 && tile_y >= 0 && tile_y < 10 && wasHolding == false)
                    {
                        for (int i = 0; i < playerShips.Count; i++)
                        {
                            // 0 - horizontal, 1 - vertical
                            Ship currentShip = playerShips[i];
                            bool clickingCurrent = false;
                            for (int j = 0; j < currentShip.Length; j++)
                            {
                                if (currentShip.X + (1 - currentShip.Rotation)*j == tile_x && currentShip.Y + (currentShip.Rotation)*j==tile_y)
                                {
                                    if (draggedShip == null)
                                    {
                                        draggedShip = currentShip;
                                        rotation = draggedShip.Rotation;
                                        clickingCurrent = true;
                                    }
                                }
                            }
                        }
                        
                    }

                    if (draggedShip != null)
                    {
                        if (currentMouseState.ScrollWheelValue != previousScrollWheel)
                        {
                            rotation = 1 - rotation;
                            
                        }
                    }
                }
                // Attack
                if (screen == 200)
                {
                    if (wasHoldingHeatmap == false) {
                        if (heatmapButton.Update(currentMouseState.X, currentMouseState.Y, ww, wh))
                        {
                            showHeatmap = !showHeatmap;
                            wasHoldingHeatmap = true;
                        }
                    }


                    if (tile_x >= 0 && tile_x < 10 && tile_y >= 0 && tile_y < 10 && isMyTurn && wasHolding==false)
                    {
                        Vector2 guess = new Vector2((currentMouseState.X - board_x-gap_between_boards) / (board_length / 10), (currentMouseState.Y - board_y) / (board_length / 10));
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
                                    if (ai_layout[(int)(guess.Y), (int)(guess.X)] == 0)
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
               
            }
            else
            {
                wasHoldingHeatmap = false;
                wasHolding = false;
                if (draggedShip != null)
                {
                    bool canDo = true;
                    try
                    {
                        for (int j = 0; j < draggedShip.Length; j++)
                        {
                            if (player_layout[(int)((currentMouseState.Y - board_y) / (board_length / 10)) + (rotation) * j, (int)((currentMouseState.X - board_x) / (board_length / 10)) + (1 - rotation) * j] != 0 && player_layout[(int)((currentMouseState.Y - board_y) / (board_length / 10)) + (rotation) * j, (int)((currentMouseState.X - board_x) / (board_length / 10)) + (1 - rotation) * j] != draggedShip.Id)
                            {
                                canDo = false;
                                break;
                            }

                        }

                        if (canDo)
                        {
                            for (int j = 0; j < draggedShip.Length; j++)
                            {
                                player_layout[draggedShip.Y + (draggedShip.Rotation) * j, draggedShip.X + (1 - draggedShip.Rotation) * j] = 0;

                            }
                            draggedShip.Rotation = rotation;
                            for (int j = 0; j < draggedShip.Length; j++)
                            {
                                player_layout[(int)((currentMouseState.Y - board_y) / (board_length / 10)) + (draggedShip.Rotation) * j, (int)((currentMouseState.X - board_x) / (board_length / 10)) + (1 - draggedShip.Rotation) * j] = draggedShip.Id;

                            }
                            playerShips = ExtractShipsFromBoard(player_layout);
                        }
                    } catch
                    {

                    }

                }
                draggedShip = null;
            }

            if (!isMyTurn)
            {
                waitForTurn -= 1;
            }

            for (int y = 0; y < 10; y++) 
            {
                for (int x = 0; x < 10; x++) 
                {
                    if (player_layout[y, x] != 0 && gueses_ai[y, x] == 1) 
                    {
                        shipPartsHit[player_layout[y, x]] += 1;  
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
                            if (player_layout[y, x] == key)
                            {
                                sunken[y, x] = 1;
                            }
                        }
                    }
                }
            }

            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 10; x++)
                {
                    if (ai_layout[y, x] != 0 && gueses_player[y, x] == 1)
                    {
                        shipPartsHit2[ai_layout[y, x]] += 1;
                    }
                }
            }

            List<int> shiLengths2 = new List<int> { };

            foreach (int key in shipPartsHit2.Keys)
            {
                if (key % 10 != shipPartsHit2[key])
                {
                    shiLengths2.Add(key % 10);
                }
                else
                {
                    if (sunkenShips[key] == false)
                    {
                        sunkenShips[key] = true;
                        notifications.Add(0);
                    }
                }
                
            }

            if (shiLengths2.Count == 0)
            {
                screen = 500;
                //Exit();
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
                        
                        guess_ai = bestMove;
                    }
                    else
                    {
                        guess_ai = new Vector2(nr.Next(0, 10), nr.Next(0, 10));
                    }
                    int gys = (int)(guess_ai.Y);
                    int gxs = (int)(guess_ai.X);

                    if (gueses_ai[gys,gxs] == 0)
                    {
                        if (player_layout[gys,gxs] == 0)
                        {
                            gueses_ai[gys, gxs] = 2;
                        }
                        else
                        {
                            gueses_ai[gys, gxs] = 1;
                        }
                        heatmap = GenerateHeatmap(gueses_ai, shiLengths, sunken);
                        bestMove = getBestMoveFromHeatmap(heatmap);
                        break;
                    }
                    
                }
                
                waitForTurn = 0;
                isMyTurn = true;
            }

            if (keyboard.IsKeyDown(Keys.Escape))
            {
                if (wasHoldingEsc == false) { Exit(); }
            }
            else
            {
                wasHoldingEsc = false;
            }

            previousScrollWheel = currentMouseState.ScrollWheelValue;
            base.Update(gameTime);


        }

        protected override void Draw(GameTime gameTime)
        {
            //gtd
            GraphicsDevice.Clear(Color.Gray);
            gap_between_boards = (int)(board_length * 1.2f);
            _spriteBatch.Begin();

            if (screen == 400 && endFade>128)
            {
                if (endFade>128)
                {

                    _spriteBatch.Draw(tex_DefeatScreen, new Rectangle(0, 0, ww, wh), Color.White);
                }
                _spriteBatch.End();
                base.Draw(gameTime);
                return;
            }

            if (screen == 500 && endFade>128)
            {

                if (endFade > 128)
                {

                    _spriteBatch.Draw(tex_VictoryScreen, new Rectangle(0, 0, ww, wh), Color.White);
                }
                _spriteBatch.End();
                base.Draw(gameTime);
                return;
            }

            if (screen == 0)
            {
                _spriteBatch.Draw(tex_menu, new Rectangle(0, 0, ww, wh), Color.White);
                _spriteBatch.Draw(tex_logo, new Rectangle((int)(0.035f * ww), (int)(0.015f * wh), (int)(0.28f * ww), (int)(0.28f * wh)), Color.White);
                b_vsAi.Draw(_spriteBatch, font, ww, wh);
                //b_vsPlayer.Draw(_spriteBatch, font, ww, wh);
                b_exit.Draw(_spriteBatch, font, ww, wh);
                _spriteBatch.DrawString(font, "Luka Markovic &", new Vector2(ww - 200, wh - 80), Color.Black);
                _spriteBatch.DrawString(font, "Stribor Pavlovic", new Vector2(ww - 200, wh - 50), Color.Black);

            }
            if (screen == 1)
            {
                _spriteBatch.Draw(tex_menu, new Rectangle(0, 0, ww, wh), Color.White);
                b_startAI.Draw(_spriteBatch, font, ww, wh);
                b_exitToMenu.Draw(_spriteBatch, font, ww, wh);
                diff_slider.Draw(_spriteBatch);
                _spriteBatch.DrawString(font, "Difficulty:", new Vector2((int)(ww * 0.1f) - 20, (int)(0.25f * wh)),Color.Black);
            }

            if (screen == 100)
            {
                _spriteBatch.Draw(tex_menu, new Rectangle(0, 0, ww, wh), Color.White);
                b_subStart.Draw(_spriteBatch,font,ww,wh);
                for (int y = 0; y < 10; y++)
                {
                    for (int x = 0; x < 10; x++)
                    {
                        _spriteBatch.Draw(waterImgs[waterframes_player[y, x]], new Rectangle(board_x + (x * board_length) / 10, board_y + (y * board_length) / 10, board_length / 10, board_length / 10), Color.White);
                    }
                }
                foreach (Ship cur in playerShips)
                {
                    float rotation = cur.Rotation == 0 ? MathF.PI / 2 : 0f;
                    Vector2 origin = new Vector2(0, 0);

                    origin = new Vector2(0, 0);


                    int width = board_length / 10;
                    int height = (board_length / 10) * cur.Length;

                    int xOffset = 0;

                    if (cur.Rotation == 0)
                        xOffset += (int)(cur.Length * board_length / 10);

                    _spriteBatch.Draw(
                        brodovi["ship" + cur.Id.ToString()],
                        new Rectangle(xOffset + board_x + (cur.X * board_length) / 10, board_y + (cur.Y * board_length) / 10, width, height),
                        null,
                        Color.White,
                        rotation,
                        origin,
                        SpriteEffects.None,
                        0f
                    );


                }
                // Placement overlay

                if (draggedShip != null)
                {
                    float rotation2 = rotation == 0 ? MathF.PI / 2 : 0f;
                    Vector2 origin = new Vector2(0, 0);

                    origin = new Vector2(0, 0);


                    int width = board_length / 10;
                    int height = (board_length / 10) * draggedShip.Length;

                    int xOffset = 0;

                    if (rotation == 0)
                        xOffset += (int)(draggedShip.Length * board_length / 10);

                    _spriteBatch.Draw(
                        brodovi["ship" + draggedShip.Id.ToString()],
                        new Rectangle((int)(currentMouseState.X)+ xOffset, (int)(currentMouseState.Y), width, height),
                        null,
                        Color.White,
                        rotation2,
                        origin,
                        SpriteEffects.None,
                        0f
                    );
                }



                for (int y = 0; y <= 10; y++)
                {
                    int yPos = board_y + (y * board_length) / 10;
                    _spriteBatch.Draw(tex_white, new Rectangle(board_x, yPos, board_length, 2), Color.Black);
                }

                for (int x = 0; x <= 10; x++)
                {
                    int xPos = board_x + (x * board_length) / 10;
                    _spriteBatch.Draw(tex_white, new Rectangle(xPos, board_y, 2, board_length), Color.Black);
                }
            }
            if (screen == 200 || (screen==400 && endFade<64) || (screen==500 && endFade<64))
            {
                _spriteBatch.Draw(tex_menu, new Rectangle(0, 0, ww, wh), Color.White);
                for (int y = 0; y < 10; y++)
                {
                    for (int x = 0; x < 10; x++)
                    {
                        _spriteBatch.Draw(waterImgs[waterframes_player[y,x]], new Rectangle(board_x + (x * board_length) / 10, board_y + (y * board_length) / 10, board_length / 10, board_length / 10), Color.White);
                    }
                }

                
                // Make water darker
                foreach (Ship cur in playerShips)
                {
                    float rotation = cur.Rotation == 0 ? MathF.PI / 2 : 0f;
                    Vector2 origin = new Vector2(0, 0);

                    origin = new Vector2(0, 0);


                    int width = board_length / 10;
                    int height = (board_length / 10) * cur.Length;

                    int xOffset = 0;

                    if (cur.Rotation == 0)
                        xOffset += (int)(cur.Length * board_length / 10);

                    _spriteBatch.Draw(
                        brodovi["ship" + cur.Id.ToString()],
                        new Rectangle(xOffset+board_x + (cur.X * board_length) / 10, board_y + (cur.Y * board_length) / 10, width, height),
                        null,
                        Color.White,
                        rotation,
                        origin,
                        SpriteEffects.None,
                        0f
                    );


                }
                    
                


                




                for (int y = 0; y < 10; y++)
                {
                    for (int x = 0; x < 10; x++)
                    {
                        // 1. Calculate tile position and size
                        int tileX = gap_between_boards + board_x + ((0 + x) * board_length) / 10;
                        int tileY = board_y + ((0 + y) * board_length) / 10;
                        int tileSize = board_length / 10;


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
                                    new Rectangle(tileX+tileSize, tileY + tileSize, tileSize, tileSize),
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
                            _spriteBatch.Draw(x_img, new Rectangle(board_x + x * board_length / 10, board_y + y * board_length / 10, board_length / 10, board_length / 10), Color.White);
                        }
                        else if (gueses_ai[y, x] == 1)
                        {


                            int sizeDif = 30;
                            Texture2D explosionTex = explosions[explosionCounter / 7];

                            
                            // Full explosion
                            _spriteBatch.Draw(
                                explosionTex,
                                new Rectangle(board_x + x * board_length / 10,
                                                board_y + y * board_length / 10,
                                                board_length / 10,
                                                board_length / 10),
                                Color.White
                            );
                            
                        }

                    }
                }

             
                

                for (int y = 0; y <= 10; y++)
                {
                    int yPos = board_y + (y * board_length) / 10;
                    _spriteBatch.Draw(tex_white, new Rectangle(board_x+gap_between_boards, yPos, board_length, 2), Color.Black);
                }



                for (int x = 0; x <= 10; x++)
                {
                    int xPos = board_x + (x * board_length) / 10;
                    _spriteBatch.Draw(tex_white, new Rectangle(xPos+gap_between_boards, board_y, 2, board_length), Color.Black);
                }
                for (int y = 0; y < 10; y++)
                {
                    for (int x = 0; x < 10; x++)
                    {
                        
                        if (gueses_player[y, x] == 1)
                        {



                            Texture2D explosionTex = explosions[explosionCounter / 7];
                            
                            // Full explosion
                            _spriteBatch.Draw(
                                explosionTex,
                                new Rectangle(gap_between_boards + board_x + x * board_length / 10,
                                                board_y + y * board_length / 10,
                                                board_length / 10 + 6,
                                                board_length / 10),
                                Color.White
                            );
                            
                        }

                    }
                }

                

                for (int y = 0; y <= 10; y++)
                {
                    int yPos = board_y + (y * board_length) / 10;
                    _spriteBatch.Draw(tex_white, new Rectangle(board_x, yPos, board_length, 2), Color.Black);
                }

                for (int x = 0; x <= 10; x++)
                {
                    int xPos = board_x + (x * board_length) / 10;
                    _spriteBatch.Draw(tex_white, new Rectangle(xPos, board_y, 2, board_length), Color.Black);
                }
                heatmapButton.Draw(_spriteBatch, font, ww, wh);

                if (showHeatmap)
                {
                    Color transparentRed = new Color(255, 0, 0, 128);
                    for (int y = 0; y < 10; y++)
                    {
                        for (int x = 0; x < 10; x++)
                        {
                            Color newCol = new Color(100 + (int)(Math.Pow((heatmap[y, x] / 2), 2.2f)), 0, 0, 128);
                            if (bestMove.X == x && bestMove.Y == y)
                            {
                                newCol = new Color(0, 255, 0, 128);
                            }
                            
                            _spriteBatch.Draw(tex_white, new Rectangle(board_x+x*board_length/10, board_y+y*board_length/10, board_length/10, board_length/10), newCol);
                        }
                    }
                }

                for (int i = 0; i < notifications.Count; i++)
                {
                    if (notifications[i] < 32)
                    {
                        Color newcol2 = new Color(255, 255, 255, notifications[i]*8);
                        _spriteBatch.Draw(tex_sunkBanner, new Rectangle(ww / 2 - 230, -30+ notifications[i], 460, 270), newcol2);
                    }
                    else if (notifications[i] < 202 && notifications[i]>150)
                    {
                        Color newcol2 = new Color(255, 255, 255, (int)((182-notifications[i]) * 4.5));
                        _spriteBatch.Draw(tex_sunkBanner, new Rectangle(ww / 2 - 230, (150 - notifications[i])*4, 460, 270), newcol2);
                    }
                    else if (notifications[i] < 150)
                    {
                        _spriteBatch.Draw(tex_sunkBanner, new Rectangle(ww / 2 - 230, 0, 460, 270), Color.White);
                    }
                }

            }


            //_spriteBatch.Draw(_whiteTexture, new Rectangle(100, 100, 200, 100), Color.White);

            if (screen == 400 )
            {
                if (endFade > 0 && endFade < 64)
                {
                    Color newCol = new Color(0, 0, 0, endFade * 4);
                    _spriteBatch.Draw(tex_white, new Rectangle(0, 0, ww, wh), newCol);
                }
                else if (endFade > 64 && endFade < 128)
                {
                    Color newCol = new Color(0, 0, 0, 255-(endFade-64) * 4);
                    _spriteBatch.Draw(tex_DefeatScreen, new Rectangle(0, 0, ww, wh), Color.White);
                    _spriteBatch.Draw(tex_white, new Rectangle(0, 0, ww, wh), newCol);
                }
            }

            if (screen == 500)
            {
                if (endFade > 0 && endFade < 64)
                {
                    Color newCol = new Color(0, 0, 0, endFade * 4);
                    _spriteBatch.Draw(tex_white, new Rectangle(0, 0, ww, wh), newCol);
                }
                else if (endFade > 64 && endFade < 128)
                {
                    Color newCol = new Color(0, 0, 0, 255 - (endFade - 64) * 4);
                    _spriteBatch.Draw(tex_VictoryScreen, new Rectangle(0, 0, ww, wh), Color.White);
                    _spriteBatch.Draw(tex_white, new Rectangle(0, 0, ww, wh), newCol);
                }
            }


            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
