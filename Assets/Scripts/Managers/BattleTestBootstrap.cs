using UnityEngine;
using ProjectM.Map;
using ProjectM.Skills;

namespace ProjectM.Managers
{
    /// <summary>
    /// Script CHỈ DÙNG ĐỂ TEST — gắn vào 1 GameObject trong BattleScene.
    /// Nếu GameManager chưa có RunData (tức là bạn bấm Play thẳng từ BattleScene),
    /// script này sẽ tự khởi tạo RunData với giá trị mặc định để GoldDisplay và các UI khác hoạt động đúng.
    /// Tự động đọc ChampionSetup từ SkillHandManager — không cần kéo thả trùng lặp.
    /// DISABLE hoặc XÓA script này khi build game thật.
    /// </summary>
    public class BattleTestBootstrap : MonoBehaviour
    {
        [Header("Test Settings")]
        [Tooltip("Số vàng ban đầu để test (phải khớp với MapRunData.InitNewRun)")]
        public int startingGold = 10;

        private void Awake()
        {
            // Nếu GameManager chưa tồn tại → tạo mới
            if (GameManager.Instance == null)
            {
                var go = new GameObject("GameManager [TEST]");
                go.AddComponent<GameManager>();
                Debug.Log("[BattleTestBootstrap] Đã tạo GameManager test.");
            }

            // Chỉ inject nếu chưa có RunData (tránh đè mất dữ liệu khi đến từ MapScene)
            if (GameManager.Instance.RunData == null)
            {
                var runData = new MapRunData();
                runData.InitNewRun(seed: 0);
                runData.gold = startingGold;

                // Đọc ChampionSetup trực tiếp từ SkillHandManager trong Scene
                // — không cần kéo thả lại, tránh trùng lặp
                var shm = Object.FindAnyObjectByType<SkillHandManager>();
                if (shm != null && shm.championSetup != null)
                {
                    // TẠO COPY CỦA STARTER DECK ĐỂ KHÔNG LÀM HỎNG ASSET GỐC
                    var setupCopy = ScriptableObject.CreateInstance<ChampionSetup>();
                    setupCopy.champions = new System.Collections.Generic.List<ChampionEntry>();
                    
                    foreach (var champ in shm.championSetup.champions)
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
                    setupCopy.supportDeck = new System.Collections.Generic.List<SkillData>(shm.championSetup.supportDeck);
                    setupCopy.ownedRelics = new System.Collections.Generic.List<RelicData>(shm.championSetup.ownedRelics);
                    setupCopy.ownedTrinkets = new System.Collections.Generic.List<TrinketData>(shm.championSetup.ownedTrinkets);

                    runData.championSetup = setupCopy;
                    foreach (var skill in setupCopy.supportDeck)
                        if (skill != null) runData.playerDeckIDs.Add(skill.name);
                    foreach (var entry in setupCopy.champions)
                        if (entry?.championData != null) runData.playerDeckIDs.Add(entry.championData.name);
                    
                    // Cập nhật lại vào SkillHandManager để nó dùng bản copy
                    shm.championSetup = setupCopy;
                    
                    Debug.Log($"[BattleTestBootstrap] Đã nạp bản sao StarterDeck '{shm.championSetup.name}' vào RunData.");
                }
                else
                {
                    Debug.LogWarning("[BattleTestBootstrap] Không tìm thấy SkillHandManager hoặc ChampionSetup chưa được gán!");
                }

                GameManager.Instance.InjectRunDataForTest(runData);
                Debug.Log($"[BattleTestBootstrap] RunData test khởi tạo xong! Gold={startingGold}");
            }
            else
            {
                Debug.Log($"[BattleTestBootstrap] RunData đã có sẵn (đến từ MapScene). Gold={GameManager.Instance.RunData.gold}");
            }
        }
    }
}
