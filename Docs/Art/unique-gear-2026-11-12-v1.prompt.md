# 신규 고유 아이템 7종 — 2026-11-12 v1

후속 상태: 2026-11-12 사용자가 랜턴을 제외한 6종을 현재 v1 그대로 승인했다. 랜턴은 디자인·그림체를 유지하되 최신 점유 모양(윗줄 세 칸 + 오른쪽 아래 한 칸)에 맞춰 v2로 수정했다. 확정 파일과 최신 랜턴은 [v2 기록](unique-gear-2026-11-12-v2.prompt.md)을 따른다. 아래 데이터와 그림은 최초 생성 당시의 기록이다.
도구: 내장 이미지 생성 도구, 모든 호출 transparent_background=true.

## 데이터와 제작 기준

사용자가 지정한 7종의 최신 ItemData 및 UniqueGearData를 생성 직전에 읽었다. 모두 고유 장비이고 회전 가능이다. 기존 고정 목록을 그대로 사용하지 않았으며 점유 형태는 _occupiedCells만으로 확인했다. 효과 범위인 _placementEffects._cells를 점유 칸으로 포함하지 않았다.

기존 확정 아이템의 굵은 픽셀 윤곽, 재질별 단계적 명암, 읽기 쉬운 색면을 참고했다. 서리여명의 흰 지팡이·하늘색 수정·얼어붙은 상단·떠 있는 얼음 결정, 백야의 검은 장대와 중세 역병 의사풍 랜턴, 돌 골렘 조각상, 많은 가시가 달린 철구, 불꽃 같은 깃털, 파란 눈의 흰 토끼와 눈꽃, 연보라색 종이에 빨간 글씨 모양의 문양을 각각 반영했다.

## 산출물과 점유 형태

좌표 원점은 좌상단이며 x는 오른쪽, y는 아래로 증가한다. 크기는 가로×세로다.

| 아이템 | 점유 크기/칸 수 | 실제 좌표 | PNG |
|---|---|---|---|
| 서리여명의 편린 | 1×3 / 3칸 | (0,0), (0,1), (0,2) | [724×2172](../../Assets/_Project/Art/Items/FrostDawnFragment/frost-dawn-fragment-2026-11-12-v1.png) |
| 백야의 랜턴 | 2×3 / 4칸 | (0,0), (0,1), (0,2), (1,2) | [1024×1536](../../Assets/_Project/Art/Items/WhiteNightLantern/white-night-lantern-2026-11-12-v1.png) |
| 바위 수호자의 우상 | 1×1 / 1칸 | (0,0) | [1254×1254](../../Assets/_Project/Art/Items/StoneGuardianIdol/stone-guardian-idol-2026-11-12-v1.png) |
| 처형자의 가시공 | 2×2 / 4칸 | (0,0), (1,0), (0,1), (1,1) | [1254×1254](../../Assets/_Project/Art/Items/ExecutionersSpikedBall/executioners-spiked-ball-2026-11-12-v1.png) |
| 불사조의 깃털 | 1×2 / 2칸 | (0,0), (0,1) | [887×1774](../../Assets/_Project/Art/Items/PhoenixFeather/phoenix-feather-2026-11-12-v1.png) |
| 눈꽃송이 토끼 | 1×2 / 2칸 | (0,0), (0,1) | [887×1774](../../Assets/_Project/Art/Items/SnowflakeRabbit/snowflake-rabbit-2026-11-12-v1.png) |
| 매혹의 부적 | 1×3 / 3칸 | (0,0), (0,1), (0,2) | [724×2172](../../Assets/_Project/Art/Items/CharmAmulet/charm-amulet-2026-11-12-v1.png) |

백야의 랜턴 기본 방향:

```text
#.
#.
##
```

긴 장대는 왼쪽 세 칸에, 아래쪽 연결 갈고리와 매달린 랜턴은 맨 아래 두 칸에 있다. 오른쪽 위와 오른쪽 가운데는 비점유 칸이다. 원본 아이템 데이터는 변경하지 않았다.

## 검수 결과와 적용 전 남은 사항

