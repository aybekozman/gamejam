using UnityEngine;
namespace Abyss {
public class PassingFish:MonoBehaviour {public float velocity;float age;void Update(){transform.position+=Vector3.right*velocity*Time.deltaTime;if((age+=Time.deltaTime)>65)Destroy(gameObject);}}
}
