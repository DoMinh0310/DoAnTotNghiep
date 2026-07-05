using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectM.UI
{
    public class MainMenuSettingsController : MonoBehaviour
    {
        [Header("Menu Panels")]
        [Tooltip("Kéo thả GameObject Panel hình tờ giấy vào đây")]
        public GameObject settingsPanel; 
        
        [Header("Buttons")]
        public Button btnCloseSettings; // Nút tắt bảng Setting
        public Button btnOpenSettings;  // Nút mở bảng Setting (ở ngoài Menu chính)

        [Header("Audio Settings")]
        public Slider sliderMusic;
        public Slider sliderSFX;
        
        [Header("Video Settings")]
        public TMP_Dropdown dropdownResolution;
        public Toggle toggleFullscreen;
        
        private Resolution[] resolutions;

        private void Start()
        {
            // 1. Ẩn panel lúc mới vô game
            if (settingsPanel != null) settingsPanel.SetActive(false);

            // 2. Gán sự kiện cho các nút bật tắt
            if (btnCloseSettings != null) btnCloseSettings.onClick.AddListener(CloseSettings);
            if (btnOpenSettings != null) btnOpenSettings.onClick.AddListener(OpenSettings);

            // 3. Setup Âm Thanh
            if (sliderMusic != null)
            {
                sliderMusic.value = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
                sliderMusic.onValueChanged.AddListener(SetMusicVolume);
                SetMusicVolume(sliderMusic.value);
            }
            
            if (sliderSFX != null)
            {
                sliderSFX.value = PlayerPrefs.GetFloat("SFXVolume", 0.5f);
                sliderSFX.onValueChanged.AddListener(SetSFXVolume);
                SetSFXVolume(sliderSFX.value);
            }

            // 4. Setup Hình ảnh
            InitializeVideoSettings();
        }

        private void Update()
        {
            // Bấm ESC để bật/tắt bảng setting nhanh (Sử dụng New Input System)
            if (UnityEngine.InputSystem.Keyboard.current != null && 
                UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (settingsPanel != null)
                {
                    if (settingsPanel.activeSelf)
                        CloseSettings();
                    else
                        OpenSettings();
                }
            }
        }

        public void OpenSettings()
        {
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        public void CloseSettings()
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
            
            // Khi đóng bảng lại thì Save cứng mọi cấu hình vào máy!
            PlayerPrefs.Save(); 
        }

        // ==========================================
        // QUẢN LÝ AUDIO
        // ==========================================
        public void SetMusicVolume(float volume)
        {
            PlayerPrefs.SetFloat("MusicVolume", volume);
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMusicVolume(volume);
            }
        }

        public void SetSFXVolume(float volume)
        {
            PlayerPrefs.SetFloat("SFXVolume", volume);
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetSFXVolume(volume);
            }
        }

        // ==========================================
        // QUẢN LÝ VIDEO
        // ==========================================
        private void InitializeVideoSettings()
        {
            if (dropdownResolution == null || toggleFullscreen == null) return;

            Resolution[] rawResolutions = Screen.resolutions;
            System.Collections.Generic.Dictionary<string, Resolution> bestResMap = new System.Collections.Generic.Dictionary<string, Resolution>();
            System.Collections.Generic.List<string> orderKeys = new System.Collections.Generic.List<string>();

            // Lọc trùng lặp: mỗi kích thước Width x Height chỉ giữ lại 1 option có tần số quét tốt nhất
            for (int i = 0; i < rawResolutions.Length; i++)
            {
                Resolution r = rawResolutions[i];
                string key = r.width + "x" + r.height;
                if (!bestResMap.ContainsKey(key))
                {
                    bestResMap[key] = r;
                    orderKeys.Add(key);
                }
                else
                {
                    bestResMap[key] = r; // Lấy bản có tần số quét cao hơn (đứng sau)
                }
            }

            System.Collections.Generic.List<Resolution> uniqueList = new System.Collections.Generic.List<Resolution>();
            foreach (var k in orderKeys)
            {
                uniqueList.Add(bestResMap[k]);
            }

            resolutions = uniqueList.ToArray();
            dropdownResolution.ClearOptions();

            System.Collections.Generic.List<string> options = new System.Collections.Generic.List<string>();
            int currentResIndex = 0;

            for (int i = 0; i < resolutions.Length; i++)
            {
                string option = resolutions[i].width + " x " + resolutions[i].height;
                options.Add(option);

                if (resolutions[i].width == Screen.currentResolution.width &&
                    resolutions[i].height == Screen.currentResolution.height)
                {
                    currentResIndex = i;
                }
            }

            dropdownResolution.AddOptions(options);

            int savedIndex = PlayerPrefs.GetInt("ResolutionIndex", currentResIndex);
            if (savedIndex >= resolutions.Length || savedIndex < 0) savedIndex = currentResIndex;

            dropdownResolution.value = savedIndex;
            dropdownResolution.RefreshShownValue();
            
            dropdownResolution.onValueChanged.AddListener(SetResolution);

            toggleFullscreen.isOn = Screen.fullScreen;
            toggleFullscreen.onValueChanged.AddListener(SetFullscreen);
        }

        public void SetResolution(int resolutionIndex)
        {
            Resolution res = resolutions[resolutionIndex];
            Screen.SetResolution(res.width, res.height, Screen.fullScreen);
            
            PlayerPrefs.SetInt("ResolutionIndex", resolutionIndex);
        }

        public void SetFullscreen(bool isFullscreen)
        {
            Screen.fullScreen = isFullscreen;
        }
    }
}
