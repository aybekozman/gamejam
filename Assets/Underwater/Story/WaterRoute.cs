using System.Collections.Generic;
using UnityEngine;

namespace Abyss.Story
{
    public static class WaterRoute
    {
        static readonly RaycastHit2D[] hits=new RaycastHit2D[64];
        static readonly Collider2D[] overlaps=new Collider2D[64];
        static ContactFilter2D Filter=>new ContactFilter2D{useTriggers=false};
        public static bool Clear(Vector2 position,float radius=.42f)
        {
            int count=Physics2D.OverlapCircle(position,radius,Filter,overlaps);
            for(int i=0;i<count;i++)if(overlaps[i]&&overlaps[i].enabled&&!overlaps[i].isTrigger&&!overlaps[i].attachedRigidbody)return false;
            return true;
        }
        public static bool Segment(Vector2 from,Vector2 to,float radius=.42f)
        {
            if(!Clear(to,radius))return false;var delta=to-from;if(delta.sqrMagnitude<.001f)return true;
            int count=Physics2D.CircleCast(from,radius,delta.normalized,Filter,hits,delta.magnitude);
            for(int i=0;i<count;i++)if(hits[i].collider&&hits[i].collider.enabled&&!hits[i].collider.isTrigger&&!hits[i].collider.attachedRigidbody)return false;
            return true;
        }
        public static List<Vector2> Find(Vector2 from,Vector2 goal,float radius=.42f)
        {
            if(Segment(from,goal,radius))return new List<Vector2>{goal};
            const float cell=.65f;var min=Vector2.Min(from,goal)-Vector2.one*8;var max=Vector2.Max(from,goal)+Vector2.one*8;
            min.y=Mathf.Max(min.y,-33);max.y=Mathf.Min(max.y,11.3f);
            int w=Mathf.CeilToInt((max.x-min.x)/cell)+1,h=Mathf.CeilToInt((max.y-min.y)/cell)+1;
            if(w*h>50000)return new List<Vector2>();
            int Index(Vector2 p)=>Mathf.Clamp(Mathf.RoundToInt((p.y-min.y)/cell),0,h-1)*w+Mathf.Clamp(Mathf.RoundToInt((p.x-min.x)/cell),0,w-1);
            Vector2 Position(int index)=>min+new Vector2(index%w,index/w)*cell;
            int start=Index(from),end=Index(goal);var open=new List<int>{start};var g=new Dictionary<int,float>{{start,0}};var previous=new Dictionary<int,int>();var visited=new HashSet<int>();
            while(open.Count>0)
            {
                int best=0;float value=float.PositiveInfinity;
                for(int i=0;i<open.Count;i++){float score=g[open[i]]+Vector2.Distance(Position(open[i]),goal);if(score<value){value=score;best=i;}}
                int current=open[best];open.RemoveAt(best);if(!visited.Add(current))continue;
                if((current==end||Vector2.Distance(Position(current),goal)<cell)&&Segment(Position(current),goal,radius))
                {
                    var path=new List<Vector2>{goal};while(current!=start){path.Add(Position(current));current=previous[current];}path.Reverse();
                    var smooth=new List<Vector2>();Vector2 anchor=from;for(int i=0;i<path.Count;){int last=i;while(last+1<path.Count&&Segment(anchor,path[last+1],radius))last++;smooth.Add(path[last]);anchor=path[last];i=last+1;}return smooth;
                }
                int cx=current%w,cy=current/w;
                for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                {
                    if(dx==0&&dy==0)continue;int x=cx+dx,y=cy+dy;if(x<0||x>=w||y<0||y>=h)continue;int next=y*w+x;if(visited.Contains(next))continue;
                    Vector2 a=current==start?from:Position(current),b=Position(next);if(!Segment(a,b,radius))continue;float cost=g[current]+Vector2.Distance(a,b);
                    if(g.TryGetValue(next,out float old)&&old<=cost)continue;g[next]=cost;previous[next]=current;if(!open.Contains(next))open.Add(next);
                }
            }
            return new List<Vector2>();
        }
    }
}
