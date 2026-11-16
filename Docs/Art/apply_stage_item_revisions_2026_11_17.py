"""Record user approvals and revised art; copy PNG bytes without pixel edits."""
import json, shutil, hashlib
from pathlib import Path
from PIL import Image

root=Path(__file__).resolve().parents[2]
doc=root/'Docs/Art'
manifest=doc/'stage-items-2026-11-16.manifest.json'
items=json.loads((doc/'stage-items-2026-11-17.before-user-revisions.json').read_text(encoding='utf-8'))
edits=json.loads((doc/'stage-items-2026-11-17.revision-prompts.json').read_text(encoding='utf-8'))
by_id={e['id']:e for e in edits}
rects={'battle_axe':[36,108,952,1428],'dragonslayer':[75,0,700,1750],'war_hero_halberd':[0,0,880,1760],'excalibur':[178,0,480,1881]}
approved=[]
for i in items:
    if i['id'] in by_id:
        e=by_id[i['id']]
        rec=json.loads((doc/'stage-item-results'/f"{i['id']}-user-revision-2026-11-17.json").read_text(encoding='utf-8'))
        assert rec['output']==e['output']
        assert hashlib.sha256((root/e['output']).read_bytes()).digest()==hashlib.sha256(Path(rec['source']).read_bytes()).digest()
        i['revisionBaseline']=i['output']
        i['revisionBaselineRect']=i['spriteRectTopLeft']
        i['revisionBaselineCanvas']=i['canvasSize']
        i['output']=e['output']
        i['prompt']=e['prompt']
        i['references']=e['references']
        im=Image.open(root/i['output'])
        i['spriteRectTopLeft']=rects.get(i['id'],[0,0,*im.size])
        i['status']='review-draft'
        i['revisionDate']='2026-11-17'
    else:
        src=root/i['output']
        dest=src.parent/(i['id'].replace('_','-')+'-approved.png')
        if dest.exists():
            assert dest.read_bytes()==src.read_bytes(),dest
        else:
            shutil.copyfile(src,dest)
        i['approvedFrom']=i['output']
        i['output']=dest.relative_to(root).as_posix()
        i['status']='approved'
        i['approvedDate']='2026-11-17'
        approved.append({'id':i['id'],'name':i['name'],'source':i['approvedFrom'],'output':i['output'],'sha256':hashlib.sha256(dest.read_bytes()).hexdigest()})
assert len(approved)==52 and len(by_id)==13
manifest.write_text(json.dumps(items,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
(doc/'stage-items-2026-11-17.approvals.json').write_text(json.dumps(approved,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('52 approved copies; 13 revisions selected; source hashes verified.')
