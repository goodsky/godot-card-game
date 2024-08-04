using Godot;
using System.Linq;

public partial class Card : Node2D
{
    private bool _isDragging = false;

    // While dragging, this tracks if there is an active card slot available for a drop.
    private CardSlot _activeCardSlot = null;

    // When not dragging, the target slot for this card to live at.
    private CardSlot _homeCardSlot { get; set; }

    public override void _Ready()
    {
        SetProcessInput(false);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_isDragging)
        {
            GlobalPosition = GlobalPosition.Lerp(GetGlobalMousePosition(), 15f * (float)delta);
        }
        else if (_homeCardSlot != null)
        {
            GlobalPosition = GlobalPosition.Lerp(_homeCardSlot.GlobalPosition, 10f * (float)delta);
        }
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (_isDragging && inputEvent is InputEventMouseButton mouseButtonEvent &&
            !mouseButtonEvent.Pressed)
        {
            StopDragging();
        }

        if (_isDragging && inputEvent is InputEventMouseMotion mouseMotionEvent)
        {
            var activeCardSlots = GetTree()
                .GetNodesInGroup(Constants.ActiveCardSlotGroup)
                .Where(node => node is CardSlot)
                .Select(node => node as CardSlot)
                .ToArray();

            if (activeCardSlots.Length == 0)
            {
                _activeCardSlot = null;
            }
            else if (activeCardSlots.Length == 1)
            {
                _activeCardSlot = activeCardSlots[0];
            }
            else if (activeCardSlots.Length > 1)
            {
                // TODO: Will this ever happen?
                GD.Print("Multiple Active Card Slots: ", activeCardSlots.Select(node => node.Name));
                _activeCardSlot = activeCardSlots[0];
            }
        }
    }

    public void SetCardSlot(CardSlot cardSlot)
    {
        _homeCardSlot = cardSlot;
    }

    public void StartDragging()
    {
        _isDragging = true;
        SetProcessInput(true);
    }

    public void StopDragging()
    {
        if (_activeCardSlot != null)
        {
            if (_homeCardSlot != null)
            {
                _homeCardSlot.Card = null;
            }

            _homeCardSlot = _activeCardSlot;
            _homeCardSlot.Card = this;
            _activeCardSlot = null;
        }

        _isDragging = false;
        SetProcess(false);
    }
}
