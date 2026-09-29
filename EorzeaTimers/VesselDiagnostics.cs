using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.NativeWrapper;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace EorzeaTimers;

// Read-only research, not a voyage synchronizer. No native pointers are retained
// between callbacks. Manual captures are queued onto the framework thread.
internal sealed class VesselDiagnostics : IDisposable
{
    private const int ReportLimit = 60000;
    private const int ValueLimit = 256;
    private static readonly AddonEvent[] Events =
    [
        AddonEvent.PostSetup, AddonEvent.PreRefresh, AddonEvent.PostRefresh,
        AddonEvent.PostRequestedUpdate, AddonEvent.PostReceiveEvent, AddonEvent.PreFinalize,
    ];
    private static readonly string[] RelatedNames =
    [
        "ContentsTimer", "ContentsInfo", "CompanyCraft", "CompanyShip", "Airship",
        "Submarine", "Submersible", "Voyage", "Exploration", "Housing",
    ];
    private readonly object gate = new();
    private readonly StringBuilder report = new();
    private readonly ConcurrentQueue<string> snapshots = new();
    private readonly SortedSet<string> discovered = new(StringComparer.Ordinal);
    private readonly HashSet<string> watched = new(StringComparer.Ordinal);
    private readonly HashSet<ushort> relatedIds = new();
    private readonly HashSet<string> seenEvents = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string[]> lastValues = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DateTime> lastUpdates = new(StringComparer.Ordinal);
    private bool listening;
    private bool wasLoaded;
    private bool clearRequested;
    private bool limited;
    private string? extraAddon;
    private string lastState = string.Empty;
    private DateTime sessionEndUtc;
    private DateTime loginSampleUtc;
    private DateTime nextPollUtc;
    private string cachedReport = string.Empty;
    private bool reportDirty = true;

    internal string Report
    {
        get
        {
            lock (gate)
            {
                if (reportDirty)
                {
                    cachedReport = report.ToString();
                    reportDirty = false;
                }
                return cachedReport;
            }
        }
    }

    internal string Status
    {
        get
        {
            lock (gate)
                return limited ? "Capture paused. See the last report line and copy it before starting a new report."
                    : listening ? "Recording. You can close this window while following the test."
                    : "Capture stopped. The report stays available until the plugin unloads.";
        }
    }

    internal string[] DiscoveredAddons { get { lock (gate) return discovered.ToArray(); } }
    internal void RequestSnapshot(string label) => snapshots.Enqueue(label);
    internal void RequestNewReport() { lock (gate) clearRequested = true; }

    internal void WatchAddon(string name)
    {
        lock (gate)
        {
            if (!discovered.Contains(name)) return;
            extraAddon = name;
            watched.Add(name);
            snapshots.Enqueue("Selected addon: " + name);
        }
    }

    internal void Update(bool enabled)
    {
        lock (gate)
        {
            if (clearRequested)
            {
                clearRequested = false;
                ResetReport();
            }
            if (enabled != listening)
            {
                if (enabled)
                {
                    if (report.Length == 0) ResetReport();
                    foreach (var evt in Events) Plugin.AddonLifecycle.RegisterListener(evt, OnAddonEvent);
                    listening = true;
                    sessionEndUtc = DateTime.UtcNow.AddMinutes(15);
                    Append("Capture enabled. A new process is required for a cold-login baseline.");
                }
                else
                {
                    StopListening();
                    Append("Capture stopped by user.");
                }
            }
            if (!listening || limited)
            {
                while (snapshots.TryDequeue(out _)) { }
                return;
            }
            if (!Plugin.PlayerState.IsLoaded)
            {
                if (wasLoaded) Append("Character logged out; no native state sampled until next login.");
                wasLoaded = false;
                while (snapshots.TryDequeue(out _)) { }
                return;
            }
            if (!wasLoaded)
            {
                wasLoaded = true;
                loginSampleUtc = DateTime.UtcNow.AddSeconds(5);
                sessionEndUtc = DateTime.UtcNow.AddMinutes(15);
                lastState = string.Empty;
                Append("Character loaded or capture enabled while already logged in. Automatic snapshot in five seconds.");
            }
            if (DateTime.UtcNow >= sessionEndUtc)
            {
                Append("Capture paused after 15 minutes. Copy this report, then start a new report if needed.");
                limited = true;
                return;
            }
            try
            {
                if (loginSampleUtc != default && DateTime.UtcNow >= loginSampleUtc)
                {
                    loginSampleUtc = default;
                    Snapshot("Automatic: five seconds after character detected");
                }
                while (snapshots.TryDequeue(out var label)) Snapshot(label);
                if (DateTime.UtcNow < nextPollUtc) return;
                nextPollUtc = DateTime.UtcNow.AddSeconds(1);
                var state = ReadState();
                if (state != lastState)
                {
                    lastState = state;
                    Append("STATE " + state);
                }
                foreach (var name in watched.ToArray()) SampleAddon(name, false);
            }
            catch (Exception ex)
            {
                Append("Capture paused after managed error: " + ex.GetType().Name);
                limited = true;
            }
        }
    }

