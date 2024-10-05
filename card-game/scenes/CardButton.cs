using Godot;

public partial class CardButton : BaseButton
{
    public CardInfo? Info { get; private set; }

    [Export]
    private CardVisual Visual;

    [Export]
    public bool ShowCardBack { get; set; }

    public override void _Ready()
    {
        MouseEntered += MouseEnter;
        MouseExited += MouseExit;
    }

    public void SetDisabled(bool isDisabled)
    {
        Disabled = isDisabled;
        Visual.Modulate = isDisabled ? new Color(1, 1, 1, 0.5f) : new Color(1, 1, 1, 1);
        Visual.SetHighlight(false);
    }

    public void SetCard(CardInfo? cardInfo)
    {
        Info = cardInfo;
        if (cardInfo.HasValue)
        {
            Visual.Visible = true;
            Visual.Update(cardInfo.Value, firstUpdate: true);
            Visual.ShowCardBack(ShowCardBack);
        }
        else
        {
            Visual.Visible = false;
        }
    }

    private void MouseEnter()
    {
        if (!Disabled)
        {
            Visual.SetHighlight(true);
        }
    }

    private void MouseExit()
    {
        Visual.SetHighlight(false);
    }
}