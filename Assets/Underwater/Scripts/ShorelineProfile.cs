using UnityEngine;

namespace Abyss
{
    public class ShorelineProfile : MonoBehaviour
    {
        public Vector2[] bed = {
            new(-344,14.4f), new(-328,14.1f), new(-316,13.3f), new(-310,12.6f),
            new(-304,10.3f), new(-296,4.7f), new(-286,-3), new(-274,-12.3f),
            new(-262,-20.2f), new(-250,-26.9f), new(-240,-30.5f), new(-232,-31.6f)
        };
        public float surfaceY=12;
        public float WaterlineX => XAtHeight(surfaceY);
        public float BedHeight(float x)
        {
            for(int i=1;i<bed.Length;i++) if(x<=bed[i].x) return Mathf.Lerp(bed[i-1].y,bed[i].y,Mathf.InverseLerp(bed[i-1].x,bed[i].x,x));
            return bed[^1].y;
        }
        public float XAtHeight(float y)
        {
            for(int i=1;i<bed.Length;i++) if(y<=bed[i-1].y&&y>=bed[i].y) return Mathf.Lerp(bed[i-1].x,bed[i].x,Mathf.InverseLerp(bed[i-1].y,bed[i].y,y));
            return bed[0].x;
        }
    }
}
