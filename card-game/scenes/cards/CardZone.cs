using Godot;

public partial class CardZone : Node2D
{
    private bool _isMouseOver = false;

    [Export]
    public Sprite2D DefaultSprite { get; set; }

    [Export]
    public Sprite2D SelectedSprite { get; set; }

    public override void _Ready()
    {
        SetProcessInput(false);
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (_isMouseOver &&
            inputEvent is InputEventMouseButton mouseButtonEvent &&
            mouseButtonEvent.ButtonIndex == MouseButton.Left &&
            !mouseButtonEvent.Pressed)
        {
            OnMouseExit();
        }
    }

    public void OnMouseEnter()
    {
        _isMouseOver = true;
        SetProcessInput(true);
        AddToGroup(Constants.HoveredCardZoneGroup);

        if (DefaultSprite != null)
        {
            DefaultSprite.Visible = false;
        }

        if (SelectedSprite != null)
        {
            SelectedSprite.Visible = true;
        }
    }

    public void OnMouseExit()
    {
        _isMouseOver = false;
        SetProcessInput(false);
        RemoveFromGroup(Constants.HoveredCardZoneGroup);

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
