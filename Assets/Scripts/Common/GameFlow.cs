using System;
using System.IO;
using System.Linq;
using UnityEngine;
using NekoGraph;

public class GameFlow : SingletonMono<GameFlow>
{
    public enum LaunchMode
    {
        BuildMainMenu = 0,
        QuickNewSave = 1,
        QuickLoadSave = 2,
        QuickLoadLatest = 3
    }

    [Header("Boot")]
    [SerializeField] private bool _runOnStart = true;
    [SerializeField] private LaunchMode _launchMode = LaunchMode.BuildMainMenu;

    [Header("Debug Save")]
    [SerializeField] private string _debugSaveName = "debug_save";

    [Header("Post Events")]
    [SerializeField] private bool _sendEnterRootMenuAfterBoot = true;
    [SerializeField] private bool _sendGameStartedAfterBoot = false;

    private bool _hasBootstrapped;

    private string SaveDirectory => Path.Combine(Application.persistentDataPath, "Saves");

    private void Start()
    {
        if (_runOnStart)
        {
            Bootstrap();
        }
    }

    [ContextMenu("Bootstrap Now")]
    public void Bootstrap()
    {
        if (_hasBootstrapped)
        {
            Debug.LogWarning("[GameFlow] Bootstrap skipped: already executed.");
            return;
        }

        _hasBootstrapped = true;
        Debug.Log($"[GameFlow] Bootstrap mode: {_launchMode}");

        switch (_launchMode)
        {
            case LaunchMode.BuildMainMenu:
                EnterMainMenuState();
                break;

            case LaunchMode.QuickNewSave:
                QuickCreateSave(_debugSaveName);
                break;

            case LaunchMode.QuickLoadSave:
                QuickLoadSave(_debugSaveName);
                break;

            case LaunchMode.QuickLoadLatest:
                QuickLoadLatest();
                break;

            default:
                EnterMainMenuState();
                break;
        }
    }

    public void EnterMainMenuState()
    {
        MainModel.Instance.ClearCurrentStage();
        GraphHub.Instance?.ApplyUser(null);
        DispatchBootEvents();
        Debug.Log("[GameFlow] Entered main-menu state without creating a save.");
    }

    public void QuickCreateSave(string saveName)
    {
        if (string.IsNullOrWhiteSpace(saveName))
        {
            Debug.LogError("[GameFlow] QuickCreateSave failed: save name is empty.");
            return;
        }

        SaveManager.Instance.CreateNewSave(saveName);
        DispatchBootEvents();
        Debug.Log($"[GameFlow] Quick-created save: {saveName}");
    }

    public void QuickLoadSave(string saveName)
    {
        if (string.IsNullOrWhiteSpace(saveName))
        {
            Debug.LogError("[GameFlow] QuickLoadSave failed: save name is empty.");
            return;
        }

        SaveManager.Instance.LoadSave(saveName);
        DispatchBootEvents();
        Debug.Log($"[GameFlow] Quick-loaded save: {saveName}");
    }

    public void QuickLoadLatest()
    {
        string latestSave = GetLatestSaveName();
        if (string.IsNullOrEmpty(latestSave))
        {
            Debug.LogWarning("[GameFlow] No existing save found. Falling back to quick create.");
            QuickCreateSave(_debugSaveName);
            return;
        }

        QuickLoadSave(latestSave);
    }

    private string GetLatestSaveName()
    {
        if (!Directory.Exists(SaveDirectory))
        {
            return null;
        }

        return Directory
            .GetFiles(SaveDirectory, "*.json")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .Select(Path.GetFileNameWithoutExtension)
            .FirstOrDefault();
    }

    private void DispatchBootEvents()
    {
        if (_sendEnterRootMenuAfterBoot)
        {
            PostSystem.Instance?.Send("进入根界面", null);
        }

        if (_sendGameStartedAfterBoot)
        {
            PostSystem.Instance?.Send("GameStarted", null);
        }
    }
}
