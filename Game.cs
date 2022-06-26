using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using MonoGame.Extended;

namespace Tetris;

using static Utils;

//////////////////////////////// Starting point
partial class MyGame : Game
{
    //Main stuff
    int step;
    int nextStep = 12;
    int inputRefresh = inputRefreshWait;

    const int inputRefreshWait = 3;
    const int fastNextStep = 3;
    const int normalNextStep = 20;

    static int score = 0;
    int highScore = 0;
    const int addScore = 100;
    string score_path = @"Content\scores.txt";

    bool start = false;
    Texture2D squareTexture;

    //Sounds
    SoundEffectInstance snd_line;
    SoundEffectInstance snd_rotate;
    SoundEffectInstance snd_move;
    SoundEffectInstance snd_drop;
    SoundEffectInstance snd_lose;

    const float defaultVolume = 0.3f;

    //Pieces
    Piece? movingPiece = null;
    Piece? shadow = null;
    Piece? nextPiece = null;

    readonly Point nextPiecePos = new(11, 9);
    Piece.PieceType randType = Piece.PieceType.max;

    //More important stuff
    public static readonly Point screenSize = new(512, 640);
    const string gameName = "Tetris";

    //Stuff
    static public GraphicsDeviceManager Graphics => graphics;
    static GraphicsDeviceManager graphics;
    SpriteBatch spriteBatch;
    public static MouseState mouse { get => Mouse.GetState(); }
    public static KeyboardState keys { get => Keyboard.GetState(); }

    //Buttons
    const int buttonSize = 48;
    const int buttonY = 100;
    static readonly int buttonX = (int)get_center(512 - 192, 512, buttonSize);

    readonly int resetRectX = buttonX - buttonSize + 16;
    readonly int soundRectX = buttonX + buttonSize - 16;

    static bool pressingSpace = false;
    static bool pressingR = false;
    static bool pressingC = false;
    bool isRotating = false;
    static public bool clicking { get; private set; }
    public static bool HoldingLeft => clicking;

    CheckBox soundButton;


    private void GenerateRandType()
    {
        randType = (Piece.PieceType)(new Random(DateTime.Now.Millisecond).Next((int)Piece.PieceType.max));
        nextPiece = new Piece(randType, nextPiecePos);

        Point addition = new Point(16,16);

        if(randType == Piece.PieceType.o) addition = new Point(32, 16);
        if(randType == Piece.PieceType.l) addition = new Point(0, 0);

        for (int i=0; i < nextPiece.Rects.Length;  ++i)
            nextPiece.Rects[i].Location += addition;
    }

    private void NewPiece()
    {
        movingPiece = new Piece(randType, new Point(4,0));
        UpdateShadow();
        GenerateRandType();
    }

    private void UpdateShadow()
    {
        shadow = (Piece)movingPiece.Clone();
        shadow.Color = new Color(40,40,40, 150);

        while (true)
        {
            if (shadow.MoveDown() != 0)
                break;
        }
    }

    private int Move()
    {
        step = 0;

        int collision = movingPiece.MoveDown();

        if(collision != 0)
        {
            snd_drop.Stop();
            snd_drop.Play();
        }

        if (collision == 1)
        {
            if(Board.UpdateBoard(movingPiece.SquarePos, movingPiece.Color))
            {
                snd_line.Stop();
                snd_line.Play();
            }
            NewPiece();
        }
            
        if (collision == 2)
        {
            snd_lose.Play();
            Console.WriteLine("Game over");
            Reset();
        }

        return collision;
    }

