using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

// Routes touch selection to the existing upgrade handler even when a legacy HUD
// canvas wins EventSystem raycasts over the UI Toolkit panel.
public sealed class VoyageCardTouch : MonoBehaviour
{
    ShipUIManager ui;
    int owner = -1, pressedCard = -1;
    Vector2 began;
    void Start() { ui = GetComponent<ShipController>().uiManager; }
    int CardAt(Vector2 screen)
    {
        var root = ui.levelUpDocument.rootVisualElement;
        if (root?.panel == null || root.resolvedStyle.display == DisplayStyle.None) return -1;
        var point = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(screen.x, Screen.height - screen.y));
        for (int i = 0; i < 3; i++)
        {
            var card = root.Q<VisualElement>("Card_" + (i + 1)) ?? root.Q<VisualElement>("Card" + i);
            if (card != null && card.resolvedStyle.display != DisplayStyle.None && card.worldBound.Contains(point)) return i;
        }
        return -1;
    }
    void Update()
    {
        if (!ui || !ui.levelUpDocument || Time.timeScale > 0 || VoyagePauseMenu.IsOpen || Touchscreen.current == null)
        { owner = pressedCard = -1; return; }
        foreach (var touch in Touchscreen.current.touches)
        {
            int id = touch.touchId.ReadValue();
            var point = touch.position.ReadValue();
            if (touch.press.wasPressedThisFrame && owner == -1)
            { owner = id; began = point; pressedCard = CardAt(point); }
            if (id != owner || !touch.press.wasReleasedThisFrame) continue;
            int card = CardAt(point);
            if (touch.phase.ReadValue() != UnityEngine.InputSystem.TouchPhase.Canceled && card >= 0 && card == pressedCard && Vector2.Distance(point, began) < 40)
                ui.OnCardClicked(card);
            owner = pressedCard = -1;
        }
    }
}
