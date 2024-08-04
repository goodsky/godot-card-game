using Godot;
using System.Linq;

public partial class Card : Node2D
{
    private bool _isDragging = false;
    private CardZone _activeCardZone = null;

    [Export]
    public CardZone CurrentZone { get; set; }

    public override void _Ready()
    {
        SetProcessInput(false);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_isDragging)
        {
            GlobalPosition = GlobalPosition.Lerp(GetGlobalMousePosition(), 10f * (float)delta);
        }
        else 
        {
            GlobalPosition = GlobalPosition.Lerp(CurrentZone.GlobalPosition, 10f * (float)delta);
        }
    }

    public void _OnArea2DInputEvent(Node viewport, InputEvent inputEvent, int shape_idx)
    {
        if (inputEvent.IsActionPressed("click"))
        {
            SetDragging(true);
        }
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (_isDragging && inputEvent is InputEventMouseButton mouseButtonEvent)
        {
            if (!mouseButtonEvent.Pressed)
            {
                SetDragging(false);
            }
        }

        if (_isDragging && inputEvent is InputEventMouseMotion mouseMotionEvent)
        {
            var activeCardZones = GetTree()
                .GetNodesInGroup(Constants.HoveredCardZoneGroup)
                .Where(node => node is CardZone)
                .Select(node => node as CardZone)
                .ToArray();

            if (activeCardZones.Length == 0)
            {
                _activeCardZone = null;
            }
            else if (activeCardZones.Length == 1)
            {
                _activeCardZone = activeCardZones[0];
            }
            else if (activeCardZones.Length > 1)
            {
                // TODO: Will this ever happen?
                GD.Print("Multiple Active Card Zones: ", activeCardZones.Select(node => node.Name));
                _activeCardZone = activeCardZones[0];
            }
        }
    }

    private void SetDragging(bool isDragging)
    {
        if (_isDragging && !isDragging && _activeCardZone != null)
        {
            CurrentZone = _activeCardZone;
            _activeCardZone = null;
        }

        _isDragging = isDragging;
        SetProcessInput(isDragging);
    }
}