    public void Dispose() { lock (gate) StopListening(); }

    private void StopListening()
    {
        if (!listening) return;
        foreach (var evt in Events) Plugin.AddonLifecycle.UnregisterListener(evt, OnAddonEvent);
        listening = false;
        wasLoaded = false;
    }

    private void ResetReport()
    {
        report.Clear();
        reportDirty = true;
        limited = false;
        discovered.Clear();
        watched.Clear();
        relatedIds.Clear();
        watched.Add("ContentsTimer");
        if (extraAddon != null) watched.Add(extraAddon);
        seenEvents.Clear();
        lastValues.Clear();
        lastUpdates.Clear();
        lastState = string.Empty;
        wasLoaded = false;
        nextPollUtc = default;
        sessionEndUtc = DateTime.UtcNow.AddMinutes(15);
        Append($"EORZEA TIMERS VESSEL REPORT | plugin {Plugin.CurrentVersion} | UTC timestamps");
        Append($"Dalamud {typeof(AddonArgs).Assembly.GetName().Version}; FFXIVClientStructs {typeof(AgentContentsTimer).Assembly.GetName().Version}");
        Append("Read-only: setup/refresh values, related window text, agent identity and mapped workshop state. No game requests or UI automation.");
        Append("DISCOVER lines contain window names/agent IDs only. Payloads are limited to timer/voyage/housing windows, related child windows, or the addon you explicitly select.");
        Append("Numeric fields are raw observations, NOT confirmed return times. Text is display-only. Global UI arrays, arbitrary pointers and vectors are not decoded.");
        Append("Changes only after first observation; same-as-previous means unchanged. Limits: 15 minutes, 60000 characters, 256 values/window, 300 nodes/window, 512 characters/string.");
    }

    private void Append(string text)
    {
        if (limited) return;
        var line = $"[{DateTime.UtcNow:HH:mm:ss.fff}] {text}\n";
        if (report.Length + line.Length > ReportLimit)
        {
            report.AppendLine("[LIMIT] Report full. Capture paused; no earlier observations were discarded.");
            limited = true;
        }
        else report.Append(line);
        reportDirty = true;
    }

