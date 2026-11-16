import json,hashlib
from pathlib import Path
root=Path.cwd();doc=root/'Docs/Art'
p=doc/'monster-items-2026-11-17.delivery.json'
items=json.loads(p.read_text(encoding='utf-8'))
revision=json.loads((doc/'monster-item-results/enemy_demon_weapon_master_axe-v3.json').read_text(encoding='utf-8'))
approved=[]
for i in items:
    if i['id']==revision['id']:
        for k in ['output','previousOutput','prompt','references','status']:i[k]=revision[k]
        i['revisionNote']='외곽선을 굵은 짙은 보라색 도트로 통일하고 금속·가죽·금색 명암을 기존 아이템에 맞춤. 4칸 모양 유지.'
    else:
        digest=hashlib.sha256((root/i['output']).read_bytes()).hexdigest()
        assert digest==i['sha256'],i['id']
        i['status']='approved'
        approved.append({k:i[k] for k in ['id','name','output','sha256']})
assert len(approved)==30
p.write_text(json.dumps(items,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
(doc/'monster-items-2026-11-17.approved.json').write_text(json.dumps(dict(approved=approved,pending=['enemy_demon_weapon_master_axe'],reason='사용자가 마족의 손도끼를 제외한 30종을 확정함'),ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
p=doc/'prepare_monster_item_delivery.py'
s=p.read_text(encoding='utf-8')
s=s.replace('    for i in group:\n        sub.append(', '    for i in group:\n        state="확정" if i["status"]=="approved" else "수정본 · 검토 중"\n        sub.append(')
s=s.replace('<p class="tag">{len(i["cells"])}칸', '<p class="tag">{state} · {len(i["cells"])}칸')
s=s.replace("head='''<!doctype", "approved_count=sum(i['status']=='approved' for i in items)\nstate_summary=f'{approved_count}종 확정 · {len(items)-approved_count}종 검토 중'\nhead='''<!doctype")
s=s.replace("(DOC/'monster-item-gallery-2026-11-17.html').write_text(head+", "head=head.replace('현재 결과는 검토용이며 인게임 참조는 아직 교체하지 않았습니다.',state_summary+'입니다. 인게임 참조는 아직 교체하지 않았습니다.')\n(DOC/'monster-item-gallery-2026-11-17.html').write_text(head+")
s=s.replace("'상태: 검토용. 인게임 아이템 참조는 변경하지 않았습니다.'", "state_summary+'. 인게임 아이템 참조는 변경하지 않았습니다.'")
p.write_text(s,encoding='utf-8')
print('Recorded30approvals;updatedaxeV3')

