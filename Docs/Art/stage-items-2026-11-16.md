# 인스테이지 아이템 65종 이미지

사용자가 제공한 65종 메모와 `Assets/_Project/Data/Items/Stage`의 실제 `_occupiedCells`를 대조했다. 모양 표기의 `/`는 다음 행, `#`는 점유 칸, `.`은 빈 칸이다. 위에서 아래로 읽으며 회전하지 않은 기본 배치다.

[전체 갤러리](stage-item-gallery-2026-11-16.html) · [전체 프롬프트 및 현재 이미지 목록](stage-items-2026-11-16.manifest.json) · [최초 생성 프롬프트](stage-items-2026-11-16.v1.manifest.json) · [알파/배치 검사](stage-items-2026-11-16.qa.json)

## 스타일과 제작

내장 image_gen 도구로 개별 생성했다. 기사의 보검과 바위거인의 흉갑을 스타일 참고로 사용했다. 큰 사각 픽셀, 짙은 보라/먹색 외곽선, 제한된 색 단계, 계단식 하이라이트, 작은 인벤토리에서도 구분되는 실루엣을 기준으로 했다. 그림 안에 텍스트나 격자는 그리지 않았다. 이미지별 정확한 프롬프트와 생성 원본 경로는 manifest 및 `stage-item-results`에 보관했다.

디자인이 구체적으로 지정되지 않은 용의 숨결은 불꽃이 담긴 용 장식 병, 만천화우는 최초 십자형 발사 장치에서 사용자 요청에 따라 사방으로 퍼지는 동양풍 독침과 암기로 수정했다. 2026-11-17 사용자가 마지막 용살자와 전쟁 영웅의 미늘창까지 승인하여 65종 모두 확정본으로 저장했다. 승인한 이미지의 PNG 데이터와 점유 칸 설정을 그대로 보존했다. [승인 기록](stage-items-2026-11-17.approved.json)과 [수정 프롬프트](stage-items-2026-11-17.revision3-prompts.json)를 참고한다.

## 파일 사용

PNG는 `Assets/_Project/Art/Items/Stage/<아이템 ID>/`에 있다. PNG 픽셀은 생성 원본 그대로 보존했다. Unity 메타는 Point 필터, 무압축, mipmap 없음, Full Rect로 설정했다. 일부 원본 캔버스는 점유 칸 비율과 달라 투명 여백만 제외하는 스프라이트 영역을 지정했다. 이 경우 Unity에서는 Multiple 안의 아이템 이름 스프라이트를 사용한다. 갤러리에도 동일한 영역을 적용한다. 좌표는 manifest의 `spriteRectTopLeft`에 기록되어 있다. 기존 게임 ItemData의 이미지 참조는 이 작업에서 변경하지 않았다.

빈 칸 검사는 스프라이트 영역을 실제 점유 칸으로 나눈 뒤 alpha 128 이상인 실루엣 픽셀이 없는지 확인한다. 생성 이미지의 아주 낮은 알파 경계까지 완전한 0으로 재가공한 것은 아니다. 현재 선택된 버전과 스프라이트 영역은 manifest가 기준이다. 엑스칼리버 수정본은 날과 가드를 자르지 않도록 영역을 보존했으며, 1×4 미리보기 안에서 가로세로 비율을 약 2% 조정하여 표시한다.

