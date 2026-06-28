using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    // Cấu trúc Singleton giúp các script khác dễ dàng gọi: AudioManager.Instance.PlayMusic();
    public static AudioManager Instance { get; private set; }

    [Header("Audio Settings")]
    [Tooltip("Kéo thả thành phần AudioSource cho Menu Music")]
    public AudioSource menuMusic;
    [Tooltip("Kéo thả thành phần AudioSource cho Battle Music")]
    public AudioSource battleMusic;
    [Tooltip("Kéo thả thành phần AudioSource cho SFX (hiệu ứng)")]
    public AudioSource sfxSource;

    [Header("SFX Clips")]
    public AudioClip cardPickUpClip;
    public AudioClip cardDropClip;
    public AudioClip cardFlipClip;
    public AudioClip attackClip;
    public AudioClip hitClip;

    [Header("Custom Loop Settings")]
    [Tooltip("Tổng thời gian 1 vòng lặp (1 phút 5 giây = 65 giây)")]
    public float totalLoopDuration = 65f;
    [Tooltip("Thời gian nhỏ dần ở cuối vòng lặp (giây)")]
    public float fadeOutDuration = 4.0f;

    // Lưu trữ mức âm lượng chuẩn mà bạn đã thiết lập ở Inspector
    private float defaultVolume = 0.3f;
    private Coroutine loopCoroutine;

    private void Awake()
    {
        // Khởi tạo Singleton
        if (Instance == null)
        {
            Instance = this;
            if (menuMusic != null)
            {
                // Lưu lại âm lượng gốc trước khi code nhúng tay vào làm nhỏ đi
                defaultVolume = menuMusic.volume;
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayMenuMusic()
    {
        if (menuMusic != null)
        {
            // Tắt tính năng Loop mặc định của Unity để tự mình điều khiển Loop bằng script
            menuMusic.loop = false;
            
            // Xóa vòng lặp cũ nều có để tránh trùng nhảy nhạc
            if (loopCoroutine != null) StopCoroutine(loopCoroutine);
            
            loopCoroutine = StartCoroutine(CustomLoopSequence());
        }
    }

    public void StopMusic()
    {
        if (menuMusic != null) menuMusic.Stop();
        if (battleMusic != null) battleMusic.Stop();
        if (loopCoroutine != null) StopCoroutine(loopCoroutine);
    }

    public void PlayBattleMusic()
    {
        if (battleMusic != null && !battleMusic.isPlaying)
        {
            StopMusic(); // Dừng nhạc cũ
            battleMusic.loop = true; // Nhạc battle lặp mặc định
            battleMusic.volume = defaultVolume;
            battleMusic.Play();
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    // ─── THÊM HÀM ĐỂ SETTING MENU CÓ THỂ ĐIỀU CHỈNH ÂM LƯỢNG ───
    public void SetMusicVolume(float volume)
    {
        defaultVolume = volume; // Cập nhật lại gốc để CustomLoop không đè lại
        if (menuMusic != null) menuMusic.volume = volume;
        if (battleMusic != null) battleMusic.volume = volume;
    }

    public void SetSFXVolume(float volume)
    {
        if (sfxSource != null) sfxSource.volume = volume;
    }
    // ────────────────────────────────────────────────────────

    /// <summary>
    /// Phát một đoạn âm thanh SFX với điểm bắt đầu và độ dài tùy chỉnh.
    /// Cho phép lấy bất kỳ đoạn nào trong file mà không cần cắt file gốc.
    /// </summary>
    /// <param name="clip">Âm thanh cần phát</param>
    /// <param name="duration">Độ dài muốn phát (giây). 0 = phát hết file.</param>
    /// <param name="startTime">Bắt đầu từ giây bao nhiêu trong file. Mặc định = 0 (từ đầu).</param>
    /// <param name="volume">Âm lượng riêng cho clip này (0-1). -1 = dùng âm lượng mặc định của sfxSource.</param>
    public void PlaySFXClipped(AudioClip clip, float duration = 0f, float startTime = 0f, float volume = -1f)
    {
        if (clip == null) return;

        // Tạo một AudioSource tạm để không cắt ngang các SFX khác đang phát
        AudioSource tempSource = gameObject.AddComponent<AudioSource>();
        tempSource.clip   = clip;
        tempSource.volume = volume >= 0f ? volume : (sfxSource != null ? sfxSource.volume : defaultVolume);
        tempSource.time   = Mathf.Clamp(startTime, 0f, clip.length); // Tua đến điểm bắt đầu
        tempSource.Play();

        // Thời gian hủy: nếu duration = 0 thì phát hết phần còn lại của clip
        float destroyAfter = duration > 0f ? duration : (clip.length - startTime);
        Destroy(tempSource, destroyAfter);
    }

    private IEnumerator CustomLoopSequence()
    {
        while (true) // Bắt đầu vòng lặp vô hạn
        {
            // Reset nhạc về đầu và trả lại âm lượng chuẩn
            menuMusic.time = 0f;
            menuMusic.volume = defaultVolume;
            menuMusic.Play();

            // Tính toán khoảng thời gian hát bình thường (trước khi tới đoạn cần nhỏ dần)
            // Ví dụ: dài 65s, mờ 3s -> Vậy hát căng 62s đầu.
            float normalPlayTime = totalLoopDuration - fadeOutDuration;
            
            // Quãng 1: Ngồi chờ nó hát bình thường
            yield return new WaitForSeconds(normalPlayTime);

            // Quãng 2: Bắt đầu 3 giây cuối làm nhỏ dần âm lượng xuống 0 (Fade Out)
            float fadeTimer = 0f;
            while (fadeTimer < fadeOutDuration)
            {
                // Lerp để chuyển mượt từ mức âm chuẩn xuống 0 theo đúng nhịp đồng hồ
                menuMusic.volume = Mathf.Lerp(defaultVolume, 0f, fadeTimer / fadeOutDuration);
                fadeTimer += Time.deltaTime;
                yield return null;
            }

            // Ép volume về 0 để kết liễu hoàn toàn âm thanh
            menuMusic.volume = 0f;
            menuMusic.Stop();

            // Vòng quay của lệnh while(true) sẽ quay lại lên trên cùng: Tự động tua về đầu và hát lại!
        }
    }
}
