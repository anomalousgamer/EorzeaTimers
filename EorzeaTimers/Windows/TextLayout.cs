using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;

namespace EorzeaTimers.Windows;

internal static class TextLayout
{
    internal static void TooltipWrapped(string text)
    {
        ImGui.BeginTooltip();
        ImGui.PushTextWrapPos(420f * ImGuiHelpers.GlobalScale);
        ImGui.TextUnformatted(text);
        ImGui.PopTextWrapPos();
        ImGui.EndTooltip();
    }

    internal static void DisabledWrapped(string text)
    {
        ImGui.PushTextWrapPos(0);
        ImGui.TextDisabled(text);
        ImGui.PopTextWrapPos();
    }

    internal static void ColoredWrapped(Vector4 color, string text)
    {
        ImGui.PushTextWrapPos(0);
        ImGui.TextColored(color, text);
        ImGui.PopTextWrapPos();
    }
}
