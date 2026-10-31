import re,json
from pathlib import Path
from PIL import Image
import numpy as np
before=Path('Tools/Revision2/UnderwaterWorld.before-expansion.unity').read_text(encoding='utf-8-sig');after=Path('Assets/Scenes/UnderwaterWorld.unity').read_text(encoding='utf-8-sig')
def blocks(s):return {m.group(2):(int(m.group(1)),m.group(3)) for m in re.finditer(r'--- !u!(\d+) &(\d+)\n(.*?)(?=\n--- !u!|\Z)',s,re.S)}
b,a=blocks(before),blocks(after);changes=[]
for k,(typ,body) in b.items():
 if typ not in [4,60,61,58,70]:continue
 if k not in a:changes.append((k,'missing'));continue
 fields=['m_LocalPosition','m_LocalRotation','m_LocalScale'] if typ==4 else ['m_Enabled']
 for field in fields:
  old=re.search(r'^  '+field+r': (.+)$',body,re.M);new=re.search(r'^  '+field+r': (.+)$',a[k][1],re.M)
  if old and (not new or old[1]!=new[1]):changes.append((k,field,old[1],new[1] if new else None))
assert len(changes)==2 and all(c[1]=='m_LocalPosition' for c in changes),changes
metrics={}
for name in ['animal_2','whale_0','whale_1','animal_0']:
 bounds=[]
 for i in range(4):
  im=Image.open(f'Assets/Underwater/Resources/Abyss/{name}_{i}.png');bb=im.getchannel('A').point(lambda v:255 if v>35 else 0).getbbox();assert bb[0]>0 and bb[1]>0 and bb[2]<im.width and bb[3]<im.height
  if name=='animal_0':
   ar=np.array(im);r,g,blue=ar[:,:,0].astype(float),ar[:,:,1].astype(float),ar[:,:,2].astype(float);ys,xs=np.where((r>g*1.15)&(g>blue*1.1)&(r>45)&(ar[:,:,3]>150));width=int(xs.max()-xs.min()+1)
  else:width=bb[2]-bb[0]
  bounds.append(width)
 assert max(bounds)-min(bounds)<=2,(name,bounds)
 metrics[name]=bounds
Path('Tools/Revision2/static-results.json').write_text(json.dumps({'preservation':'All original transform/rotation/scale and collider enabled states unchanged except the two outer boundary positions','changedBoundaryPositions':changes,'bodyWidths':metrics},indent=2))
print('PASS authored layout and collider flags preserved; only two outer boundary positions changed.');print('PASS stable ray/whale body widths and turtle shell widths; no cropped frame edges.');print(metrics)
