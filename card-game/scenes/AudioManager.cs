using System.Collections.Generic;
using Godot;

public partial class AudioManager : Node
{
    private static readonly int ChannelCount = 8;
    private Queue<AudioStreamPlayer> _channels = new Queue<AudioStreamPlayer>();


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

    public void Play(AudioStream audio, string bus = "Master", float pitch = 1.0f, float volume = 1.0f)
    {
        if (_channels.Count == 0)
        {
            GD.PushError($"Dropping Audio! No available channels for {audio.ResourcePath}");
            return;
        }
        AudioStreamPlayer channel = _channels.Dequeue();
        channel.Stream = audio;
        channel.Bus = bus;
        channel.PitchScale = pitch;
        GD.Print("volume to db ", volume, " = ", Mathf.LinearToDb(volume), "dB");
        channel.VolumeDb = Mathf.LinearToDb(volume) - Mathf.LinearToDb(1.0f);
        channel.Play();
    }

    private void OnStreamFinished(AudioStreamPlayer channel)
    {
        _channels.Enqueue(channel);
    }
}