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
                    // TẠO COPY CỦA STARTER DECK ĐỂ KHÔNG LÀM HỎNG ASSET GỐC
                    var setupCopy = ScriptableObject.CreateInstance<ChampionSetup>();
                    setupCopy.champions = new System.Collections.Generic.List<ChampionEntry>();
                    
                    foreach (var champ in starterDeck.champions)
                    {
                        if (champ != null)
                        {
                            setupCopy.champions.Add(new ChampionEntry
                            {
                                championData = champ.championData,
                                equippedRelic = champ.equippedRelic,
                                equippedTrinket = champ.equippedTrinket
                            });
                        }
                    }
                    setupCopy.supportDeck = new System.Collections.Generic.List<SkillData>(starterDeck.supportDeck);
                    setupCopy.ownedRelics = new System.Collections.Generic.List<RelicData>(starterDeck.ownedRelics);
                    setupCopy.ownedTrinkets = new System.Collections.Generic.List<TrinketData>(starterDeck.ownedTrinkets);

                    // Lưu bản copy vào RunData
                    runData.championSetup = setupCopy;

                    // Đồng thời copy tên thẻ vào playerDeckIDs để các hệ thống khác tra cứu
                    foreach (var skill in setupCopy.supportDeck)
                        if (skill != null) runData.playerDeckIDs.Add(skill.name);
                    foreach (var entry in setupCopy.champions)
                    {
                        if (entry?.championData != null) 
                        {
                            runData.playerDeckIDs.Add(entry.championData.name);
                            if (entry.championData.signatureCards != null)
                            {
                                foreach (var sig in entry.championData.signatureCards)
                                {
                                    if (sig != null) runData.playerDeckIDs.Add(sig.name);
                                }
                            }
                        }
                    }
                    Debug.Log($"[MapTestBootstrap] Đã nạp bản sao của '{starterDeck.name}' vào RunData.");
                }
                else
                {
                    Debug.LogWarning("[MapTestBootstrap] Chưa kéo StarterDeck vào Inspector! Bộ bài sẽ trống.");
                }

                // Inject thủ công
                GameManager.Instance.InjectRunDataForTest(runData);
            }
        }
    }
}
