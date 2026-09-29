using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace EorzeaTimers.Windows;

internal sealed class VesselDiagnosticsWindow : Window
{
    private readonly Plugin plugin;
    private readonly VesselDiagnostics diagnostics;
    private string selectedAddon = string.Empty;
    private string copiedReport = string.Empty;
    private string copyStatus = string.Empty;
    private string[] copyParts = [];
    private int nextPart;
    private bool confirmNewReport;
    private static readonly string[] Checkpoints =
    [
        "1. Before Ctrl+U", "2. Timers open", "3. Estate open",
        "4. Voyages visible", "5. Windows closed", "6. Workshop",
    ];

    internal VesselDiagnosticsWindow(Plugin plugin, VesselDiagnostics diagnostics)
        : base("Eorzea Timers - Vessel Report###EorzeaTimersVesselReport")
    {
        this.plugin = plugin;
        this.diagnostics = diagnostics;
        Size = new Vector2(700, 690);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(600, 460),
            MaximumSize = new Vector2(1100, 1000),
        };
    }

    public override void Draw()
    {
        ImGui.TextWrapped($"Vessel research preview {Plugin.CurrentVersion}");
        var enabled = plugin.Configuration.VesselDiagnosticsEnabled;
        if (ImGui.Checkbox("Enable capture, including after restarting FFXIV", ref enabled))
            plugin.SetVesselDiagnosticsEnabled(enabled);
        TextLayout.DisabledWrapped(diagnostics.Status);

        var currentReport = diagnostics.Report;
        using (ImRaii.Disabled(currentReport.Length == 0))
        {
            if (ImGui.Button("Stop & copy report"))
            {
                plugin.SetVesselDiagnosticsEnabled(false);
                copiedReport = currentReport;
                copyParts = SplitReport(copiedReport);
                nextPart = 0;
                ImGui.SetClipboardText(copiedReport);
                copyStatus = "Report copied. Paste it into the chat with Ctrl+V. Capture is now off.";
            }
        }
        ImGui.SameLine();
        if (ImGui.Button("New report")) confirmNewReport = true;
        if (confirmNewReport)
        {
            ImGui.TextWrapped("Starting a new report clears the current observations. Copy them first if you need them.");
            if (ImGui.Button("Clear & start capture"))
            {
                diagnostics.RequestNewReport();
                plugin.SetVesselDiagnosticsEnabled(true);
                copyStatus = string.Empty;
                copiedReport = string.Empty;
                copyParts = [];
                nextPart = 0;
                confirmNewReport = false;
            }
            ImGui.SameLine();
            if (ImGui.Button("Keep current report")) confirmNewReport = false;
        }
        if (copyStatus.Length > 0) ImGui.TextWrapped(copyStatus);
        if (copyParts.Length > 1)
        {
            TextLayout.DisabledWrapped("If the full paste is too long, use these smaller parts from the same saved copy:");
            if (ImGui.Button($"Copy part {nextPart + 1} of {copyParts.Length}"))
            {
                ImGui.SetClipboardText($"Eorzea Timers vessel report - part {nextPart + 1}/{copyParts.Length}\n" + copyParts[nextPart]);
                copyStatus = $"Part {nextPart + 1} of {copyParts.Length} copied. Paste it into the chat.";
                nextPart = (nextPart + 1) % copyParts.Length;
            }
        }
        ImGui.Separator();
        if (ImGui.CollapsingHeader("Test instructions", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.TextWrapped("Prepare one deployed vessel and, if possible, one idle vessel. Enable capture above, then exit FFXIV completely and restart it. The setting survives the restart.");
            ImGui.TextWrapped("After login, wait 10 seconds without opening Ctrl+U or the workshop. Open this report with /etimers vesselprobe. Press each checkpoint below AFTER reaching that screen and waiting for its data to appear.");
            ImGui.TextWrapped("Order: Before Ctrl+U; Ctrl+U Timers; Estate; Exploratory/Subaquatic Voyages; close those game windows. Wait 5 seconds before the Windows closed checkpoint. Visiting the workshop afterwards is optional.");
            TextLayout.DisabledWrapped("Keep this report open beside the game window if helpful. Finish with Stop & copy report. Reports live in memory; copy before restarting or unloading the plugin. This preview does not create voyage timers automatically.");
        }
        using (ImRaii.Disabled(!enabled))
        {
            var width = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X * 2) / 3;
            for (var i = 0; i < Checkpoints.Length; i++)
            {
                if (i % 3 != 0) ImGui.SameLine();
                if (ImGui.Button(Checkpoints[i], new Vector2(width, 0)))
                    diagnostics.RequestSnapshot(Checkpoints[i]);
            }
        }
        if (ImGui.CollapsingHeader("Advanced: inspect a discovered game window"))
        {
            TextLayout.DisabledWrapped("Use only if the voyage window is missing from the report. Select its addon name, then capture it while it is open. Selected window text will be included in your report.");
            ImGui.SetNextItemWidth(-1);
            if (ImGui.BeginCombo("##VesselAddon", selectedAddon.Length == 0 ? "Select a discovered addon" : selectedAddon))
            {
                foreach (var name in diagnostics.DiscoveredAddons)
                    if (ImGui.Selectable(name, name == selectedAddon)) selectedAddon = name;
                ImGui.EndCombo();
            }
            using (ImRaii.Disabled(!enabled || selectedAddon.Length == 0))
                if (ImGui.Button("Capture selected addon")) diagnostics.WatchAddon(selectedAddon);
        }
        ImGui.Separator();
        TextLayout.DisabledWrapped($"Report: {currentReport.Length:N0} characters. Review the text before sharing; it can include vessel names and the selected game window's text.");
        using var preview = ImRaii.Child("VesselReportPreview", new Vector2(-1, MathF.Max(120 * ImGuiHelpers.GlobalScale, ImGui.GetContentRegionAvail().Y)), true);
        if (preview.Success)
        {
            ImGui.PushTextWrapPos(0);
            ImGui.TextUnformatted(currentReport.Length == 0 ? "Enable capture to begin." : currentReport);
            ImGui.PopTextWrapPos();
        }
    }

    private static string[] SplitReport(string text)
    {
        const int partLength = 12000;
        var parts = new System.Collections.Generic.List<string>();
        for (var start = 0; start < text.Length;)
        {
            var length = Math.Min(partLength, text.Length - start);
            if (start + length < text.Length)
            {
                var newline = text.LastIndexOf('\n', start + length - 1, length);
                if (newline >= start) length = newline - start + 1;
            }
            parts.Add(text.Substring(start, length));
            start += length;
        }
        return parts.ToArray();
    }
}
