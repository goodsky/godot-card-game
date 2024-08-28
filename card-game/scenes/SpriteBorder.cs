
using Godot;

[Tool]
public partial class SpriteBorder : Sprite2D
{
    private int _width = 2;
    private Color _color = Colors.Black;

    [Export]
    public int Width
    {
        get
        {
            return _width;
        }

        set
        {
            _width = value;
            QueueRedraw();
        }
    }

    [Export]
    public Color Color
    {
        get
        {
            return _color;
        }

        set
        {
            _color = value;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        base._Draw();
        Vector2 size = Texture.GetSize();
        Vector2 pos = Centered ? new Vector2(Offset.X - size.X / 2, Offset.Y - size.Y / 2) : new Vector2(Offset.X, Offset.Y);
        DrawRect(new Rect2(pos, size), _color, filled: false, width: _width);
    }
}