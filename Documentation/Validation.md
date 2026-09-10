# Unity 프로토타입 검증 기록

검증일: 2026-09-10  
최종 프로젝트: `C:\SecondWindGames\Repositoires\Unity\W06`  
Unity: 6000.3.23f1

## 결과

- Unity C# 컴파일 및 Android IL2CPP 빌드 성공, 빌드 오류 0개.
- 규칙·저장 테스트 15개 그룹 통과. 고정 12턴 예제, 지연 생산, 자원 상한, 행동 중복 방지, 저장 복구 및 백업을 포함합니다.
- 시드가 다른 AI 대전 200판에서 합법 행동, 공개 정보만을 이용한 판단, 자원 및 저장 상태 검증 통과.
- Unity Play 모드의 UI Toolkit 패널로 홈·4턴 게임·18점 결과 화면을 390×844로 렌더링하고 이미지를 확인했습니다. 화면은 검증용 상태이며 사용자의 플레이 저장 파일에 쓰지 않습니다.
- APK 서명 검증 통과: Android Debug 인증서, APK Signature Scheme v2.
- APK 정보: `com.secondwindgames.spacestation`, 0.3.0 / versionCode 3, ARM64, 최소 API 25, Target API 36, 세로 화면.
- Android INTERNET 권한 없음. 서버, WebView, 온라인 대전 없이 게임 로직과 AI가 Unity 안에서 동작합니다.

APK 파일 크기: **21,895,055 bytes** (약 21.9 MB). `Unity-Build-Result.txt`의 totalSize는 Unity 빌드 보고서의 집계 크기이며 최종 APK 파일 크기와 다릅니다.

APK SHA-256:

```text
6F9B66F2BE0BAEA7353D9649DC789732DD3CFB179EABEDD785AC95B63FEED97E
```

## 검증 방식과 범위

사용자의 W06 Unity 에디터가 열린 상태여서 기존 세션을 닫지 않고 동일한 Assets, Packages, ProjectSettings를 별도 임시 프로젝트에서 컴파일·빌드했습니다. 최종 W06 Assets 전체가 검증본과 SHA-256으로 일치함을 확인했습니다. `output/Unity-Settings.txt`의 Project 경로는 이 검증 프로젝트 경로입니다. W06에서 `Space Station > 1. Apply W01 Android Settings`를 실행하면 현재 프로젝트 경로로 다시 기록됩니다.

최종 설정은 W06의 `ProjectSettings`에 저장했습니다. 열린 에디터의 다음 파일 새로고침 시 같은 설정을 적용하는 요청도 준비했습니다. W01은 참고 자료로만 읽었습니다. 프로젝트의 사용자 키와 비밀번호는 복사하지 않았습니다.

검증 에디터 시작 시 Unity 검색 인덱스의 `UnityEditor.Search.SearchDatabase` 예외가 기록되었으나 C# 컴파일, 게임 패널 렌더링 및 APK 빌드는 정상 완료했습니다. 게임 코드의 런타임 예외는 캡처 로그에 없습니다.

실제 Android 기기 설치, 터치 조작, 노치, 앱 강제 종료 후 복구는 아직 검증하지 않았습니다. 제공 APK는 설치 테스트용 디버그 서명이고, AAB는 W01을 참고한 배포 프로필만 준비했습니다.
