using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>Reproducible, ship-scene-only preparation. Never imports or changes source sheets.</summary>
public static class ShipOceanSetup
{
    const string Output = "Assets/Sprites/ShipWaterProcessed";
    const string ScenePath = "Assets/Scenes/ShipScene.unity";
    [MenuItem("Tools/Ship Water/Rebuild processed assets and configure ShipScene")]
    public static void Configure()
    {
        Directory.CreateDirectory(Output);
        var ocean = Read("Assets/Sprites/OkyanusAçık_12_Kare_4x3_Spritesheet.png");
        // Mirror a reduced 4:3 tile in both axes. Opposing boundary pixels match exactly.
        // No cross-fade haze and no edit to the user's source texture.
        var tile = new Texture2D(512, 384, TextureFormat.RGBA32, false);
        for (int y = 0; y < 384; y++) for (int x = 0; x < 512; x++)
        {
            int sx = x < 256 ? x : 511 - x;
            int sy = y < 192 ? y : 383 - y;
            var c = ocean.GetPixel(Mathf.FloorToInt((sx + .5f) * ocean.width / 256), Mathf.FloorToInt((sy + .5f) * ocean.height / 192));
            c = Color.Lerp(c, new Color(.32f, .77f, .76f), .22f); c.a = 1;
            tile.SetPixel(x, y, c);
        }
        Write(tile, "Ocean_Seamless.png", Vector2.one * .5f, 12.8f, true);
        UnityEngine.Object.DestroyImmediate(ocean);
        var sides = Read("Assets/Sprites/Piksel_tekne_yan_kopuk_spritelar-2.png");
        // Actual visible columns are offset: these are deliberately not equal-grid slices.
        int[] columns = { 175, 510, 850, 1190 };
        for (int i = 0; i < 4; i++)
        {
            Extract(sides, new RectInt(columns[i], 20, 170, 475), "Left_" + i, false, false);
            Extract(sides, new RectInt(columns[i], 525, 170, 465), "Right_" + i, true, false);
        }
        UnityEngine.Object.DestroyImmediate(sides);
        var wakes = Read("Assets/Sprites/Piksel_sanat_gemi_kopuk_izi-1.png");
        for (int i = 0; i < 4; i++)
            Extract(wakes, new RectInt(i % 2 == 0 ? 240 : 890, i < 2 ? 10 : 525, 410, 475), "Wake_" + i, false, true);
        UnityEngine.Object.DestroyImmediate(wakes);
        var stern = Read("Assets/Sprites/ShipWaterGenerated/SternFoam_Sheet_v1.png");
        for (int i = 0; i < 4; i++) ExtractGenerated(stern, i);
        UnityEngine.Object.DestroyImmediate(stern);
        AssetDatabase.Refresh();
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
        if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        var water = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<InfiniteOcean>(true)).FirstOrDefault();
        if (!water || water.gameObject.scene != scene) throw new Exception("ShipScene ocean missing");
        var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/CalmPixelOcean.shader");
        if (!shader || ShaderUtil.ShaderHasError(shader)) throw new Exception("Ocean shader did not compile");
        Directory.CreateDirectory("Assets/Materials/ShipWater");
        var waterMaterial = MaterialAsset("Assets/Materials/ShipWater/CalmOcean.mat", shader);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Output + "/Ocean_Seamless.png");
        waterMaterial.SetTexture("_OceanTex", texture);
        waterMaterial.SetVector("_RepeatSize", new Vector4(80, 60, 0, 0));
        waterMaterial.SetVector("_Flow", new Vector4(.12f, .03f, .65f, .8f));
        var renderer = water.GetComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Output + "/Ocean_Seamless.png");
        renderer.drawMode = SpriteDrawMode.Simple;
        renderer.sharedMaterial = waterMaterial;
        renderer.sortingOrder = -100;
        renderer.color = Color.white;
        var animator = water.GetComponent<Animator>();
        if (animator) animator.enabled = false;
        water.worldSpaceWater = true;
        water.oceanTexture = texture;
        water.repeatSize = new Vector2(80, 60);
        water.currentSpeed = .12f;
        water.currentDirection = new Vector2(1, .25f);
        water.waveStrength = .65f;
        water.waveSpeed = .8f;
        water.tint = Color.white;
        var ship = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ShipController>(true)).FirstOrDefault();
        if (!ship || ship.gameObject.scene != scene) throw new Exception("ShipScene controller missing");
        var foam = ship.GetComponent<ShipMovementFoam>();
        if (!foam) foam = ship.gameObject.AddComponent<ShipMovementFoam>();
        foam.movement = ship;
        foam.body = ship.GetComponent<Rigidbody2D>();
        foam.leftAnchor = Anchor(ship.transform, "Foam Port", new Vector3(-2.25f, -.15f, 0));
        foam.rightAnchor = Anchor(ship.transform, "Foam Starboard", new Vector3(2.25f, -.15f, 0));
        foam.sternAnchor = Anchor(ship.transform, "Foam Stern", new Vector3(0, -6.8f, 0));
        foam.bowAnchor = Anchor(ship.transform, "Foam Bow", new Vector3(0, 7.05f, 0));
        foam.leftFrames = Frames("Left"); foam.rightFrames = Frames("Right");
        foam.bowFrames = Frames("Wake");
        foam.wakeFrames = Frames("Stern");
        foam.bowScale = new Vector2(.48f, .24f);
        foam.foamMaterial = MaterialAsset("Assets/Materials/ShipWater/Foam.mat", Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
        foam.sideScale = Vector2.one;
        foam.wakeScale = new Vector2(.8f, .55f);
        foam.sortingOrder = -20;
        EditorUtility.SetDirty(water); EditorUtility.SetDirty(foam);
        EditorUtility.SetDirty(waterMaterial);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssetIfDirty(waterMaterial);
        AssetDatabase.SaveAssetIfDirty(foam.foamMaterial);
        ValidateAssets();
        ConfigureSuppliedWaves();
        Debug.Log("SHIP_WATER_SETUP_OK");
    }
    [MenuItem("Tools/Ship Water/Update supplied bow, stern and surface waves")]
    public static void ConfigureSuppliedWaves()
    {
        Directory.CreateDirectory(Output + "/UserWaves");
        var surface = CleanSheet("Assets/Sprites/ShipWaterSources/Sakin_okyanus_dalgack_sprite_sayfas-1.png", "Surface_CleanSheet.png", false);
        for (int row = 0; row < 3; row++) for (int frame = 0; frame < 4; frame++)
            SliceCell(surface, 4, 3, frame, row, "Surface_" + row + "_" + frame, 96, 86, new Vector2(.5f,.5f), 32, false);
        UnityEngine.Object.DestroyImmediate(surface);
        var shipSheet = CleanSheet("Assets/Sprites/ShipWaterSources/Piksel_denizde_pruva_dalgas_ve_kc_izi.png", "BowStern_CleanSheet.png", true);
        for (int frame = 0; frame < 4; frame++)
        {
            SliceCell(shipSheet, 4, 2, frame, 0, "Bow_" + frame, 112, 112, new Vector2(.5f,.8f), 20, true);
            SliceCell(shipSheet, 4, 2, frame, 1, "Arc_" + frame, 112, 112, new Vector2(.5f,.76f), 20, false);
        }
        UnityEngine.Object.DestroyImmediate(shipSheet);
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
        if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        var foam = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ShipMovementFoam>(true)).First();
        foam.bowFrames = UserFrames("Bow");
        foam.sternArcFrames = UserFrames("Arc");
        // Exact V apex at the visible bow tip; arms point back along the hull.
        foam.bowAnchor.localPosition = new Vector3(0, 7.15f, 0);
        foam.bowScale = new Vector2(1.65f, 1.05f);
        foam.sternArcScale = new Vector2(1.05f, 1.15f);
        var root = scene.GetRootGameObjects().FirstOrDefault(r => r.name == "Surface Wavelets");
        if (!root)
        {
            root = new GameObject("Surface Wavelets");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
        }
        var waves = root.GetComponent<OceanSurfaceWavelets>();
        if (!waves) waves = root.AddComponent<OceanSurfaceWavelets>();
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        root.transform.localScale = Vector3.one;
        waves.view = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>()).First(c => c.CompareTag("MainCamera"));
        waves.shipHull = foam.GetComponentsInChildren<SpriteRenderer>(true).First(r => r.sprite && r.gameObject.name.StartsWith("Topsuz"));
        waves.material = foam.foamMaterial;
        waves.types = new OceanSurfaceWavelets.WaveType[3];
        for (int row = 0; row < 3; row++) waves.types[row] = new OceanSurfaceWavelets.WaveType { frames = UserFrames("Surface_" + row) };
        waves.activeLimit = 8;
        waves.fps = 5;
        waves.opacity = .32f;
        waves.minimumDistance = 6;
        waves.spawnInterval = .075f;
        EditorUtility.SetDirty(foam); EditorUtility.SetDirty(waves);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("SUPPLIED_WAVES_CONFIGURED: 3x4 ambient frames, 4 bow + 4 stern arcs; source files untouched");
    }
    static Sprite[] UserFrames(string prefix) => Enumerable.Range(0,4).Select(i => AssetDatabase.LoadAssetAtPath<Sprite>(Output + "/UserWaves/" + prefix + "_" + i + ".png")).ToArray();
    static Texture2D CleanSheet(string path, string name, bool ship)
    {
        var source = Read(path);
        var pixels = source.GetPixels32();
        var clean = new Color32[pixels.Length];
        int zero = 0, partial = 0, kept = 0;
        for (int y = 0; y < source.height; y++) for (int x = 0; x < source.width; x++)
        {
            int index = y * source.width + x;
            Color32 c = pixels[index];
            if (c.a == 0) zero++; else if (c.a < 255) partial++;
            bool white = c.a > 90 && c.r > 175 && c.g > 202 && c.b > 165;
            bool cyan = c.a > 60 && c.g > (ship ? 135 : 160) && c.b > (ship ? 130 : 150) && (c.g - c.r > 20 || c.b-c.r > 25);
            bool sharp = false;
            if (cyan && !white)
                for (int offset = -4; offset <= 4 && !sharp; offset += 2)
                {
                    var a = pixels[y * source.width + Mathf.Clamp(x+offset,0,source.width-1)];
                    var b = pixels[Mathf.Clamp(y+offset,0,source.height-1)*source.width+x];
                    sharp = Math.Abs(c.r-a.r)>40 || Math.Abs(c.g-a.g)>40 || Math.Abs(c.b-a.b)>40 ||
                        Math.Abs(c.r-b.r)>40 || Math.Abs(c.g-b.g)>40 || Math.Abs(c.b-b.b)>40;
                }
            if (white || (cyan && sharp)) { c.a = 255; clean[index] = c; kept++; }
        }
        var result = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        result.SetPixels32(clean); result.Apply();
        var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        copy.SetPixels32(clean); copy.Apply();
        Write(copy, "UserWaves/" + name, Vector2.one*.5f,32,false);
        Debug.Log($"ALPHA_AUDIT {path}: {source.width}x{source.height}, transparent={zero}, partial={partial}, retained sharp pixels={kept}; output uses only alpha 0/255");
        UnityEngine.Object.DestroyImmediate(source);
        return result;
    }
    static void SliceCell(Texture2D source, int columns, int rows, int column, int row, string name,
        int width, int height, Vector2 pivot, float ppu, bool alignApex)
    {
        float cellW = source.width / (float)columns, cellH = source.height / (float)rows;
        float left = column*cellW, top = row*cellH;
        Vector2 offset = Vector2.zero;
        if (alignApex)
        {
            // Only the attachment shifts, never the frame scale: pin each V tip to its pivot.
            bool found = false;
            for (int y = 0; y < cellH*.4f && !found; y++)
            {
                float sumX = 0; int count = 0;
                for (int x = (int)(cellW*.4f); x < cellW*.6f; x++)
                    if (source.GetPixel((int)left+x, source.height-1-(int)top-y).a > .5f) { sumX += x; count++; }
                if (count > 0) { offset = new Vector2(sumX/count-cellW*pivot.x, y-cellH*(1-pivot.y)); found = true; }
            }
            if (!found) throw new Exception("No bow apex in frame " + name);
        }
        var result = new Texture2D(width,height,TextureFormat.RGBA32,false);
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
        {
            int x0 = Mathf.FloorToInt(left+x*cellW/width+offset.x);
            int x1 = Mathf.CeilToInt(left+(x+1)*cellW/width+offset.x);
            int y0 = Mathf.FloorToInt(top+y*cellH/height+offset.y);
            int y1 = Mathf.CeilToInt(top+(y+1)*cellH/height+offset.y);
            Color best = Color.clear;
            for (int sy = y0; sy < y1; sy++) for (int sx = x0; sx < x1; sx++)
            {
                if (sx < left || sx >= left+cellW || sy < top || sy >= top+cellH) continue;
                Color c = source.GetPixel(sx,source.height-1-sy);
                if (c.a>.5f && c.g>best.g) best=c;
            }
            result.SetPixel(x,height-1-y,best);
        }
        Write(result,"UserWaves/"+name+".png",pivot,ppu,false);
    }
    static Transform Anchor(Transform ship, string name, Vector3 position)
    {
        var anchor = ship.Find(name);
        if (!anchor) { anchor = new GameObject(name).transform; anchor.SetParent(ship, false); }
        anchor.localPosition = position;
        return anchor;
    }
    static Material MaterialAsset(string path, Shader shader)
    {
        if (!shader) throw new Exception("Required shader missing: " + path);
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
        mat.shader = shader;
        return mat;
    }
    static Sprite[] Frames(string prefix) => Enumerable.Range(0, 4).Select(i => AssetDatabase.LoadAssetAtPath<Sprite>(Output + "/" + prefix + "_" + i + ".png")).ToArray();
    static Texture2D Read(string path)
    {
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(File.ReadAllBytes(path))) throw new Exception("Cannot read " + path);
        return tex;
    }
    static bool IsFoam(Color c)
    {
        // Warm white foam or saturated cyan water strokes. Neutral blurred glow is excluded.
        return (c.r > .80f && c.g > .83f && c.b > .72f) ||
            (c.g > .47f && c.b > .58f && c.b - c.r > .23f && c.b - c.g > .025f);
    }
    static void ExtractGenerated(Texture2D source, int index)
    {
        int cellW = source.width / 2, cellH = source.height / 2;
        int originX = index % 2 * cellW;
        int originY = (1 - index / 2) * cellH;
        int minX = cellW, minY = cellH, maxX = 0, maxY = 0;
        for (int y = 0; y < cellH; y++) for (int x = 0; x < cellW; x++)
            if (source.GetPixel(originX + x, originY + y).a > .18f)
            { minX = Math.Min(minX,x); minY = Math.Min(minY,y); maxX = Math.Max(maxX,x); maxY = Math.Max(maxY,y); }
        if (maxX <= minX || maxY <= minY) throw new Exception("Generated stern frame empty");
        var result = new Texture2D(96, 192, TextureFormat.RGBA32, false);
        for (int y = 0; y < 192; y++) for (int x = 0; x < 96; x++)
        {
            float u = (x - 1f) / 94, v = (y - 1f) / 190;
            Color c = Color.clear;
            if (x > 0 && x < 95 && y > 0 && y < 191)
                c = source.GetPixel(originX + minX + Mathf.RoundToInt(u * (maxX - minX)), originY + minY + Mathf.RoundToInt(v * (maxY - minY)));
            result.SetPixel(x, y, c); // Preserve generated RGBA, including internal transparent holes.
        }
        Write(result, "Stern_" + index + ".png", new Vector2(.5f,.98f), 24, false);
    }
    static bool IsFoamPixel(Texture2D source, int x, int topY)
    {
        int y = source.height - 1 - topY;
        Color c = source.GetPixel(x, y);
        if (!IsFoam(c)) return false;
        if (c.r > .80f && c.g > .83f && c.b > .72f) return true;
        // Cyan haze varies smoothly; retain cyan only near the hard pixel-art strokes.
        // The six-source-pixel neighbourhood also keeps the interiors of flat colour blocks.
        for (int offset = -6; offset <= 6; offset += 2)
        {
            Color a = source.GetPixel(Mathf.Clamp(x + offset, 0, source.width - 1), y);
            Color b = source.GetPixel(x, Mathf.Clamp(y + offset, 0, source.height - 1));
            if (Mathf.Abs(c.r-a.r) > .16f || Mathf.Abs(c.g-a.g) > .16f || Mathf.Abs(c.b-a.b) > .16f ||
                Mathf.Abs(c.r-b.r) > .16f || Mathf.Abs(c.g-b.g) > .16f || Mathf.Abs(c.b-b.b) > .16f) return true;
        }
        return false;
    }
    static void Extract(Texture2D source, RectInt region, string name, bool right, bool wake)
    {
        // Detect the visible art in each hand-selected region instead of assuming uniform cells.
        int minX = region.xMax, maxX = region.x, minY = region.yMax, maxY = region.y;
        for (int y = region.y; y < region.yMax; y++) for (int x = region.x; x < region.xMax; x++)
            if (IsFoamPixel(source, x, y))
            { minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); minY = Math.Min(minY, y); maxY = Math.Max(maxY, y); }
        if (maxX <= minX || maxY <= minY) throw new Exception("Empty foam frame: " + name);
        int width = wake ? 160 : 64, height = wake ? 192 : 224;
        var result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
        {
            // Max coverage in each destination footprint preserves tiny detached foam pixels.
            int x0 = minX + Mathf.FloorToInt(x * (maxX - minX + 1f) / width);
            int x1 = minX + Mathf.CeilToInt((x + 1) * (maxX - minX + 1f) / width);
            int y0 = minY + Mathf.FloorToInt(y * (maxY - minY + 1f) / height);
            int y1 = minY + Mathf.CeilToInt((y + 1) * (maxY - minY + 1f) / height);
            Color best = Color.clear;
            for (int sy = y0; sy < y1; sy++) for (int sx = x0; sx < x1; sx++)
            {
                var c = source.GetPixel(sx, source.height - 1 - sy);
                if (IsFoamPixel(source, sx, sy) && c.g > best.g) { best = c; best.a = 1; }
            }
            // First/last transparent border prevents clamp edge extrusion.
            if (x == 0 || y == 0 || x == width - 1 || y == height - 1) best = Color.clear;
            result.SetPixel(x, height - 1 - y, best);
        }
        Write(result, name + ".png", wake ? new Vector2(.5f, .98f) : new Vector2(right ? .42f : .58f, .5f), 20, false);
    }
    static void Write(Texture2D tex, string name, Vector2 pivot, float ppu, bool repeat)
    {
        string path = Output + "/" + name;
        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu;
        var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.alphaIsTransparency = !repeat;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
        var factory = new SpriteDataProviderFactories(); factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        if (provider == null) throw new Exception("Sprite provider unavailable: " + path);
        provider.InitSpriteEditorDataProvider();
        var edit = provider.GetDataProvider<ISpriteFrameEditCapability>();
        if (edit == null || !edit.GetEditCapability().HasCapability(EEditCapability.EditPivot))
            throw new Exception("Importer does not support pivot editing: " + path);
        var rects = provider.GetSpriteRects();
        foreach (var rect in rects) { rect.alignment = SpriteAlignment.Custom; rect.pivot = pivot; }
        provider.SetSpriteRects(rects);
        provider.Apply();
        importer.SaveAndReimport();
    }
    static void ValidateAssets()
    {
        foreach (var path in Directory.GetFiles(Output, "*.png"))
        {
            var t = Read(path);
            var pixels = t.GetPixels32();
            bool ocean = path.Contains("Ocean_");
            if (!ocean && (!pixels.Any(p => p.a == 0) || !pixels.Any(p => p.a == 255))) throw new Exception("Invalid real alpha: " + path);
            if (ocean)
            {
                for (int x = 0; x < t.width; x++) if (t.GetPixel(x, 0) != t.GetPixel(x, t.height - 1)) throw new Exception("Vertical tile seam");
                for (int y = 0; y < t.height; y++) if (t.GetPixel(0, y) != t.GetPixel(t.width - 1, y)) throw new Exception("Horizontal tile seam");
            }
            UnityEngine.Object.DestroyImmediate(t);
        }
    }
}
