import json,re,yaml
from pathlib import Path
root=Path(__file__).resolve().parents[2]
def read(p):
 s=p.read_text(encoding='utf-8-sig');s='\n'.join(l for l in s.splitlines() if not l.startswith(('%','---')))
 return yaml.safe_load(s)['MonoBehaviour']
guid={}
for p in (root/'Assets/_Project').rglob('*.meta'):
 m=re.search(r'^guid: (\w+)',p.read_text(encoding='utf-8-sig'),re.M)
 if m:guid[m[1]]=Path(str(p)[:-5])
roster=json.loads((root/'Docs/Art/monster-roster-2026-11-16.json').read_text(encoding='utf-8'))['monsters']
result=[]
for enc in sorted((root/'Assets/_Project/Data/Enemies/Stages').glob('Encounter_*.asset')):
 d=read(enc);ep=guid[d['_enemy']['guid']];enemy=read(ep);eid=enemy['_enemyId']
 art=guid[enemy['_illustration']['guid']].relative_to(root).as_posix()
 known=next((r for r in roster if r['slug'].replace('-','_')==eid),{})
 for place in d['_items']:
  p=guid[place['_item']['guid']];v=read(p);cells=[[c['x'],c['y']] for c in v['_occupiedCells']]
  w=max(c[0] for c in cells)+1;h=max(c[1] for c in cells)+1
  result.append(dict(id=v['_itemId'],name=v['_displayName'],monsterId=eid,monsterName=enemy['_displayName'],monsterPath=ep.relative_to(root).as_posix(),illustration=art,monsterDesign=known.get('design',''),stage=known.get('stage',1 if eid=='goblin' else 'mine'),round=known.get('round','기본'),trait=enemy.get('_trait',0),encounter=enc.relative_to(root).as_posix(),placement=place,path=p.relative_to(root).as_posix(),cells=cells,w=w,h=h,shape='/'.join(''.join('#' if [x,y] in cells else '.' for x in range(w)) for y in range(h)),effect=v.get('_effectDescription',''),activation=v.get('_activation'),weaponStats=v.get('_weaponStats'),data=v,status='review-draft'))
assert len(result)==len({r['id'] for r in result})
(root/'Docs/Art/monster-items-2026-11-17.manifest.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
for i in result:print(json.dumps({k:i[k] for k in ['id','name','monsterName','shape','effect','activation','weaponStats']},ensure_ascii=False))
print('TOTAL',len(result),'MONSTERS',len({i['monsterId'] for i in result}))
