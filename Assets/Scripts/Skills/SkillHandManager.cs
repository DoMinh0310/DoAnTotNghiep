using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using ProjectM.Cards;


namespace ProjectM.Skills
{
    /// <summary>
    /// Quản lý tay skill + deck cycle của người chơi trong trận đấu.
    ///
    /// DECK SYSTEM:
    ///   - _drawPile  : bài còn lại (đã xáo)
    ///   - _hand      : bài đang cầm (tối đa maxHandSize = 6)
    ///   - _discardPile : bài đã dùng (xáo lại khi _drawPile hết)
    ///
    /// FLOW:
    ///   1. BattleManager gọi DealSkillsWithAnimation() sau khi tất cả tướng đặt xong.
    ///      → Deal random 6 lá đầu tiên.
    ///   2. Mỗi lần người chơi bấm chuông → BattleManager gọi DrawOneCardIfNeeded().
    ///      → Nếu tay RỖNG (0 thẻ): tự động fill lại FULL tay (maxHandSize lá).
    ///      → Nếu tay còn thẻ nhưng chưa đầy: rút thêm 1 lá + animation.
    ///   3. Khi skill được dùng → OnSkillUsed() → card vào _discardPile.
    ///   4. Relic spawn skill → SpawnRelicSkillCard() → thêm vào tay (không từ deck).
    ///
    /// UNITY SETUP:
    ///   1. Gán handContainer  → Panel chứa các thẻ skill (Horizontal Layout Group)
    ///   2. Gán skillPrefab    → Skill_Prefab với SkillExecutor + SkillDragHandler
    ///   3. Gán inventoryBag   → GameObject "Inventory_Bag" trong scene
    ///   4. Gán mainCanvas     → Canvas gốc của scene
    ///   5. Gán championSetup  → ChampionSetup ScriptableObject (thay cho startingSkills cũ)
    /// </summary>
    public class SkillHandManager : MonoBehaviour
    {
        public static SkillHandManager Instance { get; private set; }

        [Header("References")]
        [Tooltip("Panel chứa các thẻ skill (nên có Horizontal Layout Group)")]
        public Transform handContainer;

        [Tooltip("Prefab thẻ skill (Skill_Prefab với SkillExecutor + SkillDragHandler)")]
        public GameObject skillPrefab;

        [Tooltip("Kéo GameObject InventoryBag vào đây (điểm xuất phát animation)")]
        public Transform inventoryBag;

        [Tooltip("Canvas gốc của scene (dùng để convert toạ độ)")]
        public Canvas mainCanvas;

        [Header("Deck Config")]
        [Tooltip("ChampionSetup asset chứa danh sách tướng và pool bài hỗ trợ.")]
        public ChampionSetup championSetup;

        [Tooltip("Số lá deal tối đa vào tay khi bắt đầu combat")]
        public int maxHandSize = 6;

        [Header("Animation Settings")]
        [Tooltip("Khoảng cách giữa các thẻ khi xếp trên tay (px)")]
        public float cardSpacing = 130f;

        [Tooltip("Thời gian (giây) mỗi thẻ bay từ túi → vị trí")]
        public float flyDuration = 0.35f;

        [Tooltip("Delay (giây) giữa mỗi thẻ spawn")]
        public float delayBetweenSpawns = 0.12f;

        [Tooltip("Delay (giây) giữa mỗi thẻ lật")]
        public float delayBetweenFlips = 0.08f;

        // ── Deck Runtime ──────────────────────────────────────────────────
        private readonly List<SkillData> _drawPile    = new();
        private readonly List<SkillData> _hand        = new();
        private readonly List<SkillData> _discardPile = new();

        // ── Hand card tracking (handler → data) ───────────────────────────
        private readonly Dictionary<SkillDragHandler, SkillData>    _handCards    = new();
        // Track relic-spawned cards (handler → RelicHandler nguồn)
        private readonly Dictionary<SkillDragHandler, RelicHandler> _relicCards   = new();
        // Danh sách GO theo thứ tự trái→phải để lật
        private readonly List<GameObject> _spawnedInOrder = new();

        // ── Properties ────────────────────────────────────────────────────
        public int DrawPileCount    => _drawPile.Count;
        public int HandCount        => _hand.Count;
        public int DiscardPileCount => _discardPile.Count;
        public bool IsHandFull      => _hand.Count >= maxHandSize;

        /// <summary>Trả về danh sách bài đã dùng (read-only) để DiscardPileUI hiển thị.</summary>
        public IReadOnlyList<SkillData> GetDiscardPile() => _discardPile;

        // ═════════════════════════════════════════════════════════════════
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            // ── Ưu tiên lấy ChampionSetup từ GameManager.RunData (đã chọn từ màn hình chọn tướng)
            // Đặt trong Awake() để đảm bảo BattleManager (gọi trong Start) đọc đúng data.
            var gm = GameManager.Instance;
            if (gm != null && gm.RunData != null)
            {
                if (gm.RunData.championSetup != null)
                {
                    championSetup = gm.RunData.championSetup;
                    Debug.Log($"[SkillHandManager] Đọc ChampionSetup từ GameManager.RunData: '{championSetup.name}' ({championSetup.champions.Count} tướng).");
                }
                else if (championSetup != null)
                {
                    // LÀM LẠI CHAMPION SETUP TỪ FILE SAVE
                    Debug.Log("[SkillHandManager] Đang phục hồi ChampionSetup từ Save File dựa trên Inspector fallback...");
                    
                    var rebuiltSetup = ScriptableObject.CreateInstance<ChampionSetup>();
                    rebuiltSetup.name = "Rebuilt_From_Save";
                    
                    // Khôi phục danh sách Tướng đã chọn
                    rebuiltSetup.champions = new List<ChampionEntry>();
                    rebuiltSetup.unchosenChampions = new List<ChampionEntry>();
                    
                    foreach (var entry in championSetup.champions)
                    {
                        if (entry.championData != null && gm.RunData.playerDeckIDs.Contains(entry.championData.name))
                        {
                            rebuiltSetup.champions.Add(new ChampionEntry 
                            { 
                                championData = entry.championData,
                                equippedRelic = entry.equippedRelic,
                                equippedTrinket = entry.equippedTrinket
                            });
                        }
                        else
                        {
                            rebuiltSetup.unchosenChampions.Add(entry);
                        }
                    }

                    // Khôi phục Support Deck (tạm thời chỉ khôi phục được các thẻ có sẵn trong Starter Deck)
                    rebuiltSetup.supportDeck = new List<SkillData>();
                    if (championSetup.supportDeck != null)
                    {
                        foreach (var card in championSetup.supportDeck)
                        {
                            if (card != null && gm.RunData.playerDeckIDs.Contains(card.name))
                                rebuiltSetup.supportDeck.Add(card);
                        }
                    }

                    rebuiltSetup.buildingDeck = new List<ProjectM.Cards.CardData>(championSetup.buildingDeck);

                    // Khôi phục Relics & Trinkets (từ Starter Deck)
                    rebuiltSetup.ownedRelics = new List<RelicData>();
                    if (championSetup.ownedRelics != null)
                    {
                        foreach (var r in championSetup.ownedRelics)
                        {
                            if (r != null && gm.RunData.ownedRelicIDs.Contains(r.name))
                                rebuiltSetup.ownedRelics.Add(r);
                        }
                    }
                    
                    rebuiltSetup.ownedTrinkets = new List<TrinketData>();
                    if (championSetup.ownedTrinkets != null)
                    {
                        foreach (var t in championSetup.ownedTrinkets)
                        {
                            if (t != null && gm.RunData.ownedTrinketIDs.Contains(t.name))
                                rebuiltSetup.ownedTrinkets.Add(t);
                        }
                    }

                    gm.RunData.championSetup = rebuiltSetup;
                    championSetup = rebuiltSetup;
                }
            }
            else
            {
                Debug.Log("[SkillHandManager] Không có GameManager.RunData, dùng ChampionSetup mặc định từ Inspector (test mode).");
            }
        }

        // ═════════════════════════════════════════════════════════════════
        // PUBLIC API — DEAL (gọi lần đầu khi bắt đầu combat phase)
        // ═════════════════════════════════════════════════════════════════

        /// <summary>
        /// Khởi tạo deck từ ChampionSetup, deal maxHandSize lá ngẫu nhiên.
        /// BattleManager yield trên coroutine này sau phase đặt tướng.
        /// </summary>
        public IEnumerator DealSkillsWithAnimation()
        {
            List<SkillData> pool = new List<SkillData>();

            if (championSetup != null)
            {
                // Thêm Support Deck
                if (championSetup.supportDeck != null)
                {
                    foreach(var card in championSetup.supportDeck)
                        if (card != null) pool.Add(card);
                }

                // Thêm Signature Cards từ các Tướng đang có
                if (championSetup.champions != null)
                {
                    foreach (var entry in championSetup.champions)
                    {
                        if (entry?.championData?.signatureCards != null)
                        {
                            foreach (var sig in entry.championData.signatureCards)
                            {
                                if (sig != null) pool.Add(sig);
                            }
                        }
                    }
                }
            }

            if (pool.Count == 0)
            {
                Debug.LogWarning("[SkillHandManager] Bộ bài trống! Vui lòng kiểm tra lại Support Deck hoặc Signature Cards của Tướng.");
                yield break;
            }

            // Khởi tạo draw pile = toàn bộ deck (đã xáo)
            _drawPile.Clear();
            _discardPile.Clear();
            _hand.Clear();

            ShuffleList(pool);
            _drawPile.AddRange(pool);

            // Deal tối đa maxHandSize lá đầu tiên
            int dealCount = Mathf.Min(maxHandSize, _drawPile.Count);
            var toDeal = _drawPile.GetRange(0, dealCount);
            _drawPile.RemoveRange(0, dealCount);

            yield return StartCoroutine(DealCardsWithAnimation(toDeal));
        }

        // ═════════════════════════════════════════════════════════════════
        // PUBLIC API — DRAW (gọi mỗi lượt chuông)
        // ═════════════════════════════════════════════════════════════════

        /// <summary>
        /// Gọi bởi BattleManager sau mỗi lần bấm chuông.
        /// - Nếu tay RỖNG (0 thẻ): auto fill lại toàn bộ tay (maxHandSize lá) với animation batch.
        /// - Nếu tay còn thẻ nhưng chưa đầy: rút thêm 1 lá.
        /// - Nếu tay đầy: không làm gì.
        /// </summary>
        public IEnumerator DrawOneCardIfNeeded()
        {
            if (IsHandFull)
            {
                Debug.Log("[SkillHandManager] Tay đầy, không rút thêm.");
                yield break;
            }

            // ─── AUTO REFILL: Tay trống → fill lại toàn bộ ───
            if (_hand.Count == 0)
            {
                Debug.Log("[SkillHandManager] Tay trống! Auto-fill lại toàn bộ tay.");
                yield return StartCoroutine(DrawUntilFull());
                yield break;
            }

            // ─── NORMAL DRAW: Tay còn thẻ nhưng chưa đầy → rút 1 lá ───
            if (_drawPile.Count == 0)
            {
                if (_discardPile.Count == 0)
                {
                    Debug.Log("[SkillHandManager] Không còn bài nào để rút!");
                    yield break;
                }
                ReshuffleDiscardIntoDraw();
            }

            SkillData card = _drawPile[0];
            _drawPile.RemoveAt(0);
            yield return StartCoroutine(DealOneCardWithAnimation(card));
        }

        /// <summary>
        /// Fill tay lên đủ maxHandSize lá bằng cách dùng DealCardsWithAnimation batch.
        /// Tự động xáo discard vào draw khi cần.
        /// </summary>
        public IEnumerator DrawUntilFull()
        {
            int needed = maxHandSize - _hand.Count;
            if (needed <= 0) yield break;

            var toDeal = new List<SkillData>();
            for (int i = 0; i < needed; i++)
            {
                if (_drawPile.Count == 0)
                {
                    if (_discardPile.Count == 0)
                    {
                        Debug.Log("[SkillHandManager] Hết bài, fill được " + toDeal.Count + "/" + needed + " lá.");
                        break;
                    }
                    ReshuffleDiscardIntoDraw();
                }
                toDeal.Add(_drawPile[0]);
                _drawPile.RemoveAt(0);
                _hand.Add(toDeal[toDeal.Count - 1]); // pre-add để SpawnSkillCard không double-add
            }

            // Undo pre-add vì DealCardsWithAnimation → SpawnSkillCard sẽ Add vào _hand
            foreach (var s in toDeal) _hand.Remove(s);

            if (toDeal.Count > 0)
                yield return StartCoroutine(DealCardsWithAnimation(toDeal));
        }

        // ═════════════════════════════════════════════════════════════════
        // PUBLIC API — SKILL USED
        // ═════════════════════════════════════════════════════════════════

        /// <summary>Gọi bởi SkillDragHandler sau khi skill được dùng thành công.</summary>
        public void OnSkillUsed(SkillDragHandler handler)
        {
            if (handler == null) return;

            // Nếu là skill từ Relic → báo RelicHandler biết để resume countdown
            if (_relicCards.TryGetValue(handler, out var relicHandler))
            {
                relicHandler?.OnRelicSkillUsed();
                _relicCards.Remove(handler);
            }

            if (_handCards.TryGetValue(handler, out var data))
            {
                _hand.Remove(data);
                
                // Skill từ Trinket (specificGeneratedSkills) hoặc Relic sẽ KHÔNG vào discard (chưa có list specificGeneratedSkills ở đây, nhưng tạm thời dùng isExhaust)
                if (data.isExhaust)
                {
                    Debug.Log($"[SkillHandManager] Đã dùng skill '{data.skillName}'. Thẻ có cờ Exhaust nên bị tiêu hủy khỏi trận đấu.");
                }
                else
                {
                    Debug.Log($"[SkillHandManager] Đã dùng skill '{data.skillName}', đưa vào discard.");
                    _discardPile.Add(data);    // → đống bỏ
                }
                
                _handCards.Remove(handler);
                _spawnedInOrder.Remove(handler.gameObject);
            }

            Destroy(handler.gameObject);
        }

        // ═════════════════════════════════════════════════════════════════
        // PUBLIC API — RELIC SPAWN
        // ═════════════════════════════════════════════════════════════════

        /// <summary>
        /// Spawn thẻ skill từ Relic vào tay (không lấy từ draw pile, không đi vào discard khi unused).
        /// Gọi bởi RelicHandler khi countdown đạt 0.
        /// </summary>
        public void SpawnRelicSkillCard(SkillData skillData, RelicHandler source)
        {
            if (skillData == null || skillPrefab == null) return;

            var go = SpawnSkillCard(skillData);
            if (go == null) return;

            // Đánh dấu nguồn gốc relic để OnSkillUsed biết gọi OnRelicSkillUsed()
            var handler = go.GetComponentInChildren<SkillDragHandler>(true);
            if (handler != null && source != null)
                _relicCards[handler] = source;

            // Animate bay từ túi vào tay
            StartCoroutine(AnimateSingleCardIn(go));
        }

        // ═════════════════════════════════════════════════════════════════
        // PUBLIC API — TRINKET SPAWN
        // ═════════════════════════════════════════════════════════════════

