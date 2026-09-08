using Dalamud.Game.ClientState.Keys;

namespace EorzeaTimers.Models;

public enum OverlayDisplayMode
{
    Off,
    Persistent,
    KeyBound,
}

public readonly struct OverlayKeyOption
{
    public OverlayKeyOption(VirtualKey key, string name)
    {
        Key = key;
        Name = name;
    }

    public VirtualKey Key { get; }

    public string Name { get; }
}

public static class OverlayKeys
{
    public static readonly OverlayKeyOption[] Options =
    [
        new(VirtualKey.F1, "F1"),
        new(VirtualKey.F2, "F2"),
        new(VirtualKey.F3, "F3"),
        new(VirtualKey.F4, "F4"),
        new(VirtualKey.F5, "F5"),
        new(VirtualKey.F6, "F6"),
        new(VirtualKey.F7, "F7"),
        new(VirtualKey.F8, "F8"),
        new(VirtualKey.F9, "F9"),
        new(VirtualKey.F10, "F10"),
        new(VirtualKey.F11, "F11"),
        new(VirtualKey.F12, "F12"),
        new(VirtualKey.SHIFT, "Shift"),
        new(VirtualKey.CONTROL, "Ctrl"),
        new(VirtualKey.MENU, "Alt"),
        new(VirtualKey.SPACE, "Space"),
        new(VirtualKey.KEY_0, "0"),
        new(VirtualKey.KEY_1, "1"),
        new(VirtualKey.KEY_2, "2"),
        new(VirtualKey.KEY_3, "3"),
        new(VirtualKey.KEY_4, "4"),
        new(VirtualKey.KEY_5, "5"),
        new(VirtualKey.KEY_6, "6"),
        new(VirtualKey.KEY_7, "7"),
        new(VirtualKey.KEY_8, "8"),
        new(VirtualKey.KEY_9, "9"),
        new(VirtualKey.A, "A"),
        new(VirtualKey.B, "B"),
        new(VirtualKey.C, "C"),
        new(VirtualKey.D, "D"),
        new(VirtualKey.E, "E"),
        new(VirtualKey.F, "F"),
        new(VirtualKey.G, "G"),
        new(VirtualKey.H, "H"),
        new(VirtualKey.I, "I"),
        new(VirtualKey.J, "J"),
        new(VirtualKey.K, "K"),
        new(VirtualKey.L, "L"),
        new(VirtualKey.M, "M"),
        new(VirtualKey.N, "N"),
        new(VirtualKey.O, "O"),
        new(VirtualKey.P, "P"),
        new(VirtualKey.Q, "Q"),
        new(VirtualKey.R, "R"),
        new(VirtualKey.S, "S"),
        new(VirtualKey.T, "T"),
        new(VirtualKey.U, "U"),
        new(VirtualKey.V, "V"),
        new(VirtualKey.W, "W"),
        new(VirtualKey.X, "X"),
        new(VirtualKey.Y, "Y"),
        new(VirtualKey.Z, "Z"),
    ];

    public static bool IsSupported(VirtualKey key)
    {
        foreach (var option in Options)
        {
            if (option.Key == key)
            {
                return true;
            }
        }

        return false;
    }

    public static string GetName(VirtualKey key)
    {
        foreach (var option in Options)
        {
            if (option.Key == key)
            {
                return option.Name;
            }
        }

        return "F10";
    }
}
