"""Build a comparison page from saved sprites; no raster processing."""
import json,html
from pathlib import Path
root=Path(__file__).resolve().parents[2]
doc=root/'Docs/Art'
items=json.loads((doc/'stage-items-2026-11-16.manifest.json').read_text(encoding='utf-8'))
descriptions={
'dragonslayer':'창날을 큰 이전 버전과 작은 직전 버전의 중간 크기로 조정했습니다. 도끼날은 빈 칸을 침범하지 않도록 온전한 형태로 소폭 보정했습니다.',
'war_hero_halberd':'창날을 조금 키우고 원래 장대 끝의 은색 띠를 복구했습니다. 보석 장식은 손잡이 맨 아래로 옮겼습니다.'}
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
page='''<!doctype html><html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>아이템 2종 수정 전후</title><style>
*{box-sizing:border-box}body{margin:0;background:#15111f;color:#f2eafa;font:15px/1.6 system-ui,sans-serif}body>header,main{max-width:1450px;margin:auto;padding:24px}h1{margin:0;font-size:28px}p{color:#c0b2cf}a{color:#c7b4ff}nav{display:flex;gap:20px;flex-wrap:wrap;align-items:center}select{background:#30263e;color:#fff;padding:8px;border:1px solid #776387;border-radius:6px}main{display:grid;grid-template-columns:repeat(auto-fit,minmax(420px,1fr));gap:20px}article{background:#251d32;border:1px solid #493b59;border-radius:12px;overflow:hidden}article header,footer{padding:16px}h2{margin:0;font-size:20px}article p{margin:6px 0 0;min-height:48px}h3{text-align:center;font-size:13px;color:#c0b2cf;margin:8px}.pair{display:grid;grid-template-columns:1fr 1fr;gap:1px;background:#493b59}.pair>div{background:#251d32}.art{height:340px;display:flex;justify-content:center;align-items:center;background:#332943}.viewport{position:relative;overflow:hidden;flex:none}.viewport img{position:absolute;image-rendering:pixelated;max-width:none}.cells{position:absolute;inset:0;z-index:2;pointer-events:none;display:none}.grid-on .cells{display:block}.cell{position:absolute;width:64px;height:64px;border:1px solid #b8a3d275}.cell.empty{background:#ed516a36;border:1px dashed #ed6987aa}body[data-bg=light] .art{background:#e4dfeb}body[data-bg=checker] .art{background-color:#393140;background-image:conic-gradient(#554b60 25%,transparent 0 50%,#554b60 0 75%,transparent 0);background-size:24px 24px}footer{font-size:13px;color:#c0b2cf}@media(max-width:460px){main{grid-template-columns:1fr;padding:12px}.art{height:330px}.pair{overflow:auto}}
</style></head><body class="grid-on"><header><h1>아이템 2종 · 수정 전후</h1><p>2026-11-17 요청 반영 · 이 두 수정본도 확정되어 전체 65종의 승인이 완료되었습니다.</p><nav><label><input type="checkbox" id="grid" checked> 점유 칸 표시</label><select id="bg" aria-label="배경"><option value="dark">어두운 배경</option><option value="light">밝은 배경</option><option value="checker">체크 배경</option></select><a href="stage-item-gallery-2026-11-16.html">전체 65종 갤러리</a><a href="stage-items-2026-11-17.revision3-prompts.json">수정 프롬프트</a></nav></header><main>'''+''.join(cards)+'''</main><script>document.querySelector('#grid').addEventListener('change',e=>document.body.classList.toggle('grid-on',e.target.checked));document.querySelector('#bg').addEventListener('change',e=>document.body.dataset.bg=e.target.value);</script></body></html>'''
(doc/'stage-item-revisions3-2026-11-17.html').write_text(page,encoding='utf-8')
print('2 before/after comparisons saved.')
