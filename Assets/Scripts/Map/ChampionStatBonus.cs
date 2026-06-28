using System;

namespace ProjectM.Map
{
    /// <summary>
    /// Lưu bonus ATK và HP tích lũy từ Smith Event cho 1 tướng.
    /// Serializable để lưu vào MapRunData.
    /// </summary>
    [Serializable]
    public struct ChampionStatBonus
    {
        public int attackBonus;
        public int healthBonus;
    }
}
