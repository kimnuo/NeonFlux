using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Neon Flux: 플레이어 차량을 부드럽게 추적하는 카메라 스크립트.
/// 물리 연산(FixedUpdate) 이후에 카메라가 이동하도록 LateUpdate를 사용하며,
/// Vector3.Lerp를 통해 부드러운 모션을 구현합니다.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("추적 대상 (플레이어 차량)")]
    // 런타임에 GameManager나 RoadManager에 의해 자동으로 할당되는 것이 좋습니다.
    public Transform target;

    [Header("카메라 오프셋 설정")]
    // 차량 기준 카메라의 상대적 위치 (세로형 화면에 맞춘 뷰포트)
    // 추천값: 9:21 해상도 기준, X=0, Y=7~10, Z=-12~-15
    public Vector3 offset = new Vector3(0f, 7.0f, -12.0f);

    [Header("추적 부드러움 정도 (높을수록 빠름)")]
    // 모바일 환경 추천값: 10f ~ 15f
    public float smoothSpeed = 10f;

    private void Start()
    {
        TryFindTarget();

        if (target != null)
        {
            transform.position = target.position + offset;
        }
    }

    /// <summary>
    /// 모든 Update 및 FixedUpdate가 종료된 후 호출됩니다.
    /// 플레이어의 Rigidbody 이동이 완료된 후 카메라 위치를 잡아야 떨림이 없습니다.
    /// </summary>
    private void LateUpdate()
    {
        // 1. 추적 대상이 없으면 매 프레임 재탐색
        if (target == null)
        {
            TryFindTarget();
            return;
        }

        // 2. 게임 상태 체크 (Playing 상태일 때만 추적)
        // GameManager 싱글톤 패턴이 구현되어 있다고 가정합니다.
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
        {
            return;
        }

        // 3. 목표 위치 계산 (차량 위치 + 오프셋)
        // Neon Flux는 Point-to-Point이므로 Z축 전진을 그대로 쫓아갑니다.
        Vector3 desiredPosition = target.position + offset;

        // 4. 현재 위치에서 목표 위치로 부드럽게 이동 (보간)
        // Time.deltaTime을 곱하여 프레임률에 독립적인 이동 속도를 확보합니다.
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * smoothSpeed);

        // 5. 최종 위치 적용
        transform.position = smoothedPosition;

        // 6. (선택 사항) 카메라가 항상 차량을 살짝 내려다보도록 회전 고정
        // 인스펙터에서 수동으로 맞춘 회전값을 유지하거나, 코드로 고정할 수 있습니다.
        // 여기서는 인스펙터에서 설정한 회전값을 유지한다고 가정합니다.
    }

    /// <summary>
    /// 외부에서 추적 대상을 동적으로 할당할 때 사용합니다.
    /// 예: GameManager가 플레이어 차량을 스폰한 직후 호출
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;

        // 대상이 할당되는 순간, 카메라를 즉시 목표 위치로 이동시켜
        // 게임 시작 시 카메라가 멀리서 날아오는 현상을 방지합니다.
        if (target != null)
        {
            transform.position = target.position + offset;
        }
    }

    private void TryFindTarget()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
            return;
        }

        PlayerController playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            target = playerController.transform;
        }
    }
}
