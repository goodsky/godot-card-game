using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;

public static class DeckLoader
{
	private static readonly string GameDeckDirectory = "res://decks";
	private static readonly string UserDeckDirectory = "user://decks";

	public static List<(Deck deck, string path)> GetAvailableDecks()
	{
		var decks = new List<(Deck, string)>();

		var directoryPaths = new string[] { GameDeckDirectory, UserDeckDirectory };
		foreach (var directoryPath in directoryPaths)
		{
			var dir = DirAccess.Open(directoryPath);
			if (dir == null) continue;

			var fileNames = dir.GetFiles();
			foreach (var fileName in fileNames)
			{
				if (!fileName.EndsWith(".deck.json")) continue;

				var deckPath = Path.Combine(directoryPath, fileName);
				var deck = LoadDeck(deckPath);
				if (deck == null) continue;
				decks.Add((deck, deckPath));
			}
		}

		return decks;
	}

	public static Deck LoadDeck(string deckPath)
	{
		GD.Print("Loading deck at ", deckPath);

		var fileContent = Godot.FileAccess.GetFileAsString(deckPath);
		if (string.IsNullOrEmpty(fileContent))
		{
			GD.PrintErr($"Failed to load deck from {deckPath}. Error=\"{Godot.FileAccess.GetOpenError()}\"");
			return null;
		}

		var deckJson = Json.ParseString(fileContent);
		var deckDict = deckJson.AsGodotDictionary();
		if (deckDict == null || deckDict.Count == 0)
		{
			GD.PrintErr($"Invalid JSON. Failed to load deck {deckPath}.");
			return null;
		}

		if (!deckDict.TryGetValue("name", out var name))
		{
			GD.PrintErr($"Schema Error. Mising deck name in {deckPath}.");
			return null;
		}

		if (!deckDict.TryGetValue("cards", out var cards))
		{
			GD.PrintErr($"Schema Error. Mising cards in {deckPath}.");
			return null;
		}

		var cardInfos = new List<CardInfo>();
		foreach (var card in cards.AsGodotArray())
		{
			var cardDict = card.AsGodotDictionary();
			var cardInfo = new CardInfo()
			{
				Name = cardDict["name"].As<string>(),
				AvatarResource = cardDict["img"].As<string>(),
				Attack = cardDict["attack"].As<int>(),
				Defense = cardDict["defense"].As<int>(),
				BloodCost = cardDict["cost"].As<int>(),
			};

			cardInfos.Add(cardInfo);
		}

		return new Deck(cardInfos, name.AsString());
	}

	public static void SaveDeck(Deck deck, string filename = null)
	{
		var deckCardsArray = new Godot.Collections.Array();
		foreach (var cardInfo in deck.Cards)
		{
			deckCardsArray.Add(new Godot.Collections.Dictionary() {
				{ "name", cardInfo.Name },
				{ "img", cardInfo.AvatarResource },
				{ "attack", cardInfo.Attack },
				{ "defense", cardInfo.Defense },
				{ "cost", cardInfo.BloodCost },
			});
		}

		var deckDict = new Godot.Collections.Dictionary() {
			{ "name", deck.Name },
			{ "cards", deckCardsArray }, 
		};

		var deckJson = Json.Stringify(deckDict, indent: "   ");

		if (string.IsNullOrEmpty(filename))
		{
			var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
			filename = $"{timestamp}.deck.json";
		}

		if (!filename.EndsWith(".deck.json"))
		{
			filename = filename + ".deck.json";
		}
		
		DirAccess.MakeDirRecursiveAbsolute(UserDeckDirectory);
		var filePath = Path.Combine(UserDeckDirectory, filename);
		GD.Print("Saving deck at ", filePath);

		var file = Godot.FileAccess.Open(filePath, Godot.FileAccess.ModeFlags.Write);
		if (file == null)
		{
			throw new InvalidOperationException($"Failed to save deck at {filePath}: {Godot.FileAccess.GetOpenError()}");
		}
		file.StoreString(deckJson);
		file.Close();
	}

	// Test code to validate the end to end serialization and deserialization of decks
	public static void Debug_TestEndToEnd(int deckSize)
	{
		var adjectivesPath = Path.Combine(GameDeckDirectory, "generator/adjectives.txt");
		var nounsPath = Path.Combine(GameDeckDirectory, "generator/nouns.txt");

		var adjectives = Godot.FileAccess.GetFileAsString(adjectivesPath).Split("\n").Select(s => s.Trim()).ToArray();
		var nouns = Godot.FileAccess.GetFileAsString(nounsPath).Split("\n").Select(s => s.Trim()).ToArray();

		var blueMonsterAvatars = new[] {
			"res://assets/sprites/avatars/avatar_blue_monster_00.jpeg",
			"res://assets/sprites/avatars/avatar_blue_monster_01.jpeg",
			"res://assets/sprites/avatars/avatar_blue_monster_02.jpeg",
			};

		var generateName = () => {
			var adjective = adjectives[Random.Shared.Next(adjectives.Length)];
			var noun = nouns[Random.Shared.Next(nouns.Length)];
			return $"{adjective} {noun}";
		};

		var cards = new List<CardInfo>();
		for (int i = 0; i < deckSize; i++)
		{
			cards.Add(new CardInfo() {
				Name = generateName(),
				AvatarResource = blueMonsterAvatars[Random.Shared.Next(blueMonsterAvatars.Length)],
				Attack = Random.Shared.Next(1, 6),
				Defense = Random.Shared.Next(1, 11),
				BloodCost = Random.Shared.Next(1, 4),
			});
		}

		var testDeck = new Deck(cards, "Test Deck");
		SaveDeck(testDeck, "test_e2e");

		var decks = GetAvailableDecks();
		GD.Print("Deck Names: ", string.Join(", ", decks.Select(t => t.deck.Name)));

		var testDeckPath = decks.Where(t => t.deck.Name == "Test Deck").Select(t => t.path).FirstOrDefault();
		if (testDeckPath == null)
		{
			throw new Exception("TEST FAILED. Could not find generated deck after save.");
		}

		var loadedDeck = LoadDeck(testDeckPath);
		if (loadedDeck.Name != testDeck.Name ||
			loadedDeck.Cards.Count != testDeck.Cards.Count)
		{
			throw new Exception("TEST FAILED. Loaded Deck does not match test generated deck.");
		}
	}
}