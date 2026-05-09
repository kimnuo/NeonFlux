using UnityEngine;

public class InputManager : Singleton<InputManager>
{
    // 외부 클래스(PlayerController)에서 GC 할당 없이 읽어갈 수 있는 상태 프로퍼티
    public bool IsHolding { get; private set; }
    public float SlideDirection { get; private set; }

    [Header("고정 슬라이드 바")]
    [SerializeField] private bool useFixedSlideBar = true;
    [SerializeField] private bool autoCreateSlideBar = true;
    [SerializeField] private FixedSlideBar fixedSlideBar;

    [Header("슬라이드 입력 설정")]
    [SerializeField] private float referenceWidth = 1080f;
    [SerializeField] private float dragDeadZonePixels = 2f;
    [SerializeField] private float fullSwipePixels = 28f;
    [SerializeField] private float inputSmoothing = 14f;
    [SerializeField] private float releaseDamping = 10f;
    [SerializeField] private float inputSnapThreshold = 0.03f;

    private bool _hasPointer;
    private Vector2 _lastPointerPosition;

    private void Start()
    {
        if (!useFixedSlideBar) return;

        if (fixedSlideBar == null)
        {
            fixedSlideBar = FindObjectOfType<FixedSlideBar>();
        }

        if (fixedSlideBar == null && autoCreateSlideBar)
        {
            fixedSlideBar = FixedSlideBar.CreateRuntime();
        }
    }

    private void Update()
    {
        if (useFixedSlideBar && fixedSlideBar != null)
        {
            IsHolding = fixedSlideBar.IsInteracting;
            float targetDirection = IsHolding ? fixedSlideBar.NormalizedX : 0f;
            if (Mathf.Abs(targetDirection) < inputSnapThreshold) targetDirection = 0f;

            if (!IsHolding)
            {
                SlideDirection = Mathf.Lerp(SlideDirection, targetDirection, Time.deltaTime * releaseDamping);
            }
            else
            {
                SlideDirection = Mathf.Lerp(SlideDirection, targetDirection, Time.deltaTime * inputSmoothing);
            }

            if (Mathf.Abs(SlideDirection) < inputSnapThreshold) SlideDirection = 0f;
            return;
        }

        float deltaX = 0f;
        IsHolding = false;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            HandlePointer(touch.phase, touch.position, ref deltaX);
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                _hasPointer = true;
                _lastPointerPosition = Input.mousePosition;
            }

            if (Input.GetMouseButton(0) && _hasPointer)
            {
                HandlePointer(TouchPhase.Moved, Input.mousePosition, ref deltaX);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                _hasPointer = false;
            }
        }

        if (!IsHolding)
        {
            SlideDirection = Mathf.Lerp(SlideDirection, 0f, Time.deltaTime * releaseDamping);
            if (Mathf.Abs(SlideDirection) < inputSnapThreshold) SlideDirection = 0f;
            return;
        }

        float widthScale = Mathf.Max(Screen.width, 1f) / Mathf.Max(referenceWidth, 1f);
        float deadZonePixels = dragDeadZonePixels * widthScale;
        float fullSwipePixelsScaled = Mathf.Max(fullSwipePixels * widthScale, 1f);
        float rawSlide = Mathf.Abs(deltaX) < deadZonePixels
            ? 0f
            : Mathf.Clamp(deltaX / fullSwipePixelsScaled, -1f, 1f);

        SlideDirection = Mathf.Lerp(SlideDirection, rawSlide, Time.deltaTime * inputSmoothing);
        if (Mathf.Abs(SlideDirection) < inputSnapThreshold) SlideDirection = 0f;
    }

    private void HandlePointer(TouchPhase phase, Vector2 position, ref float deltaX)
    {
        switch (phase)
        {
            case TouchPhase.Began:
                _hasPointer = true;
                _lastPointerPosition = position;
                IsHolding = true;
                deltaX = 0f;
                break;
            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                if (!_hasPointer)
                {
                    _hasPointer = true;
                    _lastPointerPosition = position;
                }

                deltaX = position.x - _lastPointerPosition.x;
                _lastPointerPosition = position;
                IsHolding = true;
                break;
            default:
                _hasPointer = false;
                IsHolding = false;
                deltaX = 0f;
                break;
        }
    }
}