| 아이템 | 등급 | 칸 수 | 배치 | 이미지 | 상태 |
|---|---|---:|---|---|---|
| 전투 도끼 | 희귀 | 4 | `##/#./#.` | [PNG](../../Assets/_Project/Art/Items/Stage/battle-axe/battle-axe-approved.png) | 확정 |
| 눈먼 독버섯 | 고급 | 2 | `#/#` | [PNG](../../Assets/_Project/Art/Items/Stage/blind-mushroom/blind-mushroom-approved.png) | 확정 |
| 최후의 철혈갑주 | 전설 | 6 | `##/##/##` | [PNG](../../Assets/_Project/Art/Items/Stage/blood-iron-armor/blood-iron-armor-approved.png) | 확정 |
| 혈장미 | 영웅 | 2 | `#/#` | [PNG](../../Assets/_Project/Art/Items/Stage/blood-rose/blood-rose-approved.png) | 확정 |
| 볼라 | 희귀 | 4 | `##/##` | [PNG](../../Assets/_Project/Art/Items/Stage/bola/bola-approved.png) | 확정 |
| 타오르는 망토 | 희귀 | 4 | `##/##` | [PNG](../../Assets/_Project/Art/Items/Stage/burning-cloak/burning-cloak-approved.png) | 확정 |
| 고양이 걸음 | 희귀 | 2 | `#/#` | [PNG](../../Assets/_Project/Art/Items/Stage/cat-step/cat-step-approved.png) | 확정 |
| 저주받은 네잎클로버 | 희귀 | 1 | `#` | [PNG](../../Assets/_Project/Art/Items/Stage/cursed-clover/cursed-clover-approved.png) | 확정 |
| 눈속임 망토 | 희귀 | 4 | `##/##` | [PNG](../../Assets/_Project/Art/Items/Stage/deception-cloak/deception-cloak-approved.png) | 확정 |
| 용의 숨결 | 영웅 | 2 | `#/#` | [PNG](../../Assets/_Project/Art/Items/Stage/dragons-breath/dragons-breath-approved.png) | 확정 |
| 용살자 | 전설 | 7 | `#./##/##/#./#.` | [PNG](../../Assets/_Project/Art/Items/Stage/dragonslayer/dragonslayer-approved.png) | 확정 |
| 일식 | 전설 | 4 | `.##/##.` | [PNG](../../Assets/_Project/Art/Items/Stage/eclipse/eclipse-approved.png) | 확정 |
| 적염의 활 | 희귀 | 3 | `#/#/#` | [PNG](../../Assets/_Project/Art/Items/Stage/ember-bow/ember-bow-approved.png) | 확정 |
| 엑스칼리버 | 전설 | 4 | `#/#/#/#` | [PNG](../../Assets/_Project/Art/Items/Stage/excalibur/excalibur-approved.png) | 확정 |
| 네잎클로버 | 고급 | 1 | `#` | [PNG](../../Assets/_Project/Art/Items/Stage/four-leaf-clover/four-leaf-clover-approved.png) | 확정 |
| 서리 칼날 | 희귀 | 3 | `#/#/#` | [PNG](../../Assets/_Project/Art/Items/Stage/frost-blade/frost-blade-approved.png) | 확정 |
| 창세의 수호갑옷 | 전설 | 6 | `##/##/##` | [PNG](../../Assets/_Project/Art/Items/Stage/genesis-armor/genesis-armor-approved.png) | 확정 |
| 빙하의 심장 | 영웅 | 4 | `##/##` | [PNG](../../Assets/_Project/Art/Items/Stage/glacial-heart/glacial-heart-approved.png) | 확정 |
| 그레이트 소드 | 영웅 | 4 | `#/#/#/#` | [PNG](../../Assets/_Project/Art/Items/Stage/great-sword/great-sword-approved.png) | 확정 |
| 호크아이 | 영웅 | 2 | `##` | [PNG](../../Assets/_Project/Art/Items/Stage/hawkeye/hawkeye-approved.png) | 확정 |
| 회복 물약 | 일반 | 1 | `#` | [PNG](../../Assets/_Project/Art/Items/Stage/healing-potion/healing-potion-approved.png) | 확정 |
| 사냥꾼의 활 | 희귀 | 3 | `#/#/#` | [PNG](../../Assets/_Project/Art/Items/Stage/hunters-bow/hunters-bow-approved.png) | 확정 |
| 얼음 수정 | 고급 | 2 | `#/#` | [PNG](../../Assets/_Project/Art/Items/Stage/ice-crystal/ice-crystal-approved.png) | 확정 |
| 철 단검 | 고급 | 2 | `#/#` | [PNG](../../Assets/_Project/Art/Items/Stage/iron-dagger/iron-dagger-approved.png) | 확정 |
| 아이언 메이든 | 영웅 | 4 | `##/##` | [PNG](../../Assets/_Project/Art/Items/Stage/iron-maiden/iron-maiden-approved.png) | 확정 |
| 무쇠가시 투구 | 희귀 | 1 | `#` | [PNG](../../Assets/_Project/Art/Items/Stage/iron-thorn-helmet/iron-thorn-helmet-approved.png) | 확정 |
| 철가시 | 고급 | 1 | `#` | [PNG](../../Assets/_Project/Art/Items/Stage/iron-thorns/iron-thorns-approved.png) | 확정 |
| 가죽 갑옷 | 고급 | 4 | `##/##` | [PNG](../../Assets/_Project/Art/Items/Stage/leather-armor/leather-armor-approved.png) | 확정 |
| 가죽 장화 | 고급 | 2 | `#/#` | [PNG](../../Assets/_Project/Art/Items/Stage/leather-boots/leather-boots-approved.png) | 확정 |
| 가죽 장갑 | 고급 | 1 | `#` | [PNG](../../Assets/_Project/Art/Items/Stage/leather-gloves/leather-gloves-approved.png) | 확정 |
| 월식 | 전설 | 6 | `##/##/##` | [PNG](../../Assets/_Project/Art/Items/Stage/lunar-eclipse/lunar-eclipse-approved.png) | 확정 |
| 미스릴 | 영웅 | 1 | `#` | [PNG](../../Assets/_Project/Art/Items/Stage/mithril/mithril-approved.png) | 확정 |
| 미스릴 갑옷 | 영웅 | 4 | `##/##` | [PNG](../../Assets/_Project/Art/Items/Stage/mithril-armor/mithril-armor-approved.png) | 확정 |
| 미스릴 방패 | 영웅 | 2 | `#/#` | [PNG](../../Assets/_Project/Art/Items/Stage/mithril-shield/mithril-shield-approved.png) | 확정 |
| 달맞이꽃 | 희귀 | 2 | `#/#` | [PNG](../../Assets/_Project/Art/Items/Stage/moonflower/moonflower-approved.png) | 확정 |
| 만천화우 | 전설 | 5 | `.#./###/.#.` | [PNG](../../Assets/_Project/Art/Items/Stage/needle-storm/needle-storm-approved.png) | 확정 |
| 밤의 장막 | 영웅 | 4 | `##/##` | [PNG](../../Assets/_Project/Art/Items/Stage/night-veil/night-veil-approved.png) | 확정 |
| 페가수스의 깃털 | 희귀 | 2 | `#/#` | [PNG](../../Assets/_Project/Art/Items/Stage/pegasus-feather/pegasus-feather-approved.png) | 확정 |
| 만년설 | 영웅 | 1 | `#` | [PNG](../../Assets/_Project/Art/Items/Stage/permafrost/permafrost-approved.png) | 확정 |
| 저금통 | 일반 | 1 | `#` | [PNG](../../Assets/_Project/Art/Items/Stage/piggy-bank/piggy-bank-approved.png) | 확정 |
| 플래티넘 카드 | 희귀 | 1 | `#` | [PNG](../../Assets/_Project/Art/Items/Stage/platinum-card/platinum-card-approved.png) | 확정 |
| 플래티넘 클로버 | 영웅 | 1 | `#` | [PNG](../../Assets/_Project/Art/Items/Stage/platinum-clover/platinum-clover-approved.png) | 확정 |
| 독병 | 고급 | 1 | `#` | [PNG](../../Assets/_Project/Art/Items/Stage/poison-bottle/poison-bottle-approved.png) | 확정 |
| 만독불침의 서 | 영웅 | 4 | `##/##` | [PNG](../../Assets/_Project/Art/Items/Stage/poison-immunity-book/poison-immunity-book-approved.png) | 확정 |
| 맹독 화살통 | 희귀 | 2 | `#/#` | [PNG](../../Assets/_Project/Art/Items/Stage/poison-quiver/poison-quiver-approved.png) | 확정 |
| 태초의 불꽃 | 전설 | 4 | `#/#/#/#` | [PNG](../../Assets/_Project/Art/Items/Stage/primal-flame/primal-flame-approved.png) | 확정 |
| 화살통 | 고급 | 2 | `#/#` | [PNG](../../Assets/_Project/Art/Items/Stage/quiver/quiver-approved.png) | 확정 |
| 서슬퍼런 가시 | 희귀 | 1 | `#` | [PNG](../../Assets/_Project/Art/Items/Stage/razor-thorn/razor-thorn-approved.png) | 확정 |
| 설화만개 | 전설 | 3 | `#/#/#` | [PNG](../../Assets/_Project/Art/Items/Stage/snow-bloom/snow-bloom-approved.png) | 확정 |
| 태양의 광휘 | 영웅 | 4 | `##/##` | [PNG](../../Assets/_Project/Art/Items/Stage/solar-radiance/solar-radiance-approved.png) | 확정 |
| 창 | 고급 | 3 | `#/#/#` | [PNG](../../Assets/_Project/Art/Items/Stage/spear/spear-approved.png) | 확정 |
| 강철 검 | 고급 | 3 | `#/#/#` | [PNG](../../Assets/_Project/Art/Items/Stage/steel-sword/steel-sword-approved.png) | 확정 |
| 횃불 | 고급 | 2 | `#/#` | [PNG](../../Assets/_Project/Art/Items/Stage/torch/torch-approved.png) | 확정 |
| 정사필중 | 전설 | 4 | `#/#/#/#` | [PNG](../../Assets/_Project/Art/Items/Stage/true-shot/true-shot-approved.png) | 확정 |
| 흡혈충 | 희귀 | 2 | `#/#` | [PNG](../../Assets/_Project/Art/Items/Stage/vampire-bug/vampire-bug-approved.png) | 확정 |
| 뱀파이어 나이프 | 영웅 | 2 | `#/#` | [PNG](../../Assets/_Project/Art/Items/Stage/vampire-knife/vampire-knife-approved.png) | 확정 |
| 맹독충 | 희귀 | 1 | `#` | [PNG](../../Assets/_Project/Art/Items/Stage/venom-bug/venom-bug-approved.png) | 확정 |
| 전쟁영웅의 미늘창 | 영웅 | 5 | `#./##/#./#.` | [PNG](../../Assets/_Project/Art/Items/Stage/war-hero-halberd/war-hero-halberd-approved.png) | 확정 |
| 숫돌 | 일반 | 1 | `#` | [PNG](../../Assets/_Project/Art/Items/Stage/whetstone/whetstone-approved.png) | 확정 |
| 윈드서퍼 | 영웅 | 4 | `##/##` | [PNG](../../Assets/_Project/Art/Items/Stage/windsurfer/windsurfer-approved.png) | 확정 |
| 전령신의 날개신발 | 전설 | 4 | `##/##` | [PNG](../../Assets/_Project/Art/Items/Stage/winged-boots/winged-boots-approved.png) | 확정 |
| 나무 활 | 일반 | 3 | `#/#/#` | [PNG](../../Assets/_Project/Art/Items/Stage/wooden-bow/wooden-bow-approved.png) | 확정 |
| 나무 방패 | 고급 | 2 | `#/#` | [PNG](../../Assets/_Project/Art/Items/Stage/wooden-shield/wooden-shield-approved.png) | 확정 |
| 목검 | 일반 | 3 | `#/#/#` | [PNG](../../Assets/_Project/Art/Items/Stage/wooden-sword/wooden-sword-approved.png) | 확정 |
| 허름한 투구 | 일반 | 1 | `#` | [PNG](../../Assets/_Project/Art/Items/Stage/worn-helmet/worn-helmet-approved.png) | 확정 |
