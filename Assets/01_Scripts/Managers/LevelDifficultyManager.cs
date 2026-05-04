using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelDifficultyManager : Singleton<LevelDifficultyManager>
{


    public float minimumReactionTime = 0.5f;


    public float scalingConstant = 0.99f;

    private float _timeElapsed = 0f;

    /// <summary>
    /// 로드 매니저가 장애물을 생성할 때 호출하여 스폰 확률(밀집도)을 가져오는 함수
    /// </summary>
    public float CalculateObstacleDensity(float currentRoadSpeed)
    {
        if (GameManager.Instance.CurrentState == GameState.Playing)
        {
            _timeElapsed += Time.deltaTime;
        }

        // 휴리스틱 공식 적용: Density(t) = d * v * (1 - C^t) 
        float density = minimumReactionTime * currentRoadSpeed * (1f - Mathf.Pow(scalingConstant, _timeElapsed));

        // 난이도가 너무 쉬운 초반을 방지하기 위한 최소 스폰율 보장
        return Mathf.Max(0.1f, density);
    }

    public void ResetDifficulty()
    {
        _timeElapsed = 0f;
    }
}