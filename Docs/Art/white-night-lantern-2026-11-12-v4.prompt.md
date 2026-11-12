# 백야의 랜턴 원본 그림체 기준 재작업 — 2026-11-12 v4

상태: 사용자 검토용 수정 시안. 아직 확정 전.

사용자가 반복 편집으로 그림체가 변했다고 판단하여, 직접 첨부한 초기 이미지 exec-43913eb4-1718-40b8-9f1c-48ae3dc0c1ae.png를 기준으로 현재 모양을 다시 그리도록 요청했다. 첨부 경로의 /C:/ 접두 오류를 C:/로 정규화해 실제 파일을 확인했다.

## 참조 우선순위

1. **그림체·재질 표현의 우선 기준:** [사용자 지정 초기 이미지](../../Assets/_Project/Art/Items/WhiteNightLantern/white-night-lantern-original-style-reference.png). 원본 바이트 그대로 프로젝트에 보존했다. 낮은 채도의 청동, 미세한 재질 변화, 얇게 통합된 어두운 윤곽과 섬세한 명암을 기준으로 삼았다.
2. **배치·구조의 기준:** [v3](../../Assets/_Project/Art/Items/WhiteNightLantern/white-night-lantern-2026-11-12-v3.png). 가로 장대와 오른쪽 아래로 매달린 랜턴, 작고 곧게 정렬된 마지막 사슬고리.

이후 수정에서도 v3의 단순화된 명암을 스타일 원본으로 혼동하지 않는다. 사용자 지정 초기 그림체와 최신 배치 기준을 구분한다.

## 산출물과 확인

- [v4 PNG](../../Assets/_Project/Art/Items/WhiteNightLantern/white-night-lantern-2026-11-12-v4.png): 1536×1024 RGBA.
- 최신 에셋 좌표를 다시 확인했다: (0,0), (1,0), (2,0), (2,1), 3×2 영역의 네 칸.
- 내장 이미지 생성 도구, transparent_background=true, 두 참조를 위 순서대로 전달했다.
- 생성 원본: exec-60480687-556d-48ee-8b8e-eabfb5a1f832.png. 원본 그대로 복사했으며 이전 시안은 보존했다.
- 알파 범위 0~255. 빈 아래 왼쪽 두 칸에 최대 알파 1/255 잔여 픽셀 1834개가 있어 완전한 알파 0 조건을 충족하지는 않는다(알파 16 이상 0개). 게임 적용 전 마스크 정리 사항은 남아 있다. 스타일 복원을 우선하여 자동 후속 재생성을 반복하지 않았다.

## 실제 프롬프트

```text
Use case: style-transfer with strict composition preservation.
Two input images have DIFFERENT roles:
IMAGE 1 (VERTICAL POLE) is the USER-CHOSEN AUTHORITATIVE ART STYLE and original design reference. Match its actual rendering very closely: refined subdued pixel-art texture, smaller organically arranged pixel clusters, softly stepped nuanced material shading, thin dark plum/charcoal outlines integrated into surfaces, muted tarnished bronze fittings with small highlights rather than bright shiny gold beads, dark weathered iron, subtly textured brown leather, translucent olive-yellow glass with an irregular delicate ivory flame. It has understated hand-painted pixel-art character and dimensional materials. Preserve that subtle visual richness. Do not flatten or coarsen it, do not use thick black cartoon outlines, bright flat yellow panes, broad shiny gold highlights or large repetitive diagonal highlight stripes seen in image 2.
IMAGE 2 (HORIZONTAL POLE) is ONLY the REQUIRED CURRENT GEOMETRY / COMPOSITION reference. Repaint this horizontal arrangement using IMAGE 1's rendering, materials, palettes, details and contour weight. Image 2 must NOT dictate the painting style.
Output one complete inventory sprite on 1536x1024 transparent landscape canvas. Long horizontal black carrying pole across upper row, original leather handgrip and capped pommel at left, bent iron hook at right, short bronze chain hanging down, upright Gothic lantern suspended at bottom-right. Preserve image 2's relative placement, proportions and overall silhouette. Preserve the lantern design from image 1 including peaked hood, Gothic glass arches, restrained bronze studs and bottom finial. The final chain link attached to the lantern must be a small straight upright oval matching the preceding oval links in thickness and size, NOT an oversized tilted ring. Natural connected chain.
Inventory occupied cells on an invisible 3-column by 2-row square grid: (0,0),(1,0),(2,0),(2,1). Thus upper row occupied, only bottom-right occupied. Bottom-left and bottom-middle regions x<1024,y>=512 must stay transparent, no object or stray glow. Keep all solid art inside those four cells with margin. Preserve actual transparent alpha backdrop, no opaque background, checkerboard, grid, labels, text, UI, watermark or extra items. Do NOT rotate the composition back to image 1's vertical pole. This is recovering the exact original visual style while retaining the latest horizontal shape and corrected chain.
```

