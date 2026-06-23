using UnityEngine;
using System.Collections.Generic;
using ProjectM.Cards;

namespace ProjectM.Managers
{
    // ─────────────────────────────────────────────────────────────────
    // Enum: Đại diện cho từng slot địch trên sàn
    // Thứ tự từ gần tâm ra ngoài: Top0 < Top1 < Top2, Bot0 < Bot1 < Bot2
    // Dùng để cấu hình vị trí spawn trong Inspector mà không cần kéo thả scene objects
    // ─────────────────────────────────────────────────────────────────
    public enum EnemySlotId
    {
        Top0 = 0,   // Hàng trên, slot gần tâm nhất
        Top1 = 1,
        Top2 = 2,
        Bot0 = 3,   // Hàng dưới, slot gần tâm nhất
        Bot1 = 4,
        Bot2 = 5,
    }

    // ─────────────────────────────────────────────────────────────────
    // Một entry spawn cố định khi wave bắt đầu
    // ─────────────────────────────────────────────────────────────────
    [System.Serializable]
    public class EnemySpawnEntry
    {
        [Tooltip("Prefab thẻ địch (dùng chung Card_Prefab)")]
        public GameObject cardPrefab;

        [Tooltip("Dữ liệu của con quái này")]
        public CardData cardData;

        [Tooltip("Slot trên sàn mà quái này sẽ xuất hiện")]
        public EnemySlotId slotId;
    }

    // ─────────────────────────────────────────────────────────────────
    // Một entry trong pool fill ô trống
    // ─────────────────────────────────────────────────────────────────
    [System.Serializable]
    public class FillEnemyEntry
    {
        [Tooltip("Prefab thẻ địch")]
        public GameObject cardPrefab;

        [Tooltip("Dữ liệu của con quái này")]
        public CardData cardData;

        [Tooltip("Thứ tự ưu tiên — số nhỏ hơn = xuất hiện trước khi fill vào ô trống")]
        public int priority = 0;
    }

    // ─────────────────────────────────────────────────────────────────
    // Cấu hình 1 wave
    // ─────────────────────────────────────────────────────────────────
    [System.Serializable]
    public class WaveConfig
    {
        [Tooltip("Tên wave để debug")]
        public string waveName = "Wave";

        [Tooltip("Danh sách quái cố định spawn khi wave này bắt đầu (theo slot chỉ định)")]
        public List<EnemySpawnEntry> spawnList = new();

        [Tooltip("Pool quái dùng để fill ô trống — được WAVE TRƯỚC sử dụng khi cần fill.\n" +
                 "Ví dụ: fillPool của waves[1] sẽ được dùng khi wave 0 đang diễn ra và có ô trống.")]
        public List<FillEnemyEntry> fillPool = new();

        [Tooltip("Số battle tick (mỗi lần speed giảm = 1 tick) để 1 ô trống chờ trước khi được fill.\n" +
                 "0 = không bao giờ fill.")]
        public int fillDelayTurns = 5;
    }

    // ─────────────────────────────────────────────────────────────────
    // ScriptableObject chứa toàn bộ dữ liệu của 1 Stage
    // ─────────────────────────────────────────────────────────────────
    [CreateAssetMenu(fileName = "StageData_01", menuName = "Project M/Stage Data")]
    public class StageData : ScriptableObject
    {
        [Tooltip("Danh sách wave theo thứ tự. Wave 0 spawn đầu tiên.")]
        public List<WaveConfig> waves = new();
    }
}
