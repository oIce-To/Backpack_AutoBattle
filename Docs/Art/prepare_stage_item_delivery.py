"""Package the 65 generated PNGs without changing image pixels. Run from repo root."""
import json, re, uuid, html
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
DOC = ROOT / 'Docs/Art'
MANIFEST = DOC / 'stage-items-2026-11-16.manifest.json'
items = json.loads(MANIFEST.read_text(encoding='utf-8'))
assert len(items) == len({i['id'] for i in items}) == 65
# x,y use top-left image coordinates; Unity uses bottom-left coordinates.
template = (ROOT / 'Assets/_Project/Art/Items/KnightSword/knight-sword-2026-11-06-v1.png.meta').read_text(encoding='utf-8')
rarities = {'0':'일반','1':'고급','2':'희귀','3':'영웅','4':'전설'}
qa, cards, rows = [], [], []
for i in items:
    path = ROOT / i['output']
    im = Image.open(path)
    assert im.mode == 'RGBA', i['id']
    iw, ih = im.size
    a = im.getchannel('A')
    rx, ry, rw, rh = i.get('spriteRectTopLeft', [0, 0, iw, ih])
    assert rx >= 0 and ry >= 0 and rx+rw <= iw and ry+rh <= ih
    bbox = a.point(lambda v: 255 if v >= 128 else 0).getbbox()
    assert rx <= bbox[0] and ry <= bbox[1] and rx+rw >= bbox[2] and ry+rh >= bbox[3], (i['id'],bbox)
    assert abs((rw/rh)/(i['w']/i['h'])-1) < 0.03, i['id']
    live = (ROOT / i['path']).read_text(encoding='utf-8')
    block = re.search(r'  _occupiedCells:\n((?:  - .*\n?)+)',live).group(1)
    cells = [[int(x),int(y)] for x,y in re.findall(r'x: (-?\d+), y: (-?\d+)',block)]
    assert cells == i['cells'], i['id']
    empty = []
    for y in range(i['h']):
        for x in range(i['w']):
            if [x,y] in cells: continue
            cell = a.crop((round(rx+x*rw/i['w']),round(ry+y*rh/i['h']),round(rx+(x+1)*rw/i['w']),round(ry+(y+1)*rh/i['h'])))
            hist = cell.histogram()
            empty.append({'cell':[x,y],'max_alpha':cell.getextrema()[1],'solid_pixels':sum(hist[128:])})
    qa.append({'id':i['id'],'size':[iw,ih],'sprite_rect_top_left':[rx,ry,rw,rh],'alpha':a.getextrema(),'solid_bbox':bbox,'empty':empty})
    i['spriteRectTopLeft'] = [rx,ry,rw,rh]
    i['canvasSize'] = [iw,ih]
    i.setdefault('status', 'review-draft')
    # Preserve existing GUID if Unity has already imported this new asset.
    meta_path = Path(str(path)+'.meta')
    old = meta_path.read_text(encoding='utf-8') if meta_path.exists() else ''
    match = re.search(r'^guid: (\w+)',old,re.M)
    guid = match.group(1) if match else uuid.uuid4().hex
    meta = re.sub(r'^guid: \w+', 'guid: '+guid, template, flags=re.M)
    meta = re.sub(r'  internalIDToNameTable:.*?(?=  externalObjects:)', '  internalIDToNameTable: []\n',meta,flags=re.S)
    for key,value in [('maxTextureSize','4096'),('filterMode','0'),('textureCompression','0'),('spriteMeshType','0'),('spriteGenerateFallbackPhysicsShape','0')]:
        meta = re.sub(r'(^\s*'+key+r':) .*',r'\g<1> '+value,meta,flags=re.M)
    custom = [rx, ry, rw, rh] != [0, 0, iw, ih]
    meta = meta.replace('  spriteMode: 1','  spriteMode: '+('2' if custom else '1'))
    sprite_name = i['id']
    sprite_id = uuid.uuid5(uuid.NAMESPACE_URL,i['output']).hex
    if custom:
        sprites = f'''    sprites:
    - serializedVersion: 2
      name: {sprite_name}
      rect:
        serializedVersion: 2
        x: {rx}
        y: {ih-ry-rh}
        width: {rw}
        height: {rh}
      alignment: 0
      pivot: {{x: 0.5, y: 0.5}}
      border: {{x: 0, y: 0, z: 0, w: 0}}
      customData:
      outline: []
      physicsShape: []
      tessellationDetail: -1
      bones: []
      spriteID: {sprite_id}
      internalID: 21300000
      vertices: []
      indices:
      edges: []
      weights: []
'''
        names = f'    nameFileIdTable:\n      {sprite_name}: 21300000\n'
    else:
        sprites = '    sprites: []\n'
        names = '    nameFileIdTable: {}\n'
    sheet = '  spriteSheet:\n    serializedVersion: 2\n'+sprites+f'''    outline: []
    customData:
    physicsShape: []
    bones: []
    spriteID: {sprite_id}
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
'''+names
    meta = re.sub(r'  spriteSheet:.*?(?=  mipmapLimitGroupName:)',sheet,meta,flags=re.S)
    meta_path.write_text(meta,encoding='utf-8')
    i['spriteGuid'] = guid
    link = '../../'+i['output']
    name = html.escape(i['name'])
    rarity = rarities.get(i['rarity'],i['rarity'])
    state = '확정' if i['status']=='approved' else '수정본 · 검토 중'
    cells_html = ''.join(f'<span class="cell {"occupied" if [x,y] in cells else "empty"}" style="left:{x*48}px;top:{y*48}px"></span>' for y in range(i['h']) for x in range(i['w']))
    sx,sy = i['w']*48/rw,i['h']*48/rh
    image_style = f'width:{iw*sx}px;height:{ih*sy}px;left:{-rx*sx}px;top:{-ry*sy}px'
    cards.append(f'''<article class="card" data-search="{name} {i['id']} {rarity}" data-rarity="{rarity}" data-status="{i['status']}">
<a class="preview" href="{link}" target="_blank" aria-label="{name} 원본 열기"><div class="viewport" style="width:{i['w']*48}px;height:{i['h']*48}px"><div class="cells">{cells_html}</div><img src="{link}" alt="{name}" style="{image_style}" loading="lazy"></div></a>
<div class="info"><span class="tag">{rarity} · {len(cells)}칸 · {state}</span><h2>{name}</h2><div class="details"><code>{i['shape']}</code><a href="{link}" download>PNG</a></div><p>{html.escape(i['effect'])}</p></div></article>''')
    rows.append(f"| {i['name']} | {rarity} | {len(cells)} | `{i['shape']}` | [PNG]({link}) | {state} |")

