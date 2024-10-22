using Godot;
using System;

public partial class CardVisual : Node2D
{
	private string _loadedAvatarResource = null;

	[Export]
	public Sprite2D CardFront { get; set; }

	[Export]
	public Sprite2D CardBack { get; set; }

	[Export]
	public Sprite2D CardHighlight { get; set; }

	[Export]
	public Sprite2D Avatar { get; set; }

	[Export]
	public Sprite2D[] BloodCostIcons { get; set; }

	[Export]
	public CanvasText NameLabel { get; set; }

	[Export]
	public CanvasText AttackLabel { get; set; }

	[Export]
	public CanvasText HealthLabel { get; set; }

	[Export]
	public CanvasText[] AbilityLabels { get; set; }

	public void SetHighlight(bool showHighlight)
	{
		CardHighlight.Visible = showHighlight;
	}

	public void ShowCardBack(bool showBack)
	{
		CardBack.Visible = showBack;
		CardFront.Visible = !showBack;
	}

	public void Update(CardInfo cardInfo, CardCombatInfo combatInfo = null)
	{
		NameLabel.Text = cardInfo.Name;
		AttackLabel.Text = cardInfo.Attack.ToString();

		int health = Math.Max(0, cardInfo.Health - (combatInfo?.DamageReceived ?? 0));
		HealthLabel.Text = health.ToString();

		if (_loadedAvatarResource != cardInfo.AvatarResource)
		{
			_loadedAvatarResource = cardInfo.AvatarResource;
			Avatar.Texture = ResourceLoader.Load<CompressedTexture2D>(cardInfo.AvatarResource);
		}

		if (cardInfo.CardFoilHexcode != default)
		{
			CardFront.SelfModulate = new Color(cardInfo.CardFoilHexcode);
		}
		else
		{
			switch (cardInfo.Rarity)
			{
				case CardRarity.Sacrifice:
					CardFront.SelfModulate = new Color("88615f");
					break;

				case CardRarity.Common:
					CardFront.SelfModulate = new Color("777168");
					break;

				case CardRarity.Uncommon:
					CardFront.SelfModulate = new Color("5659ae");
					break;

				case CardRarity.Rare:
					CardFront.SelfModulate = new Color("cfcf00");
					break;
			}
		}

		for (int i = 0; i < BloodCostIcons.Length; i++)
		{
			BloodCostIcons[i].Visible = i < ((int)cardInfo.BloodCost);
		}

		for (int i = 0; i < AbilityLabels.Length; i++)
		{
			if (cardInfo.Abilities == null ||
				i >= cardInfo.Abilities.Count ||
				cardInfo.Abilities[i] == CardAbilities.None)
			{
				AbilityLabels[i].Text = string.Empty;
			}
			else
			{
				AbilityLabels[i].Text = cardInfo.Abilities[i].ToString();
			}
		}
	}
}
