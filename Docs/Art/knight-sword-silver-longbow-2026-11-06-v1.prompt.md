# 기사의 보검·은빛 장궁 생성 기록

작성일: 2026-11-06.
도구: 내장 이미지 생성 도구, transparent_background=true.
상태: 사용자 검토용 v1. PNG 생성 및 프로젝트 저장 완료. 게임 UI 연결은 별도.

## 이번 요청과 재확인

사용자는 향후 제작 때마다 대상 아이템을 지정하며, 추가·삭제·외형 변경 가능성이 있으므로 매번 최신 아이템을 재확인하도록 요청했다. 기존 20종 목록은 2026-11-06 당시의 스냅샷이며 고정 제작 목록이 아니다.

이번 생성 직전에 Item_KnightSword.asset과 Item_SilverLongbow.asset을 다시 읽었다. 두 아이템 모두 점유 좌표 (0,0), (0,1), (0,2), 가로 1칸 × 세로 3칸, 회전 가능이다.

- 기사의 보검: 과하지 않은 장식, 손잡이 중앙에 보석. 작은 푸른 보석을 손잡이 중간에 배치했다.
- 은빛 장궁: 세레나 확정 일러스트의 활을 직접 참고. 흰색 활대, 금색 끝장식과 연결부, 짙은 녹색 손잡이, 금빛 시위를 유지하고 세로로 정렬했다.
- 그림체: 사용자가 제공한 인벤토리 참고 이미지의 큼직하고 간결한 도트 방향.

## 산출물

- [기사의 보검](../../Assets/_Project/Art/Items/KnightSword/knight-sword-2026-11-06-v1.png)
- [은빛 장궁](../../Assets/_Project/Art/Items/SilverLongbow/silver-longbow-2026-11-06-v1.png)
- 두 파일 모두 724×2172 RGBA PNG, 정확한 1:3 캔버스. 원본 바이트 그대로 복사했으며 세 칸 모두 본체가 존재하고 투명 알파가 포함되는지 확인했다.
- 생성 원본: 검 exec-29635415-abab-4451-be71-c27a435e4d3d.png, 활 exec-14d9654f-1977-404f-a854-2e11c25e5445.png.
- 활 디자인 참조: Assets/_Project/Art/Characters/Serena/serena-standing-approved.png.
- 활 도트 스타일 참조: 사용자 첨부 codex-clipboard-39422b42-61ce-4ca1-a324-cf3abb80b917.png.

## 기사의 보검 실제 프롬프트

```text
Use case: stylized-concept. Create ONE standalone transparent inventory sprite of "Knight's treasured sword" for a fantasy backpack auto-battler. Match the chunky low-resolution fantasy inventory pixel-art aesthetic visible in the user's item-grid reference images, NOT a detailed character illustration. True clean square pixel clusters, stepped edges, dark muted purple-gray contour, simple 3-tone material shading, roughly a 32 by 96 logical pixel sprite enlarged crisply without smoothing. No tiny noisy details.
Composition: exactly 1:3 portrait transparent canvas, preferably 512x1536. Logical occupied cells (0,0),(0,1),(0,2), square cells in one vertical column. Entire sword upright, tip up, grip down. Blade spans upper two thirds, small crossguard and handle occupy lower third. Balanced generous readability, object fills most height with a small equal margin at top/bottom, narrow crossguard comfortably within column width. No grid drawn.
Design: understated practical knight's straight double-edged silver steel sword, pointed tip, pale cool silver highlight on one edge and blue-gray shadow plane. Modest straight metal crossguard, deep navy leather-wrapped grip, small plain steel pommel. ONE clearly visible small sapphire-blue gemstone physically inset at the exact middle of the HANDLE GRIP below the crossguard, not on the blade, not only on the pommel, not a floating jewel. Simple small metal bezel around this grip-center jewel. Keep remaining decoration minimal, not royal or ornate. Left-upper lighting in hard pixel steps.
Actual transparent alpha background. No scene, checkerboard pattern, floor shadow, glow, text, UI, frame, grid lines, watermark, hand, character, scabbard or extra weapons. No antialiasing blur, gradients, 3D rendering, painterly texture or overly fine detail. Deliver only this single complete sword.
```

## 은빛 장궁 실제 프롬프트

```text
Use case: precise-object-edit / asset extraction and style adaptation. From the FIRST supplied image, extract and redraw ONLY the exact bow held by Serena as a standalone inventory item sprite. The second supplied image is STYLE REFERENCE ONLY for coarse readable fantasy inventory pixel art. Do not copy any UI or other item from it.
Preserve Serena's bow DESIGN: long slim ivory-white/silver limbs, asymmetric flowing recurve silhouette as visible in reference, angular gold pointed end caps at BOTH tips, sparse gold trim and gold angular joints near the limb bases, dark forest-green central grip between small gold collars, thin gold bowstring connecting tip to tip. Preserve these colors and structural landmarks. Remove the character, all fingers/hands, all scenery, all illumination haze. Naturally complete only the short grip section hidden by Serena's hand. Do not substitute a brown wooden bow, plain silver bow, add a jewel, arrow, quiver, extra bow, ornamental wings or new decorations.
Reorient bow upright to fit its actual inventory footprint: 1 column by 3 square cells; coordinates (0,0),(0,1),(0,2). Exactly 1:3 portrait transparent canvas, preferably 512x1536. Full bow including both gold tip caps and entire string inside the canvas with modest margins. String on the LEFT, curved bow body on the RIGHT, grip centered vertically, tips vertically aligned for a narrow vertical silhouette. Upper limb in upper cell, grip and inner limb roots in middle cell, lower limb in lower cell. All of the bow fits within one-cell width. Open interior between string and bow is transparent. Do not draw the grid.
Render as chunky low-resolution inventory pixel art matching second reference: approximately 32x96 logical pixels enlarged crisply, clear square pixels and stepped curves, dark muted outline, 3-4 discrete shading colors per material, broad ivory limb surfaces, restrained gold highlights. Deliberately much coarser and simpler pixel density than the Serena character illustration, while retaining the SAME bow design. Make the thin string continuous and legible as a hard pixel line. No gradients, antialiased smoothing, particle effects, soft glows or overly intricate texture.
Actual transparent alpha background, no painted checkerboard, no background color, text, watermark, frame, character or shadow. One complete bow only.
```

