using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;

public static class GameLoader
{
	private static readonly string GameDeckDirectory = "res://decks";
	private static readonly string UserDeckDirectory = "user://decks";

	public static List<(CardPool cards, string path)> GetAvailableCardPools()
	{
		var cardPools = new List<(CardPool, string)>();

		var directoryPaths = new string[] { GameDeckDirectory, UserDeckDirectory };
		foreach (var directoryPath in directoryPaths)
		{
			var dir = DirAccess.Open(directoryPath);
			if (dir == null) continue;

			var fileNames = dir.GetFiles();
			foreach (var fileName in fileNames)
			{
				if (!fileName.EndsWith(".cards.json")) continue;

				var cardPoolPath = Path.Combine(directoryPath, fileName);
				var cardPool = LoadCardPool(cardPoolPath);
				if (cardPool == null) continue;
				cardPools.Add((cardPool, cardPoolPath));
			}
		}

		return cardPools;
	}

	public static CardPool LoadCardPool(string cardPoolPath)
	{
		GD.Print("Loading card pool at ", cardPoolPath);

		var fileContent = Godot.FileAccess.GetFileAsString(cardPoolPath);
		if (string.IsNullOrEmpty(fileContent))
		{
			GD.PrintErr($"Failed to load card pool from {cardPoolPath}. Error=\"{Godot.FileAccess.GetOpenError()}\"");
			return null;
		}

		var cardPoolJson = Json.ParseString(fileContent);
		var cardPoolDict = cardPoolJson.AsGodotDictionary();
		if (cardPoolDict == null || cardPoolDict.Count == 0)
		{
			GD.PrintErr($"Invalid JSON. Failed to load card pool {cardPoolPath}.");
			return null;
		}

		if (!cardPoolDict.TryGetValue("name", out var name))
		{
			GD.PrintErr($"Schema Error. Mising card pool name in {cardPoolPath}.");
			return null;
		}

		if (!cardPoolDict.TryGetValue("cards", out var cards))
		{
			GD.PrintErr($"Schema Error. Mising cards in {cardPoolPath}.");
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
				Health = cardDict["defense"].As<int>(),
				BloodCost = cardDict["cost"].As<CardBloodCost>(),
			};

			cardInfos.Add(cardInfo);
		}

		return new CardPool(cardInfos, name.AsString());
	}

	public static void SaveCardPool(CardPool cards, string filename = null)
	{
		var cardsArray = new Godot.Collections.Array();
		foreach (var cardInfo in cards.Cards)
		{
			cardsArray.Add(new Godot.Collections.Dictionary() {
				{ "name", cardInfo.Name },
				{ "img", cardInfo.AvatarResource },
				{ "attack", cardInfo.Attack },
				{ "defense", cardInfo.Health },
				{ "cost", (int)cardInfo.BloodCost },
			});
		}

		var cardPoolDict = new Godot.Collections.Dictionary() {
			{ "name", cards.Name },
			{ "cards", cardsArray }, 
		};

		var cardPoolJson = Json.Stringify(cardPoolDict, indent: "   ");

		if (string.IsNullOrEmpty(filename))
		{
			var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
			filename = $"{timestamp}.cards.json";
		}

		if (!filename.EndsWith(".cards.json"))
		{
			filename = filename + ".cards.json";
		}
		
		DirAccess.MakeDirRecursiveAbsolute(UserDeckDirectory);
		var filePath = Path.Combine(UserDeckDirectory, filename);
		GD.Print("Saving card pool at ", filePath);

		var file = Godot.FileAccess.Open(filePath, Godot.FileAccess.ModeFlags.Write);
		if (file == null)
		{
			throw new InvalidOperationException($"Failed to save card pool at {filePath}: {Godot.FileAccess.GetOpenError()}");
		}
		file.StoreString(cardPoolJson);
		file.Close();
	}

	// Test code to validate the end to end serialization and deserialization of game data
	public static void Debug_TestEndToEnd()
	{
		var testCardPool = CardGenerator.GenerateRandomCardPool(CardGenerator.DefaultArgs, "Test Cards");
		SaveCardPool(testCardPool, "test");

		var cardPools = GetAvailableCardPools();
		GD.Print("Card Pool Names: ", string.Join(", ", cardPools.Select(t => t.cards.Name)));

		var testDeckPath = cardPools.Where(t => t.cards.Name == "Test Cards").Select(t => t.path).FirstOrDefault();
		if (testDeckPath == null)
		{
			throw new Exception("TEST FAILED. Could not find generated deck after save.");
		}

		var loadedDeck = LoadCardPool(testDeckPath);
		if (loadedDeck.Name != testCardPool.Name ||
			loadedDeck.Cards.Count != testCardPool.Cards.Count)
		{
			throw new Exception("TEST FAILED. Loaded Deck does not match test generated deck.");
		}
	}
}