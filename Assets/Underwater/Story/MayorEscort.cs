using System.Collections.Generic;
using UnityEngine;

namespace Abyss.Story
{
    public class MayorEscort:MonoBehaviour
    {
        public StorySwimmer mayor;public Transform player;public float stopDistance=2.4f;
        public bool Following{get;private set;}public float Separation=>Vector2.Distance(mayor.Position,player.position);
        readonly Queue<Vector2> breadcrumbs=new();Vector2 last;float refresh;
        public void Begin(){breadcrumbs.Clear();last=player.position;breadcrumbs.Enqueue(last);Following=true;refresh=0;}
        public void End(){Following=false;breadcrumbs.Clear();mayor.Stop();}
        void Update()
        {
            if(!Following)return;
            if(Vector2.Distance(last,player.position)>1){last=player.position;breadcrumbs.Enqueue(last);}
            if(Separation<stopDistance){mayor.Stop();return;}
            if((refresh-=Time.deltaTime)>0)return;refresh=.35f;
            while(breadcrumbs.Count>0&&Vector2.Distance(mayor.Position,breadcrumbs.Peek())<.8f)breadcrumbs.Dequeue();
            if(breadcrumbs.Count>0)
            {
                var points=breadcrumbs.ToArray();int lastVisible=-1;
                for(int i=points.Length-1;i>=0;i--)if(WaterRoute.Segment(mayor.Position,points[i])){lastVisible=i;break;}
                if(lastVisible>=0){var target=points[lastVisible];for(int i=0;i<=lastVisible;i++)breadcrumbs.Dequeue();mayor.Go(target);}
                else if(mayor.Arrived)mayor.Go(points[0]);
            }
            else if(mayor.Arrived&&WaterRoute.Segment(mayor.Position,player.position))mayor.Go((Vector2)player.position+((mayor.Position-(Vector2)player.position).normalized*stopDistance));
        }
    }
}
