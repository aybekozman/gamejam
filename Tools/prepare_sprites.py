from PIL import Image, ImageDraw
import pathlib, numpy as np, json
src=pathlib.Path('Assets/Sprites/Underwater'); files=[src/n for n in ['4x4 sprite sayfasında gölge balıkları.png', 'Deniz Yosunları Sallanma Animasyon Spritleri.png', 'Dört deniz canlısı retro piksel sprite seti.png', 'Dört tür balık piksel animasyon seti.png', 'Piksel balıklar ve su örnekleme araçları.png', 'Piksel deniz tabanı kaya modülleri.png', 'Piksel sualtı ışık huzmeleri.png', 'Retro piksel köpekbalığı yüzme sprite’ları (1).png', 'Retro piksel mavi gökyüzü-1.png', 'Retro su altı baloncuk sprite’ları.png', 'Sakin Piksel Su Yüzeyi Şeridi-2.png', 'Sekiz kareli turkuaz mercan balığı animasyonu.png', 'Su altı balık köyü yapı seti.png', 'Su altı topu piksel efektleri.png', 'Turkuazdan laciverte sade piksel okyanusu.png', 'Üç renkli resif balığı sprite sayfası.png', 'Üç sualtı kaya grubu.png', 'Üç Uzak Kaya ve Yosun Silueti.png', 'İki balinanın retro piksel yüzüşü.png']]; dst=pathlib.Path('Assets/Underwater/Resources/Abyss')
report=[]
def sheet(index,name,cols,rows):
 im=Image.open(files[index]).convert('RGBA'); a=np.array(im); a[:,:,3][a[:,:,3]<12]=0; im=Image.fromarray(a)
 # Grid regions are only search windows verified from contact sheet; actual alpha bounds determine crops.
 def bands(values, expected, gap=10):
  ids=np.where(values>max(values)*.025)[0]
  groups=[g for g in np.split(ids,np.where(np.diff(ids)>gap)[0]+1) if len(g)>8]
  if len(groups)!=expected: raise ValueError((name,expected,[(int(g[0]),int(g[-1])) for g in groups]))
  edges=[0]+[int((groups[i][-1]+groups[i+1][0])/2) for i in range(len(groups)-1)]+[len(values)]
  return edges
 rowEdges=bands((a[:,:,3]>60).sum(axis=1),rows)
 for r in range(rows):
  crops=[]
  colEdges=bands((a[rowEdges[r]:rowEdges[r+1],:,3]>60).sum(axis=0),cols,30 if name=="bubble" else 10)
  for c in range(cols):
   box=(colEdges[c],rowEdges[r],colEdges[c+1],rowEdges[r+1])
   cut=im.crop(box); mask=cut.getchannel('A').point(lambda v:255 if v>35 else 0); bounds=mask.getbbox()
   if not bounds: continue
   cut=cut.crop(bounds); crops.append(cut); report.append(dict(source=files[index].name,group=name,row=r,frame=c,search=box,bounds=bounds))
  if not crops: continue
  # Equal canvas, bottom/root anchor for plants; normalized body height for animal frames.
  h=round(np.median([p.height for p in crops])); resized=[]
  for p in crops:
   if name in ['hero','reef','fish','animal','shark','whale']: p=p.resize((round(p.width*h/p.height),h),Image.Resampling.NEAREST)
   resized.append(p)
  w=max(p.width for p in resized)+12; hh=max(p.height for p in resized)+12
  for c,p in enumerate(resized):
   canvas=Image.new('RGBA',(w,hh)); canvas.paste(p,((w-p.width)//2,hh-p.height-6))
   canvas=canvas.resize((round(w/3),round(hh/3)),Image.Resampling.NEAREST); canvas.save(dst/f'{name}_{r}_{c}.png')
for args in [(0,'shadow',4,4),(1,'kelp',4,4),(2,'animal',4,4),(3,'fish',4,4),(5,'ground',3,2),(6,'ray',5,1),(7,'shark',4,2),(9,'bubble',4,2),(11,'hero',4,2),(12,'house',3,2),(15,'reef',4,3),(16,'rock',3,1),(17,'distant',3,1),(18,'whale',4,2)]: sheet(*args)
for i,n in [(8,'sky'),(10,'surface'),(14,'water')]:
 im=Image.open(files[i]).convert('RGBA'); a=np.array(im); a[:,:,3][a[:,:,3]<12]=0; im=Image.fromarray(a); b=im.getchannel('A').point(lambda v:255 if v>35 else 0).getbbox(); im=im.crop(b); im.resize((im.width//3,im.height//3),Image.Resampling.NEAREST).save(dst/(n+'.png'))
pathlib.Path('Tools/sprite-bounds.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf8')
ims=list(dst.glob('hero*.png')); out=Image.new('RGB',(640,240),'#223849');d=ImageDraw.Draw(out)
for i,p in enumerate(ims):
 im=Image.open(p); im.thumbnail((150,100));out.paste(im,(i%4*160,i//4*120),im);d.text((i%4*160,i//4*120+100),p.stem,fill='white')
out.save('Tools/hero-review.png')




exec(pathlib.Path("Tools/water_texture.py").read_text(encoding="utf-8-sig"))

exec(pathlib.Path("Tools/stabilize_ambient.py").read_text(encoding="utf-8-sig"))
