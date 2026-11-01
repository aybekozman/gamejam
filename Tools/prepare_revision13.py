from pathlib import Path
from PIL import Image
import numpy as np
src=Path('Assets/Sprites/Underwater');out=Path('Assets/Underwater/Resources/Abyss')
im=Image.open(src/'Denizaltı tuzlu su akışı efekti-1.png').convert('RGBA');w,h=im.size
for i in range(4):
 a=np.array(im.crop((i*w//4,0,(i+1)*w//4,h)));rgb=a[:,:,:3].astype(float);valid=(a[:,:,3]>100)&((rgb.max(2)-rgb.min(2))<95);a[:,:,3]=np.where(valid,a[:,:,3],0); yy=np.arange(h)[:,None]/h;xx=np.abs(np.arange(a.shape[1])[None,:]-a.shape[1]/2)/(a.shape[1]/2);a[:,:,3]=(a[:,:,3]*np.clip((1-yy)*1.5,0,1)*np.clip((1-xx)*2,0,1)).astype('uint8');Image.fromarray(a).resize((128,256),Image.Resampling.NEAREST).save(out/f'brine_{i}.png')
im=Image.open(src/'Denizaltı piksel kalp sağlık ikonları-2.png').convert('RGBA');parts=[]
for i in range(3):
 p=im.crop((i*im.width//3,0,(i+1)*im.width//3,im.height));a=np.array(p);a[:,:,3]=np.where(a[:,:,3]>150,255,0).astype('uint8');p=Image.fromarray(a);p=p.crop(p.getbbox());p.resize((64,56),Image.Resampling.NEAREST).save(out/f'heart_{i}.png')
