using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

namespace ProjectM.Cards
{
    [RequireComponent(typeof(CanvasGroup))]
    public class CardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public static bool isAnyCardDragging = false; // Ngăn chặn các thẻ khác nhấp nháy khi đang kéo 1 thẻ

        public Transform previousParent { get; private set; }
        private CanvasGroup canvasGroup;
        private Canvas mainCanvas;
        
        [Header("Snapping")]
        public float snapSpeed = 15f;
        private Transform targetParent;
        public bool isDragging { get; private set; } = false;

        void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Khóa input trong lúc animation deal skill hoặc lúc combat
            if (Managers.BattleManager.IsInputBlocked) return;

            // Đảm bảo tìm được mainCanvas đúng lúc bắt đầu kéo (vì lúc Awake có thể chưa có Parent)
            if (mainCanvas == null)
            {
                Transform curr = transform.parent;
                while (curr != null)
                {
                    mainCanvas = curr.GetComponent<Canvas>();
                    if (mainCanvas != null) break;
                    curr = curr.parent;
                }
            }

            isDragging = true;
            isAnyCardDragging = true;
            previousParent = transform.parent;

            // Kill tất cả DOTween đang chạy trên thẻ (hover scale/rotation/pos)
            transform.DOKill();
            var rect = GetComponent<RectTransform>();
            if (rect != null) rect.DOKill();

            // Kill theo string ID mà PlayerHand dùng
            int id = transform.GetHashCode();
            DOTween.Kill($"hp{id}");
            DOTween.Kill($"hr{id}");
            DOTween.Kill($"hs{id}");

            // Đưa thẻ lên Canvas root
            if (mainCanvas != null)
                transform.SetParent(mainCanvas.transform);
            else
                transform.SetParent(transform.root);

            transform.SetAsLastSibling();

            // Reset rotation THẲNG sau khi đổi parent
            // (phải sau SetParent vì worldPositionStays=true có thể thay đổi localRotation)
            transform.localRotation = Quaternion.identity;

            // Tắt cản tia chuột để chuột có thể nhìn xuyên qua thẻ bài
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.8f;

            // Phát âm thanh nhấc bài
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.cardPickUpClip);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging) return;
            if (Managers.BattleManager.IsInputBlocked)
            {
                ForceCancelDrag();
                return;
            }
            transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging) return;
            if (Managers.BattleManager.IsInputBlocked)
            {
                ForceCancelDrag();
                return;
            }
            
            isDragging = false;
            isAnyCardDragging = false;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;

            // Phát âm thanh đặt bài
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.cardDropClip);

            // Nếu thẻ bài vẫn nằm trơ trọi trên Canvas (không được Slot nào hút vào)
            bool isDangling = mainCanvas != null ? (transform.parent == mainCanvas.transform) : (transform.parent == transform.root);

            if (isDangling)
            {
                // Bay ngược về chỗ cũ
                SetNewParent(previousParent);
            }
            else
            {
                // Thẻ được thả vào một slot hợp lệ
                Cards.CardDropZone dropZone = transform.parent?.GetComponent<Cards.CardDropZone>();
                if (dropZone != null && dropZone.isPlayerZone)
                {
                    // Chỉ tính là 1 lượt (kết thúc lượt) nếu thẻ được kéo ra từ TAY BÀI
                    bool cameFromHand = previousParent != null && previousParent.GetComponent<PlayerHand>() != null;
                    
                    if (cameFromHand)
                    {
                        Managers.BattleManager.Instance?.OnCardPlayedToBoard();
                    }
                    else
                    {
                        Managers.BattleDebugger.Log("🔄 Di chuyển tướng trên sàn (Không tốn lượt).");

                        // Dồn hàng cũ lại để lấp chỗ trống sau khi thẻ bị dời đi
                        CardDropZone prevZone = previousParent?.GetComponent<CardDropZone>();
                        if (prevZone != null)
                            Managers.BattleGrid.Instance?.ShiftRowOf(prevZone);
                    }
                }
            }
        }

        // Hàm này được gọi bởi CardDropZone khi thẻ lọt vào vùng từ trường
        public void SetNewParent(Transform newParent, bool animate = true)
        {
            targetParent = newParent;
            Vector3 worldPos = transform.position; // Lưu world position trước khi đổi parent
            
            transform.SetParent(newParent);
            
            CardDropZone dropZone = newParent.GetComponent<CardDropZone>();
            if (dropZone != null)
            {
                RectTransform slotRect = newParent.GetComponent<RectTransform>();
                RectTransform cardRect = GetComponent<RectTransform>();
                
                // Tính scale để bài vừa khít Slot
                float scaleFactor = slotRect.rect.width / cardRect.rect.width;
                Vector3 newScale = new Vector3(scaleFactor, scaleFactor, 1f);
                
                if (animate && gameObject.activeInHierarchy)
                {
                    transform.DOKill(); // Dừng các animation trước đó
                    transform.position = worldPos; // Giữ nguyên vị trí lúc đầu để không giật
                    
                    // Bay cực nhanh vào vị trí (0.125s = nhanh gấp đôi)
                    cardRect.DOAnchorPos(Vector2.zero, 0.125f).SetEase(Ease.OutQuad);
                    transform.DOScale(newScale, 0.125f).SetEase(Ease.OutQuad);
                    transform.DOLocalRotateQuaternion(Quaternion.identity, 0.125f).SetEase(Ease.OutQuad);
                }
                else
                {
                    transform.localScale = newScale;
                    transform.localRotation = Quaternion.identity;
                    cardRect.anchoredPosition = Vector2.zero;
                }
                
                // Báo cho CardHoverHandler biết trạng thái gốc mới để Hover hoạt động đúng
                CardHoverHandler hover = GetComponent<CardHoverHandler>();
                if (hover != null)
                {
                    hover.UpdateBaseState(Vector2.zero, Quaternion.identity, newScale);
                }
            }
            else 
            {
                transform.localScale = Vector3.one;
            }
        }

        private void ForceCancelDrag()
        {
            isDragging = false;
            isAnyCardDragging = false;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
            SetNewParent(previousParent);
        }
    }
}
