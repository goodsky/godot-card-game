using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

public static class GameLoader
{
	private static readonly string GameDeckDirectory = "res://decks";
	private static readonly string UserDeckDirectory = "user://decks";

	public class SavedCardPool
	{
		[JsonPropertyName("name")]
		public string Name { get; set; }

		[JsonPropertyName("cards")]
		public List<CardInfo> Cards { get; set; }
	}

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

		var cardPool = JsonSerializer.Deserialize<SavedCardPool>(fileContent);
		return new CardPool(cardPool.Cards, cardPool.Name);
	}

	public static void SaveCardPool(CardPool cards, string filename = null)
	{
		if (string.IsNullOrEmpty(filename))
		{
			var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
			filename = $"{timestamp}.cards.json";
		}

		if (!filename.EndsWith(".cards.json"))
		{
			filename = filename + ".cards.json";
		}

		var cardPoolJson = JsonSerializer.Serialize(new SavedCardPool() { Cards = cards.Cards, Name = cards.Name });

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