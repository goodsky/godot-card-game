using System.Linq;
using Godot;

/**
 * Node where a dragged Card can be dropped.
 */
public abstract partial class CardDrop : Node2D
{
	[Export(PropertyHint.None)] 
	public Card[] DebugCards = null;

	protected Node CardsNode;

	protected int CardCount => GetChildCards().Length;

	protected abstract int MaxCards { get; }

	public override void _Ready()
	{
		CardsNode = new Node2D();
		CardsNode.Name = "CardDrop";
		AddChild(CardsNode);

		if (DebugCards != null)
		{
			foreach (var card in DebugCards)
			{
				TryAddCard(card, null);
			}
		}
	}

	public virtual bool CanDropCard()
	{
		return CardCount < MaxCards;
	}

	public virtual bool TryAddCard(Card card, Vector2? globalPosition)
	{
		int cardCount = CardCount;
		// GD.Print($"Dropping Card: {card.Name} into {this.Name} - {cardCount}/{MaxCards} - IsAncestor: {CardsNode.IsAncestorOf(card)}");
		if (cardCount < MaxCards && !CardsNode.IsAncestorOf(card))
		{
			CardsNode.AddChild(card);
			if (globalPosition.HasValue)
			{
				card.GlobalPosition = globalPosition.Value;
			}

			return true;
		}
		return false;
	}

	public virtual bool TryRemoveCard(Card card)
	{
		// GD.Print($"Picking Up Card: {card.Name} from {this.Name} - IsAncestor: {CardsNode.IsAncestorOf(card)}");
		if (CardsNode.IsAncestorOf(card))
		{
			CardsNode.RemoveChild(card);
			return true;
		}
		return false;
	}

	protected Card[] GetChildCards()
	{
		return CardsNode.GetChildren()
			.Where(child => child is Card)
			.Select(child => child as Card)
			.ToArray();
	}
}