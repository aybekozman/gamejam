using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class OceanCrestSetup
{
    [MenuItem("Tools/Ship Water/Configure subtle crest shader")]
    public static void Configure()
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before configuring water.");
        var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/CalmPixelOcean.shader");
        if(!shader || ShaderUtil.ShaderHasError(shader)) throw new Exception("Ocean shader compilation failed.");
        foreach(string path in new[]{"Assets/Scenes/ShipScene.unity","Assets/Scenes/HudFirstTry.unity"})
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
            bool opened=!scene.isLoaded;
            if(opened) scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var water=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<InfiniteOcean>(true)).Single();
                var renderer=water.GetComponent<SpriteRenderer>();
                string materialPath=water.worldSpaceWater?"Assets/Materials/ShipWater/CalmOcean.mat":"Assets/Materials/ShipWater/HUDOceanCrests.mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if(!material) {material=new Material(shader);AssetDatabase.CreateAsset(material,materialPath);}
                material.shader=shader;
                material.SetVector("_CrestMotion",new Vector4(1.25f,1.1f,.14f,6));
                material.SetFloat("_UseSpriteUV",water.worldSpaceWater?0:1);
                renderer.sharedMaterial=material;
                water.crestDisplacementPixels=1.25f; water.crestSpeed=1.1f;
                water.crestShimmer=.14f; water.crestFramesPerSecond=6;
                EditorUtility.SetDirty(water);EditorUtility.SetDirty(renderer);EditorUtility.SetDirty(material);
                AssetDatabase.SaveAssetIfDirty(material);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            finally {if(opened) EditorSceneManager.CloseScene(scene,true);}
        }
        Debug.Log("OCEAN_CREST_SETUP_OK: ShipScene + HudFirstTry; original texture and sprite Animator retained.");
    }
}
