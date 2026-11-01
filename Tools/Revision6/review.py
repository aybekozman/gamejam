from PIL import Image,ImageDraw
from pathlib import Path
files=[p for p in Path('Assets/Sprites/Underwater').glob('*.png') if any(t in p.name for t in ['Kapalı','paneli','balıklar ve'])]
out=Image.new('RGB',(1000,((len(files)+1)//2)*280),'#263849');d=ImageDraw.Draw(out)
for i,p in enumerate(files):
 im=Image.open(p).convert('RGBA');print(p.name,im.size,im.getchannel('A').getextrema());im.thumbnail((490,240));out.paste(im,(i%2*500,i//2*280),im);d.text((i%2*500,i//2*280+242),str(i)+' '+p.name[:30],fill='white')
out.save('Tools/Revision6/new-assets.png')
