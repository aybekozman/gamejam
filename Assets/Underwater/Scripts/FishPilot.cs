using UnityEngine;
using UnityEngine.InputSystem;
namespace Abyss {
[RequireComponent(typeof(Rigidbody2D))]
public class FishPilot : MonoBehaviour {
 public float speed=6,acceleration=28,braking=35,tilt=12,animationRate=8,bubbleInterval=.65f;
 public Vector2 bubbleLifetime=new(1.2f,2f),bubbleSize=new(.12f,.22f),blinkInterval=new(3.5f,7f);
 public float bubbleRiseSpeed=1.4f,bubbleDrift=.35f;
 public float surfaceY=12,bodyHalfHeight=.38f,pitchResponse=9;
 public Vector2 swimMin=new(-45.3f,-32.4f),swimMax=new(45.3f,12);
 public SpriteRenderer visual; public Sprite[] swim,blink; public Sprite bubble;
 public Vector2 InputVector {get; private set;} public Vector2? testInput;
 public event System.Action BubbleGroupSpawned;
 public bool ControlsLocked {get;set;}
 public InputActionAsset movementActions;InputActionAsset actionCopy;
 public float sprintMultiplier=1.5f,sprintResponse=8; public float CurrentSpeed=>speed*sprintBlend;
 float sprintBlend=1,impactLeft;Vector2 impact;Color restColor;UnityEngine.InputSystem.InputAction sprint;
 public void HitReaction(Vector2 direction){impact=direction.normalized*3;impactLeft=.2f;visual.color=Color.red;}
 public void ClearHitReaction(){impact=Vector2.zero;impactLeft=0;visual.color=restColor;}
 void OnEnable(){if(movementActions){actionCopy=Instantiate(movementActions);sprint=actionCopy.FindAction("Player/Sprint",true);}else{sprint=new InputAction("UnderwaterSprint",InputActionType.Button,"<Keyboard>/leftShift");sprint.AddBinding("<Keyboard>/rightShift");}sprint.Enable();}
 void OnDisable(){if(actionCopy){actionCopy.Disable();Destroy(actionCopy);}else sprint?.Dispose();if(visual)ClearHitReaction();}
 Rigidbody2D body; float clock,nextBlink=4,blinkTime,bubbleClock; bool left;
 void Awake(){body=GetComponent<Rigidbody2D>();restColor=visual.color;}
 void Update(){
  sprintBlend=Mathf.Lerp(sprintBlend,!ControlsLocked&&sprint.IsPressed()?sprintMultiplier:1,1-Mathf.Exp(-sprintResponse*Time.deltaTime));
  if(ControlsLocked)ClearHitReaction();else if(impactLeft>0){impactLeft-=Time.deltaTime;visual.color=impactLeft>0?Color.red:restColor;}
  var k=Keyboard.current; Vector2 v=Vector2.zero;
  if(k!=null){v.x=(k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0);v.y=(k.wKey.isPressed||k.upArrowKey.isPressed?1:0)-(k.sKey.isPressed||k.downArrowKey.isPressed?1:0);}
  InputVector=ControlsLocked?Vector2.zero:Vector2.ClampMagnitude(testInput??v,1); if(Mathf.Abs(InputVector.x)>.01f)left=InputVector.x<0;
  visual.flipX=left; float pitch=InputVector.sqrMagnitude>.01f?Mathf.Clamp(Mathf.Atan2(InputVector.y,Mathf.Abs(InputVector.x))*Mathf.Rad2Deg,-tilt,tilt):0;
  float angle=Mathf.LerpAngle(visual.transform.localEulerAngles.z,pitch*(left?-1:1),1-Mathf.Exp(-pitchResponse*Time.deltaTime));visual.transform.localRotation=Quaternion.Euler(0,0,angle);
  clock+=Time.deltaTime*Mathf.Lerp(animationRate*.45f,animationRate,InputVector.magnitude);
  nextBlink-=Time.deltaTime;if(nextBlink<=0){blinkTime=.14f;nextBlink=Random.Range(blinkInterval.x,blinkInterval.y);}blinkTime-=Time.deltaTime;
  visual.sprite=blinkTime>0?blink[1]:swim[(int)clock%swim.Length];
  bubbleClock-=Time.deltaTime;if(InputVector.sqrMagnitude>.1f&&bubbleClock<=0){bubbleClock=bubbleInterval*Random.Range(.8f,1.5f);BubbleGroupSpawned?.Invoke();for(int i=0;i<Random.Range(1,4);i++)Bubble.Spawn(bubble,transform.position+new Vector3(left?.5f:-.5f,-.02f,0),surfaceY,i*.08f,bubbleLifetime,bubbleSize,bubbleRiseSpeed,bubbleDrift);}
 }
 void FixedUpdate(){
 impact=Vector2.MoveTowards(impact,Vector2.zero,20*Time.fixedDeltaTime);
 var position=body.position;position.x=Mathf.Clamp(position.x,swimMin.x,swimMax.x);position.y=Mathf.Clamp(position.y,swimMin.y,surfaceY-bodyHalfHeight);if(position!=body.position)body.position=position;
 var velocity=Vector2.MoveTowards(body.linearVelocity,InputVector*CurrentSpeed+impact,(InputVector.sqrMagnitude>.01f?acceleration:braking)*Time.fixedDeltaTime);
 velocity.x=Mathf.Clamp(velocity.x,(swimMin.x-position.x)/Time.fixedDeltaTime,(swimMax.x-position.x)/Time.fixedDeltaTime);
 velocity.y=Mathf.Clamp(velocity.y,(swimMin.y-position.y)/Time.fixedDeltaTime,(surfaceY-bodyHalfHeight-position.y)/Time.fixedDeltaTime);body.linearVelocity=velocity;
 }
}
}
