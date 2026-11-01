from PIL import Image
import numpy as np,pathlib
a=np.asarray(Image.open(pathlib.Path("Assets/Sprites/Underwater")/'Turkuazdan laciverte sade piksel okyanusu.png').convert('RGB').resize((556,313),Image.Resampling.NEAREST)).astype(float)
means=a.mean(axis=1);h=2816;w=1024;ys=np.linspace(0,len(a)-1,h);base=np.stack([np.interp(ys,np.arange(len(a)),means[:,c]) for c in range(3)],axis=1)
noise=a-means[:,None,:];pattern=noise[np.arange(h)%len(a)][:,np.arange(w)%a.shape[1]]
out=np.clip(base[:,None,:]+pattern,0,255);target=np.array([0,30,79]);fade=np.clip((np.arange(h)-h+100)/100,0,1)[:,None,None];out=out*(1-fade)+target*fade
Image.fromarray(out.astype('uint8')).save('Assets/Underwater/Resources/Abyss/water.png')
