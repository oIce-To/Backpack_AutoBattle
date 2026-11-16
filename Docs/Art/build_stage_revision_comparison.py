"""Build a comparison page from saved sprites; no raster processing."""
import json,html
from pathlib import Path
root=Path(__file__).resolve().parents[2]
doc=root/'Docs/Art'
items=json.loads((doc/'stage-items-2026-11-16.manifest.json').read_text(encoding='utf-8'))
descriptions={
'battle_axe':'최초 날 디자인의 곡선과 뾰족한 끝을 살리고 전체 크기를 줄였습니다.',
'dragonslayer':'용 장식과 날 전체를 함께 축소하여 아래쪽 날 끝을 온전하게 유지했습니다.',
'war_hero_halberd':'작은 푸른 보석, 금색 음각선과 띠 장식을 추가했습니다.',
'cat_step':'기존 부츠 디자인을 유지하고 가죽과 끈의 채색을 조금 어둡게 했습니다.',
'eclipse':'기존 디자인을 유지하고 바깥 칼날만 선홍색으로 바꿨습니다.',
'excalibur':'검신 중앙의 긴 금색 띠, 가드와 손잡이의 금장 및 푸른 보석을 추가했습니다.',
'frost_blade':'기존 서리와 구별되는 하늘색 중앙 띠를 검신 절반까지 추가했습니다.',
'hunters_bow':'중앙 깃털을 없애고 양끝에 깃털을 하나씩 달았습니다.',
'needle_storm':'중앙에서 사방으로 퍼지는 동양풍 독침과 암기로 변경했습니다.',
'permafrost':'주변 얼음 덩어리를 없애고 하나의 정교한 눈꽃 결정으로 만들었습니다.',
'poison_immunity_book':'실 제본, 세로 표제와 붉은 인장을 갖춘 동양풍 비급으로 변경했습니다.',
'solar_radiance':'타오르는 망토의 붉은색·주황색·노란색을 반영한 태양불꽃 방패입니다.',
'whetstone':'무광의 거친 입자와 긁힌 흔적을 추가하고 바깥 배경의 잡티를 제거했습니다.'}
def preview(i,before=False):
    output=i['revisionBaseline'] if before else i['output']
    rx,ry,rw,rh=i['revisionBaselineRect'] if before else i['spriteRectTopLeft']
    iw,ih=i['revisionBaselineCanvas'] if before else i['canvasSize']
    sx,sy=i['w']*64/rw,i['h']*64/rh
    cells=''.join(f'<span class="cell {"occupied" if [x,y] in i["cells"] else "empty"}" style="left:{x*64}px;top:{y*64}px"></span>' for y in range(i['h']) for x in range(i['w']))
    return f'<a href="../../{output}" target="_blank" class="art"><div class="viewport" style="width:{i["w"]*64}px;height:{i["h"]*64}px"><img src="../../{output}" alt="{html.escape(i["name"])}" style="width:{iw*sx}px;height:{ih*sy}px;left:{-rx*sx}px;top:{-ry*sy}px"><div class="cells">{cells}</div></div></a>'
cards=[]
for id,description in descriptions.items():
    i=next(i for i in items if i['id']==id)
    cards.append(f'<article><header><h2>{html.escape(i["name"])}</h2><p>{description}</p></header><div class="pair"><div><h3>수정 전</h3>{preview(i,True)}</div><div><h3>수정본</h3>{preview(i)}</div></div><footer>{len(i["cells"])}칸 · <code>{i["shape"]}</code> · <a href="../../{i["output"]}" download>수정본 PNG</a></footer></article>')
page='''<!doctype html><html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>아이템 13종 수정 전후</title><style>
*{box-sizing:border-box}body{margin:0;background:#15111f;color:#f2eafa;font:15px/1.6 system-ui,sans-serif}body>header,main{max-width:1450px;margin:auto;padding:24px}h1{margin:0;font-size:28px}p{color:#c0b2cf}a{color:#c7b4ff}nav{display:flex;gap:20px;flex-wrap:wrap;align-items:center}select{background:#30263e;color:#fff;padding:8px;border:1px solid #776387;border-radius:6px}main{display:grid;grid-template-columns:repeat(auto-fit,minmax(420px,1fr));gap:20px}article{background:#251d32;border:1px solid #493b59;border-radius:12px;overflow:hidden}article header,footer{padding:16px}h2{margin:0;font-size:20px}article p{margin:6px 0 0;min-height:48px}h3{text-align:center;font-size:13px;color:#c0b2cf;margin:8px}.pair{display:grid;grid-template-columns:1fr 1fr;gap:1px;background:#493b59}.pair>div{background:#251d32}.art{height:340px;display:flex;justify-content:center;align-items:center;background:#332943}.viewport{position:relative;overflow:hidden;flex:none}.viewport img{position:absolute;image-rendering:pixelated;max-width:none}.cells{position:absolute;inset:0;z-index:2;pointer-events:none;display:none}.grid-on .cells{display:block}.cell{position:absolute;width:64px;height:64px;border:1px solid #b8a3d275}.cell.empty{background:#ed516a36;border:1px dashed #ed6987aa}body[data-bg=light] .art{background:#e4dfeb}body[data-bg=checker] .art{background-color:#393140;background-image:conic-gradient(#554b60 25%,transparent 0 50%,#554b60 0 75%,transparent 0);background-size:24px 24px}footer{font-size:13px;color:#c0b2cf}@media(max-width:460px){main{grid-template-columns:1fr;padding:12px}.art{height:330px}.pair{overflow:auto}}
</style></head><body><header><h1>아이템 13종 · 수정 전후</h1><p>2026-11-17 요청 반영 · 수정본은 검토 중이며, 나머지 52종은 확정되었습니다.</p><nav><label><input type="checkbox" id="grid"> 점유 칸 표시</label><select id="bg" aria-label="배경"><option value="dark">어두운 배경</option><option value="light">밝은 배경</option><option value="checker">체크 배경</option></select><a href="stage-item-gallery-2026-11-16.html">전체 65종 갤러리</a><a href="stage-items-2026-11-17.revision-prompts.json">수정 프롬프트</a></nav></header><main>'''+''.join(cards)+'''</main><script>document.querySelector('#grid').addEventListener('change',e=>document.body.classList.toggle('grid-on',e.target.checked));document.querySelector('#bg').addEventListener('change',e=>document.body.dataset.bg=e.target.value);</script></body></html>'''
(doc/'stage-item-revisions-2026-11-17.html').write_text(page,encoding='utf-8')
print('13 before/after comparisons saved.')
