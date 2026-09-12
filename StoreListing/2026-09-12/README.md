# 우주 정거장 보급소 · Google Play 등록 자료

게임명: **우주 정거장 보급소 / Space Station Supply**<br>
패키지명: `com.secondwindgames.spacestation`<br>
대상 버전: Unity 프로토타입 0.3.0<br>
작성일: 2026-09-12

## 이미지 업로드

| Play Console 항목 | 파일 | 규격 |
|---|---|---|
| 앱 아이콘 | `images/app-icon-512.png` | 512×512, RGBA PNG, 1MB 미만 |
| 한국어 피처 그래픽 | `images/feature-graphic-ko-KR-1024x500.png` | 1024×500, RGB PNG |
| 영어 피처 그래픽 | `images/feature-graphic-en-US-1024x500.png` | 1024×500, RGB PNG |
| 휴대전화 스크린샷 | `images/phone/01-supply.png`부터 `04-ship-order.png`까지 | 1080×1920, RGB PNG 4장 |

스크린샷은 파일 번호 순서대로 등록하면 됩니다. 보급 선택 → 시설 설치 → 루미 AI 대전 → 우주선 주문 상세 순서입니다. 아이콘은 기존 게임 아이콘의 도형을 유지하고 사각형 전체를 채웠습니다. 모서리 마스크는 미리 적용하지 않았습니다.

한국어·영어 등록 페이지의 피처 그래픽은 해당 언어 파일을 사용하세요. 현재 게임 UI는 한국어이므로 휴대전화 스크린샷은 두 등록 페이지에서 공통으로 사용할 수 있습니다. 영어 자세한 설명에도 UI 언어를 명시했습니다.

## 설명문

각 언어 폴더에서 해당 텍스트를 그대로 복사하세요.

- `copy/ko-KR/title.txt`, `short-description.txt`, `full-description.txt`
- `copy/en-US/title.txt`, `short-description.txt`, `full-description.txt`
- `copy/alt-text.csv`: 이미지별 한국어·영어 대체 텍스트

간단한 설명은 80자 이내, 자세한 설명은 4,000자 이내로 검사했습니다. 실제 문구와 문자 수는 `manifest.json`에 기록했습니다.

## 제작과 검증

피처 그래픽 2장은 내장 image_gen으로 제작했습니다. 정거장·탐사선·화물선의 기존 게임 아트를 참고했으며, 생성 원본과 최종 프롬프트는 `source/feature-master-*.png`, `source/imagegen-prompts.json`에 보관했습니다. 최종 PNG는 스토어 규격에 맞게 축소·내보냈습니다.

스크린샷은 현재 W06과 동일한 런타임 소스·스타일·아트를 사용하는 별도 Unity 프로젝트의 실제 UI Toolkit 화면입니다. 합법적인 게임 진행 상태를 준비하고 1080×1920 렌더 텍스처로 직접 캡처했습니다. 이미지 생성 모델로 UI나 점수를 다시 그리지 않았습니다. 실제 Android 기기에서 캡처한 파일은 아닙니다. 화면은 자연스럽게 스크롤되는 게임 UI를 그대로 포함합니다.

6개 화면을 캡처한 뒤 게임 플레이를 보여주는 4개 화면을 등록용으로 선정했습니다. 캡처 도구는 `source/StoreCapture.cs`, 기록은 `source/Capture-report.txt`에 있습니다. 실제 W06의 게임 코드, APK, 빌드 옵션과 스플래시는 이번 자료 제작에서 변경하지 않았습니다.

크기, PNG 채널, 파일 크기, 설명문 길이, SHA-256 검증 결과는 `manifest.json`에 있습니다. 이미지와 설명문을 만든 상태이며 Play Console에 업로드하거나 게시하지 않았습니다.

참고한 공식 규격: [Google Play 미리보기 자료](https://support.google.com/googleplay/android-developer/answer/9866151?hl=ko), [Google Play 아이콘 디자인](https://developer.android.com/distribute/google-play/resources/icon-design-specifications), [기본 스토어 등록정보](https://support.google.com/googleplay/android-developer/answer/9859152?hl=ko).
