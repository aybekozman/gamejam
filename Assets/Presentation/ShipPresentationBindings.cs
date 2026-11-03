using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
// Keeps the visible badge and pixel numbers on the existing XP authority.
public static class ShipPresentationBindings {
 public static void Configure(Scene scene,ShipUIManager ui){if(!ui)return;var texts=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<TMP_Text>(true)).ToArray();var hud=StorySceneJourney.FindIn<ShipHUDController>(scene);var visible=hud?hud.levelText as TextMeshProUGUI:null;if(!visible)visible=texts.OfType<TextMeshProUGUI>().FirstOrDefault(t=>t.name=="LevelValue");if(visible)ui.levelText=visible;var pixel=Resources.Load<TMP_FontAsset>("Voyage/PixelNumbers");if(pixel)foreach(var text in texts){text.font=pixel;text.fontSharedMaterial=pixel.material;text.fontStyle=FontStyles.Normal;}if(ui.levelText)ui.levelText.text=ui.currentLevel.ToString();}
}
