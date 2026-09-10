# W01 빌드·스플래시 참고 기록

참고 프로젝트: `C:\SecondWindGames\Repositoires\Unity\W01`

읽은 자료는 `ProjectSettings/ProjectSettings.asset`, `ProjectSettings/EditorBuildSettings.asset`, `Assets/Build Profiles/Android™.asset`, `Assets/Resources/SecondWindGamesLogo.png` 및 `.meta`입니다. W01 프로젝트는 수정하지 않았습니다.

| W01 원본 | W06 반영 |
|---|---|
| Unity 6000.3.23f1 | 동일 버전 |
| `defaultScreenOrientation: 0` | Portrait |
| `AndroidTargetArchitectures: 2` | ARM64. 지원 조건에 맞춰 IL2CPP를 명시적으로 설정 |
| `AndroidMinSdkVersion: 25` / Target 0 | 최소 API 25 / 자동 Target API |
| fullscreen 1, renderOutsideSafeArea 0, useSwappy 1 | 동일 |
| Gamma / stripEngineCode 1 | 동일 |
| Release Minify 1 / Debug Minify 0 | 동일 |
| Development / Profiler / Debugging 모두 0 | 동일 |
| 프로필 CompressionType 2, BuildAppBundle 1 | LZ4HC AAB 프로필 복사. APK 프로필은 Bundle 플래그만 0 |
| 스플래시 흰색 / Unity 로고 0 / 애니메이션 0 | 동일 |
| 회사 로고 Sprite GUID `48a7157773e1d2048a54ff6bf4f37327`, duration 2 | 원본 이미지와 Sprite 메타데이터 유지, 2초 |
| user.keystore / alias secondwindgames | 릴리스 서명에서 파일 경로 참조. 비밀번호나 키 파일 복제 없음 |

게임별 값은 분리했습니다. `Violet Tap`과 `com.secondwindgames.violettap` 대신 `우주 정거장 보급소`, `com.secondwindgames.spacestation`을 사용하며, 버전은 0.3.0입니다. W01 씬·UGS·광고·결제 패키지는 포함하지 않습니다.

프로토타입 APK에는 Unity 디버그 서명을 사용합니다. 제작 도중 이 설정을 임시 변경하고 빌드 뒤 W01 참조 릴리스 설정을 복구합니다. AAB 프로필은 배포 구성의 참고/준비 상태이며 스토어 업로드를 수행하지 않습니다.
