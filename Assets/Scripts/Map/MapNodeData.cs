using UnityEngine;

namespace ProjectM.Map
{
    /// <summary>
    /// ScriptableObject định nghĩa dữ liệu hiển thị cho 1 loại event trên map.
    /// Tạo 1 asset cho mỗi NodeType: Battle, Card, Relic, Resource, Sacrifice, Smith, Boss.
    /// </summary>
    [CreateAssetMenu(fileName = "NodeData_New", menuName = "Project M/Map/Node Data")]
    public class MapNodeData : ScriptableObject
    {
        [Header("Identity")]
        public NodeType nodeType;
        public string displayName;

        [TextArea(2, 4)]
        public string description;

        [Header("Visual")]
        [Tooltip("Icon hiển thị trên node")]
        public Sprite icon;

        [Tooltip("Màu viền/highlight của node")]
        public Color accentColor = Color.white;
    }
}