- 7종 모두 PNG RGBA이며 실제 캔버스 가로:세로 비율이 최신 점유 바운딩 박스와 정확히 일치한다.
- 점유 칸 각각에 유효한 이미지 부분이 있고 배경에 알파 0이 포함되는 것을 확인했다.
- 랜턴은 첫 결과에서 한 차례 외곽과 도트 표현을 수정했다. 최종 본체는 L자 점유 영역 안에 있다.
- **랜턴은 아직 비점유 셀의 완전한 알파 0 조건을 충족하지 않는다.** 빈 두 셀(1024×1536 이미지에서 x≥512, y<1024)에 알파 1/255의 잔여 픽셀 158개가 존재한다. 해당 영역의 최대 알파는 1이며 알파 16 이상은 없다. 육안으로는 거의 보이지 않지만 게임 적용 시 점유 마스크에 맞춰 정리해야 한다. 주변의 흐린 광채도 일부 남아 있다. 이 파일을 완전한 마스크 검수 통과본으로 표시하지 않는다.
- 프롬프트의 요청 해상도와 도구의 실제 해상도는 다를 수 있다. 저장 파일은 실제 생성 원본 바이트를 그대로 복사했다.
- 시각적 도트 스타일 확인과 캔버스/알파 검수이며 픽셀 격자 정렬·팔레트 양자화·모든 반투명 가장자리 제거를 수행한 것은 아니다.
- 이번 작업은 시안 생성 및 저장이다. 게임 데이터의 아이콘 참조, 코드 및 Unity 임포트 설정은 수정하지 않았다.

## 실제 생성 프롬프트

### 서리여명의 편린

최종 생성 원본: exec-8b24d79d-6bb8-4282-b545-5e5d3674c26a.png.

```text
Create ONE standalone inventory item sprite for the same fantasy backpack game as the approved sword, bow, stone cuirass and flaming dagger shown earlier. Closely match their chunky square-pixel rendering: clearly visible stepped edges, dark muted violet/charcoal contour, broad color planes with restrained material shading and a few crisp highlights from upper left. Coarse readable pixel clusters, around 28-36 logical pixels per square inventory cell. Do not make detailed character illustration, smooth vector artwork, photoreal or 3D. Preserve some dimensional shading like the approved sprites; avoid overly flat pictograms. Actual transparent alpha PNG background. No UI, grid, cell tiles, floor, background scene, shadows outside object, labels, watermark, borders or extra items. Full object and effects within canvas, modest clear margins, centered on its specified occupied footprint. No hand or wearer. No painted checkerboard.

Exactly 1:3 tall canvas, width 768 height 2304. A slender upright magical STAFF with an ivory-WHITE shaft and a sky-blue angular ice crystal embedded at its TOP tip. Single column three square occupied cells (0,0),(0,1),(0,2). Top cell contains main crystal, small white prongs gripping its base, and THREE tiny detached ice-crystal shards floating very close beside the main crystal. All shards remain inside the single column. Ice gradually coats the upper white shaft approaching the crystal: pale cyan faceted frost sheath in upper shaft, becoming clean ivory-white farther down. Long clean white shaft continues through middle and bottom cells with modest silver bands and a simple small end cap. Main crystal sharp faceted cyan/light-blue, white highlight, deeper blue shadow; not purple, not a flame. The crystals and frost have hard pixel edges without diffuse glow or mist. No extra symbols or arrows. One complete staff only.
```

### 백야의 랜턴

최종 생성 원본: exec-45c61018-84a9-42b3-a109-7bfd4d6111ed.png.

```text
Create ONE standalone inventory item sprite for the same fantasy backpack game as the approved sword, bow, stone cuirass and flaming dagger shown earlier. Closely match their chunky square-pixel rendering: clearly visible stepped edges, dark muted violet/charcoal contour, broad color planes with restrained material shading and a few crisp highlights from upper left. Coarse readable pixel clusters, around 28-36 logical pixels per square inventory cell. Do not make detailed character illustration, smooth vector artwork, photoreal or 3D. Preserve some dimensional shading like the approved sprites; avoid overly flat pictograms. Actual transparent alpha PNG background. No UI, grid, cell tiles, floor, background scene, shadows outside object, labels, watermark, borders or extra items. Full object and effects within canvas, modest clear margins, centered on its specified occupied footprint. No hand or wearer. No painted checkerboard.

Exactly 2:3 portrait canvas, 1024x1536. ONE medieval plague-doctor lantern hanging from the LOWER END of a long black carrying POLE. The required inventory silhouette is an L with bottom foot pointing right: cells (0,0),(0,1),(0,2),(1,2). Divide the canvas invisibly into 2 columns and 3 equal square rows. Upper-right and middle-right squares MUST be COMPLETELY TRANSPARENT. Thus every pixel with x >= 50% width and y < 66.667% height must be alpha zero. THIS IS CRITICAL.
Composition: tall slim charcoal-black pole runs down the LEFT column, from x=25%,y=5% to x=25%,y=89%. An old leather grip near the upper tip. At its LOWER END it has a short crooked metal hook/arm reaching right across the bottom row at y=72%. A short chain from that low hook suspends ONE small upright Gothic lantern centered at x=73%,y=84%, all of its roof, ring and base between y=69% and y=96%, x=55%-92%. Connection crosses from left bottom cell to right bottom cell; all four cells have meaningful object content. The lantern hangs upright from the low hooked end of the pole, designed to be carried by holding the pole above it. Do NOT place lantern at top or halfway up. Do not rotate or mirror the L.
Black wrought iron frame with restrained slate-gray edge highlights, pointed medieval hood, small rivets, antique bronze fitting, narrow pale yellow-green glass panes with a tiny eerie ivory flame inside, appropriate for a plague doctor's tool. No doctor, skull accessory, gas mask, modern streetlight, stand or floor. Light stays inside the glass; no outer glow, smoke or floating particles, especially none in the TWO EMPTY RIGHT-HAND CELLS. Real transparent background.
```

최초 원본: exec-43913eb4-1718-40b8-9f1c-48ae3dc0c1ae.png. 이 원본을 참조한 후속 편집 프롬프트:

```text
Edit this exact plague-doctor pole lantern sprite. Preserve its design and EXACT L-shaped placement: tall black pole occupies the left half from top to bottom, low curved hook extends right ONLY in the bottom third, ONE Gothic greenish-yellow glass lantern hangs at bottom right. Keep leather upper grip, black iron pole, antique bronze fittings, short chain and medieval hooded lantern.
Changes: remove ALL soft surrounding haze, aura, shadow, bloom and faint residual pixels outside the solid object. Render background as exact alpha 0. No diffuse light escapes from glass. Coarsen interior details slightly to match chunky inventory sprites: broad stepped highlights, clear dark outlines, no noisy tiny texture; around 32 logical pixels per inventory cell, not a highly detailed illustration. Preserve color and structural landmarks.
Output SAME 1024x1536 transparent canvas. It is 2 columns by 3 square cells. Entire top-right and middle-right cells (x from 512 through 1023, y from 0 through 1023) are empty and MUST contain alpha 0 everywhere, no faint haze, outline or pixels. Keep hook and lantern safely BELOW y=1050, with bottom below y=1480 not clipped. Never put lantern at top. Do not move pole out of left half. No grid, text, watermark, floor or background. Full object intact, hard-edged clean transparent sprite.
```

### 바위 수호자의 우상

최종 생성 원본: exec-f70679e3-006b-4e4c-814d-8451de4adbb3.png.

