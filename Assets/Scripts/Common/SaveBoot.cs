using System;
using System.Collections.Generic;
using NekoGraph;
using UnityEngine;

public static class SaveBoot
{
    private sealed class BootEntry
    {
        public string PackId;
        public bool IsMetaPack;
        public bool Required;
    }

    private static readonly List<BootEntry> BootEntries = new()
    {
        new BootEntry { PackId = "MainStory", IsMetaPack = true, Required = true },
        new BootEntry { PackId = "Inventory", IsMetaPack = false, Required = true },
    };

    public static void Register(string packId, bool isMetaPack, bool required = true)
    {
        PackDefinitionRegistry.Register(packId, isMetaPack);
        BootEntries.Add(new BootEntry { PackId = packId, IsMetaPack = isMetaPack, Required = required });
    }

    public static void ApplyDefaultPacks(UserModel user)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        user.PackDataDict ??= new Dictionary<string, BasePackData>();
        foreach (var entry in BootEntries)
        {
            ApplyEntry(user, entry);
        }
    }

    public static void EnsureDefaultPacks(UserModel user)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        user.PackDataDict ??= new Dictionary<string, BasePackData>();
        foreach (var entry in BootEntries)
        {
            EnsureEntry(user, entry);
        }
    }

    private static void ApplyEntry(UserModel user, BootEntry entry)
    {
        PackDefinitionRegistry.Register(entry.PackId, entry.IsMetaPack);
        if (!PackDefinitionRegistry.TryCreatePack(entry.PackId, out var pack))
        {
            ReportFailure(entry.Required, $"Failed to load pack: {entry.PackId}");
            return;
        }

        pack.HasStarted = false;
        pack.ActiveSignals ??= new Queue<SignalContext>();
        pack.ActiveSignals.Clear();
        pack.Touch();
        user.PackDataDict[Guid.NewGuid().ToString("N")] = pack;
    }

    private static void EnsureEntry(UserModel user, BootEntry entry)
    {
        var existingPack = user.FindPackByPackID(entry.PackId);
        if (existingPack != null)
        {
            existingPack.PackID = string.IsNullOrWhiteSpace(existingPack.PackID) ? entry.PackId : existingPack.PackID;
            existingPack.Initialize();
            existingPack.ActiveSignals ??= new Queue<SignalContext>();
            return;
        }

        ApplyEntry(user, entry);
    }

    private static void ReportFailure(bool required, string message)
    {
        if (required)
        {
            Debug.LogError($"[SaveBoot] {message}");
        }
        else
        {
            Debug.LogWarning($"[SaveBoot] {message}");
        }
    }
}

public static class PackDefinitionRegistry
{
    private sealed class Definition
    {
        public string PackId;
        public bool IsMetaPack;
    }

    private static readonly Dictionary<string, Definition> Definitions = new(StringComparer.Ordinal)
    {
        ["MainStory"] = new Definition { PackId = "MainStory", IsMetaPack = true },
        ["Inventory"] = new Definition { PackId = "Inventory", IsMetaPack = false },
    };

    public static void Register(string packId, bool isMetaPack)
    {
        if (string.IsNullOrWhiteSpace(packId))
        {
            throw new ArgumentException("packId cannot be empty.", nameof(packId));
        }

        Definitions[packId] = new Definition
        {
            PackId = packId,
            IsMetaPack = isMetaPack
        };
    }

    public static bool TryCreatePack(string packId, out BasePackData pack)
    {
        pack = null;
        if (string.IsNullOrWhiteSpace(packId))
        {
            return false;
        }

        if (!Definitions.TryGetValue(packId, out var definition))
        {
            return false;
        }

        pack = definition.IsMetaPack
            ? MetaLib.GetPack<BasePackData>(definition.PackId)
            : new BasePackData { PackID = definition.PackId };

        if (pack == null)
        {
            Debug.LogError($"[PackDefinitionRegistry] Failed to create pack '{packId}'.");
            return false;
        }

        pack.PackID = string.IsNullOrWhiteSpace(pack.PackID) ? definition.PackId : pack.PackID;
        pack.Initialize();
        return true;
    }
}
