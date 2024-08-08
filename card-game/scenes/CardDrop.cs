using System.Linq;
using Godot;

/**
 * Node where a dragged Card can be dropped.
 */
public abstract partial class CardDrop : Node2D
{
	[Export(PropertyHint.None)] 
	public Card[] DebugCards = null;

	protected abstract int MaxCards { get; }

	public override void _Ready()
	{
		if (DebugCards != null)
		{
			foreach (var card in DebugCards)
			{
				TryAddCard(card);
			}
		}
	}

	public virtual bool TryAddCard(Card card)
	{
		int cardCount = GetChildCards().Length;
		GD.Print($"Dropping Card: {card.Name} into {this.Name} - {cardCount}/{MaxCards} - IsAncestor: {this.IsAncestorOf(card)}");
		if (cardCount < MaxCards && !this.IsAncestorOf(card))
		{
			AddChild(card);
			return true;
		}
		return false;
	}

	public virtual bool TryRemoveCard(Card card)
	{
		GD.Print($"Picking Up Card: {card.Name} from {this.Name} - IsAncestor: {this.IsAncestorOf(card)}");
		if (this.IsAncestorOf(card))
		{
			RemoveChild(card);
			return true;
		}
		return false;
	}

	protected Card[] GetChildCards()
	{
		return GetChildren()
			.Where(child => child is Card)
			.Select(child => child as Card)
			.ToArray();
	}
}