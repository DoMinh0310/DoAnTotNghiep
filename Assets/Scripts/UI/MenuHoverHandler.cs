using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class MenuHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Cursor Reference")]
    [Tooltip("Kéo thả GameObject của Cursor vào đây")]
    public GameObject cursorTarget;

    [Header("Animation Settings")]
    [Tooltip("Thời gian animation hiện/ẩn (tính bằng giây)")]
    public float animDuration = 0.15f;

    [Header("Positioning Settings")]
    [Tooltip("Bật cái này nếu dùng chung 1 Cursor cho tất cả các nút")]
    public bool moveCursorToButton = true;
    [Tooltip("Khoảng cách lệch (thường là số âm chữ X để đẩy trái)")]
    public Vector3 localOffset = new Vector3(-80f, 0, 0);

    // Các biến dùng chung giữa 4 nút
    private static Coroutine activeCoroutine;
    private static MonoBehaviour activeRunner;
    private static MenuHoverHandler currentlyHovered;
    
    // Lưu lại kích cỡ chuẩn ban đầu của con trỏ
    private static Vector3? sharedOriginalScale = null;

    private void Awake()
    {
        // Awake chạy rất sớm, tranh thủ lưu lại Scale lúc chưa ai bị thu nhỏ
        if (cursorTarget != null && sharedOriginalScale == null)
        {
            sharedOriginalScale = cursorTarget.transform.localScale;
        }
    }

    private void Start()
    {
        if (cursorTarget != null)
        {
            // Bắt buộc ẩn con trỏ ngay khi game bật lên
            cursorTarget.transform.localScale = Vector3.zero;
            cursorTarget.SetActive(false);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (cursorTarget == null) return;

        currentlyHovered = this;
        cursorTarget.SetActive(true);

        if (moveCursorToButton)
        {
            // Bước 1: Cho khớp vị trí tuyệt đối của Nút
            cursorTarget.transform.position = transform.position;
            // Bước 2: Dịch chuyển Offset bằng tọa độ địa phương rễ nhìn (chuẩn Pixel)
            cursorTarget.transform.localPosition += localOffset;
        }

        // Lấy lại scale xịn lúc đầu (hoặc mặc định (1,1,1) nếu mất)
        Vector3 targetS = sharedOriginalScale ?? Vector3.one;
        StartAnim(targetS, false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (cursorTarget == null) return;

        if (currentlyHovered == this)
        {
            currentlyHovered = null;
            StartAnim(Vector3.zero, true);
        }
    }

    private void StartAnim(Vector3 destScale, bool hideOnComplete)
    {
        if (activeRunner != null && activeCoroutine != null)
        {
            activeRunner.StopCoroutine(activeCoroutine);
        }

        activeRunner = this;
        activeCoroutine = StartCoroutine(ScaleRoutine(destScale, animDuration, hideOnComplete));
    }

    private IEnumerator ScaleRoutine(Vector3 destScale, float duration, bool hideOnComplete)
    {
        Vector3 startScale = cursorTarget.transform.localScale;
        float time = 0f;

        while (time < duration)
        {
            cursorTarget.transform.localScale = Vector3.Lerp(startScale, destScale, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        cursorTarget.transform.localScale = destScale;

        if (hideOnComplete)
        {
            cursorTarget.SetActive(false);
        }
    }
}
