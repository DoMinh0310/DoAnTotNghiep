using UnityEngine;

namespace ProjectM.Managers
{
    /// <summary>
    /// Hệ thống log trận đấu có thể bật/tắt.
    /// Đặt component này vào 1 GameObject trong BattleScene.
    /// Khi không cần nữa: bỏ tích Verbose hoặc xoá GameObject đi.
    /// </summary>
    public class BattleDebugger : MonoBehaviour
    {
        public static BattleDebugger Instance { get; private set; }

        [Header("Toggle")]
        [Tooltip("Bỏ tích để tắt toàn bộ log từ hệ thống Battle.")]
        public bool verbose = true;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ── Static helpers — dùng ở bất kỳ script nào ──────────────────

        public static void Log(string msg)
        {
            if (Instance != null && Instance.verbose)
                Debug.Log($"<color=#00CFFF>[Battle]</color> {msg}");
        }

        public static void Warn(string msg)
        {
            if (Instance != null && Instance.verbose)
                Debug.LogWarning($"<color=#FFD700>[Battle]</color> {msg}");
        }

        public static void Error(string msg)
        {
            if (Instance != null && Instance.verbose)
                Debug.LogError($"<color=#FF4444>[Battle]</color> {msg}");
        }
    }
}
