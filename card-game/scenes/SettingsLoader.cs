using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

public static class SettingsLoader
{
    private static readonly string UserSettingsPath = "user://settings.json";

	public class SavedSettings
	{
		[JsonPropertyName("effectsVolume")]
		public float EffectsVolume { get; set; }

		[JsonPropertyName("musicVolume")]
		public float MusicVolume { get; set; }
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
		GD.Print("Updating settings.");

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
}