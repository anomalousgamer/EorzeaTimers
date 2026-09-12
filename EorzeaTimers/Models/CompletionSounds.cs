using System;

namespace EorzeaTimers.Models;

public enum CompletionSound
{
    StandardNotification,
    SoundEffect1,
    SoundEffect2,
    SoundEffect3,
    SoundEffect4,
    SoundEffect5,
    SoundEffect6,
    SoundEffect7,
    SoundEffect8,
    SoundEffect9,
    SoundEffect10,
    SoundEffect11,
    SoundEffect12,
}

public static class CompletionSounds
{
    public static readonly CompletionSound[] Options =
        Enum.GetValues<CompletionSound>();

    public static string GetName(CompletionSound sound)
    {
        return sound switch
        {
            CompletionSound.StandardNotification => "Standard Notification",
            CompletionSound.SoundEffect1 => "Sound Effect 1",
            CompletionSound.SoundEffect2 => "Sound Effect 2",
            CompletionSound.SoundEffect3 => "Sound Effect 3",
            CompletionSound.SoundEffect4 => "Sound Effect 4",
            CompletionSound.SoundEffect5 => "Sound Effect 5",
            CompletionSound.SoundEffect6 => "Sound Effect 6",
            CompletionSound.SoundEffect7 => "Sound Effect 7",
            CompletionSound.SoundEffect8 => "Sound Effect 8",
            CompletionSound.SoundEffect9 => "Sound Effect 9",
            CompletionSound.SoundEffect10 => "Sound Effect 10",
            CompletionSound.SoundEffect11 => "Sound Effect 11",
            CompletionSound.SoundEffect12 => "Sound Effect 12",
            _ => "Standard Notification",
        };
    }

    public static uint? GetChatSoundEffectId(CompletionSound sound)
    {
        return sound switch
        {
            CompletionSound.SoundEffect1 => 1,
            CompletionSound.SoundEffect2 => 2,
            CompletionSound.SoundEffect3 => 3,
            CompletionSound.SoundEffect4 => 4,
            CompletionSound.SoundEffect5 => 5,
            CompletionSound.SoundEffect6 => 6,
            CompletionSound.SoundEffect7 => 7,
            CompletionSound.SoundEffect8 => 8,
            CompletionSound.SoundEffect9 => 9,
            CompletionSound.SoundEffect10 => 10,
            CompletionSound.SoundEffect11 => 11,
            CompletionSound.SoundEffect12 => 12,
            _ => null,
        };
    }
}
