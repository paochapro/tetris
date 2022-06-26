using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

using static Tetris.Utils;

namespace Tetris;

static class Utils
{
    static public void print(params object[] args)
    {
        foreach (object var in args)
            Console.Write(var + " ");
        Console.WriteLine();
    }
    static public int clamp(int value, int min, int max)
    {
        if (value > max) value = max;
        if (value < min) value = min;
        return value;
    }
    static public Point ToFloorPoint(this Vector2 vect)
    {
        return new Point((int)Math.Floor(vect.X), (int)Math.Floor(vect.Y));
    }
    static public float get_center(float x, float x2, float width)
    {
        return (x + x2) / 2 - width / 2;
    }
}

struct ColorRectangle
{
    public Rectangle rect = Rectangle.Empty;
    public Color color = Color.Gray;
    public ColorRectangle(Rectangle rect) => this.rect = rect;
    public ColorRectangle(Rectangle rect, Color color) : this(rect) => this.color = color;
    public ColorRectangle() { }
}

//Events
class Event
{
    double delay;
    double startTime;
    static double globalTime;
    Action function;

    public Event(Action function, double delay)
    {
        this.delay = delay;
        this.function = function;
        startTime = globalTime;
    }

    static List<Event> events = new();

    static public void Add(Action func, double delay) => events.Add(new Event(func, delay));

    static public void ExecuteEvents(GameTime gameTime)
    {
        for (int i = 0; i < events.Count; ++i)
        {
            Event ev = events[i];
            if ((globalTime - ev.startTime) > ev.delay)
            {
                ev.function.Invoke();
                events.Remove(ev);
                --i;
            }
        }

        globalTime += gameTime.ElapsedGameTime.TotalSeconds;
    }
    
    static public void ClearEvents() => events.Clear();
}

//Ui
abstract class UiElement
{
    //Static
    static List<UiElement> elements = new();
    static bool clicking;
    public static bool Clicking => clicking;

    //Element
    protected Rectangle rect = Rectangle.Empty;
    string text;

    bool locked = false;

    public bool Locked 
    {
        get => locked;
        set
        {
            locked = value;
            if(locked) texture = darkTexture;
        }
    }

    protected Texture2D texture;
    protected Texture2D litTexture;
    protected Texture2D darkTexture;
    protected Texture2D normalTexture;

    public abstract void Activate();

    protected virtual void Update(MouseState mouse)
    {
        texture = normalTexture;

        if (rect.Contains(mouse.Position) && !clicking)
        {
            Mouse.SetCursor(MouseCursor.Hand);
            texture = litTexture;

            if (mouse.LeftButton == ButtonState.Pressed)
                Activate();
        }
    }

    protected virtual void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(texture, rect, Color.White);
    }

    protected readonly int layer = 0;
    private static int CurrentLayer { get; set; }

    protected UiElement(Rectangle rect, string text, int layer, Texture2D texture)
    {
        this.rect = rect;
        this.text = text;
        this.layer = layer;

        this.normalTexture = texture;
        this.texture = normalTexture;

        LitTexture();
    }
    protected void LitTexture()
    {
        int w = texture.Width;
        int h = texture.Height;
        int litValue = 30;
        int darkValue = 80;
        Color[] colorData = new Color[w * h];

        //Lit texture
        litTexture = new Texture2D(MyGame.Graphics.GraphicsDevice, w, h);
        normalTexture.GetData(colorData);

        for (int i = 0; i < colorData.Length; ++i)
        {
            colorData[i].R = (byte)clamp(colorData[i].R + litValue, 0, 255);
            colorData[i].G = (byte)clamp(colorData[i].G + litValue, 0, 255);
            colorData[i].B = (byte)clamp(colorData[i].B + litValue, 0, 255);
        }
        litTexture.SetData(colorData);

        //Dark texture
        darkTexture = new Texture2D(MyGame.Graphics.GraphicsDevice, w, h);
        normalTexture.GetData(colorData);

        for (int i = 0; i < colorData.Length; ++i)
        {
            colorData[i].R = (byte)clamp(colorData[i].R - darkValue, 0, 255);
            colorData[i].G = (byte)clamp(colorData[i].G - darkValue, 0, 255);
            colorData[i].B = (byte)clamp(colorData[i].B - darkValue, 0, 255);
        }
        darkTexture.SetData(colorData);
    }

    static public void UpdateElements(MouseState mouse)
    {
        Mouse.SetCursor(MouseCursor.Arrow);

        foreach (UiElement element in elements)
            if (element.layer == CurrentLayer && !element.locked)
                element.Update(mouse);

        clicking = (mouse.LeftButton == ButtonState.Pressed);
    }
    static public void DrawElements(SpriteBatch spriteBatch)
    {
        foreach (UiElement element in elements)
            if (element.layer == CurrentLayer)
                element.Draw(spriteBatch);
    }
    static public T AddElement<T>(T elem) where T : UiElement
    {
        elements.Add(elem);
        return (T)elements.Last();
    }
}

//Button
class Button : UiElement
{
    public static SpriteFont? Font { get; set; } = null;
    static public int currentLayer { get; set; }

    event Action func;

    public Button(Rectangle rect, Action func, string text, int layer, Texture2D texture)
        : base(rect,text,layer,texture)
    {
        this.func = func;
    }
    static public Button Add(Rectangle rect, Action func, string text, int layer, Texture2D texture)
    {
        return AddElement(new Button(rect,func,text,layer,texture));
    }

    public override void Activate() => func.Invoke();
}

class CheckBox : UiElement
{
    bool isChecked = false;
    bool IsChecked => isChecked;

    Texture2D checkedTexture;
    Texture2D uncheckedTexture;

    event Action act1;
    event Action act2;

    public CheckBox(Rectangle rect, Action act1, Action act2, string text, int layer, Texture2D uncheckedTexture, Texture2D checkedTexture)
        : base(rect, text, layer, uncheckedTexture)
    {
        this.checkedTexture = checkedTexture;
        this.uncheckedTexture = uncheckedTexture;
        this.act1 = act1;
        this.act2 = act2;
    }
    static public CheckBox Add(Rectangle rect, Action act1, Action act2, string text, int layer, Texture2D uncheckedTexture, Texture2D checkedTexture)
    {
        return AddElement(new CheckBox(rect, act1, act2, text, layer, uncheckedTexture, checkedTexture));
    }

    public override void Activate()
    {
        isChecked = !isChecked;

        if (isChecked)
        {
            act1.Invoke();
            normalTexture = checkedTexture;
        }
        else
        {
            act2.Invoke();
            normalTexture = uncheckedTexture;
        }

        LitTexture();
    }
}