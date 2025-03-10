using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

public static class GameLoader
{
	private static readonly string UserSavePath = Path.Combine(Constants.UserDataDirectory, "game.json");
	private static readonly string UserSettingsPath = Path.Combine(Constants.UserDataDirectory, "settings.json");

	public class SavedCardPool
	{
		[JsonPropertyName("name")]
		public string Name { get; set; }

		[JsonPropertyName("cards")]
		public List<CardInfo> Cards { get; set; }
	}

	public class SavedGame
	{
		[JsonPropertyName("level")]
		public int Level { get; set; }

		[JsonPropertyName("score")]
		public int Score { get; set; }

		[JsonPropertyName("hand")]
		public int HandSize { get; set; }

		[JsonPropertyName("cards")]
		public string CardPoolName { get; set; }

		[JsonPropertyName("deck")]
		public List<CardInfo> DeckCards { get; set; }

		[JsonPropertyName("phase")]
		public LobbyState CurrentState { get; set; }

		[JsonPropertyName("rs")]
		public int Seed { get; set; }

		[JsonPropertyName("rn")]
		public int SeedN { get; set; }
	}

	public class SavedSettings
	{
		[JsonPropertyName("effectsVolume")]
		public float EffectsVolume { get; set; }

		[JsonPropertyName("musicVolume")]
		public float MusicVolume { get; set; }
	}

	public static bool SavedGameExists()
	{
		return Godot.FileAccess.FileExists(UserSavePath);
	}

	public static (GameProgress, RandomGenerator) LoadGame()
	{
		if (!SavedGameExists())
		{
			return (null, null);
		}

		var fileContent = Godot.FileAccess.GetFileAsString(UserSavePath);
		if (string.IsNullOrEmpty(fileContent))
		{
			GD.PrintErr($"Failed to load saved game from {UserSavePath}. Error=\"{Godot.FileAccess.GetOpenError()}\"");
			return (null, null);
		}

		var saveGame = JsonSerializer.Deserialize<SavedGame>(fileContent);
		var cardPools = GetAvailableCardPools();

		CardPool cardPool = cardPools.Select(x => x.cards).FirstOrDefault(x => x.Name == saveGame.CardPoolName);
		if (cardPool == null)
		{
			GD.PushError($"Could not find card pool for save game!");
			return (null, null);
		}

		GD.Print($"Loaded Game Seed: {saveGame.Seed}[{saveGame.SeedN}]");

		return (
			new GameProgress
			{
				Level = saveGame.Level,
				Score = saveGame.Score,
				HandSize = saveGame.HandSize,
				CardPool = cardPool,
				DeckCards = saveGame.DeckCards,
				CurrentState = saveGame.CurrentState,
				Seed = saveGame.Seed,
				SeedN = saveGame.SeedN,
			},
			new RandomGenerator(saveGame.Seed, saveGame.SeedN)
		);
	}

	public static void SaveGame(GameProgress game)
	{
		var saveGame = new SavedGame
		{
			Level = game.Level,
			Score = game.Score,
			HandSize = game.HandSize,
			CardPoolName = game.CardPool.Name,
			DeckCards = game.DeckCards,
			CurrentState = game.CurrentState,
			Seed = game.Seed,
			SeedN = game.SeedN,
		};

		GD.Print($"Saving Game. Seed = {saveGame.Seed}[{saveGame.SeedN}]");

		var saveGameJson = JsonSerializer.Serialize(saveGame);
		DirAccess.MakeDirRecursiveAbsolute(Constants.UserDataDirectory);
		var file = Godot.FileAccess.Open(UserSavePath, Godot.FileAccess.ModeFlags.Write);
		if (file == null)
		{
			GD.PushError($"Failed to save game at {UserSavePath}: {Godot.FileAccess.GetOpenError()}");
		}
		else
		{
			file.StoreString(saveGameJson);
			file.Close();
		}
	}

	public static void ClearGame()
	{
		DirAccess.RemoveAbsolute(UserSavePath);
	}

	public static List<(CardPool cards, string path)> GetAvailableCardPools()
	{
		var cardPools = new List<(CardPool, string)>();

		var directoryPaths = new string[] { Constants.GameDeckDirectory, Constants.UserDeckDirectory };
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
		// GD.Print("Loading card pool at ", cardPoolPath);

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

		DirAccess.MakeDirRecursiveAbsolute(Constants.UserDeckDirectory);
		var filePath = Path.Combine(Constants.UserDeckDirectory, filename);
		GD.Print("Saving card pool at ", filePath);

		var file = Godot.FileAccess.Open(filePath, Godot.FileAccess.ModeFlags.Write);
		if (file == null)
		{
			throw new InvalidOperationException($"Failed to save card pool at {filePath}: {Godot.FileAccess.GetOpenError()}");
		}

		file.StoreString(cardPoolJson);
		file.Close();
	}

	public static SavedSettings LoadSettings()
	{
		try
		{
			var fileContent = Godot.FileAccess.GetFileAsString(UserSettingsPath);
			return JsonSerializer.Deserialize<SavedSettings>(fileContent);
		}
		catch (Exception ex)
		{
			GD.Print($"Could not load settings file. Using the default. ", ex.Message);
			return new SavedSettings
			{
				EffectsVolume = 1.0f,
				MusicVolume = 1.0f,
			};
		}
	}

	public static void SaveSettings(
		float effectsVolume,
		float musicVolume)
	{
		var settings = new SavedSettings
		{
			EffectsVolume = Mathf.Clamp(effectsVolume, 0f, 1f),
			MusicVolume = Mathf.Clamp(musicVolume, 0f, 1f),
		};

		var settingsJson = JsonSerializer.Serialize(settings);
		DirAccess.MakeDirRecursiveAbsolute(Constants.UserDataDirectory);
		var file = Godot.FileAccess.Open(UserSettingsPath, Godot.FileAccess.ModeFlags.Write);
		if (file == null)
		{
			GD.PushError($"Failed to save settings at {UserSettingsPath}: {Godot.FileAccess.GetOpenError()}");
		}
		else
		{
			file.StoreString(settingsJson);
			file.Close();
		}
	}

	// Test code to validate the end to end serialization and deserialization of game data
	public static void Debug_TestEndToEnd()
	{
		var testCardPool = CardGenerator.GenerateRandomCardPool("Test Cards");
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