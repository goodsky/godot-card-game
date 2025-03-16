using Godot;

public partial class CardButton : BaseButton
{
    private static (Control Tooltip, CardButton Owner)? ActiveAbilityTooltip = null;

    public CardInfo? Info { get; private set; }

    [Export]
    private CardVisual Visual;

    [Export]
    public bool ShowCardBack { get; set; }

    [Export]
    public bool ShowTooltip { get; set; }

    public override void _Ready()
    {
        MouseEntered += MouseEnter;
        MouseExited += MouseExit;
    }

    public void SetDisabled(bool isDisabled, bool fade = true)
    {
        Disabled = isDisabled;
        if (fade)
        {
            Visual.Modulate = isDisabled ? new Color(1, 1, 1, 0.5f) : new Color(1, 1, 1, 1);
        }
        Visual.SetHighlight(false);
    }

    public void SetCard(CardInfo? cardInfo)
    {
        Info = cardInfo;
        if (cardInfo.HasValue)
        {
            Visual.Visible = true;
            Visual.Update(cardInfo.Value);
            Visual.ShowCardBack(ShowCardBack);
        }
        else
        {
            Visual.Visible = false;
        }
    }

    private void MouseEnter()
    {
        PopUpTooltip();
        if (!Disabled)
        {
            Visual.SetHighlight(true);
        }
    }

    private void MouseExit()
    {
        HideTooltip();
        Visual.SetHighlight(false);
    }

    private void PopUpTooltip()
    {
        if (!ShowTooltip)
        {
            return;
        }

        if (Info == null)
        {
            GD.Print("No card info available for tooltip.");
            return;
        }
        
        if (ActiveAbilityTooltip != null)
        {
            ActiveAbilityTooltip.Value.Tooltip.QueueFree();
        }

        Control tooltipNode = Constants.Tooltip.Instantiate<Control>();
        ActiveAbilityTooltip = (tooltipNode, this);

        RichTextLabel tooltipLabel = tooltipNode.FindChild("TooltipLabel") as RichTextLabel;
        GD.Print($"Tooltip height1: {tooltipLabel.GetSize().Y}");

        SceneLoader.Instance.AddChild(tooltipNode);
        tooltipNode.ZIndex = 1000;
        tooltipLabel.Text = Info?.GetCardSummary() ?? string.Empty;
        tooltipLabel.FitContent = true;
        tooltipLabel.QueueRedraw();
        GD.Print($"Tooltip height2: {tooltipLabel.GetSize().Y}");
        
        const int CARD_WIDTH = 100;
        const int TOOLTIP_WIDTH = 200;
        float viewportWidth = GetViewport().GetVisibleRect().Size.X;
        if (GlobalPosition.X + CARD_WIDTH + TOOLTIP_WIDTH > viewportWidth)
        {
            tooltipNode.GlobalPosition = GlobalPosition - new Vector2(TOOLTIP_WIDTH, 0);
        }
        else
        {
            tooltipNode.GlobalPosition = GlobalPosition + new Vector2(CARD_WIDTH, 0);
        }
        GD.Print($"Tooltip height3: {tooltipLabel.GetSize().Y}");
    }

    private void HideTooltip()
    {
        if (ActiveAbilityTooltip != null && ActiveAbilityTooltip.Value.Owner == this)
        {
            ActiveAbilityTooltip.Value.Tooltip.QueueFree();
            ActiveAbilityTooltip = null;
        }
    }
}