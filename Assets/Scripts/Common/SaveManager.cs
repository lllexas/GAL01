using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NekoGraph;
using UnityEngine;

public class SaveManager : SingletonMono<SaveManager>
{
    private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
    {
        Formatting = Formatting.Indented,
        TypeNameHandling = TypeNameHandling.None,
        NullValueHandling = NullValueHandling.Ignore
    };

    public string CurrentSaveFileName { get; private set; } = "default_save";

    private string SaveDirectory => Path.Combine(Application.persistentDataPath, "Saves");

    protected override void Awake()
    {
        base.Awake();

        if (!Directory.Exists(SaveDirectory))
        {
            Directory.CreateDirectory(SaveDirectory);
        }
    }

    public void CreateNewSave(string saveName)
    {
        if (string.IsNullOrWhiteSpace(saveName))
        {
            Debug.LogWarning("[SaveManager] CreateNewSave ignored: save name is empty.");
            return;
        }

        Debug.Log($"<color=cyan>[SaveManager]</color> Creating new save: {saveName}");

        UnloadCurrentWorld();

        var newUser = new UserModel();
        newUser.Metadata.PlayerName = "Player-" + saveName;
        newUser.Metadata.SaveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        SaveBoot.ApplyDefaultPacks(newUser);

        MainModel.Instance.SetCurrentUser(newUser);
        GraphHub.Instance?.ApplyUser(newUser);

        CurrentSaveFileName = saveName;
        SaveGameToDisk();
    }

    public void LoadSave(string saveName)
    {
        string fullPath = Path.Combine(SaveDirectory, saveName + ".json");
        if (!File.Exists(fullPath))
        {
            Debug.LogError($"<color=red>[SaveManager]</color> Save file not found: {saveName}");
            return;
        }

        Debug.Log($"<color=yellow>[SaveManager]</color> Loading save: {saveName}");
        UnloadCurrentWorld();

        try
        {
            string json = File.ReadAllText(fullPath);
            UserModel loadedUser = JsonConvert.DeserializeObject<UserModel>(json, JsonSettings);
            if (loadedUser == null)
            {
                throw new JsonException("Deserialized UserModel is null.");
            }

            MainModel.Instance.SetCurrentUser(loadedUser);
            GraphHub.Instance?.ApplyUser(loadedUser);
            CurrentSaveFileName = saveName;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] Failed to load save '{saveName}': {ex.Message}");
            Debug.LogException(ex);
        }
    }

    public void SaveGameToDisk()
    {
        var currentUser = MainModel.Instance.CurrentUser;
        if (currentUser == null)
        {
            return;
        }

        currentUser.Metadata.SaveTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        string json = JsonConvert.SerializeObject(currentUser, JsonSettings);
        string fullPath = Path.Combine(SaveDirectory, CurrentSaveFileName + ".json");
        File.WriteAllText(fullPath, json);

        Debug.Log($"<color=cyan>[SaveManager]</color> Save written: {fullPath}");
    }

    public void DeleteSave(string saveName)
    {
        string fullPath = Path.Combine(SaveDirectory, saveName + ".json");
        if (!File.Exists(fullPath))
        {
            return;
        }

        File.Delete(fullPath);
        Debug.Log($"<color=red>[SaveManager]</color> Save deleted: {saveName}");
    }

    public bool RenameSave(string oldName, string newName)
    {
        if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName))
        {
            Debug.LogWarning("<color=orange>[SaveManager]</color> Rename failed: save name is empty.");
            return false;
        }

        if (oldName == newName)
        {
            return true;
        }

        string oldPath = Path.Combine(SaveDirectory, oldName + ".json");
        string newPath = Path.Combine(SaveDirectory, newName + ".json");

        if (!File.Exists(oldPath))
        {
            Debug.LogError($"<color=red>[SaveManager]</color> Rename failed: save not found '{oldName}'.");
            return false;
        }

        if (File.Exists(newPath))
        {
            Debug.LogWarning($"<color=orange>[SaveManager]</color> Rename failed: target already exists '{newName}'.");
            return false;
        }

        try
        {
            File.Move(oldPath, newPath);

            if (CurrentSaveFileName == oldName)
            {
                CurrentSaveFileName = newName;
            }

            if (MainModel.Instance.CurrentUser != null && CurrentSaveFileName == newName)
            {
                MainModel.Instance.CurrentUser.Metadata.PlayerName = "Player-" + newName;
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"<color=red>[SaveManager]</color> Rename failed: {ex.Message}");
            return false;
        }
    }

    public List<string> GetAllSaveFiles()
    {
        if (!Directory.Exists(SaveDirectory))
        {
            return new List<string>();
        }

        return Directory
            .GetFiles(SaveDirectory, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .ToList();
    }

    private void UnloadCurrentWorld()
    {
        MainModel.Instance.ClearCurrentStage();
        MainModel.Instance.ClearCurrentUser();
        GC.Collect();
    }
}
