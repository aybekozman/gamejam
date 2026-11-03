using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
// Two persistent music voices crossfade only when the scene's score changes.
public sealed class VoyageMusic:MonoBehaviour {
 AudioSource[] voices;int active;AudioClip requested;Coroutine transition;
 void Awake(){AudioListener.volume=PlayerPrefs.GetFloat("Sound.Master",.85f);voices=new[]{gameObject.AddComponent<AudioSource>(),gameObject.AddComponent<AudioSource>()};foreach(var source in voices){source.playOnAwake=false;source.loop=true;source.spatialBlend=0;source.volume=0;}SceneManager.sceneLoaded+=Loaded;}
 void OnDestroy(){SceneManager.sceneLoaded-=Loaded;}
 void Loaded(Scene scene,LoadSceneMode mode){var stage=StorySceneJourney.Instance.Stage;string name=scene.path==StorySceneJourney.ShipPath?(stage==JourneyStage.FirstShip?"peaceful":"surface"):scene.name=="MainMenu"?"underwater":scene.path==StorySceneJourney.UnderwaterPath&&stage==JourneyStage.ThirdUnderwater?"danger":null;if(name==null){if(transition!=null)StopCoroutine(transition);transition=null;requested=null;foreach(var voice in voices){voice.Stop();voice.clip=null;voice.volume=0;}return;}var clip=name!=null?Resources.Load<AudioClip>("VoyageMusic/"+name):null;if(clip==requested)return;requested=clip;if(transition!=null)StopCoroutine(transition);transition=StartCoroutine(Change(clip));}
 IEnumerator Change(AudioClip clip){int next=1-active;voices[next].Stop();voices[next].clip=clip;voices[next].volume=0;if(clip)voices[next].Play();float previous=voices[active].volume;for(float t=0;t<1;t+=Time.unscaledDeltaTime/.8f){voices[active].volume=Mathf.Lerp(previous,0,t);voices[next].volume=Mathf.Lerp(0,Gain,t);yield return null;}voices[active].Stop();active=next;transition=null;}
 float Gain=>.3f*PlayerPrefs.GetFloat("Sound.Music",.7f)*(VoyageMainMenu.IntroVisible?0:1);
 void Update(){if(transition==null)voices[active].volume=Gain;}
}
