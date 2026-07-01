using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace ProjectM.Cards
{
    [RequireComponent(typeof(Image))]
    [ExecuteAlways]
    public class UICardShadow : BaseMeshEffect
    {
        [Header("Tự động copy hình từ Card Front hoặc Character Art")]
        public Image sourceArt;
        
        [Header("Thông số bóng (Chỉnh ở đây, KHÔNG DÙNG SHADER NỮA)")]
        public float shadowDepth = -0.5f;
        public float shadowDistance = 0.8f;
        public Color shadowColor = new Color(0, 0, 0, 0.6f);

        private Image _shadowImage;

        protected override void Awake()
        {
            base.Awake();
            _shadowImage = GetComponent<Image>();
        }

        private void Update()
        {
            if (_shadowImage == null) return;

            bool isHand = GetComponentInParent<PlayerHand>() != null || 
                          GetComponentInParent<ProjectM.Skills.SkillHandManager>() != null ||
                          GetComponentInParent<ProjectM.UI.SelectableCharacterCard>() != null;
            
            _shadowImage.enabled = !isHand;

            if (!isHand && sourceArt != null && sourceArt.sprite != null)
            {
                if (_shadowImage.sprite != sourceArt.sprite || _shadowImage.type != Image.Type.Simple)
                {
                    _shadowImage.sprite = sourceArt.sprite;
                    _shadowImage.type = Image.Type.Simple; // KHÔNG ĐƯỢC XOÁ: Dòng này ép 9-slice thành mặt phẳng
                }
            }

            if (Application.isPlaying && _shadowImage.enabled)
            {
                _shadowImage.SetVerticesDirty();
            }
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0) return;

            List<UIVertex> verts = new List<UIVertex>();
            vh.GetUIVertexStream(verts);

            // Tự động tìm đáy chính xác của tấm ảnh (bất chấp User kéo lệch Pivot đi đâu)
            float minY = float.MaxValue;
            foreach (var v in verts)
            {
                if (v.position.y < minY) minY = v.position.y;
            }

            Vector3 lightPos = Vector3.zero;
            if (ProjectM.Environment.GlobalLightSource.Instance != null)
            {
                lightPos = ProjectM.Environment.GlobalLightSource.Instance.transform.position;
            }
            
            // Hướng sáng
            Vector3 lightDir = transform.position - lightPos;
            Vector2 normalizedLightDir = new Vector2(lightDir.x, lightDir.y).normalized;

            for (int i = 0; i < verts.Count; i++)
            {
                UIVertex v = verts[i];
                float distFromBottom = v.position.y - minY;
                
                // Nén Y từ đáy
                v.position.y = minY + (distFromBottom * shadowDepth);
                
                // Trượt X từ đáy
                v.position.x += normalizedLightDir.x * distFromBottom * shadowDistance;
                
                v.color = shadowColor;
                verts[i] = v;
            }

            vh.Clear();
            vh.AddUIVertexTriangleStream(verts);
        }
    }
}
