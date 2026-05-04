using UnityEngine;


public class PlayerController : MonoBehaviour
{

    private float driftSpeed = 25f;
    private float returnSpeed = 30f;
    private float maxDriftAngle = 40f;
    private float rotationLerpSpeed = 15f;
    private float movementLerpSpeed = 10f;

    private Rigidbody _rb;
    private Vector3 _targetVelocity;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        // 강체의 물리 연산 주기를 프레임 렌더링에 맞춰 부드럽게 보간
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance.CurrentState != GameState.Playing) return;
        ApplyDriftPhysics();
    }

    private void ApplyDriftPhysics()
    {
        // 싱글톤 InputManager를 통해 캐싱된 입력 상태를 호출 (가비지 프리) 
        bool isHolding = InputManager.Instance.IsHolding;

        // 1. 횡방향 속도 계산 (Hold 시 우측 양수, Release 시 좌측 음수)
        float lateralVelocity = isHolding ? driftSpeed : -returnSpeed;

        // 상대적 전진은 RoadManager가 처리하므로 Z축 속도는 0으로 고정
        _targetVelocity = new Vector3(lateralVelocity, _rb.velocity.y, 0f);

        // 2. 부드러운 횡방향 이동 적용 (관성 시뮬레이션)
        _rb.velocity = Vector3.Lerp(_rb.velocity, _targetVelocity, Time.fixedDeltaTime * movementLerpSpeed);

        // 3. 차체 회전(Visual Rotation) 적용
        float targetYRotation = isHolding ? maxDriftAngle : -maxDriftAngle * 0.5f;
        Quaternion targetRotation = Quaternion.Euler(0, targetYRotation, 0);

        // Rigidbody.MoveRotation을 통해 물리 엔진의 충돌 연산과 동기화
        _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, targetRotation, Time.fixedDeltaTime * rotationLerpSpeed));
    }
}