using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Abyss.Story
{
    public class UnderwaterStoryUI:MonoBehaviour
    {
        [Serializable] public struct Line { public bool mayor;public string text,speakerName;public Sprite portrait;public Line(bool mayor,string text){this.mayor=mayor;this.text=text;speakerName=null;portrait=null;} public Line(string speakerName,Sprite portrait,string text,bool left=true){mayor=left;this.text=text;this.speakerName=speakerName;this.portrait=portrait;} }
        public Sprite leftPanel,rightPanel,playerPortrait,mayorPortrait;
        public float lettersPerSecond=36;
        public bool Active=>lines.Count>0;
        public bool Blocking{get;private set;}
        public string CurrentText=>Active?lines.Peek().text:"";
        public string Objective{get;private set;}
        public bool TextReady=>!Active||shown>=CurrentText.Length;
        public Font Font=>font;
        public string SpeakerName=>speaker.text;public Sprite SpeakerPortrait=>portrait.sprite;public string Hint=>hint.text;
        public string InteractionKey="E";
        Image fade;Text fadeLabel,advance,thought,health,danger;GameObject objectiveRoot;float thoughtUntil;
        public Transform ActorTarget{get;private set;}public bool ActorTargetOffscreen{get;private set;}
        public string ThoughtText=>thought.text;
        readonly Queue<Line> lines=new();
        GameObject panelRoot;RectTransform canvasRect,portraitRect,speakerRect,bodyRect,markerRect;Image panel,portrait;Text speaker,body,objective,hint,marker;Font font;
        FishPilot pilot;Action completed;float shown,autoDelay;int openedFrame;bool automatic,travelDialogue,continuePrompt;
        public bool ContinuePromptVisible=>continuePrompt;
        void Awake()
        {
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var root=new GameObject("Underwater story HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));root.transform.SetParent(transform,false);var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=Camera.main;canvas.planeDistance=1;canvas.sortingOrder=1000;canvas.pixelPerfect=true;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;canvasRect=root.GetComponent<RectTransform>();
            var objectiveBox=Rect("Objective",canvasRect,new Vector2(20,-20),new Vector2(600,94),new Vector2(0,1),new Vector2(0,1));objectiveRoot=objectiveBox.gameObject;var shade=objectiveBox.gameObject.AddComponent<Image>();shade.color=new Color(.015f,.08f,.12f,.85f);objective=Text("Objective text",objectiveBox,new Vector2(18,-10),new Vector2(564,76),25,TextAnchor.UpperLeft,new Color(.9f,.96f,.87f));
            var hintRect=Rect("Interaction hint",canvasRect,new Vector2(0,150),new Vector2(900,52),new Vector2(.5f,0),new Vector2(.5f,.5f));hint=hintRect.gameObject.AddComponent<Text>();Configure(hint,26,TextAnchor.MiddleCenter,Color.white);hint.gameObject.AddComponent<Outline>().effectColor=Color.black;
            var pr=Rect("Portrait dialogue",canvasRect,new Vector2(0,24),new Vector2(1100,320),new Vector2(.5f,0),new Vector2(.5f,0));panelRoot=pr.gameObject;panel=panelRoot.AddComponent<Image>();panel.type=Image.Type.Simple;
            portraitRect=Rect("Speaker portrait",pr,Vector2.zero,new Vector2(155,155),Vector2.zero,new Vector2(.5f,.5f));portrait=portraitRect.gameObject.AddComponent<Image>();portrait.preserveAspect=true;
            speakerRect=Rect("Speaker name",pr,Vector2.zero,new Vector2(310,42),Vector2.zero,new Vector2(.5f,.5f));speaker=speakerRect.gameObject.AddComponent<Text>();Configure(speaker,27,TextAnchor.MiddleCenter,new Color(1,.92f,.65f));
            bodyRect=Rect("Single sentence",pr,Vector2.zero,new Vector2(700,175),Vector2.zero,new Vector2(0,.5f));body=bodyRect.gameObject.AddComponent<Text>();Configure(body,30,TextAnchor.MiddleLeft,Color.white);
            advance=Text("Advance",pr,new Vector2(550,20),new Vector2(900,30),20,TextAnchor.MiddleCenter,new Color(.7f,.9f,.94f));advance.rectTransform.pivot=new Vector2(.5f,.5f);advance.rectTransform.anchorMin=advance.rectTransform.anchorMax=Vector2.zero;advance.rectTransform.anchoredPosition=new Vector2(550,62);advance.text="E  •  metni tamamla / devam et";
            markerRect=Rect("Current story target",canvasRect,Vector2.zero,new Vector2(360,66),new Vector2(.5f,.5f),new Vector2(.5f,.5f));marker=markerRect.gameObject.AddComponent<Text>();Configure(marker,23,TextAnchor.MiddleCenter,new Color(1,.9f,.45f));marker.gameObject.AddComponent<Outline>().effectColor=new Color(.02f,.08f,.12f,1);panelRoot.SetActive(false);
            thought=Text("Short thought",canvasRect,new Vector2(24,-155),new Vector2(1000,65),25,TextAnchor.MiddleLeft,Color.white);thought.gameObject.AddComponent<Outline>().effectColor=Color.black;health=Text("Chapter health",canvasRect,new Vector2(24,-113),new Vector2(300,40),23,TextAnchor.MiddleLeft,new Color(.7f,.95f,1));danger=Text("Falling danger warning",canvasRect,new Vector2(0,-24),new Vector2(550,45),24,TextAnchor.MiddleCenter,new Color(1,.83f,.4f));danger.rectTransform.anchorMin=danger.rectTransform.anchorMax=new Vector2(.5f,1);danger.rectTransform.pivot=new Vector2(.5f,1);
            var cover=Rect("Chapter fade",canvasRect,Vector2.zero,Vector2.zero,Vector2.zero,Vector2.zero);cover.anchorMax=Vector2.one;cover.offsetMin=cover.offsetMax=Vector2.zero;fade=cover.gameObject.AddComponent<Image>();fade.color=new Color(0,0,0,0);fade.raycastTarget=false;fadeLabel=Text("Time passes",cover,new Vector2(0,0),new Vector2(900,100),38,TextAnchor.MiddleCenter,Color.white);fadeLabel.rectTransform.anchorMin=fadeLabel.rectTransform.anchorMax=new Vector2(.5f,.5f);fadeLabel.rectTransform.pivot=new Vector2(.5f,.5f);fadeLabel.text="";
        }
        static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size,Vector2 anchor,Vector2 pivot){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=anchor;r.pivot=pivot;r.sizeDelta=size;r.anchoredPosition=position;return r;}
        Text Text(string name,Transform parent,Vector2 position,Vector2 size,int fontSize,TextAnchor alignment,Color color){var r=Rect(name,parent,position,size,new Vector2(0,1),new Vector2(0,1));var t=r.gameObject.AddComponent<Text>();Configure(t,fontSize,alignment,color);return t;}
        void Configure(Text text,int size,TextAnchor alignment,Color color){text.font=font;text.fontSize=size;text.alignment=alignment;text.color=color;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Overflow;text.supportRichText=false;}
        public void UseFont(Font value){font=value;foreach(var t in GetComponentsInChildren<Text>(true)){t.font=value;t.lineSpacing=1.15f;}}
        public void Bind(FishPilot player){pilot=player;}
        public void Show(IEnumerable<Line> dialogue,bool block,Action onComplete=null,bool auto=false)
        {
            Clear();foreach(var line in dialogue)lines.Enqueue(line);Blocking=block;automatic=auto;completed=onComplete;if(pilot)pilot.ControlsLocked=block;Present();
        }
        public void QueueTravelDialogue(string text)
        {
            if(Blocking)return;
            var line=new Line(false,text);
            if(Active){if(automatic)lines.Enqueue(line);return;}
            Show(new[]{line},false,null,true);travelDialogue=true;autoDelay=2.6f;
        }
        public void SetContinuePrompt(bool visible,string key="E")
        {
            if(Active)return;
            if(!visible){if(continuePrompt)panelRoot.SetActive(false);continuePrompt=false;return;}
            continuePrompt=true;panelRoot.SetActive(true);panel.sprite=rightPanel;portrait.sprite=playerPortrait;
            portraitRect.anchoredPosition=new Vector2(925,142);speakerRect.anchoredPosition=new Vector2(800,280);bodyRect.anchoredPosition=new Vector2(62,148);
            speaker.text="Oyuncu";body.text="Köpekbalıklarına doğru yola çık.\n"+key+" ile devam et.";advance.text=key+"  •  devam et";
        }
        void Present()
        {
            if(!Active){panelRoot.SetActive(false);Blocking=false;if(pilot)pilot.ControlsLocked=false;var action=completed;completed=null;action?.Invoke();return;}
            var line=lines.Peek();bool left=line.mayor;panel.sprite=left?leftPanel:rightPanel;portrait.sprite=line.portrait?line.portrait:(left?mayorPortrait:playerPortrait);
            portraitRect.anchoredPosition=new Vector2(left?170:925,142);speakerRect.anchoredPosition=new Vector2(left?300:800,280);bodyRect.anchoredPosition=new Vector2(left?340:62,148);
            speaker.text=!string.IsNullOrEmpty(line.speakerName)?line.speakerName:(left?"Başkan":"Oyuncu");advance.text=InteractionKey+"  •  metni tamamla / devam et";body.text="";shown=0;autoDelay=travelDialogue?2.6f:1.6f;openedFrame=Time.frameCount;panelRoot.SetActive(true);
        }
        public void Advance()
        {
            if(!Active||Time.frameCount<=openedFrame)return;
            if(!TextReady){shown=CurrentText.Length;body.text=CurrentText;return;}lines.Dequeue();Present();
        }
        public void Clear(){travelDialogue=false;continuePrompt=false;lines.Clear();completed=null;Blocking=false;if(pilot)pilot.ControlsLocked=false;if(panelRoot)panelRoot.SetActive(false);}
        void Update(){if(Time.unscaledTime>thoughtUntil||Blocking)thought.text="";if(!Active)return;shown=Mathf.Min(CurrentText.Length,shown+lettersPerSecond*Time.deltaTime);body.text=CurrentText.Substring(0,Mathf.FloorToInt(shown));if(automatic&&TextReady&&(autoDelay-=Time.deltaTime)<=0){lines.Dequeue();Present();}}
        public void SetObjective(string value){Objective=value;objective.text=value;objectiveRoot.SetActive(!string.IsNullOrEmpty(value));}
        public void SetHint(string value){hint.text=Active?"":value;}
        public void SetTarget(Vector3? world,string label)
        {
            ActorTarget=null;marker.gameObject.SetActive(world.HasValue&&!Blocking);if(!world.HasValue)return;
            var p=Camera.main.WorldToViewportPoint(world.Value);float width=Mathf.Max(1,canvasRect.rect.width),height=Mathf.Max(1,canvasRect.rect.height);
            float px=Mathf.Min(.49f,185/width),bottom=Mathf.Min(.32f,180/height),top=Mathf.Min(.2f,90/height);
            bool outside=p.z<0||p.x<px||p.x>1-px||p.y<bottom||p.y>1-top;
            var delta=new Vector2((p.x-.5f)*Camera.main.aspect,p.y-.5f);string arrow=Mathf.Abs(delta.x)>Mathf.Abs(delta.y)?(delta.x>0?"→":"←"):(delta.y>0?"↑":"↓");
            p.x=Mathf.Clamp(p.x,px,1-px);p.y=Mathf.Clamp(p.y,bottom,1-top);markerRect.anchoredPosition=new Vector2((p.x-.5f)*width,(p.y-.5f)*height);marker.text=(outside?arrow+" ":"◇ ")+label;

        }
        public void SetActorTarget(Transform actor,SpriteRenderer visual,string label)
        {
            var head=new Vector3(visual.bounds.center.x,visual.bounds.max.y+.65f,0);var viewport=Camera.main.WorldToViewportPoint(head);bool outside=viewport.x<.06f||viewport.x>.94f||viewport.y<.14f||viewport.y>.92f;
            SetTarget(outside?actor.position:head,label);ActorTarget=actor;ActorTargetOffscreen=outside;
        }
        public void ShowThought(string text,float duration=2.8f){if(Blocking)return;thought.text=text;thoughtUntil=Time.unscaledTime+duration;}
        public void ClearThought(){if(thought)thought.text="";thoughtUntil=0;}
        public Sprite fullHeart,emptyHeart;Image[] hearts;
        public void SetHealth(int remaining){if(health)health.text="";if(hearts==null&&fullHeart){hearts=new Image[3];for(int i=0;i<3;i++){var r=Rect("Health heart "+i,canvasRect,new Vector2(25+i*52,-122),new Vector2(42,38),new Vector2(0,1),new Vector2(0,1));hearts[i]=r.gameObject.AddComponent<Image>();hearts[i].preserveAspect=true;}}if(hearts!=null)for(int i=0;i<hearts.Length;i++){hearts[i].gameObject.SetActive(remaining>=0);hearts[i].sprite=i<remaining?fullHeart:emptyHeart;}}

        public void SetDangerWarning(string text){if(danger)danger.text=Blocking?"":text;}
        public System.Collections.IEnumerator Fade(bool toBlack,string label="")
        {
            fadeLabel.text=toBlack?label:"";float from=fade.color.a,to=toBlack?1:0,t=0;
            while(t<.45f){t+=Time.unscaledDeltaTime;float alpha=Mathf.Lerp(from,to,t/.45f);fade.color=new Color(0,0,0,alpha);fadeLabel.color=new Color(1,1,1,alpha);yield return null;}
            fade.color=new Color(0,0,0,to);if(!toBlack)fadeLabel.text="";
        }
        public void ClearFade(){if(fade)fade.color=Color.clear;if(fadeLabel)fadeLabel.text="";}
        void OnDisable(){if(pilot)pilot.ControlsLocked=false;}
    }
}
