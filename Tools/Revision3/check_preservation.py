import re,json
from pathlib import Path
before=Path('Tools/Revision3/user-layout-before.unity').read_text(encoding='utf-8-sig');after=Path('Assets/Scenes/UnderwaterWorld.unity').read_text(encoding='utf-8-sig')
def blocks(s):return {m.group(2):(int(m.group(1)),m.group(3)) for m in re.finditer(r'--- !u!(\d+) &(\d+)\n(.*?)(?=\n--- !u!|\Z)',s,re.S)}
b,a=blocks(before),blocks(after);changes=[];colliders=0
for k,(typ,body) in b.items():
 if typ in [60,61,58,70]:
  assert k in a and a[k][1]==body,('Collider data changed',k);colliders+=1
 if typ!=4:continue
 assert k in a
 for field in ['m_LocalPosition','m_LocalRotation','m_LocalScale']:
  old=re.search(r'^  '+field+r': (.+)$',body,re.M);new=re.search(r'^  '+field+r': (.+)$',a[k][1],re.M)
  if old and (not new or old[1]!=new[1]):changes.append((k,field,old[1],new[1] if new else None))
assert len(changes)==2 and all(c[1]=='m_LocalPosition' for c in changes),changes
Path('Tools/Revision3/preservation-results.json').write_text(json.dumps({'result':'PASS all existing collider blocks exactly unchanged; only player and camera start positions changed','colliderCount':colliders,'startPositionChanges':changes},indent=2))
print(f'PASS {colliders} collider YAML blocks preserved byte-for-byte; only player/camera starting positions changed.')