```text
Create ONE standalone inventory item sprite for the same fantasy backpack game as the approved sword, bow, stone cuirass and flaming dagger shown earlier. Closely match their chunky square-pixel rendering: clearly visible stepped edges, dark muted violet/charcoal contour, broad color planes with restrained material shading and a few crisp highlights from upper left. Coarse readable pixel clusters, around 28-36 logical pixels per square inventory cell. Do not make detailed character illustration, smooth vector artwork, photoreal or 3D. Preserve some dimensional shading like the approved sprites; avoid overly flat pictograms. Actual transparent alpha PNG background. No UI, grid, cell tiles, floor, background scene, shadows outside object, labels, watermark, borders or extra items. Full object and effects within canvas, modest clear margins, centered on its specified occupied footprint. No hand or wearer. No painted checkerboard.

Exactly square 1024x1024 canvas, ONE SQUARE inventory cell. A tiny carved STONE STATUETTE shaped like a squat rock GOLEM. Clearly a small hand-carved idol made of one solid gray stone, not a living giant. Broad blocky shoulders, small squared head with two shallow dark carved eyes, chunky torso, short heavy arms folded close beside body, two short square legs resting together on a minimal integrated stone base. Compact approximately square silhouette, pale warm-gray stone faces, slate-gray shadows, clean broad chiseled bevels, very few incised details. Similar stone palette to the approved stone cuirass but sculptural golem figure. No floating rocks, moss landscape, large pedestal, glowing eyes, weapon or extra objects. Render roughly 32x32 logical pixel clusters so it remains readable as a ONE-CELL object.
```

### 처형자의 가시공

최종 생성 원본: exec-14c5a6e3-0340-45ba-b233-bb87768f2cec.png.

```text
Create ONE standalone inventory item sprite for the same fantasy backpack game as the approved sword, bow, stone cuirass and flaming dagger shown earlier. Closely match their chunky square-pixel rendering: clearly visible stepped edges, dark muted violet/charcoal contour, broad color planes with restrained material shading and a few crisp highlights from upper left. Coarse readable pixel clusters, around 28-36 logical pixels per square inventory cell. Do not make detailed character illustration, smooth vector artwork, photoreal or 3D. Preserve some dimensional shading like the approved sprites; avoid overly flat pictograms. Actual transparent alpha PNG background. No UI, grid, cell tiles, floor, background scene, shadows outside object, labels, watermark, borders or extra items. Full object and effects within canvas, modest clear margins, centered on its specified occupied footprint. No hand or wearer. No painted checkerboard.

Exactly square 1024x1024 canvas, 2x2 occupied inventory cells. A classic medieval SPIKED IRON BALL, one spherical dark iron body carrying MANY stout pointed steel spikes. More spikes than a typical simple mace icon: approximately 18-22 individually readable conical spikes, about 10-12 around the silhouette and several foreshortened on the front surface. Vary angles naturally on a sphere, not a flat sun emblem. The central round ball must stay visible among spikes. Blackened iron body with cool charcoal/slate steel facets, silver-gray spike tips and restrained upper-left highlights. Every spike fully inside canvas with padding. No long handle, chain, blood, executioner character or skull. One complete compact spiked ball filling all four quadrants; maintain large simple pixel clusters rather than microscopic spikes or dense noise.
```

### 불사조의 깃털

최종 생성 원본: exec-788c32b4-d40a-4932-a0db-e855e272de61.png.

```text
Create ONE standalone inventory item sprite for the same fantasy backpack game as the approved sword, bow, stone cuirass and flaming dagger shown earlier. Closely match their chunky square-pixel rendering: clearly visible stepped edges, dark muted violet/charcoal contour, broad color planes with restrained material shading and a few crisp highlights from upper left. Coarse readable pixel clusters, around 28-36 logical pixels per square inventory cell. Do not make detailed character illustration, smooth vector artwork, photoreal or 3D. Preserve some dimensional shading like the approved sprites; avoid overly flat pictograms. Actual transparent alpha PNG background. No UI, grid, cell tiles, floor, background scene, shadows outside object, labels, watermark, borders or extra items. Full object and effects within canvas, modest clear margins, centered on its specified occupied footprint. No hand or wearer. No painted checkerboard.

Exactly 1:2 tall canvas, 1024x2048. A SINGLE phoenix FEATHER whose shape also looks like rising FLAME. One vertical gently curving golden quill from bottom to top, clearly visible central rachis, sweeping feather barbs colored bright golden yellow and orange, outer edges deep orange/red curling upward into flame-like points. Upper cell contains most of tapered feather plume and fiery tip; lower cell contains lower feather barbs and a distinct exposed gold quill at bottom. The silhouette must clearly read as a feather with a central stem and layered barbs, not a generic flame blob. Slight natural bend without going diagonal outside one-column footprint. Coarse layered pixel shading similar to the approved orange dagger; lively warm color, restrained highlights. No phoenix bird, wing, inkpot, ring, huge smoke or halo. Any small flames stay integrated into feather outline, full transparent margins.
```