    //Main
    protected override void Update(GameTime gameTime)
    {
        //Exit
        if (keys.IsKeyDown(Keys.Escape)) Exit();

        if (start)
        {
            if (step >= nextStep)
                Move();

            step++;
        }
        Controls();
        Debug();

        UiElement.UpdateElements(mouse);
        Event.ExecuteEvents(gameTime);

        //Keyboard, mouse
        clicking = (mouse.LeftButton == ButtonState.Pressed);
        pressingSpace = keys.IsKeyDown(Keys.Space);
        pressingR = keys.IsKeyDown(Keys.R);
        pressingC = keys.IsKeyDown(Keys.C);

        //isRotating = keys.IsKeyDown(Keys.Up) || keys.IsKeyDown(Keys.Z);

        base.Update(gameTime);
    }
    static public void AddScore() => score += addScore;

    private void Debug()
    {
    }

    private void Controls()
    {
        //Resetting
        if (keys.IsKeyDown(Keys.R) && !pressingR)
            Reset();

        if (keys.IsKeyDown(Keys.C) && !pressingC)
            soundButton.Activate();

        if (!start) return;

        //Horizontal move
        int horizontalMove = Convert.ToInt32(keys.IsKeyDown(Keys.Right)) - Convert.ToInt32(keys.IsKeyDown(Keys.Left));

        if (inputRefresh <= 0)
        {
            if(movingPiece.HorizontalMove(horizontalMove))
            {
                UpdateShadow();
                snd_move.Stop();
                snd_move.Play();
            }
        }

        if (horizontalMove == 0)
            inputRefresh = 0;
        else
        {
            if(inputRefresh <= 0)
                inputRefresh = inputRefreshWait;
        }
        inputRefresh--;

        //Soft drop
        if (keys.IsKeyDown(Keys.Down)) nextStep = fastNextStep;
        else nextStep = normalNextStep;

        //Hard drop
        if (keys.IsKeyDown(Keys.Space) && !pressingSpace)
        {
            while (true)
                if (Move() != 0)
                    break;
        }

        //Rotation
        int rotation = Convert.ToInt32(keys.IsKeyDown(Keys.Up)) - Convert.ToInt32(keys.IsKeyDown(Keys.Z));

        if (rotation != 0 && !isRotating)
        {
            movingPiece.Rotate(rotation);
            isRotating = true;
            UpdateShadow();
            snd_rotate.Stop();
            snd_rotate.Play();
        }
        isRotating = rotation != 0 ? true : false;
    }
    private void WriteScore()
    {
        if (!File.Exists(score_path))
        {
            Console.WriteLine("File wasn't found! WriteScore()");
            return;
        }

        if (score <= highScore) return;

        highScore = score;
        File.WriteAllText(score_path, highScore.ToString());
    }
    private void DrawUI(SpriteBatch spriteBatch)
    {
        //Grid
        Color gridColor = new Color(20,20,20);

        for (int y = 0; y < Board.boardY * 32; y += Piece.squareSize)
            spriteBatch.DrawLine(0, y, Board.boardX * 32, y, gridColor, 1);

        for (int x = 0; x < Board.boardX * 32; x += Piece.squareSize)
            spriteBatch.DrawLine(x, 0, x, Board.boardY * 32, gridColor, 1);

        //End of grid
        spriteBatch.DrawLine(Board.boardX * 32+2, 0, Board.boardX * 32+2, Board.boardY * 32, gridColor, 3);

        //Left side
        const int scoreTextY = 430;
        const int nextPieceY = 250;
        const int highScoreY = scoreTextY + 30 + 50;

        var drawSideText = (string text, float y) =>
        {
            Vector2 measure = Button.Font.MeasureString(text);
            Vector2 pos = new Vector2(get_center(512-192,512, measure.X), y);
            spriteBatch.DrawString(Button.Font, text, pos, Color.White);
        };

        drawSideText("Score:", scoreTextY);
        drawSideText(score.ToString(), scoreTextY + 30);
        drawSideText("Next Piece:", nextPieceY);
        drawSideText("High Score:", highScoreY);
        drawSideText(highScore.ToString(), highScoreY + 30);

        int keysY = buttonY + buttonSize + 4;
        Color keysColor = new Color(50, 50, 50);
        spriteBatch.DrawString(Button.Font, "R", new Vector2(resetRectX, keysY), keysColor);
        spriteBatch.DrawString(Button.Font, "C", new Vector2(soundRectX, keysY), keysColor);
    }

