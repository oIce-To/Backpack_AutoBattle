# 백야의 랜턴 마지막 사슬고리 수정 — 2026-11-12 v3

상태: 사용자 검토용. 아직 확정 전.
사용자 요청: 랜턴에 직접 연결된 마지막 사슬고리가 옆으로 비틀리고 커 보이므로 다른 사슬과 같은 크기·방향으로 수정. 나머지는 유지.

- 입력: [v2](../../Assets/_Project/Art/Items/WhiteNightLantern/white-night-lantern-2026-11-12-v2.png).
- 결과: [v3](../../Assets/_Project/Art/Items/WhiteNightLantern/white-night-lantern-2026-11-12-v3.png).
- 마지막 고리를 작고 곧게 선 타원형으로 변경하여 위의 고리들과 맞췄다. 기존 장대·랜턴의 디자인과 배치를 시각적으로 유지했다.
- 1536×1024 RGBA, 3:2 캔버스 유지. 점유 모양은 윗줄 세 칸과 오른쪽 아래 한 칸이다. 기존 v2를 보존했다.
- 내장 이미지 생성 도구, transparent_background=true. 최초 고리 수정 원본: exec-8dde1475-4c85-4a61-a41d-a968644615a4.png. 해당 결과의 빈칸 배경에 알파가 생겨 투명도 정리를 한 차례 추가했다. 최종 저장 원본: exec-e9002ac4-52f8-432e-96d3-b1845626bde7.png.
- 최종 RGBA 알파 범위 0~255. 비점유 두 칸에는 알파 1/255 잔여 픽셀 66개가 남아 있어, 완전한 알파 0 마스크 통과본은 아니다. 본체 배치와 수정된 고리는 유지했다.
- 생성형 국소 편집으로 검수했으며 수정 영역 밖의 바이트 단위 동일성을 보장하는 작업은 아니다. 기존 기록의 비점유 칸 미세 알파 정리 사항은 별도다.

## 투명도 정리 프롬프트

```text
Preserve this exact lantern sprite with its corrected final small upright chain link. Change ONLY transparency outside the solid object: remove the faint full-canvas background veil introduced by the edit. Make empty background truly alpha 0, especially bottom-left and bottom-middle inventory cells x<1024,y>=512 on this 1536x1024 canvas. Preserve the horizontal pole, all links including the corrected small last oval link, lamp position, lantern body, flame, colors, pixel detail, layout and same 1536x1024 resolution. No redesign, no movement, no new glow, no new background. Transparent cutout of the same exact object.
```

## 실제 편집 프롬프트

```text
Use case: precise-object-edit. Make ONE tiny local correction to the supplied pixel-art lantern sprite. Target ONLY the oversized diagonally twisted final bronze chain link directly above and touching the lantern roof, approximately x=1320..1415,y=514..587 on this 1536x1024 image. Replace this last link with a SMALL NARROW UPRIGHT OVAL bronze link matching the size, metal thickness, vertical orientation, gold highlight and dark outline of the normal oval chain links immediately above it. No diagonal tilt, no large sideways loop. It should hang straight down in line with the other links and connect naturally to the existing top mounting cap of the lantern. Retain natural interlocking chain construction. Preserve the rest of the chain including its total length and attachment at the hook.
Strict invariants: keep the exact 1536x1024 canvas, identical horizontal pole, leather grip, metal knob, collars, hook, chain location, lantern location, pointed roof, all rivets, glass panes, flame, colors, pixel density, shading and transparency. Do not redesign, rotate, move, scale, beautify or redraw anything outside that one last chain-link region. In particular the lantern body must remain in exactly the same position. Preserve transparent background and existing effects; no added text, UI or grid. This is a local chain-link size/orientation fix only.
```

