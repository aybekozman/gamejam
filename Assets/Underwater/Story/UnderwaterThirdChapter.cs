using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Abyss.Story
{
    public enum ThirdChapterStage { AwaitingContinue, Preparing, Travel, LeaderReady, Dialogue, Rally, Completed, Respawning }
    public class UnderwaterThirdChapter:MonoBehaviour
    {
        public UnderwaterDayStory story;public UnderwaterSecondChapter second;
        public CannonHazards hazards;public StorySwimmer leader;public StorySwimmer[] guards;
        public Sprite[] leaderSwim,leaderTalk;public SpriteRenderer warningSign;
        public Transform npcRoot;public Vector2 roadStart,leaderRest;public Vector2[] checkpoints,rallyPositions,guardRest;
        public float safeLeaderRadius=14,signReadRadius=10,talkRadius=4.2f,invulnerability=1.3f;
        public UnityEvent chapterCompleted=new();
        [Tooltip("Yalnız Editor: ilk iki bölümü tekrar oynamadan üçüncü bölümden başlat.")]
        public bool startHereInEditor;
        [SerializeField] ThirdChapterStage stage;
        public ThirdChapterStage Stage=>stage;public bool Started=>stage!=ThirdChapterStage.AwaitingContinue;public bool Complete=>stage==ThirdChapterStage.Completed;
        public int Health{get;private set;}=3;public int CheckpointIndex{get;private set;}
        public Vector2 PlayerPosition=>story.player.GetComponent<Rigidbody2D>().position;
        public bool Suspended=>stage!=ThirdChapterStage.Travel&&stage!=ThirdChapterStage.LeaderReady||story.ui.Blocking;
        public bool Protected=>Suspended||IsSafe(PlayerPosition)||Time.time<immuneUntil;
        bool signRead,nearMiss,approach,completionRaised;float immuneUntil,talkClock;Color normalColor=Color.white;
        public bool IsSafe(Vector2 p)
        {
            // Fixed safe columns separate the authored danger sections; the boss/sign never overlap them.
            if(Vector2.Distance(p,leaderRest)<safeLeaderRadius||Mathf.Abs(p.x-warningSign.transform.position.x)<signReadRadius+3)return true;
            foreach(var section in hazards.sections)if(p.x>section.left-1&&p.x<section.right+1)return false;
            return true;
        }
        public void TryContinue()
        {
            if(!Started&&second.Complete&&second.skipShipForDevelopment&&Vector2.Distance(PlayerPosition,second.departurePoint)<second.departureRadius+1&&!story.ui.Active)BeginThirdChapter();
        }
        [ContextMenu("Development/Start third underwater chapter")]
        public void BeginThirdChapter()
        {
            if(!Application.isPlaying||Started)return;stage=ThirdChapterStage.Preparing;StartCoroutine(Prepare());
        }
        IEnumerator Prepare()
        {
            hazards.Halt();story.ui.Clear();story.ui.SetHint("");story.ui.SetTarget(null,"");story.ui.SetObjective("");story.player.ControlsLocked=true;
            yield return story.ui.Fade(true);
            second.PrepareForThirdChapter();story.PrepareForSecondChapter();
            Health=3;CheckpointIndex=0;immuneUntil=Time.time+invulnerability;npcRoot.gameObject.SetActive(true);leader.Place(leaderRest);leader.Animate=true;leader.swimFrames=leaderSwim;for(int i=0;i<guards.Length;i++)guards[i].Place(guardRest[i]);
            var playerBody=story.player.GetComponent<Rigidbody2D>();playerBody.position=roadStart;playerBody.linearVelocity=Vector2.zero;
            var all=new[]{leader,guards[0],guards[1]};for(int i=0;i<all.Length;i++){Physics2D.IgnoreCollision(all[i].GetComponent<Collider2D>(),story.player.GetComponent<Collider2D>());for(int j=0;j<i;j++)Physics2D.IgnoreCollision(all[i].GetComponent<Collider2D>(),all[j].GetComponent<Collider2D>());}
            yield return story.ui.Fade(false);stage=ThirdChapterStage.Travel;story.player.ControlsLocked=false;hazards.Begin();
            story.ui.SetObjective("Köpekbalığı liderine ulaş.");story.ui.QueueTravelDialogue("Yukarıda çatışma başlamış…");StartCoroutine(SecondThought());
        }
        IEnumerator SecondThought(){yield return new WaitForSeconds(2.8f);if(stage==ThirdChapterStage.Travel)story.ui.QueueTravelDialogue("Köpekbalıklarına ulaşmalıyım.");}
        public void Tick()
        {
            story.ui.SetHealth(Started&&!Complete?Health:-1);story.player.visual.color=Time.time<immuneUntil&&Started?Color.Lerp(normalColor,new Color(.5f,.9f,1,.45f),Mathf.PingPong(Time.time*9,1)):normalColor;
            if(stage==ThirdChapterStage.Travel||stage==ThirdChapterStage.LeaderReady)
            {
                if(!signRead&&Vector2.Distance(PlayerPosition,warningSign.bounds.center)<signReadRadius){signRead=true;story.ui.QueueTravelDialogue("Köpekbalıkları bölgesi");StartCoroutine(SignThought());}
                if(!approach&&Vector2.Distance(PlayerPosition,leaderRest)<safeLeaderRadius){approach=true;stage=ThirdChapterStage.LeaderReady;story.ui.QueueTravelDialogue("İşte köpekbalıkları…");story.ui.SetObjective("Köpekbalığı lideriyle konuş.");}
                for(int i=CheckpointIndex+1;i<checkpoints.Length;i++)if(Mathf.Abs(PlayerPosition.x-checkpoints[i].x)<4&&IsSafe(checkpoints[i]))CheckpointIndex=i;
                story.ui.SetActorTarget(leader.transform,leader.visual,"Köpekbalığı Lideri");
                story.ui.SetHint(Vector2.Distance(PlayerPosition,leader.Position)<talkRadius?story.InteractionKey+" ile konuş":"");
                leader.visual.flipX=PlayerPosition.x<leader.Position.x;
            }
            else {story.ui.SetTarget(null,"");story.ui.SetHint("");story.ui.SetDangerWarning("");}
            if(stage==ThirdChapterStage.Dialogue){talkClock+=Time.deltaTime;leader.visual.sprite=leaderTalk[(int)(talkClock*2.2f)%leaderTalk.Length];}
        }
        IEnumerator SignThought(){yield return new WaitForSeconds(2.4f);if(stage==ThirdChapterStage.Travel)story.ui.QueueTravelDialogue("Burada dikkatli olmalıyım.");}
        public void NearMiss(){if(!nearMiss&&!Suspended){nearMiss=true;story.ui.QueueTravelDialogue("Toplar buraya kadar düşüyor!");}}
        public void Interact()
        {
            if((stage!=ThirdChapterStage.Travel&&stage!=ThirdChapterStage.LeaderReady)||story.ui.Active||Vector2.Distance(PlayerPosition,leader.Position)>=talkRadius)return;
            stage=ThirdChapterStage.Dialogue;hazards.Halt();leader.Animate=false;story.ui.ClearThought();story.ui.SetTarget(null,"");story.ui.SetDangerWarning("");story.ui.SetHint("");
            story.ui.Show(DialogueLines(),true,()=>StartCoroutine(Rally()));
        }
        UnderwaterStoryUI.Line L(string text)=>new("Köpekbalığı Lideri",leaderSwim[0],text);
        UnderwaterStoryUI.Line P(string text)=>new("Oyuncu",story.ui.playerPortrait,text,false);
        public UnderwaterStoryUI.Line[] DialogueLines()=>new[]{
            L("Bu gürültü de ne?"),P("Kasabamızda balıklar ölüyor."),P("Bilim ekibi geminin atıklarını araştırdı."),P("Yoğun tuzlu su yaşam alanımıza zarar veriyor."),
            L("Bizim sularda da bir tuhaflık vardı…"),P("Başkan gemiyi durdurmak için saldırı başlattı."),P("Ama küçük balıklar toplara dayanamıyor."),P("Yardımınıza ihtiyacımız var."),
            L("Toplar buraya da ulaştı."),L("Bu çatışma hepimizi tehdit ediyor."),P("Bizimle gelir misiniz?"),L("Yaralıları yalnız bırakmayacağız."),L("Sürüyü toplayın. Kasabaya gidiyoruz."),P("Size yolu göstereceğim.")};
        IEnumerator Rally()
        {
            stage=ThirdChapterStage.Rally;story.player.ControlsLocked=false;hazards.Halt();leader.Animate=true;leader.swimFrames=leaderSwim;
            leader.Go(rallyPositions[0]);yield return new WaitForSeconds(.25f);guards[0].Go(rallyPositions[1]);yield return new WaitForSeconds(.3f);guards[1].Go(rallyPositions[2]);
            while(!leader.Arrived||!guards[0].Arrived||!guards[1].Arrived)yield return null;
            if(completionRaised)yield break;completionRaised=true;stage=ThirdChapterStage.Completed;story.ui.SetObjective("Köpekbalıklarının yardımı sağlandı.");story.ui.SetTarget(null,"");story.ui.SetHealth(-1);story.player.ControlsLocked=false;chapterCompleted.Invoke();
        }
        public bool TakeHit()
        {
            if(Protected)return false;Health--;immuneUntil=Time.time+invulnerability;
            if(Health<=0){stage=ThirdChapterStage.Respawning;hazards.Halt();StartCoroutine(Respawn());}return true;
        }
        IEnumerator Respawn()
        {
            story.ui.Clear();story.ui.ClearThought();story.player.ControlsLocked=true;yield return story.ui.Fade(true);
            var body=story.player.GetComponent<Rigidbody2D>();body.position=checkpoints[CheckpointIndex];body.linearVelocity=Vector2.zero;Health=3;immuneUntil=Time.time+invulnerability;
            yield return story.ui.Fade(false);stage=ThirdChapterStage.Travel;story.player.ControlsLocked=false;hazards.Begin();story.ui.SetObjective("Köpekbalığı liderine ulaş.");
        }
        public void ResetChapter()
        {
            StopAllCoroutines();hazards.Halt();stage=ThirdChapterStage.AwaitingContinue;Health=3;CheckpointIndex=0;signRead=nearMiss=approach=completionRaised=false;immuneUntil=0;
            if(npcRoot)npcRoot.gameObject.SetActive(false);if(story&&story.ui){story.ui.SetHealth(-1);story.ui.ClearThought();story.ui.SetDangerWarning("");}if(story&&story.player)story.player.visual.color=Color.white;
        }
        void OnDisable(){if(hazards)hazards.Halt();if(story&&story.player)story.player.ControlsLocked=false;}
    }
}
