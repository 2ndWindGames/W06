# 우주 정거장 보급소 · Unity W06

프로젝트 위치: `C:\SecondWindGames\Repositoires\Unity\W06`  
Unity 버전: **6000.3.23f1**  
대상: **Android 세로 화면, ARM64, 오프라인**

기존 WebView 프로토타입을 C# 규칙 엔진과 Unity UI Toolkit 화면으로 옮겼습니다. 앱 화면·우주선 그림·한글 글꼴·저장·AI는 Unity 안에서 실행됩니다. WebView나 로컬 웹 서버가 필요하지 않습니다.

## 실행

1. Unity Hub에서 이 W06 폴더를 엽니다.
2. `Assets/SpaceStation/Scenes/Station.unity`를 열고 **Play**를 누릅니다. `Space Station > 2. Open Game Scene` 메뉴로도 열 수 있습니다.
3. 세로 Game 뷰를 사용합니다. UI 기준 해상도는 390×844이며, 작은 화면에서는 보드를 스크롤하고 하단 실행 버튼으로 행동합니다.

시작 화면에서 연습 또는 루미 AI 대전(쉬움·보통)을 선택합니다. 주문·수집·시설·교체를 선택한 뒤 하단 실행 버튼으로 확정합니다. 메뉴를 열면 AI도 정지하며, 행동마다 자동 저장합니다. 저장은 Unity의 `Application.persistentDataPath/SpaceStation` 아래에 있습니다. 이전 WebView판의 저장 파일과는 별개입니다.

## 게임 범위

- 연습: 고정 덱 또는 새 덱, 12턴 종료 시 신뢰도 14점 목표.
- AI 대전: 각자 12번 행동, 공유 주문 2장과 덱 24장, 라운드마다 선공 교대.
- 자원 3종·시설 3종·주문 6종, 자원 상한 9, 시설 중복 설치 금지.
- 시설은 다음 자신의 차례부터 생산. 주문 교체는 1턴 소모. 덱 소진 뒤 재섞기 없음.
- 자동 저장·이어하기·백업 복구·완료 결과 재열람·턴 기록·같은 덱 재도전.
- 온라인 대전·친구 방·매칭·광고·결제는 포함하지 않았습니다.

## W01을 참고한 설정

W01의 Android 빌드 프로필과 스플래시를 가져왔습니다. 전체 비교는 `Documentation/W01-build-reference.md`에 기록했습니다.

| 항목 | W06 설정 |
|---|---|
| 화면 | Portrait, 전체 화면, 안전 영역 안쪽 렌더링 |
| CPU / 백엔드 | ARM64 / IL2CPP |
| SDK | 최소 API 25, Target API 자동 |
| 릴리스 | Development / Debugging / Profiler 끔, LZ4HC, Release Minify 켬 |
| 프로필 | `Assets/Build Profiles/W06 Android APK.asset`, `W06 Android AAB.asset` |
| 스플래시 | W01의 SecondWindGamesLogo, 흰 배경, 정지 화면 2초, Unity 로고 숨김 |
| 앱 ID | `com.secondwindgames.spacestation` |
| 버전 | 0.3.0 / versionCode 3 |

W01의 앱 ID·상품명·광고/결제/온라인 서비스 설정은 가져오지 않았습니다. W06의 게임에 해당하는 설정만 적용했습니다.

## 빌드

- `Space Station > 1. Apply W01 Android Settings`: 빌드·스플래시 설정 적용.
- `Space Station > 3. Verify Rules`: C# 규칙·저장 테스트.
- `Space Station > 4. Build Prototype APK`: `output/SpaceStation-Unity-v0.3.apk` 생성.
- `Space Station > 5. Build Release AAB`: 배포용 AAB 생성. Unity Publishing Settings에 서명 비밀번호를 입력한 뒤 사용합니다.

프로젝트의 릴리스 서명 설정은 W01의 `user.keystore` 경로와 `secondwindgames` alias를 참조합니다. 키 파일과 비밀번호는 복사하지 않습니다. 테스트용 APK 메뉴는 빌드 중에만 Unity 디버그 서명을 사용한 후 원래 설정으로 복구합니다. 출시 AAB 서명본을 자동으로 제작하거나 스토어에 게시하지 않습니다.

명령행에서는 다음을 사용할 수 있습니다.

```powershell
pwsh -File Tools/build-unity.ps1 -Mode Verify
pwsh -File Tools/build-unity.ps1 -Mode Apk
```

W06이 이미 열려 있으면 스크립트가 해당 에디터에 검증/빌드 요청을 전달합니다. Unity가 파일 변경을 아직 인식하지 못했다면 에디터에서 프로젝트를 새로고침한 뒤 실행하세요.

## 주요 소스

| 파일 | 역할 |
|---|---|
| `Assets/SpaceStation/Scripts/StationRules.cs` | 규칙, 공개 정보만 사용하는 AI, 스냅샷 복구 |
| `Assets/SpaceStation/Scripts/StationSave.cs` | 원자적 파일 저장과 이전 스냅샷 백업 |
| `Assets/SpaceStation/Scripts/StationApp.cs` | Unity 화면, 터치, 효과음, 중단/재개 |
| `Assets/SpaceStation/Resources/Station/Station.uss` | 화면 스타일 |
| `Assets/SpaceStation/Resources/Station/Art` | 우주선 6종, 정거장, 시설·자원 이미지 |
| `Assets/SpaceStation/Editor/StationProject.cs` | 프로젝트 구성과 Android 빌드 메뉴 |
| `Assets/SpaceStation/Editor/StationChecks.cs` | Unity에서 실행되는 규칙·저장 검증 |

`output/Unity-Rule-Tests.txt`, `output/Unity-Settings.txt`, `output/Unity-Build-Result.txt`에서 실제 검증·빌드 결과를 확인할 수 있습니다. 화면 확인 이미지는 `output/Unity-Home.png`, `Unity-Game.png`, `Unity-Result.png`입니다. 실기기의 노치·강제 종료·비행기 모드 검증과 AI 재미 조정은 추가 기기 테스트 대상입니다.

한글 글꼴은 Google Fonts의 [Nanum Gothic](https://github.com/google/fonts/tree/main/ofl/nanumgothic)을 그대로 포함했습니다. 라이선스는 글꼴 옆 `OFL.txt`를 참고하세요. 회사 스플래시 이미지는 사용자가 보유한 W01 원본입니다.