        /// <summary>
        /// Thêm 1 thẻ kỹ năng cụ thể vào tay bằng tên asset (dùng bởi Trinket như Pocket Spikes).
        /// Thẻ này KHÔNG thuộc draw pile và KHÔNG đi vào discard khi dùng xong.
        /// </summary>
        public IEnumerator AddSpecificSkillToHand(string skillAssetName)
        {
            if (IsHandFull)
            {
                Debug.Log($"[SkillHandManager] Tay đầy, không thể thêm '{skillAssetName}'.");
                yield break;
            }

            // Tìm SkillData trong toàn bộ deck pool (draw pile + discard + hand)
            SkillData found = null;
            var allSources = new List<SkillData>();
            allSources.AddRange(_drawPile);
            allSources.AddRange(_discardPile);
            allSources.AddRange(_hand);

            foreach (var skill in allSources)
            {
                if (skill != null && (string.Equals(skill.name, skillAssetName, System.StringComparison.OrdinalIgnoreCase) ||
                                      string.Equals(skill.skillName, skillAssetName, System.StringComparison.OrdinalIgnoreCase)))
                {
                    found = skill;
                    break;
                }
            }

            // Nếu không tìm thấy trong deck, tra cứu trong toàn bộ database của InventoryManager
            if (found == null && ProjectM.Inventory.InventoryManager.Instance != null && ProjectM.Inventory.InventoryManager.Instance.allSkillAssets != null)
            {
                found = ProjectM.Inventory.InventoryManager.Instance.allSkillAssets.Find(s => s != null && 
                    (string.Equals(s.name, skillAssetName, System.StringComparison.OrdinalIgnoreCase) || 
                     string.Equals(s.skillName, skillAssetName, System.StringComparison.OrdinalIgnoreCase)));
            }

            // Nếu vẫn không tìm thấy, thử tìm trong Resources ở các đường dẫn phổ biến
            if (found == null)
            {
                found = Resources.Load<SkillData>($"Skills/{skillAssetName}");
                if (found == null)
                    found = Resources.Load<SkillData>($"Cards/Utilities/{skillAssetName}");
            }

#if UNITY_EDITOR
            // Tự động tìm trong toàn bộ project khi chạy trong Editor để hỗ trợ test ngay tức thì
            if (found == null)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:SkillData {skillAssetName}");
                foreach (string guid in guids)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<SkillData>(path);
                    if (asset != null && (string.Equals(asset.name, skillAssetName, System.StringComparison.OrdinalIgnoreCase) ||
                                          string.Equals(asset.skillName, skillAssetName, System.StringComparison.OrdinalIgnoreCase)))
                    {
                        found = asset;
                        Debug.Log($"[SkillHandManager] ⚙️ Editor Auto-Loaded '{skillAssetName}' từ path '{path}'. (LƯU Ý: Kéo asset này vào 'All Skill Assets' của InventoryManager để chạy được trên bản Build!)");
                        break;
                    }
                }
            }