    private unsafe void OnAddonEvent(AddonEvent evt, AddonArgs args)
    {
        lock (gate)
        {
            if (!listening || limited || !Plugin.PlayerState.IsLoaded || args.Addon.IsNull) return;
            if (DateTime.UtcNow >= sessionEndUtc) return;
            try
            {
                var name = args.AddonName;
                var addon = (AtkUnitBase*)args.Addon.Address;
                if (!discovered.Contains(name)
                    && (evt == AddonEvent.PostSetup || evt == AddonEvent.PreRefresh)
                    && discovered.Count < 200)
                {
                    discovered.Add(name);
                    Append($"DISCOVER {name}#{addon->Id}; {DescribeAgent(args.Addon)}; parent={addon->ParentId}, host={addon->HostId}");
                    var contentsAgent = AgentContentsTimer.Instance();
                    var owner = Plugin.GameGui.FindAgentInterface(args.Addon);
                    if ((contentsAgent != null && owner.Address == (nint)contentsAgent)
                        || (addon->ParentId != 0 && relatedIds.Contains(addon->ParentId))
                        || (addon->HostId != 0 && relatedIds.Contains(addon->HostId))) watched.Add(name);
                }
                if (RelatedNames.Any(word => name.Contains(word, StringComparison.OrdinalIgnoreCase))) watched.Add(name);
                if (!watched.Contains(name)) return;
                var key = $"{name}#{addon->Id}";
                relatedIds.Add(addon->Id);
                if (evt == AddonEvent.PreFinalize)
                {
                    relatedIds.Remove(addon->Id);
                    Append($"CLOSED {key}; native addon is being finalized.");
                    return;
                }
                if (evt == AddonEvent.PostReceiveEvent)
                {
                    if (args is AddonReceiveEventArgs receive)
                    {
                        var eventKey = $"{key}:input:{receive.AtkEventType}:{receive.EventParam}";
                        if (seenEvents.Add(eventKey))
                            Append($"INPUT {key}: {receive.AtkEventType}, param={receive.EventParam} (UI event, not a network request)");
                    }
                    return;
                }
                if (seenEvents.Add($"{key}:{evt}"))
                    Append($"EVENT {evt} {key}; {DescribeAgent(args.Addon)}; parent={addon->ParentId}, host={addon->HostId}");
                if (args is AddonSetupArgs setup)
                    CaptureValues(key + "/setup", (AtkValue*)setup.AtkValues, setup.AtkValueCount, false);
                else if (args is AddonRefreshArgs refresh)
                    CaptureValues(key + "/refresh", (AtkValue*)refresh.AtkValues, refresh.AtkValueCount, false);
                else if (evt == AddonEvent.PostRequestedUpdate)
                {
                    if (lastUpdates.TryGetValue(key, out var previous) && DateTime.UtcNow < previous.AddSeconds(1)) return;
                    lastUpdates[key] = DateTime.UtcNow;
                    // Callback array pointers address global UI arrays. Do not
                    // dump unrelated data without an established subscription.
                    CaptureValues(key + "/cached", addon->AtkValues, addon->AtkValuesCount, false);
                }
            }
            catch (Exception ex)
            {
                Append("Capture paused after lifecycle error: " + ex.GetType().Name);
                limited = true;
            }
        }
    }

    private static unsafe string DescribeAgent(AtkUnitBasePtr addon)
    {
        var owner = Plugin.GameGui.FindAgentInterface(addon);
        if (owner.IsNull) return "agent=unresolved";
        var module = AgentModule.Instance();
        if (module != null)
        {
            var agents = module->Agents;
            for (var i = 0; i < agents.Length; i++)
                if ((nint)agents[i].Value == owner.Address) return $"agent={(AgentId)i} ({i})";
        }
        return "agent=present, ID unresolved";
    }

    private unsafe void Snapshot(string label)
    {
        Append($"=== {label} | {DateTimeOffset.UtcNow:O} | unix={DateTimeOffset.UtcNow.ToUnixTimeSeconds()} ===");
        Append(ReadState());
        foreach (var name in watched.ToArray()) SampleAddon(name, true);
        var housing = HousingManager.Instance();
        var workshop = housing == null ? null : housing->WorkshopTerritory;
        if (workshop == null)
        {
            Append("Workshop unavailable here. This does not establish whether a separate remote vessel cache exists.");
            return;
        }
        var submarines = (HousingWorkshopSubmersibleSubData*)&workshop->Submersible;
        var airships = (HousingWorkshopAirshipSubData*)&workshop->Airship;
        for (var i = 0; i < 4; i++)
        {
            if (submarines[i].RankId > 0)
                Append($"WORKSHOP submarine[{i}] name=\"{Clean(Encoding.UTF8.GetString(submarines[i].Name).TrimEnd('\0'))}\" register={submarines[i].RegisterTime} return={submarines[i].ReturnTime} (mapped Unix seconds)");
            if (airships[i].RankId > 0)
                Append($"WORKSHOP airship[{i}] name=\"{Clean(Encoding.UTF8.GetString(airships[i].Name).TrimEnd('\0'))}\" register={airships[i].RegisterTime} return={airships[i].ReturnTime} (mapped Unix seconds)");
        }
    }

    private static unsafe string ReadState()
    {
        var agent = AgentContentsTimer.Instance();
        var housing = HousingManager.Instance();
        return $"ContentsTimer agent={agent != null}, active={agent != null && agent->IsAgentActive()}, context item={(agent == null ? 0u : agent->ContextMenuItemId)}; WorkshopTerritory={housing != null && housing->WorkshopTerritory != null}";
    }

