using UnityEngine;

namespace ProjectM.Environment
{
    [ExecuteAlways]
    public class GlobalLightSource : MonoBehaviour
    {
        public static GlobalLightSource Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        [Tooltip("Gắn Component này vào Mặt trăng đỏ của bạn. Nó sẽ liên tục báo toạ độ ánh sáng cho Shader.")]
        void Update()
        {
            if (Instance == null) Instance = this;
            // Bắn toạ độ mặt trăng/mặt trời vào toàn bộ Shader trong game
            Shader.SetGlobalVector("_GlobalLightPos", transform.position);
        }
    }
}
