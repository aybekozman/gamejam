using UnityEngine;
public static class VoyageTouchInput {
 public static bool Enabled=>Application.isMobilePlatform||Application.isEditor;
 public static Vector2 Move{get;private set;}public static bool StickActive{get;private set;}public static bool FishBoostHeld{get;private set;}
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Init(){Reset();if(Application.isMobilePlatform){Application.targetFrameRate=60;Screen.autorotateToPortrait=false;Screen.autorotateToPortraitUpsideDown=false;Screen.autorotateToLandscapeLeft=true;Screen.autorotateToLandscapeRight=true;Screen.orientation=ScreenOrientation.AutoRotation;}}
 public static void SetMove(Vector2 value,bool active){Move=Vector2.ClampMagnitude(value,1);StickActive=active;}
 public static void SetFishBoost(bool held)=>FishBoostHeld=held;
 public static void Reset(){Move=Vector2.zero;StickActive=false;FishBoostHeld=false;}
}
