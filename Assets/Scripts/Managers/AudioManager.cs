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
    [Tooltip("Kéo thả thành phần AudioSource cho Map Music")]
    public AudioSource mapMusic;
    [Tooltip("Kéo thả thành phần AudioSource cho Gold Event Music")]
    public AudioSource goldEventMusic;
    [Tooltip("Kéo thả thành phần AudioSource cho Shop Event Music")]
    public AudioSource shopEventMusic;

    [Header("SFX Clips - Combat")]
    public AudioClip cardPickUpClip;
    public AudioClip cardDropClip;
    public AudioClip cardFlipClip;
    public AudioClip attackClip;
    public AudioClip hitClip;
    public AudioClip cardDeathClip;         // Tiếng khi có thẻ bài chết
    public AudioClip battleWinClip;         // Tiếng thắng trận
    public AudioClip battleLoseClip;        // Tiếng thua trận
    public AudioClip clockClickClip;        // Tiếng bấm đồng hồ
    public AudioClip speedReduceClip;       // Tiếng giảm speed (bộ đếm)
    
    [Header("SFX Clips - Element Damage")]
    public AudioClip frostDamageClip;       // Tiếng nổ Frost
    public AudioClip bleedDamageClip;       // Tiếng nổ Bleed
    public AudioClip decayDamageClip;       // Tiếng nổ Decay
    public AudioClip chainDamageClip;       // Tiếng nổ Chain

    [Header("SFX Clips - Map & Events")]
    public AudioClip inventoryToggleClip;   // Mở/đóng túi đồ
    public AudioClip cardSelectClip;        // Bấm chọn thẻ trong event (Sacrifice, Smith)
    public AudioClip shopEnterClip;         // Vào event shop
    public AudioClip coinDropClip;          // Bấm event tiền
    public AudioClip buyItemClip;           // Mua thẻ/relic/trinket
    public AudioClip acquireItemLayer1;     // Lấy relic/trinket/thẻ mới (Layer 1)
    public AudioClip acquireItemLayer2;     // Lấy relic/trinket/thẻ mới (Layer 2)
    public AudioClip playerMoveClip;        // Player token nhảy di chuyển
    public AudioClip recycleClip;           // Tiếng tái chế/đốt thẻ (Event Sacrifice)
    public AudioClip genericButtonClip;     // Tiếng bấm nút bấm chung (UI Menu/Map)
    public AudioClip upgradeHPClip;         // Tiếng ấn nút +HP (Smith Event)
    public AudioClip upgradeATKClip;        // Tiếng ấn nút +ATK (Smith Event)

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
            DontDestroyOnLoad(gameObject); // Giữ AudioManager sống qua mọi scene (Menu -> Map -> Battle)

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
        if (mapMusic != null) mapMusic.Stop();
        if (goldEventMusic != null) goldEventMusic.Stop();
        if (shopEventMusic != null) shopEventMusic.Stop();
        if (loopCoroutine != null) StopCoroutine(loopCoroutine);
    }

    public void PlayBattleMusic()
    {
        StopMusic();
        if (battleMusic != null)
        {
            battleMusic.loop = true;
            battleMusic.volume = defaultVolume;
            battleMusic.Play();
        }
    }

    public void PlayMapMusic()
    {
        StopMusic();
        if (mapMusic != null)
        {
            mapMusic.loop = true;
            mapMusic.volume = defaultVolume;
            mapMusic.Play();
        }
    }

    public void PlayGoldEventMusic()
    {
        StopMusic();
        if (goldEventMusic != null)
        {
            goldEventMusic.loop = true;
            goldEventMusic.volume = defaultVolume;
            goldEventMusic.Play();
        }
    }

    public void PlayShopEventMusic()
    {
        StopMusic();
        if (shopEventMusic != null)
        {
            shopEventMusic.loop = true;
            shopEventMusic.volume = defaultVolume;
            shopEventMusic.Play();
        }
    }

    // Phát 2 âm thanh đè lên nhau khi nhận Item/Relic/Thẻ bài
    public void PlayAcquireItemCombo()
    {
        if (acquireItemLayer1 != null && sfxSource != null) sfxSource.PlayOneShot(acquireItemLayer1);
        if (acquireItemLayer2 != null && sfxSource != null) sfxSource.PlayOneShot(acquireItemLayer2);
    }

    public void PlayElementDamageSFX(ProjectM.Elements.ElementType elementType)
    {
        switch (elementType)
        {
            case ProjectM.Elements.ElementType.Frost:
                PlaySFX(frostDamageClip);
                break;
            case ProjectM.Elements.ElementType.Bleed:
                PlaySFX(bleedDamageClip);
                break;
            case ProjectM.Elements.ElementType.Decay:
                PlaySFX(decayDamageClip);
                break;
            case ProjectM.Elements.ElementType.Chain:
                PlaySFX(chainDamageClip);
                break;
        }
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, volumeScale);
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
