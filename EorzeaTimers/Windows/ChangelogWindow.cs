using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;

namespace EorzeaTimers.Windows;

public sealed class ChangelogWindow : Window
{
    private readonly Plugin plugin;

    public ChangelogWindow(Plugin plugin)
        : base("Eorzea Timers - What's New##EorzeaTimersChangelog")
    {
        this.plugin = plugin;

        Size = new Vector2(510, 360);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(430, 300),
            MaximumSize = new Vector2(700, 650),
        };
    }

    public override void Draw()
    {
        ImGui.TextUnformatted($"Version {Plugin.CurrentVersion}");
        ImGui.TextColored(new Vector4(0.85f, 0.70f, 0.35f, 1f), Changelog.Title);
        ImGui.Separator();
        ImGui.Spacing();

        foreach (var change in Changelog.Latest)
        {
            ImGui.BulletText(change);
        }

        var footerHeight = ImGui.GetFrameHeightWithSpacing() * 2f + 8f * ImGuiHelpers.GlobalScale;
        var remainingHeight = ImGui.GetContentRegionAvail().Y;
        if (remainingHeight > footerHeight)
        {
            ImGui.Dummy(new Vector2(1, remainingHeight - footerHeight));
        }

        var doNotShowAgain = string.Equals(
            plugin.Configuration.LastAcknowledgedVersion,
            Plugin.CurrentVersion,
            System.StringComparison.Ordinal);

        if (ImGui.Checkbox("Don't show again for this version", ref doNotShowAgain))
        {
            plugin.Configuration.LastAcknowledgedVersion =
                doNotShowAgain ? Plugin.CurrentVersion : string.Empty;
            plugin.Configuration.Save();
        }

        var buttonWidth = 120f * ImGuiHelpers.GlobalScale;
        var availableWidth = ImGui.GetContentRegionAvail().X;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + System.MathF.Max(0, availableWidth - buttonWidth));

        if (ImGui.Button("Got it", new Vector2(buttonWidth, 0)))
        {
            IsOpen = false;
        }
    }
}
