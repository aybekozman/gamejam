using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Abyss.Story
{
    public enum SecondChapterStage { AwaitingHomeEntry, Preparing, AttendAnnouncement, Announcement, LeaveForSharks, Completed }

    public class UnderwaterSecondChapter : MonoBehaviour
    {
        public UnderwaterDayStory story;
        [InspectorName("Gemi bölümünü geç — geliştirme modu")]
        public bool skipShipForDevelopment=true;
        [Tooltip("Yalnız Editor Play Mode: birinci bölümü oynamadan ikinci bölümden başlatır.")]
        public bool startHereInEditor;
        public Vector2 homeArrival,meetingPoint,departurePoint;
        public float meetingRadius=3,departureRadius=3;
        public Transform participantRoot;
        public StorySwimmer[] scientists,townsfolk;
        public Sprite[] scientistPortraits;
        public Sprite townPortrait;
        public UnityEvent firstChapterDepartureRequested=new();
        public UnityEvent secondChapterCompleted=new();
        [SerializeField] SecondChapterStage stage=SecondChapterStage.AwaitingHomeEntry;
        public SecondChapterStage Stage=>stage;
        public bool Started=>stage!=SecondChapterStage.AwaitingHomeEntry;
        public bool Complete=>stage==SecondChapterStage.Completed;
        bool departureRequested,completionRaised;
        readonly List<Coroutine> departures=new();

        public void RequestHomeEntry()
        {
            if(Started||departureRequested||!story.Complete||!story.NearHome||story.ui.Active)return;
            if(skipShipForDevelopment)BeginSecondChapter();
            else {departureRequested=true;firstChapterDepartureRequested.Invoke();}
        }

        // Future ship-return code can call this same entry point; it loads no scenes.
        [ContextMenu("Development/Start second underwater chapter")]
        public void BeginSecondChapter()
        {
            if(!Application.isPlaying||Started)return;
            stage=SecondChapterStage.Preparing;StartCoroutine(Prepare());
        }

        IEnumerator Prepare()
        {
            story.ui.Clear();story.ui.SetHint("");story.ui.SetTarget(null,"");story.ui.SetObjective("");
            story.player.ControlsLocked=true;
            yield return story.ui.Fade(true,"Bir süre sonra…");
            yield return new WaitForSecondsRealtime(.45f);
            story.PrepareForSecondChapter();
            var body=story.player.GetComponent<Rigidbody2D>();body.position=homeArrival;body.linearVelocity=Vector2.zero;
            story.mayor.Place(meetingPoint);story.mayor.visual.transform.localRotation=Quaternion.identity;
            participantRoot.gameObject.SetActive(true);
            foreach(var actor in scientists)actor.Stop();
            foreach(var actor in townsfolk)actor.Stop();
            Physics2D.SyncTransforms();IgnoreParticipantCollisions();
            yield return story.ui.Fade(false);
            stage=SecondChapterStage.AttendAnnouncement;story.player.ControlsLocked=false;
            story.ui.SetObjective("Başkanın duyurusuna katıl.");
            story.ui.Show(new[]{Player("Herkes meydanda toplanmış…")},false,null,true);
        }

        void IgnoreParticipantCollisions()
        {
            var actors=new List<StorySwimmer>{story.mayor};actors.AddRange(scientists);actors.AddRange(townsfolk);
            for(int i=0;i<actors.Count;i++)
            {
                var collider=actors[i].GetComponent<Collider2D>();Physics2D.IgnoreCollision(collider,story.player.GetComponent<Collider2D>());
                for(int j=0;j<i;j++)Physics2D.IgnoreCollision(collider,actors[j].GetComponent<Collider2D>());
            }
        }

        public void Tick()
        {
            if(stage==SecondChapterStage.AttendAnnouncement)
            {
                story.ui.SetTarget(meetingPoint,"Duyuru alanı");
                story.ui.SetHint(Vector2.Distance(story.player.transform.position,meetingPoint)<meetingRadius?story.InteractionKey+" • Duyuruya katıl":"");
            }
            else if(stage==SecondChapterStage.LeaveForSharks)
            {
                story.ui.SetHint("");story.ui.SetTarget(departurePoint,"Köpekbalıkları yönü");
                if(Vector2.Distance(story.player.transform.position,departurePoint)<departureRadius)Finish();
            }
            else {story.ui.SetHint("");story.ui.SetTarget(null,"");}
        }

        public void Interact()
        {
            if(stage!=SecondChapterStage.AttendAnnouncement||story.ui.Active||Vector2.Distance(story.player.transform.position,meetingPoint)>=meetingRadius)return;
            stage=SecondChapterStage.Announcement;story.ui.SetTarget(null,"");story.ui.SetObjective("Duyuruyu dinle.");
            story.ui.Show(AnnouncementLines(),true,AnnouncementFinished);
        }

        UnderwaterStoryUI.Line Mayor(string text)=>new("Başkan",story.ui.mayorPortrait,text);
        UnderwaterStoryUI.Line Expert(int index,string text)=>new("Bilim Uzmanı "+(index+1),scientistPortraits[index],text);
        UnderwaterStoryUI.Line Player(string text)=>new("Oyuncu",story.ui.playerPortrait,text,false);
        public UnderwaterStoryUI.Line[] AnnouncementLines()=>new[]{
            Mayor("Kayıplarımızın nedenini araştırdık."),Mayor("Bilim ekibi bulgularını paylaşacak."),
            Expert(0,"Yüzeydeki sudan örnekler aldık."),Expert(0,"Tuzluluk, normalin çok üzerinde."),
            Expert(1,"En yoğun bölge, geminin atık boşalttığı yer."),Expert(1,"Gemi deniz suyundan temiz su üretiyor."),Expert(1,"Geriye kalan yoğun tuzlu suyu denize bırakıyor."),
            Expert(0,"Atık yeterince seyrelmiyor."),Expert(0,"Bu koşullar balıkların yaşamını tehdit ediyor."),Expert(0,"Bulgular, ölümleri bu boşaltımla ilişkilendiriyor."),
            new UnderwaterStoryUI.Line("Kasabalı",townPortrait,"Bugün iki balık daha kayboldu!"),
            Mayor("Bekledikçe kayıplarımız artıyor."),Mayor("O gemiyi durdurmalıyız."),
            Expert(1,"Atık durmalı."),Expert(1,"Ama çatışma denizi daha da kötüleştirebilir."),
            Mayor("Daha fazla bekleyemeyiz."),Mayor("Yüzeye çıkıyoruz. Gemiyi durduracağız!"),
            Player("Peki ben ne yapacağım?"),Mayor("Köpekbalıklarına git."),Mayor("Yardımlarına ihtiyacımız var."),Player("Hemen yola çıkıyorum.")
        };

        void AnnouncementFinished()
        {
            if(stage!=SecondChapterStage.Announcement)return;
            stage=SecondChapterStage.LeaveForSharks;story.player.ControlsLocked=false;
            story.ui.SetObjective("Köpekbalıklarına haber vermek için yola çık.");
            // Two small groups, staggered starts and separate lanes; the last citizen stays.
            for(int i=0;i<townsfolk.Length-1;i++)departures.Add(StartCoroutine(Depart(townsfolk[i],i)));
            story.ui.Show(new[]{Player("Onlardan önce yardım bulmalıyım.")},false,null,true);
        }

        IEnumerator Depart(StorySwimmer actor,int index)
        {
            yield return new WaitForSeconds(.7f+(index/2)*1.6f+(index%2)*.45f);
            var idle=actor.GetComponent<MeetingFishIdle>();if(idle)idle.enabled=false;
            actor.visual.transform.localPosition=Vector3.zero;
            Vector2 goal=new(meetingPoint.x-3+index*3,story.player.surfaceY-1.5f-index*.25f);
            // Go uses the same static-collider-aware route planner as the mayor.
            while(!actor.Go(goal))yield return new WaitForSeconds(1);
        }

        void Finish()
        {
            if(stage!=SecondChapterStage.LeaveForSharks||completionRaised)return;
            stage=SecondChapterStage.Completed;completionRaised=true;story.player.ControlsLocked=false;
            story.ui.SetHint("");story.ui.SetTarget(null,"");story.ui.SetObjective("İkinci bölüm tamamlandı.");
            secondChapterCompleted.Invoke();StartCoroutine(ClearCompletion());
        }
        IEnumerator ClearCompletion(){yield return new WaitForSeconds(3);if(Complete)story.ui.SetObjective("");}

        public void PrepareForThirdChapter(){StopAllCoroutines();stage=SecondChapterStage.Completed;completionRaised=true;story.escort.End();}
        public void ResetChapter()
        {
            StopAllCoroutines();departures.Clear();stage=SecondChapterStage.AwaitingHomeEntry;departureRequested=completionRaised=false;
            if(participantRoot)participantRoot.gameObject.SetActive(false);
            if(story&&story.ui)story.ui.ClearFade();
            // Re-entering via the developer reset reuses the same authored actors.
            if(scientists!=null)foreach(var actor in scientists)if(actor)ResetActor(actor);
            if(townsfolk!=null)foreach(var actor in townsfolk)if(actor)ResetActor(actor);
        }
        void ResetActor(StorySwimmer actor){actor.Stop();var idle=actor.GetComponent<MeetingFishIdle>();if(idle){actor.Place(idle.restPosition);idle.enabled=true;actor.visual.transform.localPosition=Vector3.zero;}}
        void OnDisable(){StopAllCoroutines();if(story&&story.player)story.player.ControlsLocked=false;if(story&&story.ui)story.ui.ClearFade();}
    }
}
