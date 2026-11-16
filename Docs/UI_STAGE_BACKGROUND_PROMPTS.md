# 스테이지 배경 생성 프롬프트

내장 `image_gen`으로 두 이미지를 각각 생성했다. 배경만 포함하며 UI 텍스트·패널·캐릭터는 Unity에서 별도로 표시한다.

## 던전

적용 파일: `Assets/_Project/Resources/UI/DungeonSelection.png`

```text
Use case: stylized-concept
Asset: production background illustration for a 16:9 medieval fantasy game's dungeon selection UI, no UI.
An atmospheric ancient underground dungeon: weathered gray stone vaults, a receding corridor and iron-bound door, moss in the mortar, iron torch sconces, worn broad stairs and flagstones, restrained abandoned adventuring traces near the edges. Premium detailed 2D pixel-art illustration: visible clean square pixel clusters, crisp stepped silhouettes, nuanced limited-color material shading, no smooth 3D rendering. Grounded modest fantasy matching a cozy timber-and-plaster guild aesthetic, not monumental royal architecture.
Wide frontal landscape with depth, strong visual interest toward the perimeter, calm low-contrast dark slate/blue-green shadow in the center and right-center where stage information will overlay. Small warm amber pools of torch light against cool stone. Keep the whole scene readable in subdued light; the game will add a separate dark overlay.
No people, no monsters, no readable writing, no letters, no UI panels, no icons, no logo or watermark. Full opaque 16:9 landscape.
```

## 기원의 광산

적용 파일: `Assets/_Project/Resources/UI/OriginMineSelection.png`

```text
Use case: stylized-concept
Asset: production 16:9 background illustration for the Origin Mine selection tab of a medieval fantasy pixel-art RPG. Background only.
A working underground crystal mine, sturdy plain timber support frames, weathered rock tunnel receding into shadow, narrow rail tracks and a small wooden ore cart to one side, pickaxe and modest crates at the perimeter. Beautiful pale cyan and blue-green crystal veins and clusters embedded in the stone near the outer edges, suggesting the game's Origin Shards, with restrained cool light. A few simple iron oil lanterns provide warm amber accents. Grounded handcrafted medieval mining equipment, no machinery or sci-fi.
Premium detailed 2D pixel-art game illustration: visible intentional square pixel clusters, crisp stepped silhouettes, rich controlled palette, sophisticated material shading and atmospheric depth. Match a restrained warm medieval guild and stone dungeon art style, no smooth photorealistic or 3D rendering.
Wide frontal environment with depth. Keep the center and right-center quieter and dark so a stage information panel can overlay them, detail and largest crystals at the perimeter. Readable subdued light; game adds its own separate dark overlay.
No characters, no monsters, no writing or runes, no title, no UI panels, no buttons, no icons, no watermark or logo. Full opaque 16:9 landscape.
```
