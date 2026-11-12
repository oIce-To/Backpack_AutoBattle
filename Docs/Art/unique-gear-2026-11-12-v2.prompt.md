# 신규 고유 아이템 확정 및 랜턴 배치 수정 — 2026-11-12 v2

## 확정 6종

사용자는 디자인과 그림체를 유지하도록 요청하고 랜턴 이외 6종을 승인했다. 아래 파일은 해당 v1의 원본 바이트를 그대로 복사했으며 SHA-256 일치를 확인했다. 재생성·재가공하지 않았다.

| 아이템 | 확정 파일 | 선택 원본 |
|---|---|---|
| 서리여명의 편린 | [frost-dawn-fragment-approved.png](../../Assets/_Project/Art/Items/FrostDawnFragment/frost-dawn-fragment-approved.png) | frost-dawn-fragment-2026-11-12-v1.png |
| 바위 수호자의 우상 | [stone-guardian-idol-approved.png](../../Assets/_Project/Art/Items/StoneGuardianIdol/stone-guardian-idol-approved.png) | stone-guardian-idol-2026-11-12-v1.png |
| 처형자의 가시공 | [executioners-spiked-ball-approved.png](../../Assets/_Project/Art/Items/ExecutionersSpikedBall/executioners-spiked-ball-approved.png) | executioners-spiked-ball-2026-11-12-v1.png |
| 불사조의 깃털 | [phoenix-feather-approved.png](../../Assets/_Project/Art/Items/PhoenixFeather/phoenix-feather-approved.png) | phoenix-feather-2026-11-12-v1.png |
| 눈꽃송이 토끼 | [snowflake-rabbit-approved.png](../../Assets/_Project/Art/Items/SnowflakeRabbit/snowflake-rabbit-approved.png) | snowflake-rabbit-2026-11-12-v1.png |
| 매혹의 부적 | [charm-amulet-approved.png](../../Assets/_Project/Art/Items/CharmAmulet/charm-amulet-approved.png) | charm-amulet-2026-11-12-v1.png |

## 백야의 랜턴 v2 — 검토용

- 최신 Item_WhiteNightLantern.asset을 다시 읽었으며 점유 좌표는 (0,0), (1,0), (2,0), (2,1)이다. 3×2 바운딩 박스의 네 칸이며 회전 가능이다.
- 기존 세로 장대를 윗줄 가로 장대로 재배치하고, 오른쪽 끝에서 체인과 랜턴이 아래로 매달리게 했다. 단순 전체 회전이 아니라 랜턴의 지붕이 위를 향하도록 재구성했다.
- 가죽 감은 손잡이, 검은 철제 장대, 청동 장식, 갈고리, 고딕 랜턴과 황록색 불빛, 기존 도트 그림체를 유지했다.
- [수정 PNG](../../Assets/_Project/Art/Items/WhiteNightLantern/white-night-lantern-2026-11-12-v2.png): 1536×1024 RGBA, 원본 exec-932f16e1-e542-466a-b113-6d41f2a01006.png.
- 입력은 기존 white-night-lantern-2026-11-12-v1.png이며 별도 보존했다. v2는 아직 사용자 확정 전이다.

```text
###
..#
```

## 검수

최신 데이터와 캔버스 3:2 비율 및 네 점유 칸 각각의 본체를 확인했다. 본체는 새 점유 형태 안에 있고 아래 왼쪽 두 칸에는 본체가 없다. 다만 생성 이미지의 빈 두 칸에 알파 1/255의 잔여 픽셀 935개가 남아 있다(해당 영역 최대 알파 1, 알파 16 이상 0개). 완전한 알파 0 마스크 검수 통과본은 아니며 게임 연결 시 점유 마스크에 맞춘 정리가 필요하다. 승인된 디자인과 표현을 임의로 바꾸기 위해 재생성을 반복하지 않았다. 게임 데이터 및 UI 코드는 변경하지 않았다.

## 편집 프롬프트

내장 이미지 생성 도구, transparent_background=true, referenced_image_paths에 v1을 지정했다.

```text
Edit the provided White Night Lantern inventory sprite. User likes the current DESIGN AND PIXEL ART; preserve both. Change ONLY arrangement to fit the NEW inventory footprint.
NEW CANVAS: landscape 1536 wide x 1024 high, aspect 3:2. Divide invisibly into THREE columns and TWO rows of 512 square cells. Occupied: (0,0),(1,0),(2,0),(2,1). Mask:
###
..#
Bottom-left and bottom-middle cells must be completely transparent. The whole region x=0..1023,y=512..1023 must have exact alpha 0, with no haze/particles/shadow.
Rearrange the long BLACK POLE to run HORIZONTALLY across the TOP ROW from near x=70,y=260 to x=1330,y=260. Its original capped leather-wrapped handgrip is at LEFT end. Preserve charcoal iron shaft, silver stepped highlights, brown spiral leather, bronze collars and round rivets. At the RIGHT end, adapt the existing curved iron hook to support the short bronze chain hanging vertically downward ONLY inside the RIGHTMOST column. ONE original lantern hangs UPRIGHT from this chain in BOTTOM-RIGHT cell, around x=1270,y=760. Keep lantern top safely below y=540 and bottom above y=970, width around 260-310 pixels wholly inside x=1080..1470. The hook/chain link upper-right cell to bottom-right cell. Do not just rotate the whole image: the pole becomes horizontal but the lantern remains vertical under gravity, peaked roof at top and finial at bottom.
Strict visual invariants: same medieval plague-doctor aesthetic, same charcoal pointed Gothic lantern hood, gold/bronze fasteners, black cage arches, yellow-green glass, small ivory-yellow flame, same leather grip and knob, same dark outlined pixel art and dimensional stepped material shading. Keep recognizable original design, not a new lantern model. No extra decorative elements, no skull, no additional lamp, no ornate filigree, no characters. Complete object with small clear margins. Actual transparent PNG background, no grid, UI, text, watermark, floor or shadows. Keep any light inside glass so empty cells stay clear. Only change pose/layout to the new footprint.
```

