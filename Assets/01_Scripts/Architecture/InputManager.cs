using UnityEngine;

public class InputManager : Singleton<InputManager>
{
    // 외부 클래스(PlayerController)에서 GC 할당 없이 읽어갈 수 있는 상태 프로퍼티
    public bool IsHolding { get; private set; }

    private void Update()
    {
        // 마우스 클릭(에디터 테스트용) 또는 모바일 화면 터치 및 유지 상태(Hold)를 정확히 판별
        bool isMouseHolding = Input.GetMouseButton(0);

        bool isTouchHolding = Input.touchCount > 0 &&
                              (Input.GetTouch(0).phase == TouchPhase.Began ||
                               Input.GetTouch(0).phase == TouchPhase.Moved ||
                               Input.GetTouch(0).phase == TouchPhase.Stationary);

        IsHolding = isMouseHolding || isTouchHolding;
    }
}