import re,json,hashlib
from pathlib import Path
before=Path('Tools/Revision13/before.unity').read_text(encoding='utf-8-sig');after=Path('Assets/Scenes/UnderwaterWorld.unity').read_text(encoding='utf-8-sig')
def blocks(s):return {m[2]:(m[1],m[3]) for m in re.finditer(r'--- !u!(\d+) &(\d+)\n(.*?)(?=\n--- !u!|\Z)',s,re.S)}
b,a=blocks(before),blocks(after);changes=[];count=0
for key,(kind,body) in b.items():
 if kind in ['60','61','58','70']:assert a[key][1]==body,('collider',key);count+=1
 if kind=='4':
  for field in ['m_LocalPosition','m_LocalScale','m_LocalRotation','m_Father']:
   old=re.search('^  '+field+': (.+)$',body,re.M)[1];new=re.search('^  '+field+': (.+)$',a[key][1],re.M)[1]
   if old!=new:changes.append((key,field,old,new))
assert len(changes)==1 and changes[0][1]=='m_LocalPosition',changes
for item in json.loads(Path('Tools/Revision13/scope-before.json').read_text(encoding='utf-8-sig')):assert hashlib.sha256(Path(item['Path']).read_bytes()).hexdigest().upper()==item['Hash'],item['Path']
Path('Tools/Revision13/preservation.txt').write_text(f'PASS {count} original colliders unchanged; only authorized initial mayor transform moved. Ship scene, project settings and input asset hashes unchanged.\n'+str(changes),encoding='utf-8');print('PASS preservation')
