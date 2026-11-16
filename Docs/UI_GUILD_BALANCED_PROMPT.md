# 최종 배경 수정 프롬프트 — 관리된 소박한 길드

내장 `image_gen` 편집으로 두 이전 버전을 함께 참고했다. 원본 UI 배치는 변경하지 않고 `Assets/_Project/Resources/UI/GuildHallIllustrated.png`를 교체했다.

```text
Use case: precise-object-edit
Inputs: Image 1 is the edit target (too shabby village guild). Image 2 is a wealth/finish reference (too lavish guild).
Request: produce the balanced middle ground between these interiors, maintaining the outstanding crisp detailed pixel-art illustration quality. A respectable, modest, well-maintained small-town adventurers' guild with some age and character. Neither impoverished nor aristocratic.
Keep Image 1's human-scale timber building, frontal 16:9 composition, compact reception counter on the right, small windows, left shelf/table, open foreground and calm dark green-gray center for a character overlay. Preserve visible carefully designed square pixel clusters, nuanced light, detailed materials and warm welcoming mood.
Repair Image 1: intact lime-plaster walls with only subtle aged patina, remove large cracks, peeling patches and exposed crumbling brick. Sound oak beams with attractive grain and minor scuffs, no makeshift nailed repair boards or rope holding beams together. Even well-kept floorboards with soft sheen, not distressed or glossy. Simple sturdy handmade counter and shelves with intact clean linen, no rags or torn cloth. A tidy low bookcase with modest ledgers and maps, neat supplies, a plain healthy potted plant. Replace the ragged doormat with a modest flat woven muted burgundy rug with one simple thin border, clean intact edges, no gold embroidery.
Borrow from Image 2 only its care, polish of materials and inviting ambience, NOT the grandeur. Moderate warm light from three simple iron lanterns, modest clean windows. No giant stone arch, no carved cathedral beams, no gold fittings, no elaborate heraldry or hanging banners, no lavish carpet, no treasure display. The building should feel established and comfortably functional, not luxurious, dirty, derelict or newly renovated.
Gameplay composition invariant: a live character occupies x=38%-65%, y=18%-94%; keep this area unobstructed with a low-contrast muted green-gray wall behind them and an open floor landing. UI covers upper left/right regions. No UI, no characters, no text, no logos, no watermarks. Output background only, entire 16:9 landscape filled.
```
