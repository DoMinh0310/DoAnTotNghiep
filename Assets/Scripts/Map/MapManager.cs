using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace ProjectM.Map
{
    /// <summary>
    /// Map V2: Vertical scroll, 3 routes, 3 combats.
    /// Quản lý graph node, random event, player token, và scroll dọc.
    /// </summary>
    public class MapManager : MonoBehaviour
    {
        public static MapManager Instance { get; private set; }

        // ══════════════════════════════════════════════════════════════
        // INSPECTOR
        // ══════════════════════════════════════════════════════════════
        [Header("Scene References")]
        [Tooltip("ScrollRect của map (cuộn dọc)")]
        public ScrollRect scrollRect;

        [Tooltip("RectTransform của nội dung map (1920 × 5400)")]
        public RectTransform mapContent;

        [Tooltip("Parent chứa tất cả node GameObject")]
        public Transform nodesContainer;

        [Tooltip("Prefab node (có MapNode script)")]
        public GameObject nodePrefab;

        [Tooltip("Prefab đường nét đứt (có UIMapLine script)")]
        public GameObject linePrefab;

        [Tooltip("Icon player di chuyển theo khi click")]
        public RectTransform playerToken;

        [Tooltip("Script hiệu ứng coin burst (gắn vào 1 GameObject trong scene)")]
        public CoinBurstEffect coinBurstEffect;

        [Tooltip("RectTransform của icon vàng ở góc màn hình (Gold Display UI)")]
        public RectTransform goldIconRect;

        [Header("Resource Event")]
        [Tooltip("Số vàng thưởng tối thiểu khi vào event Resource")]
        public int resourceGoldRewardMin = 10;
        
        [Tooltip("Số vàng thưởng tối đa khi vào event Resource")]
        public int resourceGoldRewardMax = 20;

        [Tooltip("Kéo các MapNodeData asset vào đây (theo NodeType)")]
        public List<MapNodeData> allNodeDataAssets;

        [Tooltip("Kéo file cấu hình bốc thẻ (CardReward Config) vào đây")]
        public CardReward cardRewardPool;

        [Header("Scroll")]
        [Tooltip("Thời gian scroll mượt khi player di chuyển (giây)")]
        public float scrollDuration = 0.6f;

        [Tooltip("Thời gian mỗi bước nhảy (giây) — tổng thời gian = số bước × thời gian 1 bước")]
        public float hopDuration = 0.32f;

        [Tooltip("Độ cao arc tối đa khi nhảy (pixel, ở khoảng cách xa nhất)")]
        public float maxHopHeight = 120f;

        [Tooltip("Số bước nhảy cố định — 2 là vừa đẹp, tăng nếu muốn nhiều nhịp hơn")]
        public int hopSteps = 2;

        [Header("Graph – Slot Definitions")]
        [Tooltip("Danh sách 23 slot (index 0-22) khớp với background. Xem map_v2_plan.md để biết thứ tự.")]
        public List<MapSlotDefinition> slots = new List<MapSlotDefinition>();

        // ══════════════════════════════════════════════════════════════
        // RUNTIME
        // ══════════════════════════════════════════════════════════════
        private MapNode[] _spawnedNodes;           // Index khớp với slots[]
        private MapRunData _runData;

        // Trạng thái: Đang trong Event (đang chọn thẻ, mua đồ...) nhưng người chơi tạm ẩn UI để xem Map.
        public bool IsEventInProgress { get; private set; }
        private CardRewardPanel _activeEventPanel;

        // Pool event types có thể random (không phải combat)
        private static readonly NodeType[] NonCombatPool =
        {
            NodeType.Card, NodeType.Relic, NodeType.Resource,
            NodeType.Sacrifice, NodeType.Smith, NodeType.Shop
        };

        // ══════════════════════════════════════════════════════════════
        // INIT
        // ══════════════════════════════════════════════════════════════
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            _runData = GameManager.Instance?.RunData;
            if (_runData == null)
            {
                Debug.LogError("[MapManager] RunData is null. Make sure GameManager initialized a run.");
                return;
            }

            // Đảm bảo PlayerToken có anchor Top-Left để trùng khớp với hệ tọa độ của Map
            if (playerToken != null)
            {
                playerToken.anchorMin = playerToken.anchorMax = new Vector2(0f, 1f);
                playerToken.pivot = new Vector2(0.5f, 0.5f);
            }

            // Lần đầu vào map → generate random types
            if (_runData.randomSlotIndices.Count == 0)
                GenerateRandomEventTypes();

            BuildAllNodes();
            RestoreMapState();
            ScrollToCurrentNode(instant: true);
        }

        // ══════════════════════════════════════════════════════════════
        // GRAPH DEFAULT SETUP
        // ══════════════════════════════════════════════════════════════
        /// <summary>
        /// Được gọi khi Reset() trong Editor để tự điền default slots.
        /// Người dùng có thể chỉnh lại position trong Inspector.
        /// </summary>
        private void Reset()
        {
            slots = BuildDefaultSlots();
        }

        private List<MapSlotDefinition> BuildDefaultSlots()
        {
            var defs = new List<MapSlotDefinition>
            {
                // HÀNG 1: 1 vị trí (Start)
                new MapSlotDefinition { position = new Vector2(960, -200), routeIndex = 1, nextSlots = new List<int>{ 1, 2, 3 } }, // 0: Start
                
                // HÀNG 2: 3 vị trí (Trái, Giữa, Phải)
                new MapSlotDefinition { position = new Vector2(480, -600), routeIndex = 0, nextSlots = new List<int>{ 4, 5 } }, // 1: Row2 L
                new MapSlotDefinition { position = new Vector2(960, -600), routeIndex = 1, nextSlots = new List<int>{ 4, 5, 6 } }, // 2: Row2 C
                new MapSlotDefinition { position = new Vector2(1440, -600), routeIndex = 2, nextSlots = new List<int>{ 5, 6 } }, // 3: Row2 R

                // HÀNG 3: 3 vị trí (Trái, Giữa, Phải)
                new MapSlotDefinition { position = new Vector2(480, -1100), routeIndex = 0, nextSlots = new List<int>{ 7 } }, // 4: Row3 L
                new MapSlotDefinition { position = new Vector2(960, -1100), routeIndex = 1, nextSlots = new List<int>{ 7, 8 } }, // 5: Row3 C
                new MapSlotDefinition { position = new Vector2(1440, -1100), routeIndex = 2, nextSlots = new List<int>{ 8 } }, // 6: Row3 R

                // COMBAT 1: 2 vị trí (Trái, Phải)
                new MapSlotDefinition { position = new Vector2(600, -1600), routeIndex = 0, isCombat = true, nextSlots = new List<int>{ 9 } }, // 7: C1 L
                new MapSlotDefinition { position = new Vector2(1320, -1600), routeIndex = 2, isCombat = true, nextSlots = new List<int>{ 11 } }, // 8: C1 R

                // HÀNG 4: 1 vị trí (Trái)
                new MapSlotDefinition { position = new Vector2(480, -2100), routeIndex = 0, nextSlots = new List<int>{ 10, 11 } }, // 9: Row4 L

                // HÀNG 5: 2 vị trí (Trái, Giữa)
                new MapSlotDefinition { position = new Vector2(480, -2600), routeIndex = 0, nextSlots = new List<int>{ 12 } }, // 10: Row5 L
                new MapSlotDefinition { position = new Vector2(960, -2600), routeIndex = 1, nextSlots = new List<int>{ 12, 13 } }, // 11: Row5 C

                // COMBAT 2: 2 vị trí (Trái, Phải)
                new MapSlotDefinition { position = new Vector2(600, -3100), routeIndex = 0, isCombat = true, nextSlots = new List<int>{ 14, 15 } }, // 12: C2 L
                new MapSlotDefinition { position = new Vector2(1320, -3100), routeIndex = 2, isCombat = true, nextSlots = new List<int>{ 15, 16 } }, // 13: C2 R

                // HÀNG 6: 3 vị trí (Trái, Giữa, Phải)
                new MapSlotDefinition { position = new Vector2(480, -3700), routeIndex = 0, nextSlots = new List<int>{ 17 } }, // 14: Row6 L
                new MapSlotDefinition { position = new Vector2(960, -3700), routeIndex = 1, nextSlots = new List<int>{ 17, 18 } }, // 15: Row6 C
                new MapSlotDefinition { position = new Vector2(1440, -3700), routeIndex = 2, nextSlots = new List<int>{ 18 } }, // 16: Row6 R

                // HÀNG 7: 2 vị trí (Trái, Phải)
                new MapSlotDefinition { position = new Vector2(600, -4300), routeIndex = 0, nextSlots = new List<int>{ 19 } }, // 17: Row7 L
                new MapSlotDefinition { position = new Vector2(1320, -4300), routeIndex = 2, nextSlots = new List<int>{ 19 } }, // 18: Row7 R

                // BOSS CUỐI: 1 vị trí
                new MapSlotDefinition { position = new Vector2(960, -5000), routeIndex = 1, isBoss = true, isCombat = true, nextSlots = new List<int>() } // 19: Boss
            };
            return defs;
        }

        // ══════════════════════════════════════════════════════════════
        // RANDOM EVENT GENERATION
        // ══════════════════════════════════════════════════════════════
        private void GenerateRandomEventTypes()
        {
            var rng = new System.Random(_runData.mapSeed);
            var shopCount = new int[3];

            // --- Tiền xử lý: Tính nhóm "cùng hàng ngang" (Luật A) ---
            // Thay vì dùng tọa độ Y (dễ bị lỗi nếu bạn kéo lệch node), ta sẽ dùng "Độ sâu" của Node trong mạng lưới.
            var depths = new int[slots.Count];
            for (int i = 0; i < slots.Count; i++) depths[i] = 0;
            
            // Tính độ sâu bằng thuật toán tìm đường (duyệt từ trên xuống)
            for (int i = 0; i < slots.Count; i++)
            {
                foreach (int nextIdx in slots[i].nextSlots)
                {
                    if (nextIdx >= 0 && nextIdx < slots.Count)
                    {
                        if (depths[i] + 1 > depths[nextIdx])
                            depths[nextIdx] = depths[i] + 1;
                    }
                }
            }

            var rowGroups = new Dictionary<int, List<int>>(); // key = depth, value = list of slotIdx
            for (int i = 0; i < slots.Count; i++)
            {
                if (i == 0 || slots[i].isCombat || slots[i].isBoss) continue;
                int d = depths[i];
                if (!rowGroups.ContainsKey(d)) rowGroups[d] = new List<int>();
                rowGroups[d].Add(i);
            }

            // --- Tiền xử lý: Tính danh sách "cha" của mỗi slot (Luật B) ---
            // parents[i] = tất cả slot j mà slots[j].nextSlots chứa i
            var parents = new Dictionary<int, List<int>>();
            for (int i = 0; i < slots.Count; i++) parents[i] = new List<int>();
            for (int i = 0; i < slots.Count; i++)
            {
                foreach (int next in slots[i].nextSlots)
                    if (next >= 0 && next < slots.Count)
                        parents[next].Add(i);
            }

            // --- Sinh ngẫu nhiên theo thứ tự index ---
            // assigned[i] = loại event đã được chọn cho slot i (-1 = chưa gán)
            var assigned = new NodeType[slots.Count];
            for (int i = 0; i < assigned.Length; i++) assigned[i] = (NodeType)(-1);

            // --- Đảm bảo 2-3 Cửa hàng (Shop) và trải đều ---
            int targetShops = rng.Next(2, 4); // Random 2 hoặc 3
            var validShopSlots = new List<int>();
            for (int i = 0; i < slots.Count; i++)
            {
                if (i == 0 || slots[i].isCombat || slots[i].isBoss) continue;
                if (depths[i] >= 2) validShopSlots.Add(i); // Tránh Hàng 1 (depth 1)
            }

            // Trộn ngẫu nhiên danh sách
            for (int i = 0; i < validShopSlots.Count; i++)
            {
                int r = rng.Next(i, validShopSlots.Count);
                int temp = validShopSlots[i];
                validShopSlots[i] = validShopSlots[r];
                validShopSlots[r] = temp;
            }

            int placedShops = 0;
            foreach (int slotIdx in validShopSlots)
            {
                if (placedShops >= targetShops) break;

                bool tooClose = false;
                for (int j = 0; j < slots.Count; j++)
                {
                    if (assigned[j] == NodeType.Shop)
                    {
                        // Cách nhau ít nhất 2 hàng (đảm bảo không bao giờ ở gần nhau)
                        if (Mathf.Abs(depths[slotIdx] - depths[j]) < 2)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                }
                // Vẫn giữ luật max 2 Shop trên cùng 1 cột
                if (shopCount[slots[slotIdx].routeIndex] >= 2) tooClose = true;

                if (!tooClose)
                {
                    assigned[slotIdx] = NodeType.Shop;
                    shopCount[slots[slotIdx].routeIndex]++;
                    placedShops++;
                }
            }

            for (int i = 0; i < slots.Count; i++)
            {
                var def = slots[i];
                if (i == 0 || def.isCombat || def.isBoss) continue;
                if ((int)assigned[i] != -1) continue; // Đã pre-assign (Shop)

                var candidates = new List<NodeType>(NonCombatPool);
                candidates.Remove(NodeType.Shop); // Cấm random ra thêm Shop để giữ đúng số lượng

                // LUẬT B: Loại bỏ type trùng với bất kỳ node cha nào
                foreach (int parentIdx in parents[i])
                {
                    if ((int)assigned[parentIdx] != -1)
                        candidates.Remove(assigned[parentIdx]);
                }

                // LUẬT A: Loại bỏ type đã được dùng bởi node anh/em cùng hàng (đã gán trước đó)
                int d = depths[i];
                if (rowGroups.TryGetValue(d, out var sameRowSlots))
                {
                    foreach (int siblingIdx in sameRowSlots)
                    {
                        if (siblingIdx != i && (int)assigned[siblingIdx] != -1)
                            candidates.Remove(assigned[siblingIdx]);
                    }
                }

                // Fallback: nếu bị loại hết thì nới lỏng chỉ giữ Luật B
                if (candidates.Count == 0)
                {
                    candidates = new List<NodeType>(NonCombatPool);
                    candidates.Remove(NodeType.Shop);
                    foreach (int parentIdx in parents[i])
                    {
                        if ((int)assigned[parentIdx] != -1)
                            candidates.Remove(assigned[parentIdx]);
                    }
                }

                // Fallback cuối cùng
                if (candidates.Count == 0) candidates.Add(NodeType.Card);

                NodeType chosen = candidates[rng.Next(candidates.Count)];
                assigned[i] = chosen;
            }

            // Ghi lại dữ liệu sau khi gán xong toàn bộ
            for (int i = 0; i < slots.Count; i++)
            {
                if ((int)assigned[i] != -1)
                {
                    _runData.SetSlotType(i, assigned[i]);
                }
            }

            _runData.shopCountPerRoute = new List<int> { shopCount[0], shopCount[1], shopCount[2] };
        }

        // ══════════════════════════════════════════════════════════════
        // BUILD NODES
        // ══════════════════════════════════════════════════════════════
        private void BuildAllNodes()
        {
            _spawnedNodes = new MapNode[slots.Count];

            for (int i = 0; i < slots.Count; i++)
            {
                var def = slots[i];
                NodeType type;

                if (i == 0)            type = NodeType.Resource; // Dummy cho Start
                else if (def.isBoss)   type = NodeType.Boss;
                else if (def.isCombat) type = NodeType.Battle;
                else                   type = _runData.GetSlotType(i);

                var go = Instantiate(nodePrefab, nodesContainer);
                go.name = $"Node_{i}_{type}";
                
                // Ẩn hoàn toàn node Start (chỉ để làm mốc tọa độ cho Token)
                if (i == 0) go.SetActive(false);

                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot     = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = def.position;

                var node = go.GetComponent<MapNode>();
                node.Init(type, GetNodeData(type), slotIndex: i, pathIndex: def.routeIndex, nodeIndex: i);

                _spawnedNodes[i] = node;
            }

            // --- BUILD LINES ---
            if (linePrefab != null)
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    var def = slots[i];
                    foreach (int nextIdx in def.nextSlots)
                    {
                        if (nextIdx >= 0 && nextIdx < slots.Count)
                        {
                            var lineGO = Instantiate(linePrefab, nodesContainer);
                            lineGO.name = $"Line_{i}_to_{nextIdx}";
                            lineGO.transform.SetAsFirstSibling();

                            // Kéo giãn RectTransform phủ full NodesContainer
                            var lineRT = lineGO.GetComponent<RectTransform>();
                            if (lineRT != null)
                            {
                                lineRT.anchorMin = Vector2.zero;
                                lineRT.anchorMax = Vector2.one;
                                lineRT.offsetMin = Vector2.zero;
                                lineRT.offsetMax = Vector2.zero;
                                lineRT.pivot = new Vector2(0f, 1f); // QUAN TRỌNG: Đưa gốc tọa độ về góc trên bên trái
                            }

                            var mapLine = lineGO.GetComponent<UIMapLine>();
                            if (mapLine != null)
                            {
                                mapLine.Setup(def.position, slots[nextIdx].position);
                            }
                        }
                    }
                }
            }
        }

        public MapNodeData GetNodeData(NodeType type)
        {
            return allNodeDataAssets?.Find(d => d != null && d.nodeType == type);
        }

        // ══════════════════════════════════════════════════════════════
        // RESTORE STATE
        // ══════════════════════════════════════════════════════════════
        private void RestoreMapState()
        {
            int cur = _runData.currentSlotIndex;

            // Đặt token ở slot hiện tại (hoặc ẩn nếu chưa bắt đầu)
            if (cur >= 0 && cur < slots.Count)
            {
                SetTokenPosition(slots[cur].position, instant: true);
                if (playerToken != null) playerToken.gameObject.SetActive(true);
            }
            else
            {
                if (playerToken != null) playerToken.gameObject.SetActive(false);
            }

            // Vẽ trạng thái tất cả node
            for (int i = 0; i < slots.Count; i++)
            {
                if (_runData.HasVisited(i))
                    _spawnedNodes[i].SetCompleted();
                else if (IsReachable(i))
                    _spawnedNodes[i].Activate();
                else
                    _spawnedNodes[i].SetDimmed();
            }
        }

        // ══════════════════════════════════════════════════════════════
        // REACHABILITY
        // ══════════════════════════════════════════════════════════════
        /// <summary>
        /// Slot i có thể đến được từ vị trí hiện tại không?
        /// </summary>
        private bool IsReachable(int i)
        {
            int cur = _runData.currentSlotIndex;

            if (cur == -1)
            {
                // Chưa đi bước nào → chỉ Row1 (index 0,1,2) accessible
                return i <= 2;
            }

            // Kiểm tra xem i có nằm trong nextSlots của cur không
            if (cur >= 0 && cur < slots.Count)
                return slots[cur].nextSlots.Contains(i);

            return false;
        }

        // ══════════════════════════════════════════════════════════════
        // NODE CLICK
        // ══════════════════════════════════════════════════════════════
        public void OnNodeClicked(MapNode node)
        {
            int idx = node.SlotIndex;

            // Nếu đang trong Event mà người chơi tạm ẩn Panel để xem Map:
            // CHỈ CHO PHÉP BẤM VÀO ĐÚNG NODE HIỆN TẠI ĐỂ MỞ LẠI PANEL!
            if (IsEventInProgress)
            {
                if (idx == _runData.currentSlotIndex && _activeEventPanel != null)
                {
                    _activeEventPanel.Reopen();
                }
                else
                {
                    Debug.Log("[MapManager] Đang bận chọn phần thưởng! Vui lòng chọn thẻ để đi tiếp.");
                }
                return; // Chặn hoàn toàn các xử lý di chuyển khác
            }

            if (!IsReachable(idx)) return;

            var def      = slots[idx];
            var nodeType = node.NodeType;

            // Mark visited ngay khi click
            _runData.visitedSlots.Add(idx);
            _runData.currentSlotIndex = idx;

            // Cập nhật counter
            if (!def.isCombat && !def.isBoss)
                _runData.nonCombatSinceLastCombat++;
            else
                _runData.nonCombatSinceLastCombat = 0;

            // Refresh trạng thái node
            RefreshAllNodeStates();

            // Cuộn map đến node song song với di chuyển token
            StartCoroutine(ScrollToSlot(idx));

            // Di chuyển token TRƯỜC, rồi mới trigger event SAU
            StartCoroutine(MoveAndTrigger(def.position, def.isBoss, def.isCombat, nodeType, node));
        }

        /// <summary>Di chuyển token đến đích, đợi hoàn tất rồi mới khai hỏa event.</summary>
        private IEnumerator MoveAndTrigger(Vector2 targetPos, bool isBoss, bool isCombat,
                                           NodeType nodeType, MapNode sourceNode = null)
        {
            // Đợi token di chuyển xong
            yield return StartCoroutine(MoveToken(targetPos));

            // Trigger event
            if (isBoss || isCombat)
                GameManager.Instance?.OnCombatNodeEntered();
            else if (nodeType == NodeType.Resource && sourceNode != null)
                TriggerResourceEvent(sourceNode);
            else
                TriggerNonCombatEvent(nodeType);
        }

        private void RefreshAllNodeStates()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                var node = _spawnedNodes[i];
                if (_runData.HasVisited(i))
                    node.SetCompleted();
                else if (IsReachable(i))
                    node.Activate();
                else
                    node.SetDimmed();
            }
        }

        // Bật/tắt cờ chặn Map khi người chơi bấm nút "Tạm ẩn để xem Map"
        public void SetEventInProgress(bool inProgress, CardRewardPanel panel)
        {
            IsEventInProgress = inProgress;
            _activeEventPanel = panel;
            
            // Highlight node hiện tại để người chơi biết phải bấm vào đâu để quay lại
            if (inProgress && _runData.currentSlotIndex >= 0)
            {
                _spawnedNodes[_runData.currentSlotIndex].Activate();
            }
        }

        // ══════════════════════════════════════════════════════════════
        // PLAYER TOKEN
        // ══════════════════════════════════════════════════════════════
        private void SetTokenPosition(Vector2 localPos, bool instant)
        {
            if (playerToken == null) return;
            playerToken.anchoredPosition = localPos;
        }

        private IEnumerator MoveToken(Vector2 targetPos)
        {
            if (playerToken == null) yield break;
            playerToken.gameObject.SetActive(true);

            Vector2 startPos = playerToken.anchoredPosition;
            float   dist     = Vector2.Distance(startPos, targetPos);

            // Khoảng cách giữa node 4 và node 7 làm mốc (khoảng ~664px)
            float threshold4To7 = slots.Count > 7 ? Vector2.Distance(slots[4].position, slots[7].position) : 664f;

            // Số bước nhảy lấy trực tiếp từ Inspector, nhưng nếu >= khoảng cách mốc thì ép thành 4 bước
            int steps = dist >= threshold4To7 ? 4 : Mathf.Max(1, hopSteps);

            // Chiều cao arc tỷ lệ với khoảng cách (xa = nhảy cao hơn)
            float hopHeight = Mathf.Lerp(30f, maxHopHeight, Mathf.Clamp01(dist / 600f));

            for (int step = 0; step < steps; step++)
            {
                // Vị trí bắt đầu và kết thúc của bước này
                float t0 = (float)step       / steps;
                float t1 = (float)(step + 1) / steps;
                Vector2 from = Vector2.Lerp(startPos, targetPos, t0);
                Vector2 to   = Vector2.Lerp(startPos, targetPos, t1);

                float elapsed = 0f;
                while (elapsed < hopDuration)
                {
                    elapsed += Time.deltaTime;
                    float t      = Mathf.Clamp01(elapsed / hopDuration);
                    float tEased = t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t; // ease in-out

                    // Nội suy theo chiều ngang
                    Vector2 flatPos = Vector2.Lerp(from, to, tEased);

                    // Arc nhảy theo sin (lên rồi xuống trong 1 bước)
                    float arc = Mathf.Sin(t * Mathf.PI) * hopHeight;

                    playerToken.anchoredPosition = flatPos + Vector2.up * arc;
                    yield return null;
                }

                // Snap cuối bước để tránh lệch tuyết đối
                playerToken.anchoredPosition = to;
            }

            playerToken.anchoredPosition = targetPos;
        }

        // ══════════════════════════════════════════════════════════════
        // SCROLL (VERTICAL)
        // ══════════════════════════════════════════════════════════════
        private void ScrollToCurrentNode(bool instant)
        {
            int cur = _runData.currentSlotIndex;
            if (cur < 0 || cur >= slots.Count) return;
            StartCoroutine(ScrollToSlot(cur, instant));
        }

        private IEnumerator ScrollToSlot(int idx, bool instant = false)
        {
            if (scrollRect == null || mapContent == null) yield break;

            // Y âm → vị trí tuyệt đối từ top = |posY|
            float nodeY   = Mathf.Abs(slots[idx].position.y);
            float contentH = mapContent.rect.height;
            float viewH    = scrollRect.viewport.rect.height;
            float scrollable = contentH - viewH;
            if (scrollable <= 0f) yield break;

            // Căn node vào giữa viewport dọc
            float targetNorm = Mathf.Clamp01((nodeY - viewH * 0.5f) / scrollable);

            if (instant)
            {
                scrollRect.verticalNormalizedPosition = 1f - targetNorm;
                yield break;
            }

            float startNorm = 1f - scrollRect.verticalNormalizedPosition;
            float elapsed   = 0f;

            while (elapsed < scrollDuration)
            {
                elapsed += Time.deltaTime;
                float t     = Mathf.Clamp01(elapsed / scrollDuration);
                float eased = t < 0.5f ? 2 * t * t : -1f + (4f - 2f * t) * t;
                float cur2  = Mathf.Lerp(startNorm, targetNorm, eased);
                scrollRect.verticalNormalizedPosition = 1f - cur2;
                yield return null;
            }
            scrollRect.verticalNormalizedPosition = 1f - targetNorm;
        }

        // ══════════════════════════════════════════════════════════════
        // EVENT DISPATCH
        // ══════════════════════════════════════════════════════════════
        private void TriggerNonCombatEvent(NodeType type)
        {
            Debug.Log($"[MapManager] Trigger event: {type}");

            // Khóa Map lại không cho bấm lung tung (trừ khi dùng nút Peek Map)
            SetEventInProgress(true, null);

            switch (type)
            {
                case NodeType.Card:
                    TriggerCardRewardEvent();
                    break;
                case NodeType.Resource:
                    // Xử lý bằng TriggerResourceEvent (có node rect)
                    // nếu bằng cách nào khác không có node thì fallback
                    Debug.Log("[MapManager] Resource event không có node rect, tự hoàn thành.");
                    CompleteCurrentEvent();
                    break;
                default:
                    Debug.Log($"[MapManager] Event {type} chưa có chức năng. Tự động hoàn thành!");
                    CompleteCurrentEvent();
                    break;
            }
        }

        private void TriggerResourceEvent(MapNode node)
        {
            SetEventInProgress(true, null);
            
            int randomGoldReward = UnityEngine.Random.Range(resourceGoldRewardMin, resourceGoldRewardMax + 1);

            if (coinBurstEffect == null || goldIconRect == null)
            {
                Debug.LogWarning("[MapManager] CoinBurstEffect hoặc GoldIconRect chưa được gán! Fallback: cộng vàng ngay.");
                if (GameManager.Instance?.RunData != null)
                    GameManager.Instance.RunData.gold += randomGoldReward;
                CompleteCurrentEvent();
                return;
            }

            var nodeRect = node.GetComponent<RectTransform>();
            coinBurstEffect.PlayBurst(nodeRect, goldIconRect, randomGoldReward, () =>
            {
                CompleteCurrentEvent();
            });
        }

        private void TriggerCardRewardEvent()
        {
            if (cardRewardPool == null)
            {
                Debug.LogError("[MapManager] Chưa gán CardReward Pool! Vui lòng kéo file cấu hình bốc thẻ vào Inspector của MapManager.");
                CompleteCurrentEvent();
                return;
            }

            var panel = UnityEngine.Object.FindAnyObjectByType<CardRewardPanel>(FindObjectsInactive.Include);
            if (panel == null)
            {
                Debug.LogError("[MapManager] Không tìm thấy CardRewardPanel trong Scene!");
                CompleteCurrentEvent();
                return;
            }

            // Mở Panel, lấy 3 thẻ ngẫu nhiên. Cho phép dùng PeekMap.
            var choices = cardRewardPool.GetRandomChoices();
            
            // Set active panel trước khi Show (nếu nó đang bị tắt)
            panel.gameObject.SetActive(true);
            panel.Show(choices, allowPeekMap: true, onCardChosen: (chosenCard) => 
            {
                Debug.Log($"[MapManager] Player đã chọn thẻ: {chosenCard.name}");
                CompleteCurrentEvent();
            });

            // Ghi nhớ panel đang mở để xử lý Peek Map
            _activeEventPanel = panel;
        }

        public void CompleteCurrentEvent()
        {
            // Mở khóa Map
            SetEventInProgress(false, null);
            GameManager.Instance?.OnEventCompleted();
            Debug.Log("[MapManager] Event hoàn tất. Mời đi tiếp!");
        }
        // ══════════════════════════════════════════════════════════════
        // GIZMOS (VẼ TRƯỚC VỊ TRÍ TRONG SCENE VIEW ĐỂ DỄ THIẾT KẾ)
        // ══════════════════════════════════════════════════════════════
        private void OnDrawGizmos()
        {
            if (mapContent == null || slots == null || slots.Count == 0) return;

            // Tính vị trí góc trên cùng bên trái của MapContent
            Vector3[] corners = new Vector3[4];
            mapContent.GetWorldCorners(corners);
            Vector3 topLeft = corners[1]; // Góc trên trái

            Gizmos.color = Color.green;

            for (int i = 0; i < slots.Count; i++)
            {
                var def = slots[i];
                
                // Quy đổi tọa độ local của Slot thành tọa độ World trong Scene View
                // Lưu ý: vị trí này giả định Pivot của MapContent là (0, 1) như đã hướng dẫn
                Vector3 worldPos = topLeft + new Vector3(def.position.x * mapContent.lossyScale.x, def.position.y * mapContent.lossyScale.y, 0f);

                // Màu sắc: Đỏ nếu là Combat/Boss, Xanh lá nếu Random
                Gizmos.color = (def.isCombat || def.isBoss) ? Color.red : Color.green;
                Gizmos.DrawWireSphere(worldPos, 30f * mapContent.lossyScale.x);

                // Vẽ số index của Slot
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(worldPos + Vector3.up * 40f * mapContent.lossyScale.y, i.ToString());
                #endif

                // Vẽ đường kẻ nối tới các Next Slots
                Gizmos.color = Color.yellow;
                foreach (int nextIdx in def.nextSlots)
                {
                    if (nextIdx >= 0 && nextIdx < slots.Count)
                    {
                        var nextDef = slots[nextIdx];
                        Vector3 nextWorldPos = topLeft + new Vector3(nextDef.position.x * mapContent.lossyScale.x, nextDef.position.y * mapContent.lossyScale.y, 0f);
                        Gizmos.DrawLine(worldPos, nextWorldPos);
                    }
                }
            }
        }
    }
}
