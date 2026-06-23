using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MenuBootstrapper : MonoBehaviour
{
    [Header("UI & Screen")]
    [Tooltip("Kéo thả UI Image màu đen (để làm màn hình chờ) vào đây")]
    public Image blackScreen;

    [Header("Timing (Đo bằng giây)")]
    [Tooltip("Thời gian để màn hình đen tối thui (giả vờ load)")]
    public float waitLoadingTime = 2.0f;
    [Tooltip("Thời gian màn hình đen từ từ sáng lên")]
    public float fadeOutDuration = 1.5f;

    private void Start()
    {
        // 1. Đảm bảo màn hình đen ngay lúc khởi động
        if (blackScreen != null)
        {
            blackScreen.gameObject.SetActive(true);
            Color c = blackScreen.color;
            c.a = 1f;
            blackScreen.color = c;
        }

        // Tạm thời dừng nhạc cho chắc ăn
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic();
        }

        // 2. Chạy chuỗi kịch bản mở màn
        StartCoroutine(IntroSequenceRoutine());
    }

    private IEnumerator IntroSequenceRoutine()
    {
        // --- BƯỚC 1: Hiện màu đen vài giây giả vờ Load Game ---
        yield return new WaitForSeconds(waitLoadingTime);

        // --- BƯỚC 2: Gọi AudioManager gốc để phát nhạc ---
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMenuMusic();
        }

        // --- BƯỚC 3: Làm mờ dần màn hình đen (Fade Out) ---
        if (blackScreen != null)
        {
            float time = 0f;
            Color startColor = blackScreen.color;
            Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

            while (time < fadeOutDuration)
            {
                blackScreen.color = Color.Lerp(startColor, endColor, time / fadeOutDuration);
                time += Time.deltaTime;
                yield return null;
            }

            blackScreen.color = endColor;
            blackScreen.gameObject.SetActive(false);
        }
    }
}