### 눈꽃송이 토끼

최종 생성 원본: exec-56370f48-6842-4294-a326-936965fe8144.png.

```text
Create ONE standalone inventory item sprite for the same fantasy backpack game as the approved sword, bow, stone cuirass and flaming dagger shown earlier. Closely match their chunky square-pixel rendering: clearly visible stepped edges, dark muted violet/charcoal contour, broad color planes with restrained material shading and a few crisp highlights from upper left. Coarse readable pixel clusters, around 28-36 logical pixels per square inventory cell. Do not make detailed character illustration, smooth vector artwork, photoreal or 3D. Preserve some dimensional shading like the approved sprites; avoid overly flat pictograms. Actual transparent alpha PNG background. No UI, grid, cell tiles, floor, background scene, shadows outside object, labels, watermark, borders or extra items. Full object and effects within canvas, modest clear margins, centered on its specified occupied footprint. No hand or wearer. No painted checkerboard.

Exactly 1:2 tall canvas, 1024x2048. ONE adorable PURE WHITE RABBIT with clearly BLUE eyes, a game inventory pet sprite. Sitting upright facing slightly three-quarter toward the viewer, both long ears rising vertically in the upper cell, small cute head with two visible sapphire-blue eyes, tiny pale pink nose, round fluffy compact chest and tucked front paws, rounded haunches and feet in lower cell. Snow-white fur, very pale icy blue/lavender shadows, soft shape rendered in chunky square pixel clusters, not detailed hair or realistic rabbit fur. Cute alert expression. THREE small recognizable six-branched pale blue SNOWFLAKES float near the ears and sides, all inside the same single-column two-cell canvas. Snowflakes should look like little snow crystals, not dots or generic four-point sparkles. Leave transparent separation between flakes and rabbit. Keep ears, all paws, tail and every snowflake fully inside canvas. No ground, snow mound, scarf, clothing, background or other animals. Match fantasy inventory icon pixel density, not a large detailed character portrait.
```

### 매혹의 부적

최종 생성 원본: exec-7ad7ec17-145a-4ed6-92b6-416977d0b086.png.

```text
Create ONE standalone inventory item sprite for the same fantasy backpack game as the approved sword, bow, stone cuirass and flaming dagger shown earlier. Closely match their chunky square-pixel rendering: clearly visible stepped edges, dark muted violet/charcoal contour, broad color planes with restrained material shading and a few crisp highlights from upper left. Coarse readable pixel clusters, around 28-36 logical pixels per square inventory cell. Do not make detailed character illustration, smooth vector artwork, photoreal or 3D. Preserve some dimensional shading like the approved sprites; avoid overly flat pictograms. Actual transparent alpha PNG background. No UI, grid, cell tiles, floor, background scene, shadows outside object, labels, watermark, borders or extra items. Full object and effects within canvas, modest clear margins, centered on its specified occupied footprint. No hand or wearer. No painted checkerboard.

Exactly 1:3 tall canvas, 768x2304. ONE long vertical PALE LAVENDER PAPER TALISMAN charm, not a gemstone necklace. A narrow light-purple paper strip with subtly folded upper edge, squared upper corners and slightly uneven but intact lower paper edge, spanning all THREE vertical inventory cells. Lavender midtone, very pale lilac highlights and deeper violet fold shadow, dark purple pixel contour. Along its face, from upper to lower portion, a vertical column of large RED VERMILION ink markings that LOOK LIKE handwritten mystical talisman script. The user specifically requests red writing-like marks: render 4-5 bold connected brushlike angular glyph clusters in pixel art, decorative invented calligraphy rather than readable modern words or a caption. Keep red script legible in contrast with light purple paper. Minimal tiny red knot at top is allowed but no large tassels, coins, hearts, jewels, frame or glow. Mostly paper with red writing. Use same chunky pixel style and restrained shading as existing items. No UI text outside the paper.
```


