using Godot;
using System;
using CardGame.Core;

public partial class MainGame : Node
{
	private const string LabelName = "CenterContainer/Label";
	private Card _testCard;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_testCard = new Card("Hello C# in Godot", new ResourceCost { Type = CostType.UnitSacrifice, Amount = 1 });
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		var label = GetNode<Label>(LabelName);
		label.Text = $"The card name is: {_testCard.Name}";
	}
}
