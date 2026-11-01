using UnityEngine;
using UnityEngine.Audio;

namespace Abyss.Story
{
    public class UnderwaterSceneAudio:MonoBehaviour
    {
        public FishPilot player;public UnderwaterStoryUI ui;
        public AudioClip ambience,bubble,splash,impact,distantCannon;public Vector2 distantInterval=new(4,8);float nextDistant;AudioSource distant;
        public AudioMixerGroup mixer;
        [Range(0,1)]public float ambienceVolume=.1f,bubbleVolume=.025f,effectVolume=.12f;
        public float bubbleInterval=1.6f,bubbleStartTime,bubbleDuration=.65f,loopBlend=.6f;
        AudioSource[] beds,fx;AudioSource bubbles;int current;float age,nextBubble,bubbleAge=-1,blend=-1,nextEffect;
        public int BubblePlays{get;private set;}public int SourceCount=>GetComponents<AudioSource>().Length;
        AudioSource Source(){var source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;source.outputAudioMixerGroup=mixer;source.volume=0;return source;}
        void Awake(){beds=new[]{Source(),Source()};fx=new[]{Source(),Source()};bubbles=Source();var distantObject=new GameObject("Distant cannon audio");distantObject.transform.SetParent(transform,false);distant=distantObject.AddComponent<AudioSource>();distant.playOnAwake=false;distant.spatialBlend=0;distant.outputAudioMixerGroup=mixer;distantObject.AddComponent<AudioLowPassFilter>().cutoffFrequency=1400;}
        void OnEnable(){if(player)player.BubbleGroupSpawned+=PlayBubble;}
        void OnDisable(){if(player)player.BubbleGroupSpawned-=PlayBubble;foreach(var source in GetComponentsInChildren<AudioSource>())source.Stop();}
        void Start(){if(ambience){beds[0].clip=ambience;beds[1].clip=ambience;beds[0].Play();}}
        void Update()
        {
            var chapter=GetComponent<UnderwaterThirdChapter>();if(distantCannon&&chapter&&chapter.hazards.Running&&!ui.Blocking&&Time.time>nextDistant&&!distant.isPlaying){nextDistant=Time.time+Random.Range(distantInterval.x,distantInterval.y);distant.clip=distantCannon;distant.volume=.13f;distant.Play();}if(chapter&&!chapter.hazards.Running)distant.Stop();
            distant.volume=(ui&&ui.Blocking) ? .04f : .13f;
            age+=Time.unscaledDeltaTime;float gain=ambienceVolume*Mathf.Clamp01(age)*((ui&&ui.Blocking) ? .65f : 1);
            if(ambience&&ambience.length>2)
            {
                if(blend<0&&beds[current].time>=ambience.length-loopBlend){blend=0;beds[1-current].time=0;beds[1-current].Play();}
                if(blend>=0){blend+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(blend/loopBlend);beds[current].volume=gain*(1-t);beds[1-current].volume=gain*t;if(t>=1){beds[current].Stop();current=1-current;blend=-1;}}
                else beds[current].volume=Mathf.MoveTowards(beds[current].volume,gain,Time.unscaledDeltaTime*.15f);
            }
            if(bubbleAge>=0){bubbleAge+=Time.unscaledDeltaTime;bubbles.volume=bubbleVolume*Mathf.Min(Mathf.Clamp01(bubbleAge/.05f),Mathf.Clamp01((bubbleDuration-bubbleAge)/.12f));if(bubbleAge>=bubbleDuration){bubbles.Stop();bubbleAge=-1;}}
        }
        void PlayBubble()
        {
            if(!bubble||Time.unscaledTime<nextBubble||ui&&ui.Blocking)return;
            nextBubble=Time.unscaledTime+bubbleInterval;bubbles.clip=bubble;bubbles.pitch=Random.Range(.97f,1.03f);bubbles.time=Mathf.Clamp(bubbleStartTime,0,Mathf.Max(0,bubble.length-bubbleDuration));bubbles.Play();bubbleAge=0;BubblePlays++;
        }
        public void PlayImpact(bool waterEntry)
        {
            var clip=waterEntry?splash:impact;if(!clip||Time.unscaledTime<nextEffect)return;
            foreach(var source in fx)if(!source.isPlaying){source.clip=clip;source.volume=effectVolume;source.Play();nextEffect=Time.unscaledTime+.15f;return;}
        }
    }
}
