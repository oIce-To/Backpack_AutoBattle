# 추가 고유 아이템 5종 — 2026-11-11 v1

도구: 내장 이미지 생성 도구. 모든 호출에서 transparent_background=true.
상태: 검토용 이미지 생성 및 프로젝트 저장 완료. 게임 데이터·전투 코드·UI 연결은 변경하지 않았다.

후속 상태: 사용자가 실피드의 조끼·혈기사의 투구·가시 등껍질 어깨견장 v1과 바위거인의 흉갑·작열하는 황혼 v2를 모두 확정했다. 황혼의 v2 스타일 기준은 이 문서의 최종 결과가 아닌 최초 생성 시안으로 지정했다. 정확한 선택 원본과 확정 파일은 [v2 기록](unique-gear-2026-11-11-v2.prompt.md)을 따른다.

## 최신 데이터와 사용자 지정

제작 직전에 각 ItemData와 연결된 UniqueGearData를 다시 읽었다. 다섯 아이템 모두 _isUniqueGear=1, _canRotate=1이다. 기준은 현재 에셋의 _occupiedCells이며 이전 프롬프트 목록을 고정 목록으로 사용하지 않았다.

사용자 지정: 작열하는 황혼은 밝은 주황색 칼날, 노란색 손잡이의 근접 단검이며 불꽃에 휩싸여 있다. 나머지 네 아이템은 이름과 효과를 바탕으로 외형을 제안했다. 기존 기사의 보검·은빛 장궁 v1 PNG를 직접 열어 도트 크기, 외곽선과 색면 표현을 참고했다.

작열하는 황혼의 _placementEffects 좌표 (-1,1), (1,1)은 시너지 범위이며 그림의 점유 칸에 포함하지 않았다.

| 아이템 | 점유 좌표 | 크기/칸 수 | 최종 PNG |
|---|---|---|---|
| 작열하는 황혼 | (0,0), (0,1) | 1×2 / 2칸 | [887×1774](../../Assets/_Project/Art/Items/SearingTwilight/searing-twilight-2026-11-11-v1.png) |
| 바위거인의 흉갑 | (0,0), (1,0), (0,1), (1,1) | 2×2 / 4칸 | [1254×1254](../../Assets/_Project/Art/Items/StoneGiantCuirass/stone-giant-cuirass-2026-11-11-v1.png) |
| 실피드의 조끼 | (0,0), (1,0), (0,1), (1,1) | 2×2 / 4칸 | [1254×1254](../../Assets/_Project/Art/Items/SylphVest/sylph-vest-2026-11-11-v1.png) |
| 혈기사의 투구 | (0,0), (1,0), (0,1), (1,1) | 2×2 / 4칸 | [1254×1254](../../Assets/_Project/Art/Items/BloodKnightHelm/blood-knight-helm-2026-11-11-v1.png) |
| 가시 등껍질 어깨견장 | (0,0), (1,0) | 2×1 / 2칸 | [1774×887](../../Assets/_Project/Art/Items/ThornShellPauldron/thorn-shell-pauldron-2026-11-11-v1.png) |

## 검수

- 다섯 PNG의 가로:세로 비율을 최신 점유 셀 바운딩 박스와 비교하여 모두 정확히 일치함을 확인했다.
- RGBA 알파의 최소·최대값 0/255와 각 점유 칸에 이미지가 존재하는 것을 확인했다.
- 육안으로 본체, 외곽선, 아이템 식별, 사용자 지정 색상을 확인했다. 흉갑과 조끼는 상하 여백을 추가했다.
- 황혼은 초기 두 결과의 광채 및 캔버스 비율이 적합하지 않아 다시 생성했다. 최종 결과는 어두운 윤곽선을 가진 불꽃과 1:2 캔버스다.
- 생성 도구의 실제 출력 해상도는 프롬프트 요청값과 다르지만 점유 칸 비율은 일치한다. 출력 원본을 재가공 없이 복사했다.
- 투명 배경이지만 이 검수는 모든 픽셀의 격자 정렬이나 알파 이진화를 보증하는 검사는 아니다.

## 실제 프롬프트와 원본 식별자

### 작열하는 황혼

최종 원본: exec-14891dc7-7afc-4cab-9c00-a516556e5a23.png.

최초 프롬프트:

```text
Use case: stylized-concept. Create exactly ONE standalone fantasy backpack game inventory item sprite, matching the existing Knight Sword and Silver Longbow sprites shown earlier in this conversation. Same chunky low-resolution pixel-art family: visibly large square pixel clusters, clean stepped silhouettes, dark muted purple-brown outlines, simple broad material planes with 3-4 discrete shade levels, highlights from upper left. Approximately 32 logical pixels per inventory cell enlarged crisply. Prioritize legibility at small size. Do not turn it into detailed concept illustration. Absolutely no fine texture noise, blurry gradients, photographic rendering, 3D, antialiased curves or soft airbrushed shading. Center the complete object with modest transparent margins. Real alpha transparent background, no black/color background, no checkerboard painted into image, no floor shadow, no UI, cell grid, frame, text, numbers or watermark. No character, wearer, hand or mannequin. One item only, no sheet, no alternate views.

Item: Searing Twilight, a SHORT MELEE DAGGER ENGULFED IN FIRE. Exactly 1:2 portrait canvas, preferably 768x1536, occupied cells (0,0),(0,1), about 32x64 logical pixel appearance. Blade pointing straight up, handle down, no diagonal rotation. Clearly short dagger proportions: wide short pointed bright ORANGE blade, small simple guard and distinctly YELLOW handle. Orange blade must remain visible as a solid metal blade with sharp angular edge and pale orange highlight; yellow grip must remain easy to distinguish. Bold red-orange outer flame tongues with yellow inner fire wrap BOTH sides of the blade and lick around the guard/handle, making the dagger visibly engulfed in fire while keeping grip readable. Stylized opaque pixel flames, NO smoky glow or translucent bloom. Upper cell has blade and flame tips, lower cell has lower blade/guard and yellow grip with rounded simple pommel. Fire and all sparks confined within this ONE COLUMN TWO ROW footprint with clear transparent margin. Side-adjacency effect cells are NOT occupied cells and must not enlarge the asset. Not a long sword, torch, staff or ranged weapon. No jewel required. Orange blade and yellow handle are mandatory.
```

후속 편집 프롬프트 (최종 미채택):

```text
Edit this exact dagger sprite, preserving its short bright ORANGE blade, YELLOW grip, small guard, orange/yellow flame tongues and chunky dark outlined pixel-art design. Two corrections only: (1) remove ALL fuzzy red halo, soft bloom, blurred smoke, faint stray pixels and background haze. Only solid hard-edged stepped pixel flame shapes around the dagger should remain. Beyond crisp flame contour must be alpha 0. All object color faces should be opaque. (2) output on an EXACT 1:2 portrait canvas, 768 wide x 1536 high, by fitting the complete dagger and all flames within that narrower column. Scale the whole object uniformly as needed; do NOT stretch the blade or crop flame tips. Leave at least 5 percent completely transparent margin on every side. This represents exactly one column two square inventory cells. Keep dagger tip up. No new background, no UI, no grid, no checkerboard, no text. Real transparent PNG.
```

최종 재생성 프롬프트:

```text
Create one narrow vertical inventory sprite on an exactly 1024 x 2048 transparent canvas (width:height = 1:2). A short melee DAGGER, point up, with a solid bright ORANGE metal blade and solid YELLOW handle, surrounded by crisp chunky pixel-art red-orange and yellow flame shapes. The blade and grip must be clearly visible. Simple gray guard. Match a retro fantasy game item sprite: square pixel clusters, dark plum outlines, flat limited-color planes, roughly 32x64 logical pixel art enlarged nearest neighbor. The flame tongues MUST ALSO have clean dark outlines and hard pixel edges like the dagger. Every visible shape has solid opacity; every background pixel has zero alpha. NO LIGHT emitted outside the outlined flame shapes. NO halo, haze, smoke, bloom, blurred glow, gradients, background, floor, frame, grid, UI, text, watermark or hands. Overall art kept within center 80% width and 90% height, all tips fully inside image. Fits one column of TWO SQUARE inventory cells. Generate one image only.
```

### 바위거인의 흉갑

최종 원본: exec-3f55c62d-1381-49ba-b647-00d4dc39d7a8.png.

최초 프롬프트:

```text
Use case: stylized-concept. Create exactly ONE standalone fantasy backpack game inventory item sprite, matching the existing Knight Sword and Silver Longbow sprites shown earlier in this conversation. Same chunky low-resolution pixel-art family: visibly large square pixel clusters, clean stepped silhouettes, dark muted purple-brown outlines, simple broad material planes with 3-4 discrete shade levels, highlights from upper left. Approximately 32 logical pixels per inventory cell enlarged crisply. Prioritize legibility at small size. Do not turn it into detailed concept illustration. Absolutely no fine texture noise, blurry gradients, photographic rendering, 3D, antialiased curves or soft airbrushed shading. Center the complete object with modest transparent margins. Real alpha transparent background, no black/color background, no checkerboard painted into image, no floor shadow, no UI, cell grid, frame, text, numbers or watermark. No character, wearer, hand or mannequin. One item only, no sheet, no alternate views.

Item: Stone Giant's Cuirass. Exactly square 1:1 canvas, preferably 1024x1024, occupied cells (0,0),(1,0),(0,1),(1,1), 2x2 inventory footprint, about 64x64 logical pixels. A single broad, heavy wearable STONE BREASTPLATE shown front-on with slight shallow thickness. Upper quadrants contain thick shoulder straps and large chest plates around an empty neck opening; lower quadrants contain stout abdomen and waist plates. Gray and blue-gray carved rock slabs, broad chipped angular edges, just two or three readable dark cracks, restrained pale gray highlights. Dark leather joins visible between a few stone plates. Clearly human-shaped armor shell, not a stone golem body, not a pile of rocks, no arms or helmet. Squat robust balanced silhouette that spans all four quadrants with transparent gaps at neck/armholes. Minimal decoration, no runes or gems.
```

후속 편집 프롬프트:

```text
Edit this exact stone cuirass sprite only to improve framing and clean transparency. Preserve identical gray stone slabs, cracks, leather straps, design, pixel density and square 2x2 inventory shape. Keep the entire armor, including BOTH top shoulder straps and full bottom edge, away from canvas borders by scaling uniformly down slightly and centering on square canvas. Add 6 percent empty transparent padding on ALL FOUR SIDES. Complete any clipped upper strap edge naturally. Keep all material faces opaque, clean hard pixel edges, remove faint stray background pixels. Outside armor alpha must be zero. No new decoration, no wearer, no text, no shadows or glow. Square transparent PNG, same art style.
```

### 실피드의 조끼

최종 원본: exec-7345a4aa-6d77-4c0f-86b7-d5919f15a6ee.png.

최초 프롬프트:

```text
Use case: stylized-concept. Create exactly ONE standalone fantasy backpack game inventory item sprite, matching the existing Knight Sword and Silver Longbow sprites shown earlier in this conversation. Same chunky low-resolution pixel-art family: visibly large square pixel clusters, clean stepped silhouettes, dark muted purple-brown outlines, simple broad material planes with 3-4 discrete shade levels, highlights from upper left. Approximately 32 logical pixels per inventory cell enlarged crisply. Prioritize legibility at small size. Do not turn it into detailed concept illustration. Absolutely no fine texture noise, blurry gradients, photographic rendering, 3D, antialiased curves or soft airbrushed shading. Center the complete object with modest transparent margins. Real alpha transparent background, no black/color background, no checkerboard painted into image, no floor shadow, no UI, cell grid, frame, text, numbers or watermark. No character, wearer, hand or mannequin. One item only, no sheet, no alternate views.

Item: Sylph's Vest. Exactly square 1:1 canvas, preferably 1024x1024, occupied cells (0,0),(1,0),(0,1),(1,1), 2x2 footprint, about 64x64 logical pixels. One lightweight sleeveless fantasy VEST shown front-on, no wearer. Elegant but simple sea-green/teal fabric and soft leather, pale mint edging, small ivory collar opening, dark green folds, short central lacing or modest clasp. Upper quadrants contain the two shoulder sections with large empty arm openings, lower quadrants contain fitted waist and short split hem. A few sweeping feather-like seam shapes suggest sylph wind and speed without external effects. Read as a practical light vest, not heavy plate armor, robe, cape, shirt with sleeves or dress. Broad enough to occupy all four quadrants but airy with crisp simple silhouette. No wings or floating wind swirls, no jewels, restrained detail matching the previous sword and bow.
```

후속 편집 프롬프트:

```text
Edit this exact teal sylph vest sprite only to improve framing and clean transparency. Preserve identical green fabric, mint feather seams, ivory collar, laces, silhouette and chunky pixel-art style. Keep the FULL top collar and entire bottom hem away from canvas borders by scaling uniformly down slightly and centering on a square canvas representing 2x2 inventory cells. Add 6 percent empty transparent padding on ALL FOUR SIDES. Complete any clipped collar top naturally. Keep object color faces opaque with hard pixel edges, remove faint stray background pixels. Outside vest alpha must be zero. No new decoration, sleeves, body, text, background, shadows or glow. Square transparent PNG.
```

### 혈기사의 투구

최종 원본: exec-2fd7a534-ea3b-4055-81cb-3be7a6620e08.png.

최초 프롬프트:

```text
Use case: stylized-concept. Create exactly ONE standalone fantasy backpack game inventory item sprite, matching the existing Knight Sword and Silver Longbow sprites shown earlier in this conversation. Same chunky low-resolution pixel-art family: visibly large square pixel clusters, clean stepped silhouettes, dark muted purple-brown outlines, simple broad material planes with 3-4 discrete shade levels, highlights from upper left. Approximately 32 logical pixels per inventory cell enlarged crisply. Prioritize legibility at small size. Do not turn it into detailed concept illustration. Absolutely no fine texture noise, blurry gradients, photographic rendering, 3D, antialiased curves or soft airbrushed shading. Center the complete object with modest transparent margins. Real alpha transparent background, no black/color background, no checkerboard painted into image, no floor shadow, no UI, cell grid, frame, text, numbers or watermark. No character, wearer, hand or mannequin. One item only, no sheet, no alternate views.

Item: Blood Knight's Helm. Exactly square 1:1 canvas, preferably 1024x1024, occupied cells (0,0),(1,0),(0,1),(1,1), 2x2 footprint, about 64x64 logical pixels. One complete medieval knight HELMET front-facing with a little shallow visible side depth. Deep blood-red/crimson steel plates, burgundy shadows, restrained pale rose steel edge highlights, dark gunmetal trim. Rounded angular crown in upper quadrants, narrow dark horizontal visor slit across face, sturdy nose ridge and cheek guards filling lower quadrants, compact neck rim. A small simple central ridge, no huge horns or tall plume. Strong closed helmet silhouette and clearly empty visor, no human face/eyes. Theme of sacrificing health for protection conveyed by red metal, not literal blood or gore. Do not add a body, shoulder armor, skull face or elaborate royal ornaments. One clean readable helmet fills the square with breathing room.
```

### 가시 등껍질 어깨견장

최종 원본: exec-992ae8b3-6278-4c95-83d4-284ef7405978.png.

최초 프롬프트:

```text
Use case: stylized-concept. Create exactly ONE standalone fantasy backpack game inventory item sprite, matching the existing Knight Sword and Silver Longbow sprites shown earlier in this conversation. Same chunky low-resolution pixel-art family: visibly large square pixel clusters, clean stepped silhouettes, dark muted purple-brown outlines, simple broad material planes with 3-4 discrete shade levels, highlights from upper left. Approximately 32 logical pixels per inventory cell enlarged crisply. Prioritize legibility at small size. Do not turn it into detailed concept illustration. Absolutely no fine texture noise, blurry gradients, photographic rendering, 3D, antialiased curves or soft airbrushed shading. Center the complete object with modest transparent margins. Real alpha transparent background, no black/color background, no checkerboard painted into image, no floor shadow, no UI, cell grid, frame, text, numbers or watermark. No character, wearer, hand or mannequin. One item only, no sheet, no alternate views.

Item: Thorn Shell Pauldron. Exactly 2:1 landscape canvas, preferably 1536x768, occupied cells (0,0),(1,0), TWO horizontal square inventory cells, about 64x32 logical pixels. ONE broad low-domed shoulder pauldron made from olive-green tortoise-like shell armor, viewed slightly from above/front so the rounded shell cap and lower rim are both readable. Shell sweeps left-to-right across both occupied cells as one connected object, upper surface divided into a few large polygonal green scutes. Several stout ivory/tan THORNS project from its upper surface, tips entirely inside the short canvas, not tall enough to add a second cell row. Dark moss shadows, muted yellow-green highlights, brown leather attachment strap and reinforced dark rim visible just beneath shell. Low wide asymmetrical shoulder-armor silhouette, not an entire turtle, not a shield, not two separate shoulder pads, no wearer or hand. Keep spike count low and silhouettes bold. All thorns and straps fit within the 2x1 bounding rectangle with transparent margins.
```


