using Godot;
using System;

public partial class Card : Node2D
{
    private bool _isDragging = false;
    private CardZone _hoveringCardZone = null;
    
    [Export]
    public Sprite2D Sprite { get; set; }

    [Export]
    public CardZone CurrentZone { get; set; }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        if (CurrentZone != null)
        {
            Position = CurrentZone.Position;
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton mouseButtonEvent && Utils.IsPointInSprite(Sprite, mouseButtonEvent.GlobalPosition))
        {
            if (mouseButtonEvent.ButtonIndex == MouseButton.Left)
            {
                if (_isDragging && !mouseButtonEvent.Pressed)
                {
                    OnMouseRelease();
                }

                _isDragging = mouseButtonEvent.Pressed;
            }
        }

        if (_isDragging && inputEvent is InputEventMouseMotion mouseMotionEvent)
        {
            GlobalPosition = mouseMotionEvent.Position;

            var curHoveringCardZone = GetMouseOverCardZone();
            if (_hoveringCardZone != null && curHoveringCardZone != _hoveringCardZone)
            {
                _hoveringCardZone.HoverOut();
            }

            if (curHoveringCardZone != null && _hoveringCardZone == null)
            {
                curHoveringCardZone.HoverOver();
            }

            _hoveringCardZone = curHoveringCardZone;
        }
    }

    private CardZone GetMouseOverCardZone()
    {
        var cardZones = GetTree().GetNodesInGroup(Constants.CardZoneGroup);
        foreach (var node in cardZones)
        {
            if (node is CardZone cardZone)
            {
                GD.Print("CardZone Rect:", cardZone.DefaultSprite.GetRect());
                GD.Print("LocalMouse:", GetGlobalMousePosition());
                if (Utils.IsPointInSprite(cardZone.DefaultSprite, GetGlobalMousePosition()))
                {
                    return cardZone;
                }
            }
        }

        return null;
    }

    private void OnMouseRelease()
    {
        if (_hoveringCardZone != null)
        {
            _hoveringCardZone.HoverOut();
            CurrentZone = _hoveringCardZone;
        }

        Position = CurrentZone.Position;
    }
}
