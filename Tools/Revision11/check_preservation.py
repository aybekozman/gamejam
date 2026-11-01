import re,json,subprocess
from pathlib import Path
before=Path('Tools/Revision11/before.unity').read_text(encoding='utf-8-sig');after=Path('Assets/Scenes/UnderwaterWorld.unity').read_text(encoding='utf-8-sig')
def blocks(s):return {m.group(2):(int(m.group(1)),m.group(3)) for m in re.finditer(r'--- !u!(\d+) &(\d+)\n(.*?)(?=\n--- !u!|\Z)',s,re.S)}
b,a=blocks(before),blocks(after);names={}
for key,(typ,body) in b.items():
 if typ==1:
  n=re.search(r'^  m_Name: (.+)$',body,re.M)[1];names[key]=n
changed=[];count=0
for key,(typ,body) in b.items():
 if typ in [60,61,58,70]:assert a[key][1]==body,('Collider changed',key);count+=1
 if typ==4:
  name=names[re.search(r'm_GameObject: \{fileID: (\d+)\}',body)[1]]
  for field in ['m_LocalPosition','m_LocalScale','m_LocalRotation','m_Father']:
   old=re.search('^  '+field+': (.+)$',body,re.M)[1];new=re.search('^  '+field+': (.+)$',a[key][1],re.M)[1]
   assert old==new,(name,field)
paths=['Assets/Scenes/ShipScene.unity','Assets/Scripts/Ship','Assets/InputSystem_Actions.inputactions','Packages/manifest.json','ProjectSettings/ProjectSettings.asset']
import hashlib
baseline=json.loads(Path('Tools/Revision10/scope-before.json').read_text(encoding='utf-8-sig'))
for item in baseline:assert hashlib.sha256(Path(item['Path']).read_bytes()).hexdigest().upper()==item['Hash'],item['Path']
assert subprocess.run(['git','diff','--exit-code','--','Assets/Scripts/Ship','Assets/InputSystem_Actions.inputactions','Packages/manifest.json'],capture_output=True).returncode==0
Path('Tools/Revision11/preservation-results.json').write_text(json.dumps({'result':'PASS all original transforms, collider definitions, ship scenes and settings preserved','existingColliderBlocksUnchanged':count,'shipAndInputFilesUnchanged':paths},ensure_ascii=False,indent=2),encoding='utf8')
print('PASS existing collider definitions, homes/world transforms and ship/input scope preserved.')
