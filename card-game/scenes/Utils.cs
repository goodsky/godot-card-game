using Godot;

public static class Utils
{
    public static bool IsPointInSprite(Sprite2D sprite, Vector2 globalPosition)
    {
        Vector2 globalSpritePosition = sprite.GlobalPosition - (sprite.Texture.GetSize() * sprite.Scale / 2);
        Vector2 spriteSize = sprite.Texture.GetSize() * sprite.Scale;
        Rect2 globalSpriteRect = new Rect2(globalSpritePosition, spriteSize);
        return globalSpriteRect.HasPoint(globalPosition);
    }
}