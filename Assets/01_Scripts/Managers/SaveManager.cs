using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


public class SaveManager : Singleton<SaveManager>
{
    private string _savePath;
    public PlayerSaveData CurrentData;

    // 기획자가 에디터에서 수치를 밸런싱할 정적 템플릿 참조
    private UpgradeStatsSO upgradeStatsTemplate;

    protected override void Awake()
    {
        base.Awake();
        // 모바일 환경의 영구 보존 경로 할당
        _savePath = Path.Combine(Application.persistentDataPath, "neonflux_user_data.json");
        LoadData();
    }

    public void SaveData()
    {
        // 런타임 메모리에 존재하는 객체를 JSON 포맷의 문자열로 직렬화 
        string json = JsonUtility.ToJson(CurrentData);
        File.WriteAllText(_savePath, json);
    }

    public void LoadData()
    {
        if (File.Exists(_savePath))
        {
            string json = File.ReadAllText(_savePath);
            // 디스크에서 읽어온 문자열을 다시 객체로 역직렬화 
            CurrentData = JsonUtility.FromJson<PlayerSaveData>(json);
        }
        else
        {
            // 신규 유저 초기값 세팅
            CurrentData = new PlayerSaveData { currentCoins = 0, currentSpeedLevel = 1, currentDriftLevel = 1 };
        }
    }

    /// <summary>
    /// PlayerController가 현재 차량의 실제 속도 수치를 요청할 때 호출
    /// JSON의 레벨 데이터와 SO의 스탯 데이터를 융합하여 반환 
    /// </summary>
    public float GetCurrentMaxSpeed()
    {
        return upgradeStatsTemplate.speedValuesByLevel;
    }
}