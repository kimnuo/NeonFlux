using UnityEngine;

/// <summary>
/// 노치 및 하단 제스처 바를 피해 UI를 안전 영역 내로 배치하는 컨트롤러
/// </summary>

public class SafeAreaController : MonoBehaviour
{
    private RectTransform _rectTransform;
    private Rect _safeArea;
    private Vector2 _minAnchor;
    private Vector2 _maxAnchor;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    private void ApplySafeArea()
    {
        // OS가 하드웨어 정보를 바탕으로 제공하는 픽셀 단위의 안전 영역 반환 
        _safeArea = Screen.safeArea;

        // 픽셀 좌표계를 0과 1 사이의 정규화된(Normalized) 앵커 좌표계로 변환 
        _minAnchor = _safeArea.position;
        _maxAnchor = _minAnchor + _safeArea.size;

        _minAnchor.x /= Screen.width;
        _minAnchor.y /= Screen.height;
        _maxAnchor.x /= Screen.width;
        _maxAnchor.y /= Screen.height;

        // 변환된 좌표를 UI RectTransform의 Min, Max 앵커에 적용하여 여백을 동적으로 생성 
        _rectTransform.anchorMin = _minAnchor;
        _rectTransform.anchorMax = _maxAnchor;
    }
}