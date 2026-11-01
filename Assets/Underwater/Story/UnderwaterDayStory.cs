using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Abyss.Story
{
    public enum UnderwaterDayStage { Beginning, FishDeath, FollowToSurface, InspectBodies, ReportToMayor, EscortMayor, FinalConversation, Completed }

    public class UnderwaterDayStory:MonoBehaviour
    {
        public FishPilot player;
        public InputActionAsset inputActions;
        public UnderwaterStoryUI ui;
        public StorySwimmer eventFish,mayor;
        public MayorEscort escort;
        public Sprite openEyes,closedEyes;
        public Transform[] bodies;
        public float corpseRiseSpeed=4.2f;
        public Collider2D playerHome;
        public bool NearHome=>playerHome&&Vector2.Distance(player.transform.position,playerHome.ClosestPoint(player.transform.position))<2.4f;
        // These closed-eye source sprites already carry their dead-fish pose. Do not add a second 180-degree turn.
        public static float CorpseAngle(int index)=>index%2==0?-25:25;
        public UnderwaterSecondChapter secondChapter;
        public UnderwaterThirdChapter thirdChapter;
        public Vector2 passingStart,passingEnd,discoveryCenter;
        public float triggerDistance=3.2f,interactDistance=2.4f,finalMayorRadius=3.8f;
        [SerializeField] UnderwaterDayStage stage=UnderwaterDayStage.Beginning;
        [SerializeField] bool[] inspected=new bool[3];
        public UnderwaterDayStage Stage=>stage;
        public int InspectionCount=>inspected.Count(v=>v);
        public bool Complete=>stage==UnderwaterDayStage.Completed;
        public bool HasInspected(int id)=>id>=0&&id<3&&inspected[id];
        public Vector2 Discovery=>discoveryCenter;
        public bool CanFinish=>stage==UnderwaterDayStage.EscortMayor&&Vector2.Distance(player.transform.position,discoveryCenter)<5&&Vector2.Distance(mayor.Position,discoveryCenter)<finalMayorRadius;
        public string InteractionKey=>interact!=null?interact.GetBindingDisplayString(InputBinding.MaskByGroup("Keyboard&Mouse"),InputBinding.DisplayStringOptions.DontIncludeInteractions):"E";
        InputActionAsset runtimeInput;InputAction interact;Vector2 homeStart,mayorStart;Vector3[] bodyStart;bool floated,interactQueued;
        static UnderwaterStoryUI.Line P(string text)=>new(false,text);
        static UnderwaterStoryUI.Line M(string text)=>new(true,text);
        void Start()
        {
            homeStart=player.transform.position;mayorStart=mayor.transform.position;bodyStart=bodies.Select(b=>b.position).ToArray();ui.Bind(player);
            runtimeInput=Instantiate(inputActions);interact=runtimeInput.FindAction("Player/Interact",true);interact.started+=_=>interactQueued=true;interact.Enable();
            foreach(var swimmer in new[]{eventFish,mayor}){if(swimmer.gameObject.activeInHierarchy)Physics2D.IgnoreCollision(swimmer.GetComponent<Collider2D>(),player.GetComponent<Collider2D>());}
            if(eventFish.gameObject.activeInHierarchy)Physics2D.IgnoreCollision(eventFish.GetComponent<Collider2D>(),mayor.GetComponent<Collider2D>());
            ResetStory();
#if UNITY_EDITOR
            if(!(IntegratedCampaign.CampaignFlow.Instance&&IntegratedCampaign.CampaignFlow.Instance.SessionStarted)&&thirdChapter&&thirdChapter.startHereInEditor)thirdChapter.BeginThirdChapter();
            else if(!(IntegratedCampaign.CampaignFlow.Instance&&IntegratedCampaign.CampaignFlow.Instance.SessionStarted)&&secondChapter&&secondChapter.startHereInEditor)secondChapter.BeginSecondChapter();
#endif
        }
        void OnDestroy(){if(player)player.ControlsLocked=false;if(runtimeInput){runtimeInput.Disable();Destroy(runtimeInput);}}
        [ContextMenu("Development/Reset this underwater story")]
        public void ResetStory()
        {
            if(!Application.isPlaying)return;if(thirdChapter)thirdChapter.ResetChapter();if(secondChapter)secondChapter.ResetChapter();StopAllCoroutines();ui.Clear();escort.End();stage=UnderwaterDayStage.Beginning;inspected=new bool[3];floated=false;interactQueued=false;
            eventFish.Stop();eventFish.visual.sprite=openEyes;eventFish.visual.flipX=false;eventFish.visual.transform.localRotation=Quaternion.identity;eventFish.visual.transform.localPosition=Vector3.zero;eventFish.gameObject.SetActive(false);
            mayor.Place(mayorStart);mayor.visual.transform.localRotation=Quaternion.identity;
            for(int i=0;i<bodies.Length;i++){bodies[i].position=bodyStart[i];bodies[i].gameObject.SetActive(false);}
            player.ControlsLocked=false;player.GetComponent<Rigidbody2D>().position=homeStart;player.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;UpdateObjective();ui.Show(new[]{P("Bugün kasaba fazla sessiz…")},false,null,true);
        }
        void Update()
        {
            if(stage==UnderwaterDayStage.Beginning&&Vector2.Distance(player.transform.position,homeStart)>triggerDistance){stage=UnderwaterDayStage.FishDeath;UpdateObjective();StartCoroutine(Death());}
            if(floated)
            {
                var p=eventFish.transform.position;p.y=player.surfaceY-.12f+Mathf.Sin(Time.time*.8f)*.07f;eventFish.GetComponent<Rigidbody2D>().position=p;eventFish.visual.transform.localRotation=Quaternion.Euler(0,0,180+Mathf.Sin(Time.time)*2);eventFish.visual.transform.localPosition=new Vector3(Mathf.Sin(Time.time*1.2f)*.1f,0,0);
            }
            if(stage>=UnderwaterDayStage.FollowToSurface)
                for(int i=0;i<bodies.Length;i++)if(bodies[i].gameObject.activeSelf){var p=bodyStart[i];p.y=player.surfaceY-.12f+Mathf.Sin(Time.time*.8f+i)*.04f;bodies[i].position=p;bodies[i].rotation=Quaternion.Euler(0,0,CorpseAngle(i)+Mathf.Sin(Time.time*.7f+i)*2);bodies[i].GetComponent<SpriteRenderer>().flipY=false;}
            if(stage==UnderwaterDayStage.FollowToSurface&&floated&&Vector2.Distance(player.transform.position,discoveryCenter)<6)
            {
                stage=UnderwaterDayStage.InspectBodies;UpdateObjective();ui.Show(new[]{P("Burada başkaları da var…"),P("Hepsi hareketsiz.")},false,null,true);
            }
            if(CanFinish)BeginFinal();
            ui.InteractionKey=InteractionKey;ui.SetContinuePrompt(false);
            if(thirdChapter&&thirdChapter.Started)thirdChapter.Tick();else if(secondChapter&&secondChapter.Started){secondChapter.Tick();if(secondChapter.Complete&&secondChapter.skipShipForDevelopment&&thirdChapter&&Vector2.Distance(player.transform.position,secondChapter.departurePoint)<secondChapter.departureRadius+1){ui.SetHint("");ui.SetContinuePrompt(true,InteractionKey);}}else UpdateHints();
            if(interactQueued)
            {
                interactQueued=false;if(ui.Active){ui.Advance();return;}
                if(thirdChapter&&thirdChapter.Started)thirdChapter.Interact();else if(secondChapter&&secondChapter.Complete&&thirdChapter)thirdChapter.TryContinue();else if(secondChapter&&secondChapter.Started)secondChapter.Interact();else TryInteractNearest();
            }
        }
        IEnumerator Death()
        {
            eventFish.gameObject.SetActive(true);Physics2D.IgnoreCollision(eventFish.GetComponent<Collider2D>(),player.GetComponent<Collider2D>());Physics2D.IgnoreCollision(eventFish.GetComponent<Collider2D>(),mayor.GetComponent<Collider2D>());eventFish.Place(passingStart);eventFish.visual.sprite=openEyes;eventFish.Animate=false;eventFish.swimSpeed=1.7f;eventFish.Go(passingEnd);
            yield return new WaitForSeconds(2.7f);float t=0;
            while(t<.9f){t+=Time.deltaTime;eventFish.swimSpeed=Mathf.Lerp(1.7f,0,t/.9f);yield return null;}
            eventFish.Stop();eventFish.visual.sprite=closedEyes;ui.Show(new[]{P("Hey… İyi misin?")},false,null,true);
            t=0;while(t<1.4f){t+=Time.deltaTime;eventFish.visual.transform.localRotation=Quaternion.Euler(0,0,Mathf.SmoothStep(0,180,t/1.4f));yield return null;}
            stage=UnderwaterDayStage.FollowToSurface;UpdateObjective();ui.Show(new[]{P("Ne oluyor? Yukarı sürükleniyor!")},false,null,true);
            foreach(var body in bodies)body.gameObject.SetActive(true);
            eventFish.swimSpeed=corpseRiseSpeed;while(!eventFish.Go(new Vector2(discoveryCenter.x,player.surfaceY-.12f))){yield return new WaitForSeconds(1);}
            while(!eventFish.Arrived){eventFish.visual.flipX=false;eventFish.visual.transform.localRotation=Quaternion.Euler(0,0,180+Mathf.Sin(Time.time)*2);eventFish.visual.transform.localPosition=new Vector3(Mathf.Sin(Time.time*1.2f)*.1f,0,0);yield return null;}
            eventFish.Stop();eventFish.Place(new Vector2(discoveryCenter.x,player.surfaceY-.12f));floated=true;
        }
        public void TryInteractNearest()
        {
            if(ui.Active)return;
            if(stage==UnderwaterDayStage.Completed&&NearHome&&secondChapter){secondChapter.RequestHomeEntry();return;}
            if(stage==UnderwaterDayStage.InspectBodies)
            {
                int closest=-1;float distance=interactDistance;
                for(int i=0;i<bodies.Length;i++){float d=Vector2.Distance(player.transform.position,bodies[i].position);if(!inspected[i]&&d<distance){distance=d;closest=i;}}
                if(closest>=0){TryInspect(closest);return;}
            }
            if(Vector2.Distance(player.transform.position,mayor.Position)<interactDistance)TalkToMayor();
        }
        public bool TryInspect(int id)
        {
            if(ui.Active||stage!=UnderwaterDayStage.InspectBodies||id<0||id>=3||inspected[id]||Vector2.Distance(player.transform.position,bodies[id].position)>interactDistance)return false;
            inspected[id]=true;string[] text={"Bu balık da ters dönmüş.","Görünür bir yarası yok.","Aynı yerde bu kadar balık… Bir şeyler yanlış."};
            if(InspectionCount==3){stage=UnderwaterDayStage.ReportToMayor;ui.Show(new[]{P(text[id]),P("Başkana haber vermeliyim.")},true);}else ui.Show(new[]{P(text[id])},true);
            UpdateObjective();return true;
        }
        public void TalkToMayor()
        {
            if(ui.Active||Vector2.Distance(player.transform.position,mayor.Position)>interactDistance||stage==UnderwaterDayStage.Completed||stage==UnderwaterDayStage.FinalConversation)return;
            if(stage!=UnderwaterDayStage.ReportToMayor){ui.Show(new[]{M("Bir şey fark edersen bana haber ver.")},true);return;}
            ui.Show(new[]{P("Yüzeyde ölü balıklar buldum."),P("Birinin yanımda durup yukarı sürüklendiğini gördüm."),M("Bana nerede olduklarını göster.")},true,()=>{stage=UnderwaterDayStage.EscortMayor;escort.Begin();UpdateObjective();});
        }
        void BeginFinal()
        {
            escort.End();stage=UnderwaterDayStage.FinalConversation;UpdateObjective();
            ui.Show(new[]{M("Bu kadar kayıp…"),M("Bilim ekibini buraya göndereceğim."),M("Nedenini öğrenmeden karar vermeyelim."),P("Ben de yardım etmek istiyorum."),M("Şimdilik kasabaya dön. Haber vereceğim.")},true,()=>{stage=UnderwaterDayStage.Completed;player.ControlsLocked=false;UpdateObjective();});
        }
        public void PrepareForSecondChapter()
        {
            StopAllCoroutines();escort.End();stage=UnderwaterDayStage.Completed;inspected=new[]{true,true,true};
            eventFish.Stop();eventFish.Animate=false;eventFish.visual.sprite=closedEyes;eventFish.gameObject.SetActive(true);eventFish.Place(new Vector2(discoveryCenter.x,player.surfaceY-.12f));floated=true;
            foreach(var body in bodies)body.gameObject.SetActive(true);
        }
        void UpdateObjective()
        {
            string objective=stage switch {
                UnderwaterDayStage.Beginning=>"Kasabanın yakınında yüz.",UnderwaterDayStage.FishDeath=>"Yanından geçen balığa bak.",
                UnderwaterDayStage.FollowToSurface=>"Balığı yüzeye kadar takip et.",UnderwaterDayStage.InspectBodies=>$"Çevredeki balıkları incele. {InspectionCount}/3",
                UnderwaterDayStage.ReportToMayor=>"Kasabaya dön ve başkanla konuş.",UnderwaterDayStage.EscortMayor=>"Başkanı yüzeye götür.",
                UnderwaterDayStage.FinalConversation=>"Başkanı dinle.",_=>"Eve dön."};ui.SetObjective(objective);
        }
        void UpdateHints()
        {
            Vector3? target=null;string label="",hint="";
            if(stage==UnderwaterDayStage.FishDeath||stage==UnderwaterDayStage.FollowToSurface){target=eventFish.transform.position;label="Balığı takip et";}
            if(stage==UnderwaterDayStage.InspectBodies){int id=Enumerable.Range(0,3).Where(i=>!inspected[i]).OrderBy(i=>Vector2.Distance(player.transform.position,bodies[i].position)).First();var renderer=bodies[id].GetComponent<SpriteRenderer>();target=new Vector3(renderer.bounds.center.x,renderer.bounds.max.y+.65f,0);label="İncele";if(Vector2.Distance(player.transform.position,bodies[id].position)<interactDistance){label=InteractionKey+" • İncele";hint="";}}
            if(stage==UnderwaterDayStage.ReportToMayor){target=new Vector3(mayor.visual.bounds.center.x,mayor.visual.bounds.max.y+.65f,0);label="Başkan";}
            if(stage==UnderwaterDayStage.EscortMayor){target=escort.Separation>14?mayor.transform.position:(Vector3)discoveryCenter;label=escort.Separation>14?"Başkanı bekle":"Keşif alanı";}
            if(stage<UnderwaterDayStage.FinalConversation&&Vector2.Distance(player.transform.position,mayor.Position)<interactDistance&&hint=="")hint=InteractionKey+" • Başkanla konuş";
            if(stage==UnderwaterDayStage.Completed&&playerHome){target=playerHome.bounds.center;label="Eve dön";if(NearHome){hint=InteractionKey+" • Eve gir";target=null;}}
            ui.SetHint(hint);ui.SetTarget(target,label);
        }
    }
}
