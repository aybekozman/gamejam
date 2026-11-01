import re,json,subprocess
from pathlib import Path
before=Path('Tools/Revision6/user-world-before.unity').read_text(encoding='utf-8-sig');after=Path('Assets/Scenes/UnderwaterWorld.unity').read_text(encoding='utf-8-sig')
def blocks(s):return {m.group(2):(int(m.group(1)),m.group(3)) for m in re.finditer(r'--- !u!(\d+) &(\d+)\n(.*?)(?=\n--- !u!|\Z)',s,re.S)}
b,a=blocks(before),blocks(after);names={}
for key,(typ,body) in b.items():
 if typ==1:
  n=re.search(r'^  m_Name: (.+)$',body,re.M)[1];names[key]=json.loads(n) if n.startswith('"') else n
changed=[];count=0
for key,(typ,body) in b.items():
 if typ in [60,61,58,70]:assert a[key][1]==body,('Collider changed',key);count+=1
 if typ==4:
  name=names[re.search(r'm_GameObject: \{fileID: (\d+)\}',body)[1]]
  for field in ['m_LocalPosition','m_LocalScale','m_LocalRotation','m_Father']:
   old=re.search('^  '+field+': (.+)$',body,re.M)[1];new=re.search('^  '+field+': (.+)$',a[key][1],re.M)[1]
   if old!=new:assert 'shore_match_10' in name and field=='m_LocalScale',(name,field);changed.append((name,field))
paths=['Assets/Scenes/ShipScene.unity','Assets/Scripts/Ship','Assets/InputSystem_Actions.inputactions','Packages/manifest.json','ProjectSettings/ProjectSettings.asset']
result=subprocess.run(['git','diff','--exit-code','--']+paths,capture_output=True,text=True);assert result.returncode==0,result.stdout
Path('Tools/Revision6/preservation-results.json').write_text(json.dumps({'result':'PASS existing world preserved, except the single generated shore sprite scale corrected to match neighbors','existingColliderBlocksUnchanged':count,'authorizedShoreScaleCorrection':changed,'shipAndInputFilesUnchanged':paths},ensure_ascii=False,indent=2),encoding='utf8')
print('PASS existing collider definitions, homes/world transforms and ship/input scope preserved.')
