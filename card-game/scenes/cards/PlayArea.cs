using Godot;

public partial class PlayArea : CardSlot
{
    private bool _isHoverOver = false;

    [Export]
    public Sprite2D DefaultSprite { get; set; }

    [Export]
    public Sprite2D SelectedSprite { get; set; }

    public void OnArea2DInputEvent(Node viewport, InputEvent inputEvent, int shape_idx)
    {
        if (Card != null && inputEvent.IsActionPressed("click"))
        {
            Card.StartDragging();
        }
    }

    public void HoverOver()
    {
        _isHoverOver = true;
        AddToGroup(Constants.ActiveCardSlotGroup);

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
        _isHoverOver = false;
        RemoveFromGroup(Constants.ActiveCardSlotGroup);

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