MANIFEST.write_text(json.dumps(items,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
(DOC/'stage-items-2026-11-16.qa.json').write_text(json.dumps(qa,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
for folder in [ROOT/'Assets/_Project/Art/Items/Stage'] + [p for p in (ROOT/'Assets/_Project/Art/Items/Stage').iterdir() if p.is_dir()]:
    m = Path(str(folder)+'.meta')
    if not m.exists(): m.write_text(f'fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',encoding='utf-8')

head = '''<!doctype html><html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>인스테이지 아이템 65종</title><style>
*{box-sizing:border-box}body{margin:0;background:#15111f;color:#f2eafa;font:15px/1.6 system-ui,sans-serif}header,main,footer{max-width:1500px;margin:auto;padding:24px}header{padding-bottom:12px}h1{font-size:28px;margin:0}p{color:#bcb1ce;margin:8px 0}a{color:#c7b4ff}nav{display:flex;gap:12px;flex-wrap:wrap;align-items:center;margin:22px 0 12px}input,select,button{font:inherit;color:inherit;background:#2d253c;border:1px solid #645376;border-radius:8px;padding:8px 12px}input[type=search]{width:260px}input[type=checkbox]{accent-color:#b7a0e9}.grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(210px,1fr));gap:16px}.card{border:1px solid #463853;background:#231b30;border-radius:12px;overflow:hidden}.preview{height:280px;display:flex;align-items:center;justify-content:center;background:#322942}.viewport{position:relative;overflow:hidden;flex:none}.viewport img{position:absolute;max-width:none;image-rendering:pixelated}.cells{position:absolute;inset:0;z-index:2;pointer-events:none}.cell{position:absolute;width:48px;height:48px;border:1px solid #ac97cd70}.cell.empty{background:#ed516a36;border:1px dashed #ed6987aa}.cell.occupied{background:#a993d911}body.hide-grid .cells{display:none}.info{padding:14px}h2{font-size:17px;margin:6px 0}.tag{color:#c1aecf;font-size:12px}.details{display:flex;justify-content:space-between;align-items:center}code{font-family:monospace;color:#b7a8c8;font-size:12px}.info p{font-size:12px;margin-top:12px}.card[hidden]{display:none}body[data-bg=light] .preview{background:#e4dfeb}body[data-bg=checker] .preview{background-color:#393140;background-image:conic-gradient(#554b60 25%,transparent 0 50%,#554b60 0 75%,transparent 0);background-size:24px 24px}footer{font-size:13px;color:#bcb1ce}
</style></head><body class="hide-grid"><header><h1>인스테이지 아이템 · 65종</h1><p>65종 모두 확정되었습니다. 한 칸 = 48px로 크기를 비교할 수 있습니다.</p><nav><input id="search" type="search" placeholder="아이템 이름 검색" aria-label="아이템 검색"><select id="rarity" aria-label="등급"><option value="">모든 등급</option><option>일반</option><option>고급</option><option>희귀</option><option>영웅</option><option>전설</option></select><select id="status" aria-label="확정 상태"><option value="">모든 상태</option><option value="approved">확정 65종</option></select><label><input type="checkbox" id="grid"> 점유 칸 표시</label><select id="background" aria-label="미리보기 배경"><option value="dark">어두운 배경</option><option value="light">밝은 배경</option><option value="checker">체크 배경</option></select><span id="count">65 / 65</span></nav><p>칸 표시: 실선은 점유 칸, 붉은 점선은 빈 칸입니다. 이미지를 누르면 원본 PNG가 열립니다.</p></header><main class="grid">'''
tail = '''</main><footer>내장 이미지 생성 도구 · 2026-11-16 제작 시작 / 2026-11-17 보정 · 투명 PNG · 게임 데이터의 기본 회전 기준<br><a href="stage-items-2026-11-16.md">제작 목록과 설명</a> · <a href="stage-items-2026-11-16.manifest.json">아이템별 프롬프트와 스프라이트 영역</a></footer><script>
const cards=[...document.querySelectorAll('.card')],search=document.querySelector('#search'),rarity=document.querySelector('#rarity');
function filter(){let n=0;for(const c of cards){const match=c.dataset.search.toLowerCase().includes(search.value.trim().toLowerCase())&&(!rarity.value||c.dataset.rarity===rarity.value)&&(!document.querySelector('#status').value||c.dataset.status===document.querySelector('#status').value);c.hidden=!match;if(match)n++;}document.querySelector('#count').textContent=n+' / 65';}
search.addEventListener('input',filter);rarity.addEventListener('change',filter);document.querySelector('#status').addEventListener('change',filter);
document.querySelector('#grid').addEventListener('change',e=>document.body.classList.toggle('hide-grid',!e.target.checked));
document.querySelector('#background').addEventListener('change',e=>document.body.dataset.bg=e.target.value);
</script></body></html>'''
(DOC/'stage-item-gallery-2026-11-16.html').write_text(head+'\n'.join(cards)+tail,encoding='utf-8')
notes = '''# 인스테이지 아이템 65종 이미지

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
'''
(DOC/'stage-items-2026-11-16.md').write_text(notes+'\n'.join(rows)+'\n',encoding='utf-8')
violations = [(q['id'],e) for q in qa for e in q['empty'] if e['solid_pixels']]
print(json.dumps({'items':len(items),'empty_cell_violations':violations,'rgba':True,'gallery_cards':len(cards)},ensure_ascii=False))
