# 고블린 스탠딩 시안 — 2026-11-05 v1

상태: 이 파일에 기록된 마지막 보정 결과는 선택되지 않았다. 사용자는 최초 생성 결과를 2026-11-05 최종 승인했다.
도구: 내장 이미지 생성 도구, transparent_background=true.
미선택 보정본: `Assets/_Project/Art/Monsters/Goblin/goblin-standing-2026-11-05-v1.png`
확정 파일: `Assets/_Project/Art/Monsters/Goblin/goblin-standing-approved.png`
승인 원본 식별자: `exec-29aff636-fc6f-4aa7-921e-54ab07caef0e.png` (아래 최초 생성 프롬프트의 결과, 이후 보정 전).
향후 작업에는 `MONSTER_ART_STYLE.md`의 승인된 일반 몬스터 도트 기준을 우선한다. 아래 보정 프롬프트의 플레이어 수준 도트 밀도 요구는 승인된 방향이 아니다.
출력: 1024 × 1536 PNG, 알파 채널 포함.

디자인: 짙은 초록색 피부와 뾰족한 귀, 해진 짙은 갈색 가죽 누더기, 나무 몽둥이를 든 일반 고블린 적.
스타일 참조: 알렌과 세레나의 승인 이미지. 최초 생성 후 외곽선과 명암을 보정하고, 세밀해진 표면을 픽셀 색면으로 정리했다. 프롬프트의 논리 해상도는 시각적 목표이며 실제 정수 격자나 동일 도트 수를 보장하지 않는다.

## 최초 생성 프롬프트

Use case: stylized-concept.
Create one new full-body standing enemy GOBLIN illustration for the same fantasy game as the approved Allen and Serena images displayed above in this conversation. Those two images are the authoritative STYLE references only, not characters to include.
STYLE MATCH: match Allen's and Serena's deliberate pixel contours, apparent square-pixel size, interior stepped color clusters, controlled detail density, restrained fantasy stylization and material rendering. The goblin must look illustrated by the same pixel artist for the same game, not a different cartoon or realistic painting. Use visible coherent pixel clusters and dark precise stair-step outlines, 3–5 discrete shades per material, no microtexture or smooth gradients. Approximately 256x384 logical pixel illustration enlarged to a 1024x1536 portrait canvas is a stylistic target. Similar pixel cadence to their faces and leather clothing; do not increase detail density for the monster.
SUBJECT: a conventional recognizable small adult fantasy goblin enemy. Dark forest-green skin with slightly lighter muted green stepped highlights, long sharp pointed ears, broad angular head, low brow, narrow alert eyes, a short pointed nose, a crooked hostile grin with a few small uneven teeth. A wiry compact body, shorter legs and slightly long arms, modest hunched shoulders: a small common goblin, not a giant muscular orc, not a cute baby or chibi mascot. Keep facial details economical and readable.
CLOTHING: worn ragged DARK BROWN LEATHER scraps stitched into a crude sleeveless tunic and short tattered lower panels, simple rough belt, uneven torn hems and a few patches. Bare dark-green arms and lower legs; crude leather ankle wraps or simple battered footwear. Practical impoverished common-enemy appearance, no heavy armor, no elite decorations.
EQUIPMENT: exactly one plain wooden club held naturally in one hand. A rough stout brown wooden branch club, thicker at the striking end, a simple wrapped grip, no metal spikes or blade, no second weapon or shield. Clearly readable grasp, fingers around handle; club held down and outward to one side and entirely visible.
POSE: grounded combat-ready standing idle pose, feet apart, slight natural knee bend, three-quarter view with face visible to viewer, one hand carrying club and free hand relaxed in a lightly curled alert posture. Full figure from both pointed ear tips to both feet, entire club, ample clear margins. Single character centered in portrait composition, not an attack action, no other characters.
COLOR/MOOD: cohesive muted fantasy colors matching Allen and Serena; deep green skin distinct from dark warm brown leather, warm wood club, subtle earthy highlights. Slightly menacing familiar low-level enemy, no gore.
BACKGROUND/OUTPUT: genuinely transparent alpha background, no gradient, no glow, no halo, no scenery, no floor or ground shadow, no text, no watermark or border. Portrait 1024x1536 PNG if supported.
Avoid: photorealism, 3D, smooth anime painting, oversize glossy eyes, tiny noisy stippling, painterly texture, overly thick sticker outlines, unnecessary ornaments, duplicated limbs or weapons.

## 스타일 보정 프롬프트

Edit input 1 (goblin). Inputs 2 (Allen) and 3 (Serena) are authoritative rendering-style references. Output ONLY the goblin.
The goblin design is correct but its pixel scale is much too coarse, outlines much too thick and shading too flat compared to the two approved human characters. Re-render the goblin at the SAME apparent pixel size and level of finish as Allen and Serena: approximately 3–4 output pixels per visible pixel cluster edge at 1024px canvas width, rather than the current roughly 8–12px blocks. Reduce the heavy cartoon black outlines to the thin selective pixel contour weight seen on Allen. Add controlled stepped intermediate shading to the dark green skin, leather folds and wooden club using the same detailed but clearly pixel-art material language as Allen's leather armor and Serena's cloth. No smooth gradients, no blur, no random noise or pixel-filter effect. This should look like their finished fantasy RPG enemy, NOT a simplified low-resolution mascot.
Preserve this goblin's basic design and composition: pointed long ears, bald angular head, dark green skin, small wiry body, torn dark brown leather tunic and ragged lower panels, rough belt, crude footwear, one wooden club held downward in one hand, free hand slightly curled, full-body three-quarter standing pose. Keep all limbs and equipment intact. Make eyes a little smaller and less cartoonishly broad while preserving the hostile goblin expression. Skin should be a DARK forest green, less yellow olive; keep natural lighter green stepped highlights without brightening the overall skin. Leather must remain deep dark brown.
Actual transparent background, no halo, no background color, no ground shadow, no text, no extra figure. Full ears, feet and club inside frame. Portrait 1024x1536. Match the references' pixel-art finish in BOTH contours and interiors.

## 최종 도트 표현 보정 프롬프트

Edit the goblin image only. Keep its exact design, pose, dark green color, brown ragged leather outfit, anatomy and single wooden club. Correct ONLY the rendering resolution and texture to match the approved Allen reference (image 2). Current goblin image 1 looks like a high-resolution painting with very fine grain. It needs to be TRUE visible medium-resolution pixel art like Allen. Redraw edges and interior shadow shapes using a consistent visible square 4-by-4 pixel cadence on this 1024-by-1536 canvas (conceptual native canvas 256-by-384 enlarged four times). Thin but clearly pixel-stepped dark outlines, 3–5 distinct shades on each material, clean compact pixel clusters with no fine grain, no stippling, no tiny linework, no smooth blended gradients. Skin anatomy should be expressed in simplified angular grouped shadow patches. Leather folds should use broad discrete stepped shapes as in Allen's armor. Do not add a thick cartoon outline; preserve a one-logical-pixel outline. Match Allen's clean visible pixel steps on face, hair and garments. Preserve the goblin's complete silhouette and equipment, use same composition and transparent alpha background. Output only one goblin, no human reference characters, no background or glow, no words.
