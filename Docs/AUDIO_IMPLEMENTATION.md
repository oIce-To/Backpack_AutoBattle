# 게임 사운드 배치

2026-11-16

원본 폴더: `C:/Users/user/Desktop/Sounds`. 원본을 수정하지 않고 `Assets/_Project/Resources/Audio`로 복사했다. 파일명은 코드에서 사용하는 이벤트명으로 정리했다.

| 적용 위치 / 이벤트 | 프로젝트 파일 | 원본 파일 |
| --- | --- | --- |
| 타이틀 화면 | TitleScene_BGM.wav | TitleScene_BGM.wav |
| 메인 및 캐릭터·장비·스테이지 선택 | MainScene_BGM.wav | MainScene_BGM.wav |
| 스테이지 상점·준비·합성 화면 | ShopPhase_BGM.wav | ShopPhase_BGM.wav |
| 전투 및 전투 결과 화면 | Battle_BGM.wav | Battle_BGM.wav |
| 버튼·탭·아이템 클릭 | ButtonClick.wav | Button_Click_Sounds_#2-1794792557165.wav |
| 상점·가방·보관함 드래그 시작 | ItemPick.wav | An_Item_Picking_Sound_#4-1794796344553.wav |
| 가방·보관함 배치 성공 | ItemPlace.wav | An_Item_Placing_Sound_#2-1794796504525.wav |
| 리롤 성공 | Reroll.wav | Shop_Reroll_Sounds_#2-1794795904594.wav |
| 구매 확정 | Buying.wav | Item_Buying_Sounds_#3-1794797175338.wav |
| 판매 성공 | Selling.wav | Item_Selling_Sounds.wav |
| 양측 가방 아이템의 공격 명중 | SimpleHit.wav | Simple_Hitting_Sound_#3-1794794599182.wav |
| 일반 라운드 승리 | RoundClear.wav | Round_Clear_Sounds_#1-1794792809385.wav |
| 라운드 패배 / 무승부 | RoundFail.wav | Round_Fail_Sounds_#3-1794795791547.wav |
| 마지막 라운드 승리 | StageClear.wav | Bright_Stage_Clear_S_#1-1794797438722.wav |
| 합성 결과 등장 | ItemMerge.wav | Item_Merging_sounds__#2-1794794738319.wav |

## 재생 규칙

- BGM은 반복 재생하며 해당 화면군이 바뀔 때만 곡을 교체한다. 메인 탭 전환 시 같은 음악을 다시 시작하지 않는다.
- 전투 중 수동 일시정지 또는 설정 팝업의 전투 정지에 맞춰 `AudioSource.Pause()` / `UnPause()`를 사용한다. 설정을 닫아도 수동 일시정지가 남아 있으면 음악은 계속 멈춰 있다. 전투 배속은 음악 속도에 영향을 주지 않는다.
- 메인·상점 설정 팝업에서는 BGM을 계속 재생한다. 전투 결과 화면에서도 전투 BGM을 유지하고, 다음 준비 화면 또는 메인 복귀 시 전환한다.
- 마스터×BGM 및 마스터×효과음의 기존 설정을 각각 적용한다. 효과음은 `PlayOneShot`으로 재생해 클릭·전투 소리를 동시에 처리한다.
- 클릭음(`ButtonClick`)만 재생 게인을 1.3배로 높였다. 버튼·탭·아이템의 공통 클릭음에 적용하며, 다른 효과음과 BGM의 게인은 유지한다. 기존 마스터·효과음 음량 및 음소거 설정도 그대로 적용한다.
- 버튼은 공통 `PrototypeAudioButton`을 사용한다. 마우스 클릭과 키보드 제출에 한 번 재생하며, 비활성 버튼과 프로그램에서 직접 호출하는 `onClick.Invoke()`에는 효과음을 넣지 않는다. 아이템과 설정의 토글/드롭다운도 실제 클릭에 연결했다.
- 구매음은 골드 차감과 구매 확정 시에만 재생한다. 구매 후 가방/보관함 배치가 완료되면 배치음도 함께 재생한다. 실패·취소한 드롭에는 구매음과 배치음이 없고, 판매에는 판매음만 사용한다.
- 타격음은 기존 `ItemActivated`의 공격 명중 이벤트를 사용한다. 화상·중독의 상태 틱, 가시 반사, 회복·방어도 획득, 빗나감에는 붙이지 않는다.
- 스테이지 클리어 시에는 Round Clear를 중복 재생하지 않고 Stage Clear만 한 번 재생한다.
- 합성 효과음은 재료가 모인 뒤 결과 아이템이 등장하는 시점(각 합성 연출 시작 후 0.42초)에 한 번 재생한다. 여러 합성이면 각 결과에 한 번씩 재생하며, 미리보기·반복 프레임·연출 중단 정리에는 재생하지 않는다. 기존 마스터·효과음 음량 설정을 따른다.

## 임포트 및 검증

BGM은 Streaming, 효과음은 Decompress On Load와 미리 로드를 사용한다. 모두 2D 재생이며 Unity 임포트 압축은 Vorbis 품질 0.8이다.

`PrototypeAudioPlayModeTests`는 실제 씬에서 BGM 화면 전환·중첩 일시정지·효과음 음량·버튼 및 키보드 입력·성공/실패한 드래그·리롤·양측 공격/상태 피해 구분·라운드/스테이지 결과 중복 방지를 검증한다. 테스트 프로필과 설정은 사용자 저장 데이터와 분리한다. 실행 결과는 `Logs/AudioIntegration`에 기록한다.

오디오 통합 테스트 4개를 포함한 전체 PlayMode **118/118 통과** (`final.xml`, 화면 캡처 포함). 원본과 복사본 13개의 SHA-256도 일치한다. 실제 청취에 따른 음량 균형이나 곡의 루프 접점에 대한 평가는 별도로 진행할 수 있다.

전투 아이템은 이제 클릭 효과음을 내므로 기존의 '클릭 핸들러 없음' 검증을 갱신하되 드래그 불가 검증은 유지했다. 설정 버튼 레이캐스트 검증에는 화면 활성화 후 한 프레임 대기를 추가해 선택적인 화면 캡처의 렌더링 부수 효과에 의존하지 않도록 했다.

이후 설정 버튼 검증을 캡처 없이 별도 실행하여 **1/1 통과** (`nocapture.xml`). `git diff --check` 통과.

후속 요청으로 합성 효과음을 추가했다. 원본과 `ItemMerge.wav`의 SHA-256 일치를 확인했고, 합성·오디오 관련 PlayMode **15/15 통과** (`Logs/FusionAudio/tests.xml`). 결과 등장 전 무음, 결과당 1회 재생, 이후 프레임의 중복 방지, 다중 합성 및 게임 시간 정지 상태에서의 재생을 검증했다.
