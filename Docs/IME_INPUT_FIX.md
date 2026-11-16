# 튜토리얼 이름 입력 IME 수정

2026-11-17

## 확인한 증상과 한계

두 PC에서 한/영 전환 후에도 영문만 입력된다는 사용자 재현을 기준으로 확인했다. 기존 구현은 New Input System만 활성화한 상태에서 uGUI InputField를 상속하고, 조합 문자열·IME 모드를 BaseInput override로 중계하며, 확정 문자를 onTextInput에서 직접 추가했다. 커서·삭제 등의 처리는 별도로 Event.PopEvent에 의존했다.

별도 QA 프로필로 이름 입력창을 실행했을 때 입력 포커스는 활성이고 중계 모드는 On이었지만, 기본 Input.imeCompositionMode는 Auto였다 (`Logs/ImeFix/native-state.txt`). 이는 두 입력 경로가 일치하지 않음을 보여주며, 이 진단만으로 특정 Windows/Unity 내부 버그를 확정하지는 않는다.

기존 테스트는 새 Input System에 완성된 한글을 인위적으로 주입했으므로 실제 Windows 한/영 전환을 검증하지 못했다. 이번 직접 키 입력 검증 시도는 Windows 자동화의 화면 캡처 타임아웃과 창 활성화 오류(`foreground window did not report a process id`)로 진행하지 못했다. 실제 키보드에서의 최종 재확인은 필요하다.

## 변경

- Active Input Handling을 Both로 설정했다. 게임 조작은 기존 InputSystemUIInputModule과 새 Input System을 유지하고, uGUI 문자 입력에 필요한 기본 입력 경로도 활성화한다. UI 입력 모듈을 하나 더 만들지 않는다.
- 사용자명 입력은 표준 UnityEngine.UI.InputField로 처리한다. IME 조합·확정·선택·삭제·붙여넣기를 기본 구현에 맡긴다.
- BaseInput override 및 onTextInput 직접 삽입을 제거해 두 문자 경로가 중복 소비하거나 조합을 임의로 비우지 않게 했다.
- 창 포커스를 되찾을 때 이름 입력창에 포커스가 남아 있으면 기본 IME 모드를 On으로 복구한다. OS 입력 언어를 강제 변경하지 않는다.
- 이름 규칙, 저장 형식, 튜토리얼 진행 데이터는 바꾸지 않았다.

## 검증

사용자 저장 파일 대신 `Logs/ImeFix` 및 테스트별 임시 프로필·설정을 사용한다. 회귀 테스트에서 두 입력 백엔드 활성화, 기본 입력창의 IME On 상태, 입력 중계 override 제거, 한글·영문/숫자 입력 및 삭제·저장·재로드를 확인한다. 문자 테스트는 uGUI의 확정 문자 이벤트 처리와 편집/저장을 검증하며, 실제 OS 한글 조합 성공을 대신하지 않는다.

전체 PlayMode **135/135 통과** (`Logs/ImeFix/tests.xml`). `Captures/TutorialUserName.png`에서 테스트로 입력한 `모험가42`의 표시를 확인했다. `git diff --check` 통과. 임시 Editor 진단 코드는 제거했다.

입력 백엔드 설정이 바뀌므로 Unity를 재시작하고 실행한다. 다른 PC에 배포했던 기존 실행 파일에는 변경이 반영되지 않으므로 새 빌드로 확인해야 한다.
