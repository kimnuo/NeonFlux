================================================================================
  NeonFlux - 게임 실행 가이드
  작성자: 김택준 (게임소프트웨어 26001)
================================================================================

■ Unity 버전
  - Unity 2022.3.62f3 (URP)
  - 빌드 타겟: Android (ARM64 + ARMv7), SDK 34

■ 실행 방법
  1. Unity Hub에서 Unity 2022.3.62f3 설치
  2. Unity Hub → Projects → Add → 프로젝트 폴더(C:\KMU\Games\NeonFlux) 선택
  3. 프로젝트 열기 후 자동 패키지 리졸브 대기
  4. 에디터 재생 버튼으로 테스트
     - PC 테스트: 게임 시작 → 좌우 화살표/A,D 키로 조향
     - Android 빌드: File → Build Settings → Android → Build

■ Android 빌드 시 참고 사항
  - Build Settings에서 Target Architecture: ARM64 + ARMv7 체크
  - Target SDK Version: 34 (API Level 34)
  - Custom Gradle Template 사용 (mainTemplate.gradle, settingsTemplate.gradle)
  - Google Ads(AdMob) 완전 제거됨 - 빌드에 영향 없음
  - IL2CPP 스크립팅 백엔드 권장

■ 게임 조작
  - 좌우 이동: 좌우 스크롤바
  - 드리프트: 최대 조향 시 자동 진입
  - 게임 시작: 메인 메뉴에서 Start 버튼
  - 재시도: 게임 오버 화면에서 Retry 버튼
  - 메인 메뉴: 게임 오버 화면에서 Home 버튼

■ 주요 기능
  - 점수 시스템: 주행 거리 기반 자동 점수 누적 (Z축 5m당 2점, X축 1m당 1점)
  - 영구 저장: 게임 종료 시 점수 자동 저장 (Application.persistentDataPath)
  - 리더보드: 메인 메뉴에서 상위 10개 기록 확인 가능
  - 스테이지 시스템: 결승선 통과 시 다음 스테이지 진행
  - 게임 오버 UI: 현재 점수, 이전 점수, 최고 점수, 리더보드 요약 표시

■ 트러블슈팅
  Q: Unity로 실행이 안 됩니다.
  A: Unity 버전이 2022.3.62f3인지 확인하세요. 다른 버전에서는 호환성 문제가 발생할 수 있습니다.

  Q: Android 빌드가 실패합니다.
  A: Build Settings에서 Target SDK Version이 34로 설정되었는지, Custom Gradle Template이 활성화되었는지 확인하세요.

  Q: 점수가 저장되지 않습니다.
  A: Application.persistentDataPath/neonflux_user_data.json 파일을 확인하세요. Android에서는 /storage/emulated/0/Android/data/com.kmu.neonflux/files/ 경로에 저장됩니다.

================================================================================
