"""Build one original floor edge with non-repeating shaded interior; fixed pixel scale."""
from pathlib import Path
from PIL import Image
import json,numpy as np,math
config=json.loads(Path('Tools/Revision5/config.json').read_text(encoding='utf-8-sig'))
source=np.array(Image.open(config['source']).convert('RGBA'))
alpha=source[:,:,3]>80
ys,xs=np.where(alpha)
left,right=int(xs.min())+6,int(xs.max())+1-6
top=int(ys.min());cap_height=40
cap=source[top:top+cap_height,left:right].copy()
cap[:,:,3][cap[:,:,3]<20]=0
# Only the real surface receives the original edge/stone face. Interior has no tiled edge motifs.
body=source[48:73,8:180];opaque=body[body[:,:,3]>200];luma=opaque[:,:3].astype(float)@np.array([.21,.72,.07])
base=opaque[np.argmin(np.abs(luma-np.quantile(luma,.22)))].copy();base[3]=255
unique=np.unique(opaque,axis=0);distance=((unique[:,:3].astype(float)-base[:3])**2).sum(axis=1)
palette=unique[np.argsort(distance)[:8]].copy();palette[:,3]=255
px=config['scaleX']/config['pixelsPerUnit'];py=config['scaleY']/config['pixelsPerUnit']
points=config['bed'];xx=np.array([p['x'] for p in points]);yy=np.array([p['y'] for p in points])
x0=config['referenceLeft']+math.floor((xx[0]-config['referenceLeft'])/px)*px
right_edge=xx[-1]+4
pixel_width=math.ceil((right_edge-x0)/px)
bottom=math.floor(-42/py)*py
chunks=[];source_colors=set(map(tuple,source.reshape(-1,4)[:,:3]))
for index,start in enumerate(range(0,pixel_width,188)):
 width=min(188,pixel_width-start)
 worldx=x0+(start+np.arange(width)+.5)*px
 heights=np.interp(worldx,xx,yy)
 high=math.ceil((float(heights.max())+.12-bottom)/py)*py+bottom
 height=round((high-bottom)/py)
 # top-down pixel rows, with the same world pixel size as the existing seabed renderer.
 worldy=high-(np.arange(height)+.5)*py
 depth=np.floor((heights[None,:]-worldy[:,None])/py).astype(int)
 sx=(np.floor((worldx-config['referenceLeft'])/px).astype(int)-left)%(right-left)
 cap_index=np.clip(depth,0,cap_height-1)
 cv=cap[cap_index,sx[None,:]]
 # Deterministic world-space pixel grain. No image rows, mirrored rocks or upper edges repeat inside the ground.
 gx=np.floor((start+np.arange(width))/3).astype(np.int64)[None,:]
 gy=np.floor(worldy[:,None]/py/3).astype(np.int64)
 hashvalue=(gx*374761393+gy*668265263+1783)&0xffffffff
 hashvalue=((hashvalue^(hashvalue>>13))*1274126177)&0xffffffff
 noise=(hashvalue^(hashvalue>>16))%len(palette)
 fill=palette[noise]
 # Fade the one surface face into subdued material before the source sprite's lower outline.
 fade=np.clip((depth-20)/20,0,1)[:,:,None]
 shaded=cv.copy();shaded[:,:,:3]=np.rint(cv[:,:,:3]*(1-fade)+fill[:,:,:3]*fade).astype('uint8')
 out=np.where((depth<cap_height)[:,:,None],shaded,fill)
 interior_holes=(depth>=20)&(out[:,:,3]<128)
 out[interior_holes]=fill[interior_holes]
 out[depth<0]=0
 name=f'shore_match_{index:02d}'
 Image.fromarray(out.astype('uint8')).save(Path('Assets/Underwater/Resources/Abyss')/(name+'.png'))
 chunks.append({'name':name,'position':{'x':float(x0+(start+width*.5)*px),'y':float((high+bottom)*.5),'z':0},'scale':{'x':config['scaleX'],'y':config['scaleY'],'z':1},'sortingOrder':0 if worldx.max()>-240 else 4})
manifest={'chunks':chunks,'bed':points}
Path('Tools/Revision5/manifest.json').write_text(json.dumps(manifest,indent=2))
Path('Tools/Revision5/pixel-report.json').write_text(json.dumps({'source':config['source'],'sourcePixelWorldSize':[px,py],'resampling':'none; original surface edge with shaded, subdued non-repeating stone interior','chunks':len(chunks),'joinHeight':config['joinHeight']},indent=2))
print(f'{len(chunks)} sprite chunks. World pixel size {px:.8f} x {py:.8f}; no texture resampling.')

