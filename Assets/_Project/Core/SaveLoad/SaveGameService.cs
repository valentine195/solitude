using System;
using System.IO;
using UnityEngine;

namespace SOLITUDE.SaveLoad
{
    /// <summary>
    /// Scene-persistent JSON save store. Add one instance to the same bootstrap
    /// object as GameManager. World objects only address their own records by
    /// SaveableId; they do not own the shared save state.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class SaveGameService : MonoBehaviour
    {
        private const string FileName = "solitude-save.json";

        public static SaveGameService Instance { get; private set; }

        [SerializeField] private int newGameWorldSeed = 12345;
        private SaveGameData data;

        public int WorldSeed => data.worldSeed;
        private string SavePath => Path.Combine(Application.persistentDataPath, FileName);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadFromDisk();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveToDisk();
        }

        private void OnApplicationQuit() => SaveToDisk();

        public bool TryGetContainer(string saveableId, out ContainerSaveData state)
        {
            state = null;
            if (string.IsNullOrEmpty(saveableId) || data?.containers == null) return false;

            foreach (var record in data.containers)
            {
                if (record != null && record.saveableId == saveableId)
                {
                    state = record.state ?? new ContainerSaveData();
                    return true;
                }
            }

            return false;
        }

        public void SetContainer(string saveableId, ContainerSaveData state)
        {
            if (string.IsNullOrEmpty(saveableId))
            {
                Debug.LogError("[SaveGameService] Refusing to save a container with no SaveableId.");
                return;
            }

            data ??= CreateNewData();
            data.containers ??= new System.Collections.Generic.List<ContainerSaveRecord>();

            foreach (var record in data.containers)
            {
                if (record != null && record.saveableId == saveableId)
                {
                    record.state = state ?? new ContainerSaveData();
                    SaveToDisk();
                    return;
                }
            }

            data.containers.Add(new ContainerSaveRecord { saveableId = saveableId, state = state ?? new ContainerSaveData() });
            SaveToDisk();
        }

        [ContextMenu("Start New Save")]
        public void StartNewSave()
        {
            data = CreateNewData();
            SaveToDisk();
        }

        private void LoadFromDisk()
        {
            if (!File.Exists(SavePath))
            {
                data = CreateNewData();
                return;
            }

            try
            {
                data = JsonUtility.FromJson<SaveGameData>(File.ReadAllText(SavePath));
                if (data == null || data.version != SaveGameData.CurrentVersion)
                {
                    Debug.LogWarning("[SaveGameService] Save file is missing or from an unsupported version; starting a new save.");
                    data = CreateNewData();
                }
                else
                {
                    data.containers ??= new System.Collections.Generic.List<ContainerSaveRecord>();
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SaveGameService] Failed to load save file: {exception.Message}");
                data = CreateNewData();
            }
        }

        private void SaveToDisk()
        {
            if (data == null) return;

            try
            {
                File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SaveGameService] Failed to save: {exception.Message}");
            }
        }

        private SaveGameData CreateNewData() => new() { worldSeed = newGameWorldSeed };
    }
}
