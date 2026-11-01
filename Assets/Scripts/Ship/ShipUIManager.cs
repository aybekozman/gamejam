using UnityEngine;
using UnityEngine.UIElements;

public class ShipUIManager : MonoBehaviour
{
    [Header("Gereksinimler")]
    public Transform shipTransform; // Pusulanın dönmesi için ana gemiyi sürükle

    [Header("Dash Ayarları")]
    public float dashRegenRate = 20f;
    private float currentDash = 100f;
    private bool isDashing = false;

    // Arayüz Elemanları
    private VisualElement healthFill;
    private VisualElement dashFill;
    private VisualElement energyFill;
    private VisualElement xpFill;

    private VisualElement xpBarContainer;
    private VisualElement energyBarContainer;

    private VisualElement compass;
    private Toggle modeToggle;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // UXML'deki isimlere (name="") göre dolgu elemanlarını bul
        healthFill = root.Q<VisualElement>("HealthFill");
        dashFill = root.Q<VisualElement>("DashFill");
        energyFill = root.Q<VisualElement>("EnergyFill");
        xpFill = root.Q<VisualElement>("XPFill");

        // Gizleyip göstermek için ana çerçeveleri bul
        xpBarContainer = root.Q<VisualElement>("XPBarContainer");
        energyBarContainer = root.Q<VisualElement>("EnergyBarContainer");

        compass = root.Q<VisualElement>("Compass");
        modeToggle = root.Q<Toggle>("ModeToggle");

        // Toggle Mantığı: Tikliyse XP göster, değilse Enerji göster
        modeToggle.RegisterValueChangedCallback(evt =>
        {
            bool showXP = evt.newValue;
            xpBarContainer.style.display = showXP ? DisplayStyle.Flex : DisplayStyle.None;
            energyBarContainer.style.display = showXP ? DisplayStyle.None : DisplayStyle.Flex;
        });

        // Başlangıç değerlerini %100 yap
        UpdateBar(healthFill, 100f);
        UpdateBar(dashFill, 100f);
        UpdateBar(energyFill, 100f);
        UpdateBar(xpFill, 0f);
    }

    void Update()
    {
        // 1. Pusula: Geminin Z rotasyonunu pusulaya aktar
        if (shipTransform != null && compass != null)
        {
            compass.style.rotate = new StyleRotate(new Rotate(new Angle(-shipTransform.eulerAngles.z, AngleUnit.Degree)));
        }

        // 2. Dash Bar Yenilenmesi
        if (!isDashing && currentDash < 100f)
        {
            currentDash += dashRegenRate * Time.deltaTime;
            UpdateBar(dashFill, currentDash);
        }
    }

    // Barların genişliğini (Can, XP, Enerji) dışarıdan değiştirmek için kullanacağımız ana metot
    public void UpdateBar(VisualElement fillElement, float percentage)
    {
        if (fillElement != null)
        {
            percentage = Mathf.Clamp(percentage, 0, 100);
            fillElement.style.width = new Length(percentage, LengthUnit.Percent);
        }
    }

    public void ConsumeDash(float cost)
    {
        if (currentDash >= cost)
        {
            currentDash -= cost;
            UpdateBar(dashFill, currentDash);
            isDashing = true;
            Invoke(nameof(StopDashing), 0.5f);
        }
    }

    private void StopDashing() => isDashing = false;
}