using UnityEngine;
namespace Abyss {
public class Parallax : MonoBehaviour {public float factor=.15f;Transform cameraTransform;Vector3 origin,cameraOrigin;void Start(){cameraTransform=Camera.main.transform;origin=transform.position;cameraOrigin=cameraTransform.position;}void LateUpdate(){transform.position=origin+(cameraTransform.position-cameraOrigin)*factor;}}
}
