# 길드 배경 생성 기록

- 생성 방식: 내장 `image_gen` (`imagegen` 스킬), CLI/API fallback 사용 안 함.
- 적용 파일: `Assets/_Project/Resources/UI/GuildHallIllustrated.png`
- 기존 생성 원본은 보존하고 프로젝트 리소스로 복사했다.
- 게임 UI, 캐릭터, 가방·우편함 아이콘은 생성 이미지에 포함하지 않으며 Unity에서 별도로 표시한다.

## 첫 일러스트 프롬프트 (이전 버전)

```text
Use case: stylized-concept
Asset type: production background illustration for a 16:9 PC fantasy RPG main menu, background ONLY, no UI.
Primary request: a beautifully crafted high-quality pixel-art adventurers' guild interior, welcoming place to prepare before a journey. Create a wide 16:9 landscape composition.
Scene: richly worked dark oak beams, aged stone masonry, an elegant broad arched recess in the middle, brass lanterns with warm amber light, well-worn timber floor, a restrained burgundy runner leading to the foreground. Small believable guild details at the far sides: rolled maps, ledger books, shelves, a distant wooden reception counter and travel supplies. Cozy, dignified medieval fantasy, quiet depth and carefully composed environmental storytelling.
Style: premium hand-authored 2D pixel-art game background illustration, intentional visible square pixel clusters, crisp stepped contours, selective dithering, rich controlled palette and textured material shading. NOT a rough geometric wireframe, NOT smooth digital painting, NOT photorealistic, NOT 3D.
Composition: eye-level frontal interior with subtle depth, wide environment. The live game overlays a standing character at horizontal center, occupying x=38%-65%, y=18%-94%; keep that central area mostly a calm dark teal/green stone recess with no furniture obstructing the character and a clean floor landing near the bottom. UI panels will cover x=5%-31% at y=10%-65% and x=79%-97% at y=8%-64%, so put architectural interest above and around them and use subdued detail behind them. Keep foreground floor broad and readable. High craft in masonry, carved timber joints, iron hardware and lantern lighting; warm highlights against cool dark recess.
Text: none.
Constraints: no people or characters, no title, no letters or readable writing, no logo, no watermark, no UI panels, no buttons, no icons, no signs with words. Opaque background. Fill the entire landscape image.
```

## 두 번째 버전 프롬프트 — 소박하고 낡은 길드

내장 `image_gen` 편집으로 이전 배경을 참고했다. 금장 장식과 대형 석조 아치를 없애고 닳은 목재, 덧댄 회벽, 간단한 철제 등불과 가구로 바꿨다. 적용 경로는 동일하며 생성 원본은 도구 기본 저장 위치에 남아 있다.

```text
Use case: precise-object-edit
Edit target: the supplied pixel-art guild interior background. Produce a new equally high-quality 16:9 game background version that feels like a humble, shabby, underfunded small adventurers' guild in an old village, not a wealthy grand guild.
Preserve: excellent detailed hand-crafted pixel-art illustration quality, crisp visible pixel clusters and stepped edges, carefully shaded materials, eye-level frontal layout, warm welcoming mood, the large clear central area for a live character overlay, clear walkable foreground floor, no people, no UI, no writing.
Change the building's wealth and grandeur decisively: replace the grand carved stone arches and enormous beams with a low ordinary timber ceiling, rough mismatched beams, patched lime-plaster walls revealing small areas of old stone and brick, scuffed uneven floorboards and repaired joints. Remove ALL ornate crimson/gold banners, gilded emblems, elaborate carpet, palace-like archways, tall decorative windows, polished brass lanterns, elaborate cabinetry and decorative treasure chests. Use only two or three simple sooty iron oil lanterns, small plain glass windows, a short worn shelf with a few battered books and rolled maps, a rough patched counter, a stool, old wooden crates and modest everyday guild supplies. A small faded plain woven mat is okay, no luxurious runner. This is a maintained but impoverished workplace, not a destroyed ruin: weathered, handmade, modest, lived-in.
Composition: retain the wide 16:9 framing and readable central staging. The character overlays x=38%-65%, y=18%-94%; keep behind them a calm darker desaturated green-gray plaster wall/recess and an open floor near bottom. UI panels cover the left upper quarter and right upper quarter, so keep props out of the center and place modest details around outer margins. Do not bake UI or a character into the image.
Lighting: subdued warm pools from simple lamps against cool gray-green wall shadows; nuanced pixel texture and natural wear, fewer glowing highlights than the source. High rendering craft, humble architecture.
No text, no logo, no watermark, no border, no people, no gold ornament or heraldry.
```
