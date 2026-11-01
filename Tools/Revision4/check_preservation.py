import re,json
from pathlib import Path
before=Path('Tools/Revision4/user-layout-before.unity').read_text(encoding='utf-8-sig');after=Path('Assets/Scenes/UnderwaterWorld.unity').read_text(encoding='utf-8-sig')
def blocks(s):return {m.group(2):(int(m.group(1)),m.group(3)) for m in re.finditer(r'--- !u!(\d+) &(\d+)\n(.*?)(?=\n--- !u!|\Z)',s,re.S)}
b,a=blocks(before),blocks(after);colliders=0;oldNames={}
for k,(typ,body) in b.items():
 if typ==1:
  name=re.search(r'^  m_Name: (.+)$',body,re.M)
  if name:oldNames[k]=json.loads(name[1]) if name[1].startswith(chr(34)) else name[1]
changes=[]
for k,(typ,body) in b.items():
 if typ in [60,61,58,70]:assert a[k][1]==body,('Collider changed',k);colliders+=1
 if typ!=4:continue
 go=re.search(r'm_GameObject: \{fileID: (\d+)\}',body)[1];name=oldNames[go];background=name.startswith('Far rock silhouette') or name.startswith('Middle reef')
 for field in ['m_LocalPosition','m_LocalRotation','m_LocalScale','m_Father']:
  old=re.search(r'^  '+field+r': (.+)$',body,re.M);new=re.search(r'^  '+field+r': (.+)$',a[k][1],re.M)
  if old and old[1]!=new[1]:
   assert background or (name=='West boundary' and field=='m_LocalPosition'),(name,field)
   changes.append({'name':name,'field':field})
Path('Tools/Revision4/preservation-results.json').write_text(json.dumps({'result':'PASS all existing collider blocks unchanged; only requested background grounding/reparenting and west boundary relocation changed authored transforms','colliders':colliders,'requestedTransformChanges':len(changes)},indent=2))
print(f'PASS {colliders} existing collider YAML blocks identical. Homes, foreground, player and camera placement retained.')

