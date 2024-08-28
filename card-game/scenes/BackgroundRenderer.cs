
using Godot;

[Tool]
public partial class BackgroundRenderer : Node2D
{
    private Vector2 _size;
    private Color _color = Colors.Aquamarine;

    [Export]
    public Vector2 Size
    {
        get
        {
            return _size;
        }

        set
        {
            _size = value;
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
        DrawRect(new Rect2(Vector2.Zero, _size), _color);
    }
}