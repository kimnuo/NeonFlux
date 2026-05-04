using GoogleMobileAds.Api;
using System;
using UnityEditor.PackageManager;
using UnityEngine;

public class AdMobManager : Singleton<AdMobManager>
{
    private RewardedAd _rewardedAd;
    private const string adUnitId = "ca-app-pub-3940256099942544/5224354917"; // 구글 제공 테스트 ID

    // 비동기 콜백 스레드 충돌을 피하기 위한 상태 제어 플래그
    private bool _isRewardEarned = false;
    private bool _processRewardInMainThread = false;

    private void Start()
    {
        // SDK 초기화 (앱 시작 시 1회 필수 호출)
        MobileAds.Initialize(initStatus => {
            LoadRewardedAd();
        });
    }

    public void LoadRewardedAd()
    {
        if (_rewardedAd != null)
        {
            _rewardedAd.Destroy();
            _rewardedAd = null;
        }

        var adRequest = new AdRequest();
        RewardedAd.Load(adUnitId, adRequest, (RewardedAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogError("광고 로드 실패: " + error);
                return;
            }

            _rewardedAd = ad;
            RegisterEventHandlers(_rewardedAd);
        });
    }

    private void RegisterEventHandlers(RewardedAd ad)
    {
        // 광고 창이 완전히 닫힌 후 호출되는 이벤트 
        ad.OnAdFullScreenContentClosed += () =>
        {
            // 콜백은 백그라운드 스레드에서 실행될 수 있으므로, 메인 스레드에서 처리하도록 플래그만 변경
            _processRewardInMainThread = true;
        };

        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            LoadRewardedAd();
        };
    }

    public void ShowRewardedAd()
    {
        if (_rewardedAd != null && _rewardedAd.CanShowAd())
        {
            _isRewardEarned = false;
            _rewardedAd.Show((Reward reward) =>
            {
                // 유저가 광고 시청 조건을 충족함
                _isRewardEarned = true;
            });
        }
    }

    private void Update()
    {
        // 메인 스레드(Update)에서 안전하게 유니티 로직 처리 및 광고 재로드
        if (_processRewardInMainThread)
        {
            _processRewardInMainThread = false;

            if (_isRewardEarned)
            {
                Debug.Log("보상 지급 성공: 차량 업그레이드 재화 적용 또는 부활 처리");
                // TODO: SaveManager 연동 로직 호출 (Main Thread Safe)
            }
            else
            {
                Debug.Log("유저가 보상 획득 전에 광고를 닫았습니다.");
            }

            LoadRewardedAd();
        }
    }
}