# Backpack AutoBattle 타이틀

2026-11-17

- 실행 진입 씬: `Assets/Scenes/Title.unity`. 빌드 씬 목록의 첫 번째로 등록했으며 씬 빌더의 목록 갱신에서도 이 순서를 유지한다.
- 기존 메인 진입 씬과 같은 방식으로 공통 콘텐츠 씬을 로드하고 타이틀 전용 화면을 표시한다. 별도 게임 데이터·설정 저장소를 복제하지 않는다. `MainMenu.unity` 직접 실행은 기존 진입 흐름을 유지한다.
- 1280×720 가로 화면 기준. 전체 화면 배경, 상단 중앙의 두 줄 제목 `Backpack AutoBattle`, 하단 중앙의 목재 `게임 시작` 버튼, 우측 상단의 기존 톱니바퀴로 구성했다. 제목은 그림에 구워 넣지 않고 독립된 UI 텍스트다.
- 타이틀에서 기존 설정 팝업의 소리·화면·전투 기본값을 조정할 수 있다. 설정을 닫을 때 저장하며 이후 메인·전투에서도 같은 값을 사용한다. 타이틀 BGM은 설정 팝업 중에도 계속 재생한다.
- `TitleScene_BGM.wav`를 원본 Sounds 폴더에서 프로젝트 `Resources/Audio`로 복사했다. 반복·스트리밍 재생이며 최초 재생 전에 저장된 마스터/BGM 음량을 적용한다. 클릭음은 기존 1.3배 게인과 효과음 음량 설정을 공유한다.
- 시작 버튼은 약 0.28초씩 페이드아웃/인한다. 전환 중 중복 시작·설정 열기를 차단한다. 기존 저장 상태에 따라 최초 이용자는 튜토리얼, 이름 미입력 이용자는 이름 입력, 완료한 이용자는 메인 화면으로 진입한다. 타이틀 대기만으로 진행 데이터를 생성하거나 튜토리얼을 시작하지 않는다.

## 배경 제작

내장 `image_gen` 모드로 생성했다. 프로젝트 저장 위치는 `Assets/_Project/Resources/UI/TitleJourney.png`. Point 필터·밉맵 없음·비압축 Sprite로 설정했다. 길드 입구의 목재·석벽과 아침 햇살 속 들판, 먼 언덕으로 이어지는 길을 사용한다.

최종 프롬프트:

> Use case: stylized-concept. Asset type: finished wide 16:9 pixel-art background illustration for the title screen of a medieval fantasy backpack adventure game. Create a high-quality richly detailed pixel art landscape, view from the modest well-maintained adventurers' guild entrance looking out across sunlit grassy fields toward a winding dirt road disappearing over distant blue-green hills. A small amount of warm brown timber door posts and weathered-but-cared-for cream plaster/stone at the extreme left and right foreground edges connects to a cozy oak-beamed guild interior; do not frame the entire view with a heavy arch or roof. Welcoming early morning light, golden grasses, small wildflowers, gentle atmospheric depth, a sense of hopeful anticipation just before a first adventure. Landscape dominates, road starts near lower center and winds toward the middle distance. Upper central third: uncluttered softly colored blue morning sky with sparse delicate clouds, reserved for a large game title which will be added in engine. Bottom center keep visually calm for a start button. Mature professional game illustration, precise visible pixel clusters, crisp stepped edges, controlled painterly pixel shading, no blurry brushstrokes, no vector shapes, no photorealism. Restrained natural palette of honey-brown wood, sage greens and warm sunlight. No people, no foreground character, no grand palace, no ruins, no UI, no title, no text, no logos, no watermark. Wide landscape 16:9.

## 검증

열려 있는 사용자 Unity 편집기를 유지하기 위해 `Logs/TitleScreenQA/Project`에 프로젝트 복사본을 만들었다. 독립 프로필·설정 경로로 테스트하며 캡처는 `Logs/TitleScreenQA/Captures`에 기록한다. 타이틀 대기, 기존 설정 재사용·음량 저장, 중복 시작 방지, 신규/기존 이용자 진입 분기를 검증한다.

전체 PlayMode **135/135 통과** (`Logs/TitleScreenQA/tests.xml`). `TitleScreen.png`와 `TitleSettings.png`의 실제 1280×720 화면에서 제목·버튼·설정 팝업 배치를 확인했다. 검증 후 임시 프로젝트 복사본을 정리하고 로그와 캡처는 유지한다. `git diff --check` 통과.
