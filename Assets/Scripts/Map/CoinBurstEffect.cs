using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

namespace ProjectM.Map
{
    /// <summary>
    /// Hiệu ứng đồng vàng bùng nổ khi vào Resource Event.
    /// Phase 1: Coin rơi ra tứ phía từ node, rơi xuống.
    /// Phase 2: Coin bay về icon tiền ở góc màn hình và cộng vàng.
    /// 
    /// SETUP: Gắn script này vào 1 GameObject trong Map Scene.
    ///        Kéo vào field CoinBurstEffect của MapManager.
    /// </summary>
    public class CoinBurstEffect : MonoBehaviour
    {
        [Header("Coin Appearance")]
        [Tooltip("Sprite icon đồng vàng (cùng icon dùng trong GoldDisplayUI)")]
        [SerializeField] private Sprite coinSprite;
        [SerializeField] private Vector2 coinSize = new Vector2(72f, 72f);
        [SerializeField] private int coinCount = 14;

        [Header("Phase 1 – Burst & Fall")]
        [Tooltip("Lực văng ngang tối thiểu (px/s) — giảm xuống để xu ít văng sang 2 bên")]
        [SerializeField] private float horizontalSpeedMin = 50f;
        [Tooltip("Lực văng ngang tối đa (px/s)")]
        [SerializeField] private float horizontalSpeedMax = 220f;
        [Tooltip("Lực bắt đầu nảy lên tối thiểu (px/s)")]
        [SerializeField] private float verticalSpeedMin = 350f;
        [Tooltip("Lực bắt đầu nảy lên tối đa (px/s)")]
        [SerializeField] private float verticalSpeedMax = 650f;
        [SerializeField] private float gravity = 1400f;
        [SerializeField] private float burstDuration = 1.0f; // Tăng lên 1 tí để xu nằm im trên đất trước khi bay đi

        [Header("Floor (Mặt đất)")]
        [Tooltip("Đồng xu sẽ chạm đất cách tâm event bao nhiêu pixel (Min)")]
        [SerializeField] private float floorDropMin = 100f;
        [Tooltip("Đồng xu sẽ chạm đất cách tâm event bao nhiêu pixel (Max)")]
        [SerializeField] private float floorDropMax = 130f;
        [Tooltip("Độ nảy khi chạm đất (0 = không nảy, 0.5 = nảy 1 nửa)")]
        [SerializeField] private float bounciness = 0.35f;

        [Header("Phase 2 – Fly to Gold Icon")]
        [SerializeField] private float flyDuration   = 0.4f;
        [SerializeField] private float staggerDelay  = 0.055f;

        // Overlay canvas riêng — đảm bảo coin hiển thị trên mọi thứ kể cả ScrollRect
        private Canvas        _overlay;
        private RectTransform _overlayRect;
        private bool          _isPlaying;

        // ────────────────────────────────────────────────────────────
        private void Awake()
        {
            var go = new GameObject("[CoinBurstOverlay]");
            go.transform.SetParent(transform);
            _overlay = go.AddComponent<Canvas>();
            _overlay.renderMode  = RenderMode.ScreenSpaceOverlay;
            _overlay.sortingOrder = 999;
            
            var scaler = go.AddComponent<CanvasScaler>();
            var parentCanvas = GetComponentInParent<Canvas>();
            var parentScaler = parentCanvas != null ? parentCanvas.GetComponent<CanvasScaler>() : null;
            if (parentScaler != null)
            {
                scaler.uiScaleMode = parentScaler.uiScaleMode;
                scaler.referenceResolution = parentScaler.referenceResolution;
                scaler.screenMatchMode = parentScaler.screenMatchMode;
                scaler.matchWidthOrHeight = parentScaler.matchWidthOrHeight;
            }
            else
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            _overlayRect = go.GetComponent<RectTransform>();
        }

        // ────────────────────────────────────────────────────────────
        /// <summary>
        /// Chạy hiệu ứng coin burst.
        /// </summary>
        /// <param name="nodeRect">RectTransform của node đang được kích hoạt</param>
        /// <param name="goldIconRect">RectTransform của icon vàng ở góc màn hình</param>
        /// <param name="goldAmount">Số vàng thưởng</param>
        /// <param name="onComplete">Callback sau khi hiệu ứng xong (mở map tiếp)</param>
        public void PlayBurst(RectTransform nodeRect, RectTransform goldIconRect,
                              int goldAmount, Action onComplete)
        {
            if (_isPlaying)
            {
                onComplete?.Invoke();
                return;
            }
            _isPlaying = true;
            StartCoroutine(BurstRoutine(nodeRect, goldIconRect, goldAmount, onComplete));
        }

