using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using TMPro;

public static class ShipHUDSetup
{
    public const string ScenePath = "Assets/Scenes/HudFirstTry.unity";
    const string Folder = "Assets/UI/ShipHUD";
    static TMP_FontAsset font;
    static Material textMaterial;
    [MenuItem("Tools/Ship HUD/Configure HudFirstTry")]
    public static void Configure()
    {
        Directory.CreateDirectory(Folder+"/Processed");
        foreach(var path in Directory.GetFiles(Folder+"/Sources","*.png")) ImportSprite(path);
        PrepareDashIcon();
        string defaultFontPath="Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(defaultFontPath);
        if(!font)
        {
            TMP_PackageResourceImporter.ImportResources(true,false,false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(defaultFontPath);
        }
        if(!font)
        {
            EditorApplication.delayCall+=Configure;
            Debug.Log("TMP essentials import is completing; HUD setup will resume automatically.");
            return;
        }
        textMaterial=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/HUDText.mat");
        if(!textMaterial)
        {
            textMaterial=new Material(font.material);
            textMaterial.SetColor(ShaderUtilities.ID_OutlineColor,new Color(.14f,.07f,.035f,1));
            textMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth,.18f);
            textMaterial.EnableKeyword("OUTLINE_ON");
            AssetDatabase.CreateAsset(textMaterial,Folder+"/HUDText.mat");
        }
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
        if(!scene.isLoaded)
        {
            bool dirty=false;
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++) dirty|=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty;
            scene=EditorSceneManager.OpenScene(ScenePath,dirty?OpenSceneMode.Additive:OpenSceneMode.Single);
        }
        var ship=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ShipController>(true)).Single();
        int playerLayer=LayerMask.NameToLayer("Player");
        if(playerLayer>=0) ship.gameObject.layer=playerLayer;
        var energy=ship.GetComponent<PlayerEnergy>(); if(!energy) energy=ship.gameObject.AddComponent<PlayerEnergy>();
        var xp=ship.GetComponent<PlayerExperience>(); if(!xp) xp=ship.gameObject.AddComponent<PlayerExperience>();
        ship.SetDashCapacity(3); ship.dashCooldown=5f;
        var hud=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ShipHUDController>(true)).FirstOrDefault();
        if(!hud)
        {
            var root=Build();
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
            PrefabUtility.SaveAsPrefabAssetAndConnect(root,Folder+"/ShipHUD.prefab",InteractionMode.AutomatedAction);
            hud=root.GetComponent<ShipHUDController>();
        }
        hud.ship=ship; hud.health=ship.GetComponent<PlayerHealth>(); hud.energy=energy; hud.experience=xp; hud.hudFont=font;
        // Disable the old document only after the new hierarchy and references exist.
        foreach(var legacy in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ShipUIManager>(true)))
        {
            legacy.enabled=false;
            var document=legacy.GetComponent<UnityEngine.UIElements.UIDocument>();
            if(document) document.enabled=false;
            EditorUtility.SetDirty(legacy);
            if(document) EditorUtility.SetDirty(document);
        }
        var eventSystems=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true)).ToArray();
        if(eventSystems.Length==0)
        {
            var events=new GameObject("HUD EventSystem",typeof(UnityEngine.EventSystems.EventSystem),typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(events,scene);
            events.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().AssignDefaultActions();
        }
        EditorUtility.SetDirty(ship); EditorUtility.SetDirty(hud);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        OrbRewardToggleSetup.Configure();
        Debug.Log("HUD_CONFIGURED HudFirstTry; three independent 5s charges; supplied font omitted at user's request");
    }
    static GameObject Build()
    {
        var root=new GameObject("ShipHUDCanvas",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster),typeof(ShipHUDController));
        root.layer=5;
        var canvas=root.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=200; canvas.pixelPerfect=true;
        var scaler=root.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080); scaler.matchWidthOrHeight=.5f;
        var hud=root.GetComponent<ShipHUDController>(); hud.hudFont=font;
        var safe=Rect("ShipHUD",root.transform,Vector2.zero,Vector2.zero);
        safe.anchorMin=Vector2.zero; safe.anchorMax=Vector2.one; safe.offsetMin=safe.offsetMax=Vector2.zero;
        hud.safeArea=safe;
        var left=Rect("LeftHUD",safe,new Vector2(445.5f,186),new Vector2(16,-16));
        left.anchorMin=left.anchorMax=new Vector2(0,1); left.pivot=new Vector2(0,1);
        Picture("Frame",left,Sprite("HUD_Sol_Tum_Degerlere_Hazir"),new Rect(0,0,891,372));
        var level=Rect("LevelBadge",left,new Vector2(70,60),new Vector2(40,-49));
        Label("LevelCaption",level,"Lv",new Rect(0,0,140,48),18);
        hud.levelText=Label("LevelValue",level,"1",new Rect(0,47,140,90),32);
        hud.healthFill=Bar("HealthBar",left,"Can_Dolum",79,out var hp); hud.healthText=hp;
        hud.energyFill=Bar("EnergyBar",left,"Enerji_Dolum",161,out var ep); hud.energyText=ep;
        hud.xpFill=Bar("XPBar",left,"XP_Dolum",248,out var xp); hud.xpText=xp;
        var right=Rect("RightHUD",safe,new Vector2(321,182.5f),new Vector2(-16,-16));
        right.anchorMin=right.anchorMax=Vector2.one; right.pivot=Vector2.one;
        Picture("DashPanel",right,Sprite("Dash_Uc_Hak_Paneli_Bos"),new Rect(0,120,410,170));
        hud.dashFills=new UnityEngine.UI.Image[3];
        var dashSprite=AssetDatabase.LoadAssetAtPath<Sprite>(Folder+"/Processed/DashWaveIcon.png");
        for(int i=0;i<3;i++)
        {
            var slot=Rect("DashSlot_"+(i+1),right,new Vector2(34,32),new Vector2((86+i*89)*.5f,-(120+61)*.5f));
            var dark=Picture("Inactive",slot,dashSprite,new Rect(0,0,68,64)); dark.color=new Color(.12f,.25f,.30f,1);
            var bright=Picture("Recharge",slot,dashSprite,new Rect(0,0,68,64));
            bright.type=UnityEngine.UI.Image.Type.Filled; bright.fillMethod=UnityEngine.UI.Image.FillMethod.Vertical;
            bright.fillOrigin=(int)UnityEngine.UI.Image.OriginVertical.Top; bright.fillAmount=1;
            hud.dashFills[i]=bright;
        }
        var compass=Rect("Compass",right,new Vector2(146,182.5f),new Vector2(175,0));
        Picture("CompassBase",compass,Sprite("Pusula_Ibresiz_Taban"),new Rect(0,0,292,365));
        var needle=Picture("CompassNeedle",compass,Sprite("Pusula_Donen_Ibre"),new Rect(84,132,116,116));
        needle.rectTransform.pivot=Vector2.one*.5f;
        needle.rectTransform.anchoredPosition=new Vector2((84+58)*.5f,-(132+58)*.5f);
        hud.compassNeedle=needle.rectTransform;
        return root;
    }
    static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Folder+"/Sources/"+name+".png");
    static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position)
    {
        var go=new GameObject(name,typeof(RectTransform)); go.layer=5;
        var rect=go.GetComponent<RectTransform>(); rect.SetParent(parent,false);
        rect.anchorMin=rect.anchorMax=new Vector2(0,1); rect.pivot=new Vector2(0,1);
        rect.sizeDelta=size; rect.anchoredPosition=position;
        return rect;
    }
    static UnityEngine.UI.Image Picture(string name,Transform parent,Sprite sprite,Rect sourceRect)
    {
        var rect=Rect(name,parent,sourceRect.size*.5f,new Vector2(sourceRect.x,-sourceRect.y)*.5f);
        var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.sprite=sprite;
        image.raycastTarget=false;
        return image;
    }
    static TMP_Text Label(string name,Transform parent,string content,Rect sourceRect,float size)
    {
        var rect=Rect(name,parent,sourceRect.size*.5f,new Vector2(sourceRect.x,-sourceRect.y)*.5f);
        var text=rect.gameObject.AddComponent<TextMeshProUGUI>(); text.font=font; text.fontSharedMaterial=textMaterial;
        text.text=content; text.fontSize=size; text.alignment=TextAlignmentOptions.Center;
        text.color=new Color(1,.94f,.77f,1); text.raycastTarget=false; text.textWrappingMode=TextWrappingModes.NoWrap;
        text.margin=Vector4.zero;
        return text;
    }
    static UnityEngine.UI.Image Bar(string name,Transform parent,string sprite,int sourceY,out TMP_Text label)
    {
        var container=Rect(name,parent,new Vector2(238,19.5f),new Vector2(369*.5f,-sourceY*.5f));
        var fill=Picture("Fill",container,Sprite(sprite),new Rect(0,0,476,39));
        fill.type=UnityEngine.UI.Image.Type.Filled; fill.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;
        fill.fillOrigin=(int)UnityEngine.UI.Image.OriginHorizontal.Left; fill.fillAmount=1;
        label=Label("ValueText",container,"100 / 100",new Rect(0,-3,476,45),18);
        return fill;
    }
    static void PrepareDashIcon()
    {
        var source=new Texture2D(2,2); source.LoadImage(File.ReadAllBytes(Folder+"/Sources/Nautical Pixel HUD Interface.png"));
        var result=new Texture2D(68,64,TextureFormat.RGBA32,false);
        for(int y=0;y<64;y++) for(int x=0;x<68;x++)
        {
            Color c=source.GetPixel(1620+x,source.height-1-(184+y));
            bool keep=c.a>.5f && ((c.b>.49f && c.g>.41f && c.b-c.r>.02f) || (c.g>.71f && c.b>.71f && c.r>.67f));
            if(!keep) c=Color.clear;
            result.SetPixel(x,63-y,c);
        }
        // Tight alpha bounds make progress=0 start at the icon silhouette itself,
        // rather than spending the first fraction of recharge on transparent padding.
        int minX=68,minY=64,maxX=-1,maxY=-1;
        for(int y=0;y<64;y++) for(int x=0;x<68;x++) if(result.GetPixel(x,y).a>.1f)
        { minX=Math.Min(minX,x); minY=Math.Min(minY,y); maxX=Math.Max(maxX,x); maxY=Math.Max(maxY,y); }
        if(maxX<minX) throw new Exception("No blue dash icon found in the supplied reference");
        var trimmed=new Texture2D(maxX-minX+1,maxY-minY+1,TextureFormat.RGBA32,false);
        trimmed.SetPixels(result.GetPixels(minX,minY,trimmed.width,trimmed.height)); trimmed.Apply();
        UnityEngine.Object.DestroyImmediate(result); result=trimmed;
        string path=Folder+"/Processed/DashWaveIcon.png";
        File.WriteAllBytes(path,result.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(result); UnityEngine.Object.DestroyImmediate(source);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport); ImportSprite(path);
    }
    static void ImportSprite(string path)
    {
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=100; importer.filterMode=FilterMode.Point; importer.mipmapEnabled=false;
        importer.textureCompression=TextureImporterCompression.Uncompressed; importer.wrapMode=TextureWrapMode.Clamp;
        importer.alphaIsTransparency=true; importer.npotScale=TextureImporterNPOTScale.None; importer.maxTextureSize=4096;
        var settings=new TextureImporterSettings(); importer.ReadTextureSettings(settings); settings.spriteMeshType=SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings); importer.SaveAndReimport();
        var factory=new SpriteDataProviderFactories(); factory.Init();
        var provider=factory.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
        var capability=provider.GetDataProvider<ISpriteFrameEditCapability>();
        if(capability==null || !capability.GetEditCapability().HasCapability(EEditCapability.EditPivot)) throw new Exception("Sprite pivot editing unsupported: "+path);
        var rects=provider.GetSpriteRects(); foreach(var rect in rects) { rect.pivot=Vector2.one*.5f; rect.alignment=SpriteAlignment.Custom; }
        provider.SetSpriteRects(rects); provider.Apply(); importer.SaveAndReimport();
    }
}
