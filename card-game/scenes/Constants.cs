using Godot;

public static class Constants
{
	public const string ActiveCardDropGroup = "ActiveCardDrop";
	public const string DraggingCardGroup = "DraggingCard";

	public static readonly PackedScene CardScene = GD.Load<PackedScene>("res://scenes/Card.tscn");
}