using UnityEngine;
namespace Abyss {
public class SurfaceRipple : MonoBehaviour {Vector3 p;public float phase;void Start(){p=transform.position;}void Update(){transform.position=p+Vector3.up*Mathf.Sin(Time.time*1.4f+phase)*.035f;}}
}