    private unsafe void SampleAddon(string name, bool checkpoint)
    {
        var wrapper = Plugin.GameGui.GetAddonByName(name);
        if (wrapper.IsNull)
        {
            if (checkpoint) Append($"WINDOW {name}: not loaded");
            return;
        }
        var addon = (AtkUnitBase*)wrapper.Address;
        var key = $"{name}#{addon->Id}";
        if (checkpoint)
            Append($"WINDOW {key}: visible={addon->IsVisible}; {DescribeAgent(wrapper)}; parent={addon->ParentId}, host={addon->HostId}");
        CaptureValues(key + "/cached", addon->AtkValues, addon->AtkValuesCount, checkpoint);
        var nodes = new List<string>();
        var visited = new HashSet<nint>();
        ReadTextNodes(&addon->UldManager, "", nodes, visited, 0);
        RecordChanges(key + "/text (display only)", nodes.ToArray(), checkpoint);
    }

    private unsafe void CaptureValues(string key, AtkValue* values, uint count, bool checkpoint)
    {
        if (values == null || count == 0)
        {
            RecordChanges(key, ["no AtkValues"], checkpoint);
            return;
        }
        var lines = new List<string> { $"count={count}" };
        for (var i = 0; i < Math.Min(count, (uint)ValueLimit); i++)
        {
            var value = new AtkValuePtr((nint)(values + i));
            var type = value.ValueType.ToString();
            if (type.Contains("Pointer", StringComparison.Ordinal) || type.Contains("Vector", StringComparison.Ordinal)
                || type.Contains("AtkValues", StringComparison.Ordinal))
            {
                lines.Add($"[{i}] {type}: not dereferenced");
                continue;
            }
            try
            {
                var boxed = value.GetValue();
                var formatted = boxed is IFormattable number
                    ? number.ToString(null, CultureInfo.InvariantCulture)
                    : boxed?.ToString() ?? "null";
                lines.Add($"[{i}] {type}: {Clean(formatted)}");
            }
            catch (NotImplementedException)
            {
                lines.Add($"[{i}] {type}: unsupported type");
            }
        }
        if (count > ValueLimit) lines.Add("[remaining values omitted at limit]");
        RecordChanges(key, lines.ToArray(), checkpoint);
    }

    private void RecordChanges(string key, string[] current, bool checkpoint)
    {
        if (!lastValues.TryGetValue(key, out var previous))
            Append(key + " FIRST\n  " + string.Join("\n  ", current));
        else
        {
            var changes = new List<string>();
            for (var i = 0; i < current.Length; i++)
                if (i >= previous.Length || current[i] != previous[i]) changes.Add(current[i]);
            if (current.Length < previous.Length) changes.Add($"entry count reduced: {previous.Length} -> {current.Length}");
            if (changes.Count > 0) Append(key + " CHANGED\n  " + string.Join("\n  ", changes));
            else if (checkpoint) Append(key + " same as previous");
        }
        lastValues[key] = current;
    }

    private static unsafe void ReadTextNodes(AtkUldManager* manager, string path,
        List<string> lines, HashSet<nint> visited, int depth)
    {
        if (manager == null || manager->NodeList == null || depth > 4 || visited.Count >= 300) return;
        for (var i = 0; i < Math.Min((int)manager->NodeListCount, 300) && visited.Count < 300; i++)
        {
            var node = manager->NodeList[i];
            if (node == null || !visited.Add((nint)node)) continue;
            var nodePath = path + node->NodeId;
            if (node->Type == NodeType.Text)
            {
                var text = Clean(((AtkTextNode*)node)->NodeText.ToString());
                if (text.Length > 0) lines.Add($"node[{nodePath}] visible={node->IsVisible()} \"{text}\"");
            }
            else if ((int)node->Type >= 1000)
            {
                var component = ((AtkComponentNode*)node)->Component;
                if (component != null) ReadTextNodes(&component->UldManager, nodePath + "/", lines, visited, depth + 1);
            }
        }
        if (depth == 0 && visited.Count >= 300) lines.Add("[node scan stopped at limit]");
    }

    private static string Clean(string text)
    {
        var limitedText = text.Length > 512 ? text[..512] + " [truncated]" : text;
        return limitedText.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t").Replace("\0", string.Empty);
    }
}
