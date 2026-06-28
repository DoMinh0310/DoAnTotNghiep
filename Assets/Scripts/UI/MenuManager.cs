using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace ProjectM.UI
{
    /// <summary>
    /// Quản lý các nút trong MenuScene: Start, Continue, Exit.
    /// Gắn vào 1 object trong Canvas của MenuScene.
    /// </summary>
    public class MenuManager : MonoBehaviour
    {
        [Header("Buttons")]
        public Button startButton;
        public Button continueButton;
        public Button exitButton;

        [Header("Character Select Panel")]
        public CharacterSelectPanel characterSelectPanel;

        private void Start()
        {
            // Chỉ bật nút Continue nếu có save file
            if (continueButton != null)
                continueButton.interactable = GameManager.Instance != null && GameManager.Instance.HasSaveFile();

            if (startButton != null)
                startButton.onClick.AddListener(OnStartClicked);

            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueClicked);

            if (exitButton != null)
                exitButton.onClick.AddListener(OnExitClicked);
        }

        private void OnStartClicked()
        {
            // Mở panel chọn nhân vật thay vì vào map ngay
            if (characterSelectPanel != null)
                characterSelectPanel.Open();
            else
            {
                // Fallback nếu không có panel: vào game luôn
                GameManager.Instance?.StartNewRun(null);
            }
        }

        private void OnContinueClicked()
        {
            if (GameManager.Instance == null) return;

            bool ok = GameManager.Instance.TryContinueRun();
            if (ok)
                GameManager.Instance.LoadMapScene();
            else
                Debug.LogWarning("[MenuManager] Không tìm thấy save file!");
        }

        private void OnExitClicked()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
