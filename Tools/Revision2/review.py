from PIL import Image,ImageDraw
from pathlib import Path
files=[p for stem in ['animal_2','whale_0','whale_1','animal_0','animal_1','animal_3'] for p in sorted(Path('Assets/Underwater/Resources/Abyss').glob(stem+'_*.png'))]
out=Image.new('RGB',(1000,6*170),'#233e50');d=ImageDraw.Draw(out)
for i,p in enumerate(files):
 im=Image.open(p);print(p.stem,im.size,im.getchannel('A').point(lambda x:255 if x>45 else 0).getbbox());im.thumbnail((240,140));out.paste(im,(i%4*250,i//4*170),im);d.text((i%4*250,i//4*170+145),p.stem,fill='white')
out.save('Tools/Revision2/animation-after.png')
