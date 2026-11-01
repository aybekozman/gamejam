from PIL import Image
import numpy as np,json
from pathlib import Path
root=Path('Assets/Sprites/Underwater');dst=Path('Assets/Underwater/Resources/Abyss');report={}
for filename,name in [('Derinleşen Piksel Deniz Tabanı.png','seabed_panorama'),('Renkli Su Altı Ahşap İskelesi.png','shore_pier')]:
 im=Image.open(root/filename).convert('RGBA');a=np.array(im);a[:,:,3][a[:,:,3]<20]=0
 if name=='seabed_panorama':
  halo=(a[:,:,2]>150)&(a[:,:,2]>a[:,:,1].astype(float)*1.7)&(a[:,:,2]>a[:,:,0].astype(float)*2)&(a[:,:,3]<235);a[:,:,3][halo]=0
 im=Image.fromarray(a);box=im.getchannel('A').point(lambda v:255 if v>45 else 0).getbbox();crop=im.crop(box);crop=crop.resize((round(crop.width/2),round(crop.height/2)),Image.Resampling.NEAREST);crop.save(dst/(name+'.png'))
 entry={'source':filename,'sourceSize':im.size,'visibleBounds':box,'processedSize':crop.size}
 if name=='shore_pier':
  counts=(np.array(crop)[:,:,3]>140).sum(axis=1);rows=np.where(counts>crop.width*.8)[0];entry['deckRow']=int(rows[0]);entry['deckFractionFromBottom']=1-float(rows[0])/crop.height
 report[name]=entry
floor=Image.open(root/'Derinleşen Piksel Deniz Tabanı.png').convert('RGBA').crop((0,600,2172,724));a=np.array(floor);a[:,:,3]=255;Image.fromarray(a).resize((1086,62),Image.Resampling.NEAREST).save(dst/'shore_ground_fill.png')
base=Image.open(dst/'surface.png');base.crop((round(base.width*.72),0,base.width,base.height)).save(dst/'shore_surface_end.png')
Path('Tools/Revision4/asset-report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps(report,ensure_ascii=True))

