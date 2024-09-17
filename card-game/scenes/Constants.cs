using Godot;

public static class Constants
{
	public static readonly string ClickEventName = "click";
	public static readonly string RightClickEventName = "cancel_click";
	public static readonly string EscEventName = "ui_cancel";

	public static readonly string StarterDeckResourcePath = "user://decks/test.cards.json";

	public static readonly PackedScene CardScene = GD.Load<PackedScene>("res://scenes/game/Card.tscn");
	public static readonly PackedScene MainGameScene = GD.Load<PackedScene>("res://scenes/game/MainGame.tscn");
	public static readonly PackedScene MainMenuScene = GD.Load<PackedScene>("res://scenes/menu/MainMenu.tscn");

	public static readonly string ErrorAvatarPath = "res://assets/sprites/avatars/avatar_blue_monster_00.jpeg";

	public static readonly Texture2D[] CardAvatars = new Texture2D[]
	{
		ResourceLoader.Load<CompressedTexture2D>("res://assets/sprites/avatars/avatar_blue_monster_00.jpeg"),
		ResourceLoader.Load<CompressedTexture2D>("res://assets/sprites/avatars/avatar_blue_monster_01.jpeg"),
		ResourceLoader.Load<CompressedTexture2D>("res://assets/sprites/avatars/avatar_blue_monster_02.jpeg"),
	};
}