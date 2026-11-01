from pathlib import Path
from PIL import Image
import numpy as np
out=Path('Assets/Campaign/Resources/Campaign');im=Image.open('Assets/Sprites/Piksel sanatlı denizcilik menü arayüzü-1.png').convert('RGBA')
rects={'title':(48,68,1490,320),'button':(36,373,513,544),'button_hover':(528,373,1008,544),'button_pressed':(1021,373,1500,544),'settings':(35,576,978,940),'close':(1056,603,1233,777),'sound':(1254,604,1436,780)}
for name,rect in rects.items():
 p=im.crop(rect);a=np.array(p);yy,xx=np.indices(a.shape[:2]);corner=np.minimum(xx,a.shape[1]-1-xx)+np.minimum(yy,a.shape[0]-1-yy);a[:,:,3]=np.where((corner<15)|(a[:,:,3]<190),0,255).astype('uint8');Image.fromarray(a).save(out/(name+'.png'))
track=im.crop((1004,810,1498,910));fill=im.crop((1080,843,1230,866));knob=im.crop((1262,822,1328,891));a=np.array(track);patch=np.array(im.crop((1350,832,1380,883)));a[22:73,255:340]=np.array(Image.fromarray(patch).resize((85,51),Image.Resampling.NEAREST));Image.fromarray(a).save(out/'slider.png');fill.save(out/'slider_fill.png');knob.save(out/'slider_knob.png')
im=Image.open('Assets/Sprites/Beyaz teslim bayrağı sprite sayfası-2.png').convert('RGBA')
for i in range(4):
 p=im.crop((i*512,100,(i+1)*512,680));a=np.array(p);a[:,:,3]=np.where(a[:,:,3]>190,255,0).astype('uint8');p=Image.fromarray(a).resize((128,145),Image.Resampling.NEAREST)
 a=np.array(p);ys,xs=np.where(a[120:,:,3]>0);pole=int(round(float(np.median(xs))));aligned=Image.new('RGBA',(128,145));aligned.paste(p,(20-pole,0));aligned.save(out/f'flag_{i}.png')

for name in ["slider","slider_fill","slider_knob"]:
 p=out/(name+".png");a=np.array(Image.open(p).convert("RGBA"));a[:,:,3]=np.where(a[:,:,3]>190,255,0).astype("uint8");Image.fromarray(a).save(p)

p=out/"slider_knob.png";a=np.array(Image.open(p));yy,xx=np.indices(a.shape[:2]);a[:,:,3]=np.where(((xx-36)/29)**2+((yy-34)/32)**2<=1,a[:,:,3],0).astype("uint8");Image.fromarray(a).save(p)
