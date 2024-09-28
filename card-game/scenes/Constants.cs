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

	public static class Audio
	{
		public static readonly AudioStream CardsShuffle = GD.Load<AudioStream>("res://assets/audio/cards-shuffle.mp3");
		public static readonly AudioStream HoverSnap = GD.Load<AudioStream>("res://assets/audio/click-balloon-snap.mp3");
	}
}