from pathlib import Path
from PIL import Image,ImageDraw
import numpy as np,json
src=Path('Assets/Sprites/Underwater');out=Path('Assets/Underwater/Resources/Abyss')
def clean(im):
 a=np.array(im.convert('RGBA'));a[:,:,3][a[:,:,3]<18]=0;im=Image.fromarray(a);return im.crop(im.getchannel('A').point(lambda v:255 if v>50 else 0).getbbox())
a=clean(Image.open(out/'fish_2_0.png'));b=clean(Image.open(src/'Kapalı gözlü piksel balığı.png'));b=b.resize((a.width,round(b.height*a.width/b.width)),Image.Resampling.NEAREST)
parts=[]
for im in [a,b]:
 ar=np.array(im);ys,xs=np.where((ar[:,:,3]>128)&(np.indices(ar.shape[:2])[1]>im.width*.92));parts.append((im,float(np.median(ys))))
up=max(y for _,y in parts);down=max(im.height-y for im,y in parts);w=a.width+12;h=int(np.ceil(up+down))+12
for name,(im,y) in zip(['story_fish_open','story_fish_closed'],parts):
 canvas=Image.new('RGBA',(w,h));canvas.paste(im,(6,round(6+up-y)));canvas.save(out/(name+'.png'))
for name,f in [('dialogue_right','Turkuaz denizaltı diyalog paneli-2.png'),('dialogue_left','Turkuaz denizaltı diyalog paneli-2sol.png')]:
 im=clean(Image.open(src/f));im.resize((1086,round(im.height*1086/im.width)),Image.Resampling.NEAREST).save(out/(name+'.png'))
im=Image.open(src/'Piksel balıklar ve su örnekleme araçları.png').convert('RGBA')
for i in range(3):
 p=clean(im.crop((i*512,0,(i+1)*512,512)));p=p.resize((round(p.width/3),round(p.height/3)),Image.Resampling.NEAREST);p.save(out/f'story_dead_{i}.png')
review=Image.new('RGB',(700,220),'#244559');d=ImageDraw.Draw(review)
for i,n in enumerate(['story_fish_open','story_fish_closed']):
 im=Image.open(out/(n+'.png'));im=im.resize((im.width*2,im.height*2),Image.Resampling.NEAREST);review.paste(im,(i*350,0),im);d.text((i*350,180),n,fill='white')
review.save('Tools/Revision6/aligned-fish.png')
Path('Tools/Revision6/sprite-report.json').write_text(json.dumps({'canvas':[w,h],'visibleWidths':[a.width,b.width],'noseAligned':True,'sourceClosedPreserved':True},indent=2))
