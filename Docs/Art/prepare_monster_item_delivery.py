"""Package monster item review assets; never edits PNG pixels or live item data."""
import json,re,uuid,html,hashlib
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
DOC=ROOT/'Docs/Art'
items=json.loads((DOC/'monster-items-2026-11-17.delivery.json').read_text(encoding='utf-8'))
assert len(items)==len({i['id'] for i in items})==31
template=(ROOT/'Assets/_Project/Art/Items/KnightSword/knight-sword-2026-11-06-v1.png.meta').read_text(encoding='utf-8')
# Reuse the already reviewed importer construction, not the stage packaging loop.
stage=(DOC/'prepare_stage_item_delivery.py').read_text(encoding='utf-8')
meta_code=stage[stage.index('    # Preserve existing GUID'):stage.index("    link = '../../'")]
meta_code='if True:\n'+meta_code
status_names=['신속','빙결','정밀','집중','실명','민첩','화상','중독','가시','흡혈','분노','역병']
qa=[]
for i in items:
    path=ROOT/i['output'];im=Image.open(path)
    assert im.mode=='RGBA',i['id']
    iw,ih=im.size;a=im.getchannel('A')
    if i['w']==1 and i['h']>1:
        rw=ih//i['h'];rh=ih;rx=(iw-rw)//2;ry=0
    else:rx,ry,rw,rh=0,0,iw,ih
    bbox=a.point(lambda v:255 if v>=32 else 0).getbbox()
    assert rx<=bbox[0] and ry<=bbox[1] and rx+rw>=bbox[2] and ry+rh>=bbox[3],(i['id'],bbox)
    live=(ROOT/i['path']).read_text(encoding='utf-8')
    block=re.search(r'  _occupiedCells:\n((?:  - .*\n?)+)',live).group(1)
    cells=[[int(x),int(y)] for x,y in re.findall(r'x: (-?\d+), y: (-?\d+)',block)]
    assert cells==i['cells'],i['id']
    empty=[]
    for y in range(i['h']):
        for x in range(i['w']):
            if [x,y] in cells:continue
            cell=a.crop((rx+x*rw//i['w'],ry+y*rh//i['h'],rx+(x+1)*rw//i['w'],ry+(y+1)*rh//i['h']))
            hist=cell.histogram()
            empty.append(dict(cell=[x,y],max_alpha=cell.getextrema()[1],visible_pixels=sum(hist[32:])))
            assert sum(hist[32:])==0,(i['id'],x,y)
    assert a.getextrema()[0]==0
    i['canvasSize']=[iw,ih];i['spriteRectTopLeft']=[rx,ry,rw,rh]
    i['sha256']=hashlib.sha256(path.read_bytes()).hexdigest()
    # Only alpha<32 fringe can lie outside the preserved visible silhouette.
    qa.append(dict(id=i['id'],canvas=[iw,ih],sprite_rect=[rx,ry,rw,rh],visible_alpha_threshold=32,visible_bbox=bbox,empty_cells=empty,sha256=i['sha256']))
    exec(meta_code)
    effects=[]
    w=i.get('weaponStats') or {}
    attacks=any(e.get('_kind')==0 for e in (i.get('activation') or {}).get('_effects',[]))
    if w and attacks: effects.append(f"기본 공격력 {w.get('_attackDamage',0):g} · 주기 {w.get('_cooldownSeconds',0):g}초")
    for s in i['data'].get('_battleStartStatuses',[]):
        effects.append(f"전투 시작: {'자신' if s['_target']==0 else '상대'}에게 {status_names[s['_kind']]} {s['_stacks']}중첩")
    for e in (i.get('activation') or {}).get('_effects',[]):
        if '_status' in e:
            s=e['_status'];effects.append(f"{'명중 시' if e.get('_trigger')==1 else '발동 시'}: {'자신' if s['_target']==0 else '상대'}에게 {status_names[s['_kind']]} {s['_stacks']}중첩")
    if i['effect']:effects.append(i['effect'].replace('{period}',str((i.get('activation') or {}).get('_cooldownSeconds',5))))
    i['effectSummary']=' / '.join(effects) or '기본 공격'
    assert i['placement']['_rotation']==0
for folder in [ROOT/'Assets/_Project/Art/Items/Monsters']+[ROOT/str(Path(i['output']).parent) for i in items]:
    p=Path(str(folder)+'.meta')
    if not p.exists():p.write_text(f'fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',encoding='utf-8')
(DOC/'monster-items-2026-11-17.delivery.json').write_text(json.dumps(items,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
(DOC/'monster-items-2026-11-17.qa.json').write_text(json.dumps(qa,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')

def sprite(i,cell=64,grid=True):
    iw,ih=i['canvasSize'];rx,ry,rw,rh=i['spriteRectTopLeft'];scale=cell*i['w']/rw
    bg=''.join(f'<b class="cell {"on" if [x,y] in i["cells"] else "off"}" style="left:{x*cell}px;top:{y*cell}px;width:{cell}px;height:{cell}px"></b>' for y in range(i['h']) for x in range(i['w'])) if grid else ''
    return f'<div class="viewport" style="width:{cell*i["w"]}px;height:{cell*i["h"]}px">{bg}<img alt="{html.escape(i["name"])}" src="../../{i["output"]}" style="width:{iw*scale}px;height:{ih*scale}px;left:{-rx*scale}px;top:{-ry*scale}px"></div>'

cards=[]
for mid in dict.fromkeys(i['monsterId'] for i in items):
    group=[i for i in items if i['monsterId']==mid];m=group[0];bag=[]
    used=set()
    for i in group:
        ox,oy=i['placement']['_origin'].values()
        for x,y in i['cells']:
            assert (ox+x,oy+y) not in used,(mid,'overlap')
            assert 0<=ox+x<6 and 0<=oy+y<6,(mid,'bounds')
            used.add((ox+x,oy+y))
        bag.append(f'<div class="placed" style="left:{ox*40}px;top:{oy*40}px" title="{html.escape(i["name"])}">{sprite(i,40,False)}</div>')
    sub=[]
    for i in group:
        state="확정" if i["status"]=="approved" else "수정본 · 검토 중"
        sub.append(f'<div class="item"><a class="preview" href="../../{i["output"]}" target="_blank">{sprite(i)}</a><div><h3>{html.escape(i["name"])}</h3><p class="tag">{state} · {len(i["cells"])}칸 · {i["w"]}×{i["h"]} · <code>{i["shape"]}</code></p><p>{html.escape(i["effectSummary"])}</p><a href="../../{i["output"]}" download>투명 PNG</a><details><summary>디자인 설명</summary><p>{html.escape(i["design"])}</p></details></div></div>')
    cards.append(f'<article data-search="{html.escape(m["monsterName"]+" "+" ".join(i["name"] for i in group))}"><h2>{html.escape(m["monsterName"])}</h2><div class="overview"><img class="portrait" src="../../{m["illustration"]}" alt="{html.escape(m["monsterName"])}"><div><p>현재 전투 데이터의 가방 배치 · 6×6</p><div class="bag">{"".join(bag)}</div></div></div>{"".join(sub)}</article>')
approved_count=sum(i['status']=='approved' for i in items)
state_summary=f'{approved_count}종 모두 확정' if approved_count==len(items) else f'{approved_count}종 확정 · {len(items)-approved_count}종 검토 중'
head='''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>몬스터 아이템 31종 검토</title><style>
*{box-sizing:border-box}body{margin:0;background:#171320;color:#eee8f5;font:15px/1.55 system-ui,sans-serif}header,main,footer{max-width:1500px;margin:auto;padding:24px}header{position:relative}h1{margin:0 0 12px}h2{margin:0 0 18px}h3{margin:0}p{margin:8px 0}a{color:#b6d6ff}nav{display:flex;gap:16px;align-items:center;flex-wrap:wrap}input,select,button{font:inherit;padding:6px 10px}main{display:grid;grid-template-columns:repeat(auto-fit,minmax(400px,1fr));gap:20px}article{background:#252032;border:1px solid #4c415b;border-radius:12px;padding:22px}article[hidden]{display:none}.overview{display:flex;align-items:center;gap:16px;margin-bottom:24px}.portrait{width:110px;height:190px;object-fit:contain}.overview p{font-size:12px;color:#bcaecb}.bag{position:relative;width:240px;height:240px;background-color:#30263e;background-image:linear-gradient(to right,#655174 1px,transparent 1px),linear-gradient(to bottom,#655174 1px,transparent 1px);background-size:40px 40px;outline:1px solid #655174}.placed{position:absolute}.item{display:grid;grid-template-columns:145px 1fr;gap:18px;padding:18px 0;border-top:1px solid #4c415b}.item p{font-size:13px}.preview{display:flex;justify-content:center;align-items:center;min-height:150px;padding:10px;background:#1a1426}.viewport{position:relative;overflow:hidden;flex-shrink:0}.viewport img{position:absolute;max-width:none;image-rendering:pixelated;pointer-events:none}.cell{position:absolute;border:1px solid #766187;background:#322744}.cell.off{border:1px dashed #bf6076;background:#4b20312e}body.hide-grid .cell{display:none}.tag,code{color:#c2acd9;font-size:12px}.item details{font-size:12px;margin-top:10px}body[data-bg=light] .preview{background:#e4dfeb}body[data-bg=checker] .preview{background:repeating-conic-gradient(#a99fad 0% 25%,#ddd5e2 0% 50%) 50%/20px 20px}footer{font-size:13px;color:#b7a8c8}@media(max-width:600px){main{display:block;padding:12px}article{margin-bottom:16px;padding:12px}.overview{gap:8px}.portrait{width:70px}.item{grid-template-columns:130px 1fr}}
</style><body><header><h1>몬스터 아이템 · 31종</h1><p>몬스터 25종의 현재 일러스트·보유 아이템·효과·점유 칸을 바탕으로 제작한 검토용 이미지입니다.</p><p>무기와 신체 무기 28종 + 가방에 배치되는 스킬 아이템 3종. 아이템 미리보기는 한 칸 64px, 가방 배치는 한 칸 40px입니다.</p><nav><input id="search" placeholder="몬스터 / 아이템 이름 검색"><label><input id="grid" type="checkbox" checked> 점유 칸 표시</label><select id="bg"><option value="dark">어두운 배경</option><option value="light">밝은 배경</option><option value="checker">체크 배경</option></select><span id="count">25종 몬스터</span></nav><p>실선은 점유 칸, 붉은 점선은 비워 두는 칸입니다. 현재 결과는 검토용이며 인게임 참조는 아직 교체하지 않았습니다.</p></header><main>'''
tail='''</main><footer>2026-11-17 · PNG 원본을 보존하고 Unity 스프라이트 영역·Point 필터·무압축 설정을 추가했습니다.<br>표시된 공격력은 기본 아이템 데이터 기준이며 라운드별 능력치 보정은 별도입니다.<br><a href="monster-items-2026-11-17.delivery.json">납품 목록 / 프롬프트 / 스프라이트 영역</a> · <a href="monster-items-2026-11-17.qa.json">점유 칸 검사 기록</a></footer><script>
const cards=[...document.querySelectorAll('article')];document.querySelector('#search').oninput=e=>{let n=0;cards.forEach(c=>{c.hidden=!c.dataset.search.includes(e.target.value.trim());if(!c.hidden)n++});document.querySelector('#count').textContent=n+'종 몬스터'};document.querySelector('#grid').onchange=e=>document.body.classList.toggle('hide-grid',!e.target.checked);document.querySelector('#bg').onchange=e=>document.body.dataset.bg=e.target.value;
</script></body></html>'''
head=head.replace('현재 결과는 검토용이며 인게임 참조는 아직 교체하지 않았습니다.',state_summary+'입니다. 인게임 참조는 아직 교체하지 않았습니다.')
if approved_count==len(items):
    head=head.replace('몬스터 아이템 31종 검토','몬스터 아이템 31종 확정').replace('제작한 검토용 이미지입니다.','제작하여 확정한 이미지입니다.')
(DOC/'monster-item-gallery-2026-11-17.html').write_text(head+'\n'.join(cards)+tail,encoding='utf-8')
rows=['# 몬스터 아이템 이미지 31종','', state_summary+'. 인게임 아이템 참조는 변경하지 않았습니다.', '', '[전체 비교 갤러리](monster-item-gallery-2026-11-17.html)', '', '| 몬스터 | 아이템 | 점유 모양 | 이미지 |','|---|---|---|---|']
rows += [f'| {i["monsterName"]} | {i["name"]} | {i["shape"]} | [PNG](../../{i["output"]}) |' for i in items]
rows += ['', '이미지는 내장 이미지 생성 도구로 제작했습니다. PNG 픽셀을 후처리하거나 칸 경계로 잘라내지 않았습니다. Unity 스프라이트 영역은 알파 32 이상인 본체 전체를 보존하며, 빈 칸에서 해당 픽셀은 0개입니다. 생성기의 알파 1 수준 잔여 픽셀은 원본에 남아 있을 수 있습니다.', '', '스킬 3종은 숲의 손길(나뭇잎과 손), 우상의 저주(까마귀와 네 가지 상태 색), 권능 강탈(마족의 손과 흡수되는 힘)로 표현했습니다.']
(DOC/'monster-items-2026-11-17.md').write_text('\n'.join(rows)+'\n',encoding='utf-8')
print(json.dumps(dict(items=len(items),monsters=len(cards),visible_empty_cell_pixels=sum(e['visible_pixels'] for q in qa for e in q['empty_cells']),gallery='Docs/Art/monster-item-gallery-2026-11-17.html')))

