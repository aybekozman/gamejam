using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using TMPro;

public static class OrbRewardToggleSetup
{
    const string Folder="Assets/UI/ShipHUD";
    const string PrefabPath=Folder+"/ShipHUD.prefab";
    [MenuItem("Tools/Ship HUD/Install energy-XP orb selector")]
    public static void Configure()
    {
        PrepareArt();
        var prefab=PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var hud=prefab.GetComponent<ShipHUDController>();
            if(!hud.GetComponentInChildren<OrbRewardToggleView>(true)) BuildView(hud);
            PrefabUtility.SaveAsPrefabAsset(prefab,PrefabPath);
        }
        finally {PrefabUtility.UnloadPrefabContents(prefab);}
        AssetDatabase.ImportAsset(PrefabPath,ImportAssetOptions.ForceSynchronousImport);
        BindScene();
    }
    static void BindScene()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ShipHUDSetup.ScenePath);
        if(!scene.isLoaded)
        {
            bool dirty=false;
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++) dirty|=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty;
            scene=EditorSceneManager.OpenScene(ShipHUDSetup.ScenePath,dirty?OpenSceneMode.Additive:OpenSceneMode.Single);
        }
        var hud=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ShipHUDController>(true)).Single();
        var mode=hud.ship.GetComponent<OrbRewardMode>();
        if(!mode) mode=hud.ship.gameObject.AddComponent<OrbRewardMode>();
        mode.energy=hud.energy; mode.experience=hud.experience;
        var view=hud.GetComponentInChildren<OrbRewardToggleView>(true);
        if(!view) throw new Exception("Orb selector prefab update has not propagated to the scene instance");
        view.owner=mode;
        view.Refresh(true);
        EditorUtility.SetDirty(mode); EditorUtility.SetDirty(view);
        PrefabUtility.RecordPrefabInstancePropertyModifications(view);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Debug.Log("ORB_SELECTOR_CONFIGURED: left Energy, right XP; Q/click; collection-time selection; default XP");
    }
    static void BuildView(ShipHUDController hud)
    {
        var left=hud.safeArea.Find("LeftHUD");
        var track=Rect("OrbRewardSelector",left,new Vector2(300,44),new Vector2(144,-176));
        var background=Image(track,Asset("OrbSelectorTrack"));
        background.type=UnityEngine.UI.Image.Type.Sliced; background.pixelsPerUnitMultiplier=2; background.raycastTarget=true;
        var view=track.gameObject.AddComponent<OrbRewardToggleView>();
        var thumb=Rect("SlidingThumb",track,new Vector2(140,28),new Vector2(152,0));
        thumb.anchorMin=thumb.anchorMax=new Vector2(0,.5f); thumb.pivot=new Vector2(0,.5f);
        var rim=Image(thumb,Asset("OrbSelectorTrack")); rim.type=UnityEngine.UI.Image.Type.Sliced; rim.pixelsPerUnitMultiplier=3.5f;
        view.thumb=thumb;
        view.energyFill=AssetDatabase.LoadAssetAtPath<Sprite>(Folder+"/Sources/Enerji_Dolum.png");
        view.xpFill=AssetDatabase.LoadAssetAtPath<Sprite>(Folder+"/Sources/XP_Dolum.png");
        view.accent=Image(Rect("SelectedColor",thumb,new Vector2(112,16),new Vector2(14,-6)),view.xpFill);
        view.accent.color=new Color(1,1,1,.4f);
        view.energyIcon=Image(Rect("EnergyIcon",track,new Vector2(20,22),new Vector2(29,-11)),Asset("OrbEnergyIcon"));
        view.xpIcon=Image(Rect("ExperienceIcon",track,new Vector2(22,22),new Vector2(180,-11)),Asset("OrbExperienceIcon"));
        view.energyLabel=Label("EnergyLabel",track,"ENERJİ",new Vector2(80,26),new Vector2(50,-9),14,hud.hudFont);
        view.xpLabel=Label("ExperienceLabel",track,"XP",new Vector2(52,26),new Vector2(202,-9),15,hud.hudFont);
        var key=Rect("KeyHint",track,new Vector2(18,20),new Vector2(269,-12));
        var keyFrame=Image(key,Asset("OrbSelectorTrack")); keyFrame.type=UnityEngine.UI.Image.Type.Sliced; keyFrame.pixelsPerUnitMultiplier=8;
        Label("Key",key,"Q",new Vector2(18,20),Vector2.zero,10,hud.hudFont);
        view.Refresh(true);
    }
    static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position)
    {
        var go=new GameObject(name,typeof(RectTransform)); go.layer=5;
        var rect=go.GetComponent<RectTransform>(); rect.SetParent(parent,false);
        rect.anchorMin=rect.anchorMax=new Vector2(0,1); rect.pivot=new Vector2(0,1);
        rect.sizeDelta=size; rect.anchoredPosition=position; return rect;
    }
    static UnityEngine.UI.Image Image(RectTransform rect,Sprite sprite)
    {
        var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.sprite=sprite; image.raycastTarget=false; return image;
    }
    static TMP_Text Label(string name,Transform parent,string text,Vector2 size,Vector2 position,float fontSize,TMP_FontAsset font)
    {
        var label=Rect(name,parent,size,position).gameObject.AddComponent<TextMeshProUGUI>();
        label.font=font; label.fontSharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/HUDText.mat");
        label.text=text; label.fontSize=fontSize; label.alignment=TextAlignmentOptions.Center;
        label.textWrappingMode=TextWrappingModes.NoWrap; label.color=new Color(1,.95f,.8f,1); label.raycastTarget=false;
        return label;
    }
    static Sprite Asset(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(Folder+"/Processed/"+name+".png");
    static Texture2D Read(string name)
    {
        var texture=new Texture2D(2,2); texture.LoadImage(File.ReadAllBytes(Folder+"/Sources/"+name+".png")); return texture;
    }
    static void PrepareArt()
    {
        var source=Read("XP_Cerceve_Bos");
        var track=new Texture2D(520,92,TextureFormat.RGBA32,false);
        for(int y=0;y<92;y++) for(int x=0;x<520;x++)
        {
            int sx=x<48?654-x:x>=472?607+x-472:146+(x-48)%400;
            track.SetPixel(x,y,source.GetPixel(sx,y));
        }
        Write(track,"OrbSelectorTrack",new Vector4(48,20,48,20));
        UnityEngine.Object.DestroyImmediate(source);
        Icon("Enerji_Cerceve_Bos","OrbEnergyIcon");
        Icon("XP_Cerceve_Bos","OrbExperienceIcon");
    }
    static void Icon(string sourceName,string name)
    {
        var source=Read(sourceName);
        var icon=new Texture2D(65,60,TextureFormat.RGBA32,false);
        for(int y=0;y<60;y++) for(int x=0;x<65;x++)
        {
            Color c=source.GetPixel(40+x,source.height-1-(16+y));
            bool keep=c.a>.5f && c.r>.55f && c.g>.32f && c.g-c.b>.08f;
            icon.SetPixel(x,59-y,keep?c:Color.clear);
        }
        Write(icon,name,Vector4.zero); UnityEngine.Object.DestroyImmediate(source);
    }
    static void Write(Texture2D texture,string name,Vector4 border)
    {
        string path=Folder+"/Processed/"+name+".png";
        File.WriteAllBytes(path,texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
        importer.filterMode=FilterMode.Point; importer.mipmapEnabled=false; importer.wrapMode=TextureWrapMode.Clamp;
        importer.textureCompression=TextureImporterCompression.Uncompressed; importer.alphaIsTransparency=true;
        importer.npotScale=TextureImporterNPOTScale.None; importer.spritePixelsPerUnit=100;
        var settings=new TextureImporterSettings(); importer.ReadTextureSettings(settings); settings.spriteMeshType=SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        var factory=new SpriteDataProviderFactories(); factory.Init();
        var provider=factory.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
        var capability=provider.GetDataProvider<ISpriteFrameEditCapability>();
        if(capability==null || !capability.GetEditCapability().HasCapability(EEditCapability.EditPivot) || !capability.GetEditCapability().HasCapability(EEditCapability.EditBorder))
            throw new Exception("Sprite border/pivot editing unsupported: "+path);
        var rects=provider.GetSpriteRects(); foreach(var rect in rects) {rect.pivot=Vector2.one*.5f; rect.alignment=SpriteAlignment.Custom; rect.border=border;}
        provider.SetSpriteRects(rects); provider.Apply(); importer.SaveAndReimport();
    }
}
