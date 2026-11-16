import json,shutil,hashlib
from pathlib import Path
root=Path(__file__).resolve().parents[2];doc=root/'Docs/Art'
items=json.loads((doc/'stage-items-2026-11-17.before-user-revisions3.json').read_text(encoding='utf-8'))
edits=json.loads((doc/'stage-items-2026-11-17.revision3-prompts.json').read_text(encoding='utf-8'));by_id={e['id']:e for e in edits}
rects={'dragonslayer':[228,90,588,1470],'war_hero_halberd':[70,0,820,1640]}
approved=[]
for i in items:
 if i['id'] in by_id:
  e=by_id[i['id']];rec=json.loads((doc/'stage-item-results'/f"{i['id']}-user-revision3-2026-11-17.json").read_text(encoding='utf-8'))
  assert rec['output']==e['output']
  assert (root/e['output']).read_bytes()==Path(rec['source']).read_bytes()
  i['revisionBaseline']=i['output'];i['revisionBaselineRect']=i['spriteRectTopLeft'];i['revisionBaselineCanvas']=i['canvasSize']
  i.update(output=e['output'],prompt=e['prompt'],references=e['references'],spriteRectTopLeft=rects[i['id']],status='review-draft',revisionDate='2026-11-17',revisionRound=3)
 else:
  if i['status']!='approved':
   src=root/i['output'];dest=src.parent/(i['id'].replace('_','-')+'-approved.png')
   if dest.exists(): assert src.read_bytes()==dest.read_bytes()
   else:shutil.copyfile(src,dest)
   i.update(approvedFrom=i['output'],output=dest.relative_to(root).as_posix(),status='approved',approvedDate='2026-11-17')
  approved.append({'id':i['id'],'name':i['name'],'source':i['approvedFrom'],'output':i['output'],'sha256':hashlib.sha256((root/i['output']).read_bytes()).hexdigest()})
assert len(approved)==63
(doc/'stage-items-2026-11-16.manifest.json').write_text(json.dumps(items,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
(doc/'stage-items-2026-11-17.approvals3.json').write_text(json.dumps(approved,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('63 approved, 2 revised; all source copies verified.')
