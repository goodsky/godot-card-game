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
		public static readonly AudioStream BalloonSnap = GD.Load<AudioStream>("res://assets/audio/click-balloon-snap.mp3");
		public static readonly AudioStream ClickSnap = GD.Load<AudioStream>("res://assets/audio/click-loose.mp3");
		public static readonly AudioStream CardsShuffle = GD.Load<AudioStream>("res://assets/audio/cards-shuffle-short.wav");
		public static readonly AudioStream PlayCardClick = GD.Load<AudioStream>("res://assets/audio/click-slide-click.mp3");
		public static readonly AudioStream ProposeCardClick = GD.Load<AudioStream>("res://assets/audio/switch-11.ogg");
		public static readonly AudioStream ProposeSacrificeClick = GD.Load<AudioStream>("res://assets/audio/switch-32.ogg");
		public static readonly AudioStream DamageCard_Low = GD.Load<AudioStream>("res://assets/audio/hit-card-1.wav");
		public static readonly AudioStream DamageCard_High = GD.Load<AudioStream>("res://assets/audio/hit-card-2.wav");
		public static readonly AudioStream DamagePlayer_Low = GD.Load<AudioStream>("res://assets/audio/hit-player-1.wav");
		public static readonly AudioStream DamagePlayer_High = GD.Load<AudioStream>("res://assets/audio/hit-player-2.wav");
		public static readonly AudioStream Heartbeat = GD.Load<AudioStream>("res://assets/audio/heartbeat-slow.ogg");
		public static readonly AudioStream KillCard = GD.Load<AudioStream>("res://assets/audio/kill-card.wav");
		public static readonly AudioStream TurnEnd = GD.Load<AudioStream>("res://assets/audio/typewriter-bell.mp3");
		public static readonly AudioStream[] Whoosh = new[] {
			GD.Load<AudioStream>("res://assets/audio/whoosh-bamboo.mp3"),
			GD.Load<AudioStream>("res://assets/audio/whoosh-bat.mp3"),
			GD.Load<AudioStream>("res://assets/audio/whoosh-swipe.mp3"),
		};
		public static readonly AudioStream GameOver_Lose = GD.Load<AudioStream>("res://assets/audio/lose.wav");
		public static readonly AudioStream GameOver_Win = GD.Load<AudioStream>("res://assets/audio/win.wav");
	}
}