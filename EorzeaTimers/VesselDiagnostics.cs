using System;
using System.Text;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace EorzeaTimers;

// Read-only probe. The remote Estate payload and request entry point are not
// mapped by FFXIVClientStructs; this only records accessible state transitions.
internal sealed class VesselDiagnostics
{
    private bool wasLoaded;
    private bool lastAgentActive;
    private bool lastWorkshopPresent;
    private uint lastContextItemId;
    private DateTime nextLoginSampleUtc;

    internal void Update(bool enabled)
    {
        if (!enabled)
        {
            wasLoaded = false;
            return;
        }

        if (!Plugin.PlayerState.IsLoaded)
        {
            wasLoaded = false;
            return;
        }

        if (!wasLoaded)
        {
            wasLoaded = true;
            nextLoginSampleUtc = DateTime.UtcNow.AddSeconds(5);
            Plugin.Log.Information("[Vessel probe] Logged in; sampling client state in five seconds. No game UI was opened.");
        }

        if (nextLoginSampleUtc != default && DateTime.UtcNow >= nextLoginSampleUtc)
        {
            nextLoginSampleUtc = default;
            Sample("after login");
        }

        var (active, contextId, workshopPresent) = GetState();
        if (active != lastAgentActive || contextId != lastContextItemId
            || workshopPresent != lastWorkshopPresent)
        {
            lastAgentActive = active;
            lastContextItemId = contextId;
            lastWorkshopPresent = workshopPresent;
            Sample("client state changed");
        }
    }

    internal static unsafe void Sample(string reason)
    {
        if (!Plugin.PlayerState.IsLoaded)
        {
            Plugin.Log.Information("[Vessel probe] {Reason}: not logged into a character.", reason);
            return;
        }

        var agent = AgentContentsTimer.Instance();
        var housing = HousingManager.Instance();
        var workshop = housing == null ? null : housing->WorkshopTerritory;
        Plugin.Log.Information(
            "[Vessel probe] {Reason}: ContentsTimer agent={AgentPresent}, active={AgentActive}, context item={ContextId}; WorkshopTerritory={WorkshopPresent}.",
            reason,
            agent != null,
            agent != null && agent->IsAgentActive(),
            agent == null ? 0u : agent->ContextMenuItemId,
            workshop != null);

        if (workshop == null)
        {
            Plugin.Log.Information("[Vessel probe] No workshop structure accessible. This does not establish whether Ctrl+U has a separate vessel cache.");
            return;
        }

        var submarines = (HousingWorkshopSubmersibleSubData*)&workshop->Submersible;
        var airships = (HousingWorkshopAirshipSubData*)&workshop->Airship;
        for (var index = 0; index < 4; index++)
        {
            if (submarines[index].RankId > 0)
            {
                Plugin.Log.Information(
                    "[Vessel probe] Submarine slot {Slot}: {Name}, register={Register}, return={Return} (Unix seconds).",
                    index,
                    Encoding.UTF8.GetString(submarines[index].Name).TrimEnd('\0'),
                    submarines[index].RegisterTime,
                    submarines[index].ReturnTime);
            }

            if (airships[index].RankId > 0)
            {
                Plugin.Log.Information(
                    "[Vessel probe] Airship slot {Slot}: {Name}, register={Register}, return={Return} (Unix seconds).",
                    index,
                    Encoding.UTF8.GetString(airships[index].Name).TrimEnd('\0'),
                    airships[index].RegisterTime,
                    airships[index].ReturnTime);
            }
        }
    }

    private static unsafe (bool Active, uint ContextId, bool WorkshopPresent) GetState()
    {
        var agent = AgentContentsTimer.Instance();
        var housing = HousingManager.Instance();
        return (
            agent != null && agent->IsAgentActive(),
            agent == null ? 0u : agent->ContextMenuItemId,
            housing != null && housing->WorkshopTerritory != null);
    }
}
