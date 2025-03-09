using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class AudioManager : Node
{
    private static readonly int ChannelCount = 16;

    private float _effectsVolume = 1f;
    private float _musicVolume = 1f;
    private Queue<AudioStreamPlayer> _channels = new Queue<AudioStreamPlayer>();
    private Dictionary<AudioStreamPlayer, PlayingAudio> _activeChannels = new Dictionary<AudioStreamPlayer, PlayingAudio>();
    private AudioStreamPlayer _musicChannel;

    public static AudioManager Instance { get; private set; }

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always; // always process audio

        _musicChannel = new AudioStreamPlayer();
        _musicChannel.Name = "music_channel";
        _musicChannel.Bus = "Music";
        AddChild(_musicChannel);

        for (int i = 0; i < ChannelCount; i++)
        {
            AudioStreamPlayer channel = new AudioStreamPlayer();
            channel.Name = $"channel_{i}";
            channel.Bus = "Master";
            channel.Finished += () => OnStreamFinished(channel);
            _channels.Enqueue(channel);
            AddChild(channel);
        }
    }

    private static float TweakValue(float min, float max)
    {
        // try to make repetetive sounds sound less repetetive
        float delta = max - min;
        return min + ((float)Random.Shared.NextDouble() * delta);
    }

    public void UpdateEffectsVolume(float volume)
    {
        GD.Print("Updating effects volume ", volume);
        float curVolume = _effectsVolume;
        foreach (AudioStreamPlayer activeChannel in _activeChannels.Keys)
        {
            if (curVolume <= 1e-6)
            {
                activeChannel.VolumeDb = Mathf.LinearToDb(volume);
            }
            else
            {
                float linearVolume = Mathf.DbToLinear(activeChannel.VolumeDb);
                float originalVolume = linearVolume / curVolume;
                activeChannel.VolumeDb = Mathf.LinearToDb(originalVolume * volume);
            }
        }

        _effectsVolume = volume;
    }

    public Task Play(
        AudioStream audio,
        string bus = "Master",
        float pitch = 1.0f,
        float volume = 1.0f,
        string name = null,
        bool tweak = false)
    {
        if (_channels.Count == 0)
        {
            GD.PushError($"Dropping Audio! No available channels for {audio.ResourcePath}");
            return Task.CompletedTask;
        }

        AudioStreamPlayer channel = _channels.Dequeue();
        PlayingAudio audioMetadata = new PlayingAudio
        {
            Name = name,
            TaskCompletion = new TaskCompletionSource(),
        };

        if (!_activeChannels.TryAdd(channel, audioMetadata))
        {
            GD.PushError($"Could not start audio! Failed to add to active channels.");
            _channels.Enqueue(channel);
            return Task.CompletedTask;
        }

        channel.Stream = audio;
        channel.Bus = bus;

        if (tweak)
        {
            pitch += TweakValue(-0.1f, 0.1f);
            volume += TweakValue(-0.05f, 0.05f);
        }

        volume *= _effectsVolume;

        channel.PitchScale = pitch;
        channel.VolumeDb = Mathf.LinearToDb(volume);

        channel.Play();
        return audioMetadata.TaskCompletion.Task;
    }

    public void Stop(string name)
    {
        foreach (var audioKvp in _activeChannels)
        {
            PlayingAudio metadata = audioKvp.Value;
            if (metadata.Name == name)
            {
                AudioStreamPlayer player = audioKvp.Key;
                player.Stop();
            }
        }
    }

    public void PlayMusic(AudioStream audio)
    {
        _musicChannel.Stream = audio;
        _musicChannel.Play();
    }

    public void StopMusic()
    {
        _musicChannel.Stop();
    }
    
    public void UpdateMusicVolume(float volume)
    {
        _musicChannel.VolumeDb = Mathf.LinearToDb(volume);
    }

    private void OnStreamFinished(AudioStreamPlayer channel)
    {
        if (_activeChannels.Remove(channel, out PlayingAudio metadata))
        {
            metadata.TaskCompletion.SetResult();
        }
        else
        {
            GD.PushError("Audio finished without a completion source!");
        }

        _channels.Enqueue(channel);
    }

    private struct PlayingAudio
    {
        public string Name { get; set; }
        public TaskCompletionSource TaskCompletion { get; set; }
    }
}