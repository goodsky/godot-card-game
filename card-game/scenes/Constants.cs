using Godot;

public static class Constants
{
	public static readonly string ClickEventName = "click";

	public static readonly PackedScene CardScene = GD.Load<PackedScene>("res://scenes/Card.tscn");

	public static readonly Texture2D[] CardAvatars = new Texture2D[]
	{
		ResourceLoader.Load<CompressedTexture2D>("res://assets/sprites/avatars/avatar_blue_monster_00.jpeg"),
		ResourceLoader.Load<CompressedTexture2D>("res://assets/sprites/avatars/avatar_blue_monster_01.jpeg"),
		ResourceLoader.Load<CompressedTexture2D>("res://assets/sprites/avatars/avatar_blue_monster_02.jpeg"),
	};
}