using UnityEngine;
namespace Abyss {
public class FollowCamera : MonoBehaviour {
 public Transform target; public float smoothing=.15f,orthographicSize=7; public Vector2 worldMin=new(-46,-34),worldMax=new(46,16);Vector3 velocity;
 void LateUpdate(){var cam=GetComponent<Camera>();cam.orthographicSize=orthographicSize;float x=orthographicSize*cam.aspect;var goal=new Vector3(Mathf.Clamp(target.position.x,worldMin.x+x,worldMax.x-x),Mathf.Clamp(target.position.y+.7f,worldMin.y+orthographicSize,worldMax.y-orthographicSize),-10);var p=Vector3.SmoothDamp(transform.position,goal,ref velocity,smoothing);float pixel=2*orthographicSize/Mathf.Max(1,cam.pixelHeight);p.x=Mathf.Round(p.x/pixel)*pixel;p.y=Mathf.Round(p.y/pixel)*pixel;transform.position=p;}
}
}
