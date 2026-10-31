from pathlib import Path
from PIL import Image
import numpy as np,json
cfg=json.loads(Path('Tools/Revision5/config.json').read_text(encoding='utf-8-sig'));src=np.asarray(Image.open(cfg['source']).convert('RGBA'))
def colors(a):
 p=a[a[:,:,3]>0,:3].astype(np.uint32);return set(np.unique((p[:,0]<<16)|(p[:,1]<<8)|p[:,2]).tolist())
original=colors(src)
for p in Path('Assets/Underwater/Resources/Abyss').glob('shore_match_*.png'):
 image=np.asarray(Image.open(p).convert('RGBA'));assert colors(image)<=original,p.name
print('PASS all opaque shore pixel colors are copied from the original floor palette; no interpolated colors.')
