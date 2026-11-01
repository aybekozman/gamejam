import re,json
from pathlib import Path
before=Path('Tools/Revision5/user-layout-before.unity').read_text(encoding='utf-8-sig');after=Path('Assets/Scenes/UnderwaterWorld.unity').read_text(encoding='utf-8-sig')
def blocks(s):return {m.group(2):(int(m.group(1)),m.group(3)) for m in re.finditer(r'--- !u!(\d+) &(\d+)\n(.*?)(?=\n--- !u!|\Z)',s,re.S)}
b,a=blocks(before),blocks(after)
terrain=[k for k,(t,body) in b.items() if t==1 and 'Continuous shore' in body][0]
colliderCount=0;transformCount=0
for k,(typ,body) in b.items():
 if typ==4:
  assert k in a
  for field in ['m_LocalPosition','m_LocalScale','m_LocalRotation','m_Father']:
   pattern=r'^  '+field+r': (.+)$';assert re.search(pattern,body,re.M)[1]==re.search(pattern,a[k][1],re.M)[1],(k,field)
  transformCount+=1
 if typ in [60,61,58,70] and ('m_GameObject: {fileID: '+terrain+'}') not in body:assert body==a[k][1],k;colliderCount+=1
meshRenderers=[k for k,(t,body) in a.items() if t==23]
assert not meshRenderers,meshRenderers
Path('Tools/Revision5/preservation-results.json').write_text(json.dumps({'existingTransformsUnchanged':transformCount,'otherColliderBlocksIdentical':colliderCount,'meshRenderersInScene':0,'result':'PASS only generated shore collision path and its rendering replaced'},indent=2))
print(f'PASS {transformCount} existing transforms and {colliderCount} other collider blocks unchanged. Zero MeshRenderers in scene.')
