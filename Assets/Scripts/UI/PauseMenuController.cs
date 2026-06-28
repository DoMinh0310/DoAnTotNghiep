using UnityEngine;
using UnityEngine.UI;

namespace ProjectM.UI
{
    public class PauseMenuController : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Kéo GameObject Panel chứa hình nền giấy da và 3 nút vào đây")]
        public GameObject pausePanel;

        [Header("Buttons")]
        [Tooltip("Nút icon Setting nhỏ ở góc màn hình")]
        public Button btnOpenSettings; 
        
        [Tooltip("Nút Resume bên trong Panel")]
        public Button btnResume;
        
        [Tooltip("Nút Back To Menu bên trong Panel")]
        public Button btnBackToMenu;
        
        [Tooltip("Nút Quit bên trong Panel")]
        public Button btnQuit;

        private void Start()
        {
            // Đảm bảo Panel luôn ẩn khi mới bắt đầu game
            if (pausePanel != null) pausePanel.SetActive(false);

            // Gắn sự kiện cho các nút
            if (btnOpenSettings != null) btnOpenSettings.onClick.AddListener(OpenPauseMenu);
            if (btnResume != null)       btnResume.onClick.AddListener(ResumeGame);
            if (btnBackToMenu != null)   btnBackToMenu.onClick.AddListener(BackToMenu);
            if (btnQuit != null)         btnQuit.onClick.AddListener(QuitGame);
        }

        public void OpenPauseMenu()
        {
            if (pausePanel != null) pausePanel.SetActive(true);
            
            // Nếu game của bạn là Real-time, bỏ comment dòng dưới để đóng băng thời gian
            // Time.timeScale = 0f; 
        }

        public void ResumeGame()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            
            // Bỏ comment dòng dưới nếu bạn dùng Time.timeScale = 0f ở trên
            // Time.timeScale = 1f;
        }

        public void BackToMenu()
        {
            // Tắt Panel đi để dọn dẹp trạng thái
            ResumeGame(); 

            // Save game và chuyển về Menu
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SaveGame();
                GameManager.Instance.LoadMenuScene();
            }
            else
            {
                Debug.LogWarning("[PauseMenu] Không tìm thấy GameManager để Save/Load Menu!");
            }
        }

        public void QuitGame()
        {
            // Tự động Save trước khi thoát game hoàn toàn
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SaveGame();
            }
            
            Debug.Log("Đã thoát game (Quit)!");
            Application.Quit();
        }
    }
}
