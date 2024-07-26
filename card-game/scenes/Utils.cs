using Godot;

public static class Utils
{
    public static bool IsPointInSprite(Sprite2D sprite, Vector2 globalPosition)
    {
        var localPosition = globalPosition - sprite.GlobalPosition;
        return sprite.GetRect().HasPoint(localPosition);
    }
}