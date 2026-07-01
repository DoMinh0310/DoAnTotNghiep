using UnityEngine;
using UnityEngine.UI;

namespace ProjectM.UI
{
    public class PauseMenuController : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Kéo GameObject Panel chứa hình nền giấy da và 3 nút vào đây")]
        public GameObject pausePanel;

        [Header("Panels inside Pause Menu")]
        [Tooltip("Panel chứa các nút chính: Resume, Sound, Back To Menu, Quit")]
        public GameObject mainButtonsPanel;
        [Tooltip("Panel chứa các slider chỉnh âm thanh và nút quay lại")]
        public GameObject soundSettingsPanel;

        [Header("Buttons")]
        [Tooltip("Nút icon Setting nhỏ ở góc màn hình")]
        public Button btnOpenSettings; 
        
        [Tooltip("Nút Resume bên trong Panel")]
        public Button btnResume;
        
        [Tooltip("Nút mở bảng Sound")]
        public Button btnSoundMenu;

        [Tooltip("Nút Back To Menu bên trong Panel")]
        public Button btnBackToMenu;
        
        [Tooltip("Nút Quit bên trong Panel")]
        public Button btnQuit;

        [Tooltip("Nút quay lại menu chính từ bảng Sound")]
        public Button btnBackFromSound;

        [Header("Audio Settings")]
        [Tooltip("Slider chỉnh âm lượng nhạc nền")]
        public Slider sliderMusic;
        
        [Tooltip("Slider chỉnh âm lượng hiệu ứng")]
        public Slider sliderSFX;

        private void Start()
        {
            // Đảm bảo Panel luôn ẩn khi mới bắt đầu game
            if (pausePanel != null) pausePanel.SetActive(false);

            // Gắn sự kiện cho các nút
            if (btnOpenSettings != null) btnOpenSettings.onClick.AddListener(OpenPauseMenu);
            if (btnResume != null)       btnResume.onClick.AddListener(ResumeGame);
            if (btnSoundMenu != null)    btnSoundMenu.onClick.AddListener(OpenSoundSettings);
            if (btnBackToMenu != null)   btnBackToMenu.onClick.AddListener(BackToMenu);
            if (btnQuit != null)         btnQuit.onClick.AddListener(QuitGame);
            if (btnBackFromSound != null) btnBackFromSound.onClick.AddListener(CloseSoundSettings);

            // Khởi tạo thanh gạt âm thanh (nếu có)
            if (sliderMusic != null)
            {
                sliderMusic.value = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
                sliderMusic.onValueChanged.AddListener(SetMusicVolume);
            }
            if (sliderSFX != null)
            {
                sliderSFX.value = PlayerPrefs.GetFloat("SFXVolume", 0.5f);
                sliderSFX.onValueChanged.AddListener(SetSFXVolume);
            }
        }

        private void Update()
        {
            // Bấm ESC để bật/tắt menu nhanh (Sử dụng New Input System)
            if (UnityEngine.InputSystem.Keyboard.current != null && 
                UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (pausePanel != null)
                {
                    if (pausePanel.activeSelf)
                        ResumeGame();
                    else
                        OpenPauseMenu();
                }
            }
        }

        public void OpenPauseMenu()
        {
            if (pausePanel != null) pausePanel.SetActive(true);
            
            // Đảm bảo hiển thị đúng menu chính khi mở lên
            if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true);
            if (soundSettingsPanel != null) soundSettingsPanel.SetActive(false);
            
            // Nếu game của bạn là Real-time, bỏ comment dòng dưới để đóng băng thời gian
            // Time.timeScale = 0f; 
        }

        public void ResumeGame()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            
            // Bỏ comment dòng dưới nếu bạn dùng Time.timeScale = 0f ở trên
            // Time.timeScale = 1f;
        }

        public void OpenSoundSettings()
        {
            if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false);
            if (soundSettingsPanel != null) soundSettingsPanel.SetActive(true);
        }

        public void CloseSoundSettings()
        {
            if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true);
            if (soundSettingsPanel != null) soundSettingsPanel.SetActive(false);
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

        public void SetMusicVolume(float volume)
        {
            PlayerPrefs.SetFloat("MusicVolume", volume);
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetMusicVolume(volume);
        }

        public void SetSFXVolume(float volume)
        {
            PlayerPrefs.SetFloat("SFXVolume", volume);
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetSFXVolume(volume);
        }
    }
}
