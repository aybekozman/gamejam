using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEngine.TextCore.LowLevel;
public static class VoyageFontSetup {
 public static void Apply(){const string path="Assets/Presentation/Resources/Voyage/PixelNumbers.asset";var existing=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);if(existing&&existing.atlasRenderMode==GlyphRenderMode.SDFAA)return;if(existing)AssetDatabase.DeleteAsset(path);var font=TMP_FontAsset.CreateFontAsset(Resources.Load<Font>("Voyage/PixelifySans"),64,6,GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,false);font.name="PixelNumbers";AssetDatabase.CreateAsset(font,path);string glyphs="";for(int i=32;i<127;i++)glyphs+=(char)i;glyphs+="ÇçĞğİıÖöŞşÜü…–—’“”";font.TryAddCharacters(glyphs,out string missing);font.atlasPopulationMode=AtlasPopulationMode.Static;foreach(var texture in font.atlasTextures){texture.filterMode=FilterMode.Bilinear;AssetDatabase.AddObjectToAsset(texture,font);}font.material.name="PixelNumbers material";AssetDatabase.AddObjectToAsset(font.material,font);EditorUtility.SetDirty(font);AssetDatabase.SaveAssets();}
}
