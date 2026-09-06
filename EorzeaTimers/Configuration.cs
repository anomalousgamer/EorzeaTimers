using System;
using Dalamud.Configuration;
using EorzeaTimers.Models;

namespace EorzeaTimers;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public TimerEntry? Timer { get; set; }

    public string LastAcknowledgedVersion { get; set; } = string.Empty;

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
