# 캐릭터 패시브

- CharacterData.Passive에 Character Passive SO를 연결한다. 외부 능력치와 함께 스테이지 진입 시 패시브 수치를 복사하며, 진행 중인 스테이지에는 SO 수정이 반영되지 않는다.
- 굴하지 않는 자: 시작 라이프 +1. 매 전투 시작 시 (시작 라이프 - 현재 라이프) × 최대 체력 × 10%의 방어도를 얻는다. 최초 전투는 잃은 라이프가 0이므로 방어도 0이다. 패시브 방어도 중 실제 피해를 막은 양만 통계의 캐릭터 패시브 항목에 기록한다.
- 궁수의 집중: 원거리 무기 공격 시도마다 독립적으로 30% 확률로 집중 1중첩을 얻는다. 빗나감·회피·방어도 흡수 여부와 관계없으며 근접 무기·펫·지속 피해는 발동시키지 않는다. 해당 공격의 명중/치명타 판정 뒤에 중첩을 더하므로 새 집중은 이후 공격부터 적용된다.
- 캐릭터 선택 상세 박스에 장비와 같은 72×72 패시브 아이콘을 표시한다. 커서를 올리면 이름과 효과 설명을 보여준다.

## 확정 아이콘

- 알렌: 사용자가 확정한 첨부 PNG 원본을 `Assets/_Project/Art/Passives/allen-unyielding.png`에 적용했다. 주황·금색 배경에 방패로 공격을 버티는 검은 실루엣의 그림이다.
- 세레나: `Assets/_Project/Art/Passives/serena-archers-focus.png`의 한쪽 눈 클로즈업·조준선 버전을 확정했다.
- Unity에서는 두 파일 모두 Point 필터·256px 최대 크기·압축 없음으로 임포트한다. 기존 아이콘 GUID를 유지하여 패시브 SO의 연결을 이어간다.

## 세레나 아이콘 생성 프롬프트

Built-in image_gen 사용. 알렌의 현재 확정본은 사용자 제공 파일이다.

### serenaPassiveSingleEye

Edit the first supplied square pixel-art passive icon. Image 1 is the existing art and is the STYLE, eye color and targeting-reticle reference. Image 2 is only a rough COMPOSITION sketch: use just its square close-up of ONE eye, do not copy the handwritten annotation, arrow, margins or black sketch border. Recompose the icon into an extreme close-up of only ONE of Serena's violet eyes, taking up essentially the entire square. Exactly ONE eye: not two eyes and not a full face. The upper and lower eyelid extend across nearly the full icon width; a large violet iris and dark pupil dominate the middle, with a little brow and surrounding pale skin. Do not devote large regions to hair or the nose. Preserve Serena's violet iris and dark eyelashes. Retain the current pale mint-green sniper scope reticle STYLE: a thin circular scope ring with horizontal and vertical crosshair lines and small tick marks, laid over the eye and centered on its pupil. The ring may approach or touch the icon edges. Keep the eye readable under the reticle, with no extra symbols. Simplify the drawing to clean large pixel clusters, few flat shading tones and sharp pixel-art stepped outlines on a 64x64-like grid. No smooth painting, gradients, excessive hair texture or small decorative details. Square opaque image, no lettering, no watermark, no outer UI frame. It must read clearly at 72x72.
