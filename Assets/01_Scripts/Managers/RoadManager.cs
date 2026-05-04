using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 직선 도로와 Y자 분기점의 무한 생성 및 파괴(풀링)를 제어하는 시스템
/// </summary>
public class RoadManager : Singleton<RoadManager>
{

    private GameObject straightRoadPrefab;
    private GameObject yJunctionPrefab;


    private float roadSpeed = 50f;
    private float segmentLength = 20f;
    private int initialSegments = 15;

    private IObjectPool<GameObject> _straightPool;
    private IObjectPool<GameObject> _junctionPool;
    private Vector3 _nextSpawnPosition = Vector3.zero;

    protected override void Awake()
    {
        base.Awake();
        InitializePools();
    }

    private void Start()
    {
        for (int i = 0; i < initialSegments; i++)
        {
            SpawnSegment(false);
        }
    }

    private void InitializePools()
    {
        _straightPool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(straightRoadPrefab, transform),
            actionOnGet: (obj) => obj.SetActive(true),
            actionOnRelease: (obj) => obj.SetActive(false),
            actionOnDestroy: (obj) => Destroy(obj),
            collectionCheck: false,
            defaultCapacity: 20,
            maxSize: 30
        );

        _junctionPool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(yJunctionPrefab, transform),
            actionOnGet: (obj) => obj.SetActive(true),
            actionOnRelease: (obj) => obj.SetActive(false),
            actionOnDestroy: (obj) => Destroy(obj),
            collectionCheck: false,
            defaultCapacity: 5,
            maxSize: 10
        );
    }

    public void SpawnSegment(bool isJunction)
    {
        GameObject segment = isJunction ? _junctionPool.Get() : _straightPool.Get();
        segment.transform.position = _nextSpawnPosition;

        // 다음 스폰 위치 계산 (Y 분기점 통과 시 각도 및 로컬 좌표계 변환 고려)
        _nextSpawnPosition += segment.transform.forward * segmentLength;
    }

    private void Update()
    {
        // 도로를 플레이어 반대 방향으로 이동 (상대적 이동)
        foreach (Transform child in transform)
        {
            if (child.gameObject.activeInHierarchy)
            {
                child.position += Vector3.back * roadSpeed * Time.deltaTime;

                // 카메라 뒤로 넘어간 도로 조각 회수
                if (child.position.z < -segmentLength * 2)
                {
                    bool isJunc = child.CompareTag("YJunction");
                    if (isJunc) _junctionPool.Release(child.gameObject);
                    else _straightPool.Release(child.gameObject);

                    // 회수와 동시에 새로운 도로를 전방에 스폰
                    // (실제 게임 로직에서는 확률에 따라 isJunction 결정)
                    SpawnSegment(Random.value > 0.8f);
                }
            }
        }
    }
}