using UnityEngine;
using UnityEngine.UI;
// Pixel-shaped guide arrow, with no input hit area.
public sealed class VoyageGuideArrow:Graphic {
 protected override void OnPopulateMesh(VertexHelper vh){vh.Clear();Quad(vh,-10,-4,10,35);Triangle(vh,new Vector2(-27,-4),new Vector2(27,-4),new Vector2(0,-36));}
 void Quad(VertexHelper vh,float l,float b,float r,float t){int n=vh.currentVertCount;vh.AddVert(new Vector3(l,b),color,Vector2.zero);vh.AddVert(new Vector3(l,t),color,Vector2.zero);vh.AddVert(new Vector3(r,t),color,Vector2.zero);vh.AddVert(new Vector3(r,b),color,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
 void Triangle(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c){int n=vh.currentVertCount;vh.AddVert(a,color,Vector2.zero);vh.AddVert(b,color,Vector2.zero);vh.AddVert(c,color,Vector2.zero);vh.AddTriangle(n,n+1,n+2);}
}