#endif

            if (found == null)
            {
                Debug.LogWarning($"[SkillHandManager] Không tìm thấy SkillData tên '{skillAssetName}'! " +
                                 $"Hãy kiểm tra đã kéo thẻ vào danh sách All Skill Assets trong InventoryManager chưa.");
                yield break;
            }

            var go = SpawnSkillCard(found);
            if (go == null) yield break;

            Debug.Log($"[SkillHandManager] 🦷 Trinket spawn '{found.skillName}' vào tay.");
            yield return StartCoroutine(AnimateSingleCardIn(go));
        }

        // ═════════════════════════════════════════════════════════════════
        // PUBLIC API — MISC
        // ═════════════════════════════════════════════════════════════════

        public IReadOnlyCollection<SkillData> GetHandSkills() => _handCards.Values;

        // ═════════════════════════════════════════════════════════════════
        // PRIVATE — ANIMATION HELPERS
        // ═════════════════════════════════════════════════════════════════

        /// <summary>Deal nhiều lá cùng lúc (dùng cho deal đầu game).</summary>
        private IEnumerator DealCardsWithAnimation(List<SkillData> cards)
        {
            int count = cards.Count;
            if (count == 0) yield break;

            var hLayout = handContainer?.GetComponent<HorizontalLayoutGroup>();
            if (hLayout != null) hLayout.enabled = false;

            float[] slotX    = CalculateSlotPositions(count);
            Vector2 bagLocal = GetLocalPosInContainer(inventoryBag.position);

            _spawnedInOrder.Clear();

            // Spawn từ phải sang trái (thẻ cuối → slot phải nhất)
            for (int spawnStep = 0; spawnStep < count; spawnStep++)
            {
                int slotIndex = count - 1 - spawnStep;
                var go = SpawnSkillCard(cards[slotIndex]);
                if (go == null) continue;

                var display = go.GetComponentInChildren<CardDisplay>(true);
                display?.SetFaceUp(false);

                var rect = go.GetComponent<RectTransform>();
                if (rect != null) rect.anchoredPosition = bagLocal;

                _spawnedInOrder.Insert(0, go);

                if (rect != null)
                    yield return rect
                        .DOAnchorPos(new Vector2(slotX[slotIndex], 0f), flyDuration)
                        .SetEase(Ease.OutCubic)
                        .WaitForCompletion();

                yield return new WaitForSeconds(delayBetweenSpawns);
            }

            // Pause rồi lật từ phải sang trái
            yield return new WaitForSeconds(0.25f);

            for (int i = _spawnedInOrder.Count - 1; i >= 0; i--)
            {
                var cardGO = _spawnedInOrder[i];
                if (cardGO == null) continue;
                var display = cardGO.GetComponentInChildren<CardDisplay>(true);
                if (display != null)
                    yield return StartCoroutine(display.FlipToFaceUpRoutine());
                yield return new WaitForSeconds(delayBetweenFlips);
            }
        }

        /// <summary>Deal 1 lá đơn (rút thêm giữa game). Thẻ mới xuất hiện bên TRÁI nhất.</summary>
        private IEnumerator DealOneCardWithAnimation(SkillData data)
        {
            var go = SpawnSkillCard(data);
            if (go == null) yield break;

            // Cho thẻ mới nằm ở vị trí bên trái nhất
            go.transform.SetSiblingIndex(0);

            var display = go.GetComponentInChildren<CardDisplay>(true);
            display?.SetFaceUp(false);

            // Bay từ túi vào và reposition tất cả lá bài cùng lúc
            yield return StartCoroutine(AnimateAllCardsReposition(go));

            if (display != null)
                yield return StartCoroutine(display.FlipToFaceUpRoutine());
        }

        /// <summary>Bay tất cả card về đúng slot sau khi thêm lá mới (reposition cả thẻ cũ + thẻ mới).</summary>
        private IEnumerator AnimateAllCardsReposition(GameObject newCard)
        {
            if (inventoryBag == null) yield break;

            int count      = handContainer.childCount;
            float[] slots  = CalculateSlotPositions(count);
            Vector2 bagPos = GetLocalPosInContainer(inventoryBag.position);

            // Đặt thẻ mới xuất phát từ túi
            var newRect = newCard.GetComponent<RectTransform>();
            if (newRect != null) newRect.anchoredPosition = bagPos;

            // Tween tất cả card về vị trí đúng (cả cũ lẫn mới) — chạy song song
            float dur = flyDuration * 0.7f;
            for (int i = 0; i < count; i++)
            {
                var child = handContainer.GetChild(i);
                var rect  = child.GetComponent<RectTransform>();
                if (rect == null) continue;
                rect.DOAnchorPos(new Vector2(slots[i], 0f), dur).SetEase(Ease.OutCubic);
            }

            yield return new WaitForSeconds(dur);
        }

        /// <summary>Cập nhật lại vị trí các thẻ bài trên tay (dùng khi cancel drag).</summary>
        public void RepositionAllCards()
        {
            if (handContainer == null) return;
            int count = handContainer.childCount;
            float[] slots = CalculateSlotPositions(count);
            for (int i = 0; i < count; i++)
            {
                var child = handContainer.GetChild(i);
                var rect = child.GetComponent<RectTransform>();
                if (rect == null) continue;
                rect.DOAnchorPos(new Vector2(slots[i], 0f), 0.2f).SetEase(Ease.OutCubic);
                child.DORotate(Vector3.zero, 0.2f).SetEase(Ease.OutCubic);
            }
        }

        /// <summary>Bay 1 card từ túi vào tay (Relic spawn).</summary>
        private IEnumerator AnimateSingleCardIn(GameObject go)
        {
            if (inventoryBag == null || go == null) yield break;

            var rect = go.GetComponent<RectTransform>();
            if (rect == null) yield break;

            Vector2 bagLocal = GetLocalPosInContainer(inventoryBag.position);
            rect.anchoredPosition = bagLocal;

            int count    = _hand.Count;
            float[] slots = CalculateSlotPositions(count);
            // Relic skill → thêm vào cuối bên phải
            float targetX = slots[count - 1];

            yield return rect
                .DOAnchorPos(new Vector2(targetX, 0f), flyDuration)
                .SetEase(Ease.OutCubic)
                .WaitForCompletion();

            _spawnedInOrder.Add(go);
        }

        // ═════════════════════════════════════════════════════════════════
        // PRIVATE — CORE HELPERS
        // ═════════════════════════════════════════════════════════════════

        private GameObject SpawnSkillCard(SkillData data)
        {
            if (data == null || skillPrefab == null || handContainer == null) return null;

            // Đọc size từ Prefab GỐC trước khi Instantiate —
            // sizeDelta SAU Instantiate sẽ bị sai nếu Prefab dùng anchor stretch.
            var prefabRect  = skillPrefab.GetComponent<RectTransform>();
            Vector2 nativeSize = prefabRect != null ? prefabRect.sizeDelta : Vector2.zero;

            var go     = Instantiate(skillPrefab, handContainer);
            var goRect = go.GetComponent<RectTransform>();
            if (goRect != null)
            {
                goRect.anchorMin = new Vector2(0.5f, 0.5f);
                goRect.anchorMax = new Vector2(0.5f, 0.5f);
                goRect.pivot     = new Vector2(0.5f, 0.5f);
                goRect.sizeDelta = nativeSize; // Kích thước đúng từ Prefab asset gốc
            }

            // Set scale ngay lập tức = cardScale để tránh flash 1 frame ở kích thước gốc
            var playerHand = handContainer.GetComponent<PlayerHand>();
            float initScale  = playerHand != null ? playerHand.cardScale : 0.4f;
            go.transform.localScale = new Vector3(initScale, initScale, 1f);

            var executor = go.GetComponentInChildren<SkillExecutor>(true);
            var handler  = go.GetComponentInChildren<SkillDragHandler>(true);


            if (executor == null || handler == null)
            {
                Debug.LogError("[SkillHandManager] Skill_Prefab thiếu SkillExecutor hoặc SkillDragHandler!");
                Destroy(go);
                return null;
            }

            executor.Init(data);
            go.name = $"Skill_{data.skillName}";
            _handCards[handler] = data;
            _hand.Add(data);
            return go;
        }

        private void ReshuffleDiscardIntoDraw()
        {
            Debug.Log($"[SkillHandManager] Draw pile hết! Xáo {_discardPile.Count} lá discard → draw pile mới.");
            _drawPile.AddRange(_discardPile);
            _discardPile.Clear();
            ShuffleList(_drawPile);
        }

        private float[] CalculateSlotPositions(int count)
        {
            float[] slots = new float[count];
            float center  = (count - 1) / 2f;
            for (int i = 0; i < count; i++)
                slots[i] = (i - center) * cardSpacing;
            return slots;
        }

        private Vector2 GetLocalPosInContainer(Vector3 worldPos)
        {
            if (handContainer == null) return Vector2.zero;
            Camera cam = null;
            if (mainCanvas != null && mainCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = mainCanvas.worldCamera;
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                handContainer as RectTransform, screenPos, cam, out Vector2 localPos);
            return localPos;
        }

        private static void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
