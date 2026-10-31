from pathlib import Path
from PIL import Image
import numpy as np
ROOT=Path('Assets/Underwater/Resources/Abyss')
# Reject poses with an incompatible body scale. Keep stable poses; do not fill an animation with poor frames.
def stabilize(prefix,indices,mode='nose'):
 original=[Image.open(ROOT/f'{prefix}_{i}.png').convert('RGBA') for i in range(4)]
 pieces=[];reference=None
 for i in indices:
  im=original[i];a=np.array(im);box=im.getchannel('A').point(lambda v:255 if v>35 else 0).getbbox();im=im.crop(box);a=np.array(im)
  if mode=='shell':
   r,g,b=a[:,:,0].astype(float),a[:,:,1].astype(float),a[:,:,2].astype(float)
   mask=(r>g*1.15)&(g>b*1.1)&(r>45)&(a[:,:,3]>150);ys,xs=np.where(mask)
   if not len(xs):raise ValueError('No shell landmark')
   measure=xs.max()-xs.min()+1;anchor=((xs.min()+xs.max())*.5,(ys.min()+ys.max())*.5)
  else:
   measure=im.width;ys,xs=np.where((a[:,:,3]>100)&(np.indices(a.shape[:2])[1]>im.width*.92));anchor=(im.width-1,float(np.median(ys)))
  if reference is None:reference=measure
  scale=reference/measure;size=(round(im.width*scale),round(im.height*scale));im=im.resize(size,Image.Resampling.NEAREST);pieces.append((im,(anchor[0]*scale,anchor[1]*scale)))
 # Same canvas and landmark in every frame. Only append transparency; never stretch fins to match height.
 left=max(a[0] for _,a in pieces);right=max(im.width-a[0] for im,a in pieces);up=max(a[1] for _,a in pieces);down=max(im.height-a[1] for im,a in pieces)
 w=max(original[0].width,int(np.ceil(left+right))+8);h=max(original[0].height,int(np.ceil(up+down))+8)
 anchor=(left+(w-left-right)*.5,h*.5+(up-down)*.5)
 rendered=[]
 for im,a in pieces:
  out=Image.new('RGBA',(w,h));out.paste(im,(round(anchor[0]-a[0]),round(anchor[1]-a[1])));rendered.append(out)
 sequence=([0,0,1,1] if len(rendered)==2 else [0,1,2,1] if len(rendered)==3 else [0,1,2,3])
 for i,j in enumerate(sequence):rendered[j].save(ROOT/f'{prefix}_{i}.png')
 return dict(prefix=prefix,selected=indices,canvas=[w,h],bodyMeasure=reference)
report=[stabilize('animal_2',[0,2]),stabilize('whale_0',[0,2,3]),stabilize('whale_1',[0,2,3]),stabilize('animal_0',[0,1,2,3],'shell')]
Path('Tools/Revision2/animation-stabilization.json').write_text(__import__('json').dumps(report,indent=2,default=lambda x:int(x)))

