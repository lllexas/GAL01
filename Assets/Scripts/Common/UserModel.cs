using System;
using System.Collections.Generic;
using NekoGraph;

[Serializable]
public class SaveMetadata
{
    public string PlayerName = "玩家";
    public long SaveTimestamp;
}

[Serializable]
public class UserModel : IUserPackData
{
    public SaveMetadata Metadata = new SaveMetadata();
    public Dictionary<string, BasePackData> PackDataDict = new Dictionary<string, BasePackData>();
    public Dictionary<string, Dictionary<string, BasePackData>> EntityPackDataDict =
        new Dictionary<string, Dictionary<string, BasePackData>>();

    public Dictionary<string, BasePackData> GetPlayerPackDict()
    {
        PackDataDict ??= new Dictionary<string, BasePackData>();
        return PackDataDict;
    }

    public Dictionary<string, BasePackData> GetEntityPackDict(GraphInstanceSlot slot, bool createIfMissing)
    {
        return GetEntityPackDict(slot.ToString(), createIfMissing);
    }

    public Dictionary<string, BasePackData> GetEntityPackDict(string entityId, bool createIfMissing = false)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            return null;
        }

        EntityPackDataDict ??= new Dictionary<string, Dictionary<string, BasePackData>>();
        if (EntityPackDataDict.TryGetValue(entityId, out var packDict))
        {
            return packDict;
        }

        if (!createIfMissing)
        {
            return null;
        }

        packDict = new Dictionary<string, BasePackData>();
        EntityPackDataDict[entityId] = packDict;
        return packDict;
    }

    public BasePackData FindPackByPackID(string packId)
    {
        if (string.IsNullOrWhiteSpace(packId) || PackDataDict == null)
        {
            return null;
        }

        foreach (var pair in PackDataDict)
        {
            if (pair.Value != null && string.Equals(pair.Value.PackID, packId, StringComparison.Ordinal))
            {
                return pair.Value;
            }
        }

        return null;
    }
}
