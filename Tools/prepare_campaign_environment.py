"""Prepare copies only; keep the supplied sheets and their import settings intact."""
from pathlib import Path
from PIL import Image
import numpy as np
import shutil
out=Path('Assets/Campaign/Resources/Campaign')
out.mkdir(parents=True,exist_ok=True)
for source,target in [('Assets/Underwater/Fonts/PixelifySans.ttf','PixelifySans.ttf'),('Assets/Underwater/Fonts/OFL.txt','OFL.txt'),('Assets/Sprites/ShipWaterProcessed/Ocean_Seamless.png','menu_ocean.png'),('Assets/Underwater/Resources/Abyss/reef_0_0.png','mayor_portrait.png')]:
    shutil.copy2(source,out/target)
for source in Path('Assets/Sprites/Underwater').glob('Yans*.png'):
    im=Image.open(source).convert('RGBA');a=np.array(im)
    a[:,:,3]=np.where(a[:,:,3]>190,255,0)
    im=Image.fromarray(a);im=im.crop(im.getbbox())
    im.resize((1086,round(im.height*1086/im.width)),Image.Resampling.NEAREST).save(out/('ship_dialogue_left.png' if 'sol' in source.stem else 'ship_dialogue_right.png'))
for pattern,name in [('*Nehir*','town'),('*Liman*','harbor')]:
    source=next(Path('Assets/Sprites').glob(pattern+'.png'))
    a=np.array(Image.open(source).convert('RGBA'))
    if name=='town':
        a[:,:,3]=np.where(a[:,:,3]>220,255,0)
    else:
        # Preserve stone and the tank; remove opaque water/glow around the U-shaped pier.
        yy,xx=np.indices(a.shape[:2])
        mask=((xx>=126)&(xx<=1425)&(yy>=140)&(yy<=310))|((xx>=126)&(xx<=333)&(yy>=140)&(yy<=870))|((xx>=1190)&(xx<=1425)&(yy>=140)&(yy<=870))|((xx>=1180)&(xx<=1450)&(yy>=54)&(yy<=300))
        a[:,:,3]=np.where(mask,255,0)
    Image.fromarray(a).save(out/(name+'.png'))
