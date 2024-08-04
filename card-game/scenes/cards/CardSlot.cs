using Godot;

/**
 * Represents a Position & Card.
 */
public abstract partial class CardSlot : Node2D
{
    [Export]
    public Card Card = null;

    public override void _Ready()
    {
        if (Card != null)
        {
            Card.SetCardSlot(this);
        }
    }
}