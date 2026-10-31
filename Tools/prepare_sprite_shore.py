"""Build the shore from unchanged floor pixels; no resampling or stretched UVs."""
from pathlib import Path
from PIL import Image
import json,numpy as np,math
config=json.loads(Path('Tools/Revision5/config.json').read_text(encoding='utf-8-sig'))
source=np.array(Image.open(config['source']).convert('RGBA'))
alpha=source[:,:,3]>80
ys,xs=np.where(alpha)
left,right=int(xs.min()),int(xs.max())+1
top=int(ys.min());cap_height=17
cap=source[top:top+cap_height,left:right].copy()
cap[:,:,3][cap[:,:,3]<20]=0
core=source[top+cap_height:top+cap_height+22,left:right].copy()
for row in core:
 valid=np.where(row[:,3]>180)[0]
 if not len(valid):raise RuntimeError('No opaque source stone body')
 for x in np.where(row[:,3]<=180)[0]:row[x]=row[valid[np.argmin(np.abs(valid-x))]]
core[:,:,3]=255
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
 d=np.maximum(0,depth-cap_height)
 period=core.shape[0]*2
 cy=d%period
 cy=np.where(cy>=core.shape[0],period-1-cy,cy)
 fill=core[cy,sx[None,:]]
 out=np.where((depth<cap_height)[:,:,None],cv,fill)
 out[depth<0]=0
 name=f'shore_match_{index:02d}'
 Image.fromarray(out.astype('uint8')).save(Path('Assets/Underwater/Resources/Abyss')/(name+'.png'))
 chunks.append({'name':name,'position':{'x':float(x0+(start+width*.5)*px),'y':float((high+bottom)*.5),'z':0},'scale':{'x':config['scaleX'],'y':config['scaleY'],'z':1},'sortingOrder':0 if worldx.max()>-240 else 4})
manifest={'chunks':chunks,'bed':points}
Path('Tools/Revision5/manifest.json').write_text(json.dumps(manifest,indent=2))
Path('Tools/Revision5/pixel-report.json').write_text(json.dumps({'source':config['source'],'sourcePixelWorldSize':[px,py],'resampling':'none; pixels copied directly from existing floor','chunks':len(chunks),'joinHeight':config['joinHeight']},indent=2))
print(f'{len(chunks)} sprite chunks. World pixel size {px:.8f} x {py:.8f}; no texture resampling.')