    protected override void Draw(GameTime gameTime)
    {
        graphics.GraphicsDevice.Clear(Color.Black);

        spriteBatch.Begin();
        {
            DrawUI(spriteBatch);

            if(start)
            {
                shadow.Draw(spriteBatch, squareTexture);
                movingPiece.Draw(spriteBatch, squareTexture);
            }
            nextPiece.Draw(spriteBatch, squareTexture);

            Board.DrawPieces(spriteBatch, squareTexture);
            UiElement.DrawElements(spriteBatch);
        }
        spriteBatch.End();

        base.Draw(gameTime);
    }

    protected override void LoadContent()
    {
        spriteBatch = new SpriteBatch(GraphicsDevice);

        Button.Font = Content.Load<SpriteFont>("bahnschrift");
        if (Button.Font == null) Console.WriteLine("null font");

        squareTexture = Content.Load<Texture2D>("square");

        //Sounds
        snd_line = Content.Load<SoundEffect>("line").CreateInstance();
        snd_line.Volume = 0.3f;

        snd_rotate = Content.Load<SoundEffect>("rotate").CreateInstance();
        snd_rotate.Volume = 0.3f;

        snd_move = Content.Load<SoundEffect>("piece_move").CreateInstance();
        snd_move.Volume = 0.2f;

        snd_drop = Content.Load<SoundEffect>("drop").CreateInstance();
        snd_drop.Volume = 0.05f;

        snd_lose = Content.Load<SoundEffect>("lose").CreateInstance();
        snd_lose.Volume = 0.1f;
    }
    private void CreateButtons()
    {
        Rectangle resetRect = new Rectangle(resetRectX, buttonY, buttonSize, buttonSize);
        Rectangle soundRect = new Rectangle(soundRectX, buttonY, buttonSize, buttonSize);

        Button.Add(resetRect, Reset, "reset", 0, Content.Load<Texture2D>("reset48"));

        var volume0 = () => { SoundEffect.MasterVolume = 0; };
        var volumeDef = () => { SoundEffect.MasterVolume = defaultVolume; };

        soundButton = CheckBox.Add(soundRect, volume0, volumeDef, "sound", 0, Content.Load<Texture2D>("sound48"), Content.Load<Texture2D>("no_sound48"));
    }
    //Setups
    protected override void Initialize()
    {
        Window.AllowUserResizing = false;
        Window.Title = gameName;
        IsMouseVisible = true;
        graphics.PreferredBackBufferWidth = screenSize.X;
        graphics.PreferredBackBufferHeight = screenSize.Y;
        graphics.ApplyChanges();

        SoundEffect.MasterVolume = defaultVolume;

        //Create buttons
        CreateButtons();

        //Load high score
        if (!File.Exists(score_path))
            Console.Write("File wasn't found! Initialize()");
        else
        {
            try
            {
                highScore = Convert.ToInt32(File.ReadAllText(score_path));
            }
            catch
            {
                Console.WriteLine("Failed to convert to int. Setting high score as 0. Initialize()");
                File.WriteAllText(score_path, 0.ToString());
                highScore = 0;
            }
        }

        //Reset
        Reset();

        base.Initialize();
    }
    private void Reset()
    {
        Event.ClearEvents();

        start = false;
        WriteScore();
        Board.ResetBoard();
        score = 0;
        step = 0;

        GenerateRandType();
        Event.Add(NewPiece, 1f);
        Event.Add(() => start = true, 1.1f);
    }
    public MyGame() : base()
    {
        graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
    }
}

class Program
{
    public static void Main()
    {
        using (MyGame game = new MyGame())
        {
            game.Run();
        }
    }
}