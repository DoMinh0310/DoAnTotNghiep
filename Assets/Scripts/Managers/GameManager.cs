using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections.Generic;

namespace ProjectM
{
    /// <summary>
    /// Singleton tồn tại xuyên scene. Giữ MapRunData và điều phối chuyển scene.
    /// Gắn vào 1 GameObject tên "GameManager" trong MenuScene.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Scene Names (phải khớp chính xác với tên trong Build Settings)")]
        public string menuSceneName   = "MenuScene";
        public string mapSceneName    = "MapScene";
        public string battleSceneName = "BattleScene";
        public string endSceneName    = "EndScene";

        [Header("Map Node Data Assets")]
        [Tooltip("Kéo tất cả MapNodeData assets vào đây (Battle, Boss, Card, Relic, Resource, Sacrifice, Smith)")]
        public List<Map.MapNodeData> allNodeDataAssets;

        // ── Runtime State ────────────────────────────
        public Map.MapRunData RunData { get; private set; }

        // ── Save Path ────────────────────────────────
        private static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

        // ════════════════════════════════════════════
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject); // Tồn tại xuyên scene
        }

        // ════════════════════════════════════════════
        // NEW GAME / CONTINUE
        // ════════════════════════════════════════════

        /// <summary>Bắt đầu run mới, xóa save cũ.</summary>
        public void StartNewRun()
        {
            RunData = new Map.MapRunData();
            int seed = Random.Range(0, 99999);
            RunData.InitNewRun(seed);
            SaveGame();
            LoadMapScene();
        }

        /// <summary>Load run đang dở từ save file.</summary>
        public bool TryContinueRun()
        {
            if (!File.Exists(SavePath)) return false;
            string json = File.ReadAllText(SavePath);
            RunData = JsonUtility.FromJson<Map.MapRunData>(json);
            return RunData != null;
        }

        /// <summary>
        /// CHỈ DÙNG ĐỂ TEST. Inject RunData đã tạo sẵn.
        /// </summary>
        public void InjectRunDataForTest(Map.MapRunData data)
        {
            RunData = data;
        }

        // ════════════════════════════════════════════
        // SCENE TRANSITIONS
        // ════════════════════════════════════════════

        public void LoadMapScene()    => SceneManager.LoadScene(mapSceneName);
        public void LoadBattleScene() => SceneManager.LoadScene(battleSceneName);
        public void LoadMenuScene()   => SceneManager.LoadScene(menuSceneName);
        public void LoadEndScene()    => SceneManager.LoadScene(endSceneName);

        /// <summary>
        /// Gọi sau khi người chơi thắng 1 combat.
        /// Cập nhật trạng thái và về Map.
        /// </summary>
        public void OnCombatWon()
        {
            if (RunData == null) return;
            
            // Nếu currentSlotIndex là 22 (Boss), nghĩa là vừa thắng Boss
            if (RunData.currentSlotIndex == 22)
            {
                SaveGame();
                LoadEndScene();
                return;
            }

            SaveGame();
            LoadMapScene();
        }

        /// <summary>
        /// Gọi khi người chơi click vào Combat node trên map.
        /// </summary>
        public void OnCombatNodeEntered()
        {
            if (RunData == null) return;
            SaveGame();
            LoadBattleScene();
        }

        /// <summary>
        /// Gọi sau khi người chơi hoàn thành 1 random event.
        /// </summary>
        public void OnEventCompleted()
        {
            if (RunData == null) return;
            SaveGame();
        }

        // ════════════════════════════════════════════
        // SAVE / LOAD
        // ════════════════════════════════════════════

        public void SaveGame()
        {
            if (RunData == null) return;
            string json = JsonUtility.ToJson(RunData, prettyPrint: true);
            File.WriteAllText(SavePath, json);
            Debug.Log($"[GameManager] Game saved → {SavePath}");
        }

        public bool HasSaveFile() => File.Exists(SavePath);

        public void DeleteSave()
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);
        }

        // ════════════════════════════════════════════
        // HELPER: Lấy MapNodeData theo type
        // ════════════════════════════════════════════

        public Map.MapNodeData GetNodeData(Map.NodeType type)
        {
            if (allNodeDataAssets == null) return null;
            return allNodeDataAssets.Find(d => d != null && d.nodeType == type);
        }
    }
}
