using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public sealed class OrbRewardToggleView : MonoBehaviour, IPointerClickHandler
{
    public OrbRewardMode owner;
    public ShipUIManager shipProgress;
    public RectTransform thumb;
    public UnityEngine.UI.Image accent, energyIcon, xpIcon;
    public Sprite energyFill, xpFill;
    public TMP_Text energyLabel,xpLabel;
    [Min(.01f)] public float slideDuration=.16f;
    RectTransform track;
    float targetX;
    void Awake() { track=(RectTransform)transform; }
    void OnEnable()
    {
        if(owner) owner.ModeChanged+=Changed;
        Refresh(true);
    }
    void Start() { Refresh(true); }
    void OnDisable() { if(owner) owner.ModeChanged-=Changed; }
    void Changed(OrbRewardMode.RewardKind mode) { Refresh(false); }
    public void Refresh(bool instant)
    {
        if(!track) track=(RectTransform)transform;
        if(!thumb) return;
        bool left=shipProgress?!shipProgress.isLootingXP:owner && owner.CurrentMode==OrbRewardMode.RewardKind.Energy;
        targetX=left?8:track.rect.width-thumb.rect.width-8;
        if(instant) thumb.anchoredPosition=new Vector2(targetX,0);
        if(accent) accent.sprite=left?energyFill:xpFill;
        var active=new Color(1,.95f,.8f,1);
        var inactive=new Color(.64f,.59f,.48f,1);
        if(energyLabel) energyLabel.color=left?active:inactive;
        if(xpLabel) xpLabel.color=left?inactive:active;
        if(energyIcon) energyIcon.color=left?Color.white:new Color(.65f,.65f,.65f,1);
        if(xpIcon) xpIcon.color=left?new Color(.65f,.65f,.65f,1):Color.white;
    }
    void Update()
    {
        if(!thumb) return;
        if(shipProgress)Refresh(false);
        float distance=Mathf.Max(1,track.rect.width-thumb.rect.width-16);
        thumb.anchoredPosition=new Vector2(Mathf.MoveTowards(thumb.anchoredPosition.x,targetX,distance*Time.deltaTime/slideDuration),0);
    }
    public void OnPointerClick(PointerEventData data)
    {
        if((!owner&&!shipProgress) || Time.timeScale<=0 || data.button!=PointerEventData.InputButton.Left) return;
        if(RectTransformUtility.ScreenPointToLocalPointInRectangle(track,data.position,data.pressEventCamera,out var local))
            {
                if(shipProgress){bool xp=local.x>=track.rect.center.x;if(shipProgress.isLootingXP!=xp)shipProgress.ToggleLootMode();Refresh(false);}
                else owner.TrySelectMode(local.x<track.rect.center.x?OrbRewardMode.RewardKind.Energy:OrbRewardMode.RewardKind.Experience);
            }
    }
}