        // ────────────────────────────────────────────────────────────
        private IEnumerator BurstRoutine(RectTransform nodeRect, RectTransform goldIconRect,
                                         int goldAmount, Action onComplete)
        {
            if (coinSprite == null)
            {
                Debug.LogWarning("[CoinBurstEffect] Chưa gán Coin Sprite!");
                _isPlaying = false;
                onComplete?.Invoke();
                yield break;
            }

            Vector2 originLocal = WorldToOverlayLocal(GetWorldCenter(nodeRect));

            // ── Spawn coins ───────────────────────────────────────────
            var coins = new List<RectTransform>(coinCount);
            var vels  = new List<Vector2>(coinCount);
            var floorYs = new List<float>(coinCount);

            for (int i = 0; i < coinCount; i++)
            {
                var go = new GameObject($"Coin_{i}");
                go.transform.SetParent(_overlayRect, false);

                var rt = go.AddComponent<RectTransform>();
                rt.sizeDelta        = coinSize;
                rt.anchorMin        = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot            = new Vector2(0.5f, 0.5f);
                // Spawn với scatter nhỏ quanh tâm node
                rt.anchoredPosition = originLocal + UnityEngine.Random.insideUnitCircle * 25f;

                var img = go.AddComponent<Image>();
                img.sprite        = coinSprite;
                img.raycastTarget = false;

                // Velocity: parabol lên cao rồi rơi xuống
                // — chỉ văng ngang 1 chút, chủ yếu nảy thẳng lên
                float vx = UnityEngine.Random.Range(horizontalSpeedMin, horizontalSpeedMax)
                           * (UnityEngine.Random.value > 0.5f ? 1f : -1f); // ngẫu nhiên trái/phải
                float vy = UnityEngine.Random.Range(verticalSpeedMin, verticalSpeedMax); // luôn nảy lên

                vels.Add(new Vector2(vx, vy));
                coins.Add(rt);

                // Gán vị trí "mặt đất" ngẫu nhiên cho đồng xu này (cách tâm event từ 100px - 130px)
                floorYs.Add(originLocal.y - UnityEngine.Random.Range(floorDropMin, floorDropMax));
            }

            // ── Phase 1: Vật lý rơi có chạm đất ────────────────────────────────
            float elapsed = 0f;
            while (elapsed < burstDuration)
            {
                elapsed += Time.deltaTime;
                for (int i = 0; i < coins.Count; i++)
                {
                    var pos = coins[i].anchoredPosition;
                    var v = vels[i];

                    // Kiểm tra chạm đất
                    if (pos.y <= floorYs[i] && v.y < 0)
                    {
                        pos.y = floorYs[i];
                        v.y = -v.y * bounciness; // nảy ngược lên
                        v.x = v.x * 0.5f;        // ma sát ngang làm chậm lại
                        
                        // Khi nảy quá nhẹ thì dừng hẳn
                        if (v.y < 30f)
                        {
                            v.y = 0f;
                            v.x = 0f;
                        }
                    }
                    else
                    {
                        // Đang rơi tự do
                        v.y -= gravity * Time.deltaTime;
                    }

                    vels[i] = v;
                    pos += v * Time.deltaTime;
                    coins[i].anchoredPosition = pos;

                    if (Mathf.Abs(v.x) > 5f)
                        coins[i].Rotate(0f, 0f, v.x * Time.deltaTime * 0.25f);
                }
                yield return null;
            }

            // ── Phase 2: Bay về gold icon ─────────────────────────────
            Vector2 goldLocal = WorldToOverlayLocal(GetWorldCenter(goldIconRect));

            int done = 0;
            for (int i = 0; i < coins.Count; i++)
            {
                int idx = i;
                StartCoroutine(FlyToGold(coins[idx], goldLocal, flyDuration,
                                         staggerDelay * idx, () =>
                {
                    done++;
                    if (done < coins.Count) return;

                    // Tất cả coin đã đến → cộng vàng + cleanup
                    if (GameManager.Instance?.RunData != null)
                        GameManager.Instance.RunData.gold += goldAmount;

                    foreach (var c in coins)
                        if (c != null) Destroy(c.gameObject);

                    _isPlaying = false;
                    onComplete?.Invoke();
                }));
            }
        }

        private IEnumerator FlyToGold(RectTransform coin, Vector2 target,
                                       float duration, float delay, Action onDone)
        {
            yield return new WaitForSeconds(delay);
            if (coin == null) { onDone?.Invoke(); yield break; }

            Vector2 from = coin.anchoredPosition;
            float elapsed = 0f;

            while (elapsed < duration && coin != null)
            {
                elapsed += Time.deltaTime;
                float t  = Mathf.Clamp01(elapsed / duration);
                float e  = 1f - Mathf.Pow(1f - t, 3f); // ease-out cubic

                coin.anchoredPosition = Vector2.Lerp(from, target, e);
                coin.localScale       = Vector3.Lerp(Vector3.one, Vector3.one * 0.15f, e);
                yield return null;
            }

            if (coin != null) coin.localScale = Vector3.zero;
            onDone?.Invoke();
        }

        // ────────────────────────────────────────────────────────────
        // HELPERS
        // ────────────────────────────────────────────────────────────
        private static Vector3 GetWorldCenter(RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            return (corners[0] + corners[2]) * 0.5f;
        }

        private Vector2 WorldToOverlayLocal(Vector3 worldCenter)
        {
            // Với ScreenSpace-Overlay canvas, GetWorldCorners trả về tọa độ screen (pixel)
            // nên ta dùng trực tiếp làm screenPos
            Canvas parentCanvas = null;
            var allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude);
            foreach (var c in allCanvases)
                if (c != _overlay && c.isRootCanvas) { parentCanvas = c; break; }

            Camera cam = (parentCanvas != null &&
                          parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                         ? parentCanvas.worldCamera : null;

            Vector2 screenPos = cam != null
                ? (Vector2)cam.WorldToScreenPoint(worldCenter)
                : (Vector2)worldCenter;  // Overlay: worldCorners = screen pixels

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _overlayRect, screenPos, null, out Vector2 local);
            return local;
        }
    }
}
