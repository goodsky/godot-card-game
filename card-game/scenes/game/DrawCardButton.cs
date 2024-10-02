using Godot;

public partial class DrawCardButton : BaseButton
{
    private Card _card;

    [Export]
    public bool DrawFaceUp { get; set; }

    public override void _Ready()
    {
        _card = Constants.CardScene.Instantiate<Card>();
        _card.SetAnimationControl(true); // We control this card
        _card.ShowCardBack(true);
        AddChild(_card);
        _card.Position = new Vector2(CustomMinimumSize.X / 2, CustomMinimumSize.Y / 2);
    }

    public void SetDisabled(bool isDisabled)
    {
        Disabled = isDisabled;
        _card.Modulate = isDisabled ? new Color(1, 1, 1, 0.5f) : new Color(1, 1, 1, 1);
    }

    public void SetTopCard(CardInfo? cardInfo)
    {
        if (cardInfo.HasValue)
        {
            _card.Visible = true;
            _card.SetCardInfo(cardInfo.Value);
            _card.ShowCardBack(!DrawFaceUp);
        }
        else
        {
            _card.Visible = false;
        }
    }
}