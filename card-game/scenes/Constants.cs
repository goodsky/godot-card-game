using Godot;

public static class Constants
{
	public static readonly string ClickEventName = "click";
	public static readonly string RightClickEventName = "cancel_click";
	public static readonly string TestBenchEventName = "testbench_hotkey";
	public static readonly string IsaacModeEventName = "isaac_mode_hotkey";
	public static readonly string EscEventName = "ui_cancel";

	public static readonly string GameDeckDirectory = "res://cards";
	public static readonly string GameSettingsDirectory = "res://settings";
	public static readonly string UserDeckDirectory = "user://cards";
	public static readonly string UserDataDirectory = "user://data";

	public static readonly string StarterDeckResourcePath = "user://cards/test.cards.json";

	public static readonly PackedScene CardScene = GD.Load<PackedScene>("res://scenes/game/Card.tscn");
	public static readonly PackedScene CardButtonScene = GD.Load<PackedScene>("res://scenes/CardButton.tscn");
	public static readonly PackedScene SettingsPopUp = GD.Load<PackedScene>("res://scenes/SettingsPopUp.tscn");
	public static readonly PackedScene ConfirmPopUp = GD.Load<PackedScene>("res://scenes/ConfirmPopUp.tscn");
	public static readonly PackedScene DeckPopUp = GD.Load<PackedScene>("res://scenes/DeckPopUp.tscn");
	public static readonly PackedScene MainGameScene = GD.Load<PackedScene>("res://scenes/game/MainGame.tscn");
	public static readonly PackedScene MainMenuScene = GD.Load<PackedScene>("res://scenes/menu/MainMenu.tscn");
	public static readonly PackedScene GameLobbyScene = GD.Load<PackedScene>("res://scenes/game_lobby/GameLobby.tscn");
	public static readonly PackedScene SelectLevelScene = GD.Load<PackedScene>("res://scenes/game_lobby/SelectLevelPanel.tscn");
	public static readonly PackedScene Tooltip = GD.Load<PackedScene>("res://scenes/Tooltip.tscn");
	public static readonly PackedScene TestBenchScene = GD.Load<PackedScene>("res://scenes/test/TestBench.tscn");
	public static readonly PackedScene IsaaacModeScene = GD.Load<PackedScene>("res://scenes/game/isaacs.tscn");

	public static readonly string ErrorAvatarPath = "res://assets/sprites/avatars/avatar_blue_monster_00.jpeg";

	public static class Audio
	{
		public static readonly AudioStream Music_Lobby = GD.Load<AudioStream>("res://assets/audio/music_card_game_menu.mp3");
		public static readonly AudioStream Music_Game1 = GD.Load<AudioStream>("res://assets/audio/music_card_game_vib.mp3");
		public static readonly AudioStream Music_Game2 = GD.Load<AudioStream>("res://assets/audio/music_card_game_alt.mp3");
		public static readonly AudioStream BalloonSnap = GD.Load<AudioStream>("res://assets/audio/click-balloon-snap.mp3");
		public static readonly AudioStream ClickSnap = GD.Load<AudioStream>("res://assets/audio/click-loose.mp3");
		public static readonly AudioStream CardWhoosh = GD.Load<AudioStream>("res://assets/audio/woosh-6.ogg");
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
		public static readonly AudioStream TurnEnd = GD.Load<AudioStream>("res://assets/audio/player-win-10.wav");
		public static readonly AudioStream[] Whoosh = new[] {
			GD.Load<AudioStream>("res://assets/audio/whoosh-bamboo.mp3"),
			GD.Load<AudioStream>("res://assets/audio/whoosh-bat.mp3"),
			GD.Load<AudioStream>("res://assets/audio/whoosh-swipe.mp3"),
		};
		public static readonly AudioStream GameOver_Lose = GD.Load<AudioStream>("res://assets/audio/lose.wav");
		public static readonly AudioStream GameOver_Win = GD.Load<AudioStream>("res://assets/audio/win.wav");
	}
}