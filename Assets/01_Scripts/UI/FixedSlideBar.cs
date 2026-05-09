using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FixedSlideBar : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("슬라이드 바 참조")]
    [SerializeField] private RectTransform trackArea;
    [SerializeField] private RectTransform handle;

    [Header("동작 설정")]
    [SerializeField] private float returnSpeed = 8f;

    public float NormalizedX { get; private set; }
    public bool IsInteracting { get; private set; }

    private Camera _uiCamera;

    private void Awake()
    {
        if (trackArea == null) trackArea = transform as RectTransform;
        if (handle == null && transform.childCount > 0) handle = transform.GetChild(0) as RectTransform;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            _uiCamera = canvas.worldCamera;
        }

        UpdateHandlePosition();
    }

    private void Update()
    {
        if (IsInteracting) return;

        NormalizedX = Mathf.MoveTowards(NormalizedX, 0f, returnSpeed * Time.deltaTime);
        UpdateHandlePosition();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        IsInteracting = true;
        UpdateFromPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        IsInteracting = true;
        UpdateFromPointer(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        IsInteracting = false;
    }

    private void UpdateFromPointer(PointerEventData eventData)
    {
        if (trackArea == null) return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(trackArea, eventData.position, _uiCamera, out Vector2 localPoint))
        {
            return;
        }

        float halfWidth = Mathf.Max(trackArea.rect.width * 0.5f, 1f);
        NormalizedX = Mathf.Clamp(localPoint.x / halfWidth, -1f, 1f);
        UpdateHandlePosition();
    }

    private void UpdateHandlePosition()
    {
        if (trackArea == null || handle == null) return;

        float halfWidth = trackArea.rect.width * 0.5f;
        Vector2 pos = handle.anchoredPosition;
        pos.x = NormalizedX * halfWidth;
        handle.anchoredPosition = pos;
    }

    public static FixedSlideBar CreateRuntime(Canvas targetCanvas = null)
    {
        Canvas canvas = targetCanvas;
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
        }

        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("HUD_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
        }

        if (FindObjectOfType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        GameObject barRoot = new GameObject("FixedSlideBar", typeof(RectTransform), typeof(Image), typeof(FixedSlideBar));
        RectTransform rootRect = barRoot.GetComponent<RectTransform>();
        rootRect.SetParent(canvas.transform, false);
        rootRect.anchorMin = new Vector2(0.5f, 0f);
        rootRect.anchorMax = new Vector2(0.5f, 0f);
        rootRect.pivot = new Vector2(0.5f, 0f);
        rootRect.anchoredPosition = new Vector2(0f, 120f);
        rootRect.sizeDelta = new Vector2(820f, 80f);

        Image rootImage = barRoot.GetComponent<Image>();
        rootImage.color = new Color(1f, 1f, 1f, 0.18f);
        rootImage.raycastTarget = true;

        GameObject handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        RectTransform handleRect = handleGo.GetComponent<RectTransform>();
        handleRect.SetParent(rootRect, false);
        handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.sizeDelta = new Vector2(120f, 80f);
        handleRect.anchoredPosition = Vector2.zero;

        Image handleImage = handleGo.GetComponent<Image>();
        handleImage.color = new Color(1f, 1f, 1f, 0.55f);
        handleImage.raycastTarget = false;

        FixedSlideBar slideBar = barRoot.GetComponent<FixedSlideBar>();
        slideBar.trackArea = rootRect;
        slideBar.handle = handleRect;

        return slideBar;
    }
}
