using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class AudioManager : Node
{
    private static readonly int ChannelCount = 8;
    private Queue<AudioStreamPlayer> _channels = new Queue<AudioStreamPlayer>();
    private Dictionary<AudioStreamPlayer, TaskCompletionSource> _activeChannels = new Dictionary<AudioStreamPlayer, TaskCompletionSource>();


    public static AudioManager Instance { get; private set; }

    public override void _Ready()
    {
        Instance = this;
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

    public static float TweakPitch()
    {
        return 0.95f + ((float)Random.Shared.NextDouble() * 0.10f);
    }

    public Task Play(AudioStream audio, string bus = "Master", float pitch = 1.0f, float volume = 1.0f)
    {
        if (_channels.Count == 0)
        {
            GD.PushError($"Dropping Audio! No available channels for {audio.ResourcePath}");
            return Task.CompletedTask;
        }

        AudioStreamPlayer channel = _channels.Dequeue();
        var task = new TaskCompletionSource();
        if (!_activeChannels.TryAdd(channel, task))
        {
            GD.PushError($"Could not start audio! Failed to add to active channels.");
            _channels.Enqueue(channel);
            return Task.CompletedTask;
        }

        channel.Stream = audio;
        channel.Bus = bus;
        channel.PitchScale = pitch;
        channel.VolumeDb = Mathf.LinearToDb(volume) - Mathf.LinearToDb(1.0f);
        channel.Play();
        return task.Task;
    }

    private void OnStreamFinished(AudioStreamPlayer channel)
    {
        if (_activeChannels.Remove(channel, out TaskCompletionSource task))
        {
            task.SetResult();
        }
        else
        {
            GD.PushError("Audio finished without a completion source!");
        }

        _channels.Enqueue(channel);
    }
}