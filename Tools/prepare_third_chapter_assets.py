from pathlib import Path
from PIL import Image
import numpy as np,json
src=Path('Assets/Sprites/Underwater');out=Path('Assets/Underwater/Resources/Abyss')
def clean(im,threshold=120):
 a=np.array(im.convert('RGBA'));a[:,:,3]=np.where(a[:,:,3]>=threshold,255,0).astype('uint8');im=Image.fromarray(a);return im.crop(im.getbbox())
sheet=Image.open(src/'Denizaltı köyünün köpekbalığı lideri.png')
# Independently bounded poses, not a uniform grid/pivot assumption.
rects=[(18,55,765,470),(787,54,1525,470),(20,510,765,950),(786,509,1526,950)]
poses=[clean(sheet.crop(r),180) for r in rects]
# Align nose and eye rows with identical visible width; shared canvas prevents pulsing.
parts=[]
for im in poses:
 im=im.resize((220,round(im.height*220/im.width)),Image.Resampling.NEAREST)
 a=np.array(im);ys,xs=np.where((a[:,:,0]>150)&(a[:,:,1]>65)&(a[:,:,1]<215)&(a[:,:,2]<95)&(a[:,:,3]>0)&(np.indices(a.shape[:2])[1]>im.width*.72));eye=float(np.median(ys)) if len(ys) else im.height*.4
 parts.append((im,eye))
above=max(y for _,y in parts);below=max(im.height-y for im,y in parts);size=(232,int(np.ceil(above+below))+12)
for i,(im,y) in enumerate(parts):
 canvas=Image.new('RGBA',size);canvas.paste(im,(6,round(6+above-y)));canvas.save(out/f'leader_{i}.png')
sign=clean(Image.open(src/'Su altı köpekbalığı uyarı levhası.png'),120);sign=sign.resize((184,round(sign.height*184/sign.width)),Image.Resampling.NEAREST);sign.save(out/'shark_warning.png')
sheet=Image.open(src/'Su altı topu piksel efektleri.png')
for row,name in [(0,'splash'),(1,'wake'),(2,'sediment')]:
 for i in range(4):
  if row==2 and i==0:continue
  im=clean(sheet.crop((i*384,row*341,(i+1)*384,min(1024,(row+1)*341))),120);im=im.resize((max(1,im.width//3),max(1,im.height//3)),Image.Resampling.NEAREST);im.save(out/f'cannon_{name}_{i}.png')
ball=clean(sheet.crop((0,682,384,1024)),160);ball=ball.resize((48,round(ball.height*48/ball.width)),Image.Resampling.NEAREST);ball.save(out/'cannon_ball.png')
Path('Tools/Revision10/sprite-report.json').write_text(json.dumps({'leaderRects':rects,'canvas':size,'visibleWidth':220,'eyeAligned':True,'signSize':sign.size,'alphaThresholdLeader':180},indent=2))
