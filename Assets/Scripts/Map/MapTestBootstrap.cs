using UnityEngine;
using ProjectM.Skills;

namespace ProjectM.Map
{
    /// <summary>
    /// Script dùng CHỈ ĐỂ TEST — gắn vào 1 GameObject trong MapScene.
    /// Nếu GameManager chưa có RunData (tức là bạn bấm Play thẳng từ MapScene),
    /// script này sẽ tự khởi tạo 1 run mới để có thể thấy map hoạt động.
    /// XÓA hoặc DISABLE script này khi build game thật.
    /// </summary>
    public class MapTestBootstrap : MonoBehaviour
    {
        [Header("Test Settings")]
        [Tooltip("Seed cố định để test (-1 = random mỗi lần)")]
        public int fixedSeed = -1;

        [Tooltip("Giả lập đang ở segment nào (0 = đầu game)")]
        [Range(0, 3)]
        public int testSegmentIndex = 0;

        [Header("Starter Deck")]
        [Tooltip("Kéo cái StarterDeck_1 (ChampionSetup SO) vào đây để đồng bộ bộ bài ban đầu với Map Inventory")]
        public ChampionSetup starterDeck;

        private void Awake()
        {
            // Nếu GameManager chưa tồn tại → tạo mới
            if (GameManager.Instance == null)
            {
                var go = new GameObject("GameManager [TEST]");
                go.AddComponent<GameManager>();
                Debug.Log("[MapTestBootstrap] Đã tạo GameManager test.");
            }

            // Nếu chưa có RunData → khởi tạo run mới
            if (GameManager.Instance.RunData == null)
            {
                int seed = fixedSeed >= 0 ? fixedSeed : Random.Range(0, 99999);
                var runData = new MapRunData();
                runData.InitNewRun(seed);

                // --- ĐỌC STARTER DECK TỪ ChampionSetup SO ---
                if (starterDeck != null)
                {
                    // Lưu thẳng ChampionSetup vào RunData để InventoryManager dùng chung
                    runData.championSetup = starterDeck;

                    // Đồng thời copy tên thẻ vào playerDeckIDs để các hệ thống khác tra cứu
                    foreach (var skill in starterDeck.supportDeck)
                        if (skill != null) runData.playerDeckIDs.Add(skill.name);
                    foreach (var entry in starterDeck.champions)
                        if (entry?.championData != null) runData.playerDeckIDs.Add(entry.championData.name);
                    Debug.Log($"[MapTestBootstrap] Đã nạp {runData.playerDeckIDs.Count} thẻ từ StarterDeck '{starterDeck.name}' vào RunData.");
                }
                else
                {
                    Debug.LogWarning("[MapTestBootstrap] Chưa kéo StarterDeck vào Inspector! Bộ bài sẽ trống.");
                }

                // Inject thủ công (dùng reflection hoặc helper)
                GameManager.Instance.InjectRunDataForTest(runData);
            }
        }
    }
}
