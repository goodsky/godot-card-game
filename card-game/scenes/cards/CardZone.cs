using Godot;

public partial class CardZone : Node2D
{
    private bool _isHoverOVer = false;

    [Export]
    public Sprite2D DefaultSprite { get; set; }

    [Export]
    public Sprite2D SelectedSprite { get; set; }

    public override void _Input(InputEvent inputEvent)
    {
        if (_isHoverOVer &&
            inputEvent is InputEventMouseButton mouseButtonEvent &&
            mouseButtonEvent.ButtonIndex == MouseButton.Left &&
            !mouseButtonEvent.Pressed)
        {
            this.HoverOut();
        }
    }

    public void HoverOver()
    {
        _isHoverOVer = true;

        if (DefaultSprite != null)
        {
            DefaultSprite.Visible = false;
        }

        if (SelectedSprite != null)
        {
            SelectedSprite.Visible = true;
        }
    }

    public void HoverOut()
    {
        _isHoverOVer = false;

        if (DefaultSprite != null)
        {
            DefaultSprite.Visible = true;
        }

        if (SelectedSprite != null)
        {
            SelectedSprite.Visible = false;
        }
    }
}
