using System.Collections.Generic;
using DG.Tweening;
using PreferenceEnums;
using UnityEngine;
using UnityEngine.UI;

public class MenuPresenter : MonoBehaviour
{
    [SerializeField] private Button menuButton;
    [SerializeField] private RectTransform listTransform;
    [SerializeField] private Vector3 listOpenPosition;
    [SerializeField] private Vector3 listClosedPosition;
    [SerializeField] private float listTweenDuration;
    [SerializeField] private OptionEnumView optionEnumViewInstance;
    [SerializeField] private Text headerTemplate;
    [SerializeField] private GameObject customizeJoystickButtonGameObject;
    [SerializeField] private GameObject loginButtonGameObject;
    [SerializeField] private GameObject showConsoleButtonGameObject;
    [SerializeField] private ClientRunner clientRunner;

    private readonly List<OptionEnumView> optionEnumViews = new List<OptionEnumView>();
    
    private bool menuOpened;

    void Awake()
    {
        menuButton.onClick.AddListener(OnMenuButtonClicked);
        
        //Only show login button when UO client is running and we're in the login scene
        loginButtonGameObject.transform.SetAsFirstSibling();
        loginButtonGameObject.SetActive(false);

        AddHeader("MobileUO Menu");
        GetOptionEnumViewInstance().Initialize(typeof(ShowCloseButtons), UserPreferences.ShowCloseButtons, "Close Buttons", false, false);
        GetOptionEnumViewInstance().Initialize(typeof(ShowModifierKeyButtons), UserPreferences.ShowModifierKeyButtons, "Show Modifier Key Buttons", false, false);
        GetOptionEnumViewInstance().Initialize(typeof(EnableAssistant), UserPreferences.EnableAssistant, "Enable Assistant", false, false);

        AddHeader("Graphics");
        GetOptionEnumViewInstance().Initialize(typeof(ScaleSizes), UserPreferences.ScaleSize, "View Scale", true, true);
        GetOptionEnumViewInstance().Initialize(typeof(EnlargeSmallButtons), UserPreferences.EnlargeSmallButtons, "Enlarge Small Buttons", false, false);
        GetOptionEnumViewInstance().Initialize(typeof(TargetFrameRates), UserPreferences.TargetFrameRate, "Target Frame Rate", false, false);
        GetOptionEnumViewInstance().Initialize(typeof(ForceUseXbr), UserPreferences.ForceUseXbr, "Force Use Xbr", false, false);
        GetOptionEnumViewInstance().Initialize(typeof(TextureFilterMode), UserPreferences.TextureFiltering, "Texture Filtering", false, false);

        AddHeader("Input");
        GetOptionEnumViewInstance().Initialize(typeof(ContainerItemSelection), UserPreferences.ContainerItemSelection, "Container Item Selection", false, false);
        GetOptionEnumViewInstance().Initialize(typeof(VisualizeFingerInput), UserPreferences.VisualizeFingerInput, "Visualize Finger Input", false, false);
        GetOptionEnumViewInstance().Initialize(typeof(UseMouseOnMobile), UserPreferences.UseMouseOnMobile, "Use Mouse", false, false);
        GetOptionEnumViewInstance().Initialize(typeof(DisableTouchscreenKeyboardOnMobile), UserPreferences.DisableTouchscreenKeyboardOnMobile, "Disable Touchscreen Keyboard", false, false);
        GetOptionEnumViewInstance().Initialize(typeof(JoystickSizes), UserPreferences.JoystickSize, "Joystick Size", false, false);
        GetOptionEnumViewInstance().Initialize(typeof(JoystickOpacity), UserPreferences.JoystickOpacity, "Joystick Opacity", false, false);
        GetOptionEnumViewInstance().Initialize(typeof(JoystickDeadZone), UserPreferences.JoystickDeadZone, "Joystick DeadZone", false, false);
        GetOptionEnumViewInstance().Initialize(typeof(JoystickRunThreshold), UserPreferences.JoystickRunThreshold, "Joystick Run Threshold", false, false);
        GetOptionEnumViewInstance().Initialize(typeof(UseLegacyJoystick), UserPreferences.UseLegacyJoystick, "Use Legacy Joystick", false, false);
        GetOptionEnumViewInstance().Initialize(typeof(JoystickCancelsFollow), UserPreferences.JoystickCancelsFollow, "Joystick Cancels Follow", false, false);
        
        //Only show customize joystick button when UO client is running and we're in the game scene
        customizeJoystickButtonGameObject.transform.SetAsLastSibling();
        customizeJoystickButtonGameObject.SetActive(false);

        AddHeader("Developer");
        GetOptionEnumViewInstance().Initialize(typeof(ShowErrorDetails), UserPreferences.ShowErrorDetails, "Show Error Details", false, false);        
        // MobileUO: TODO: only for master branch, comment these out as they aren't used yet (requires newer CUO changes from dev branch)
        //GetOptionEnumViewInstance().Initialize(typeof(UseDrawTexture), UserPreferences.UseDrawTexture, "Use DrawTexture", false, false);
        //GetOptionEnumViewInstance().Initialize(typeof(UseSpriteSheet), UserPreferences.UseSpriteSheet, "Use Sprite Sheets", false, false);
        //GetOptionEnumViewInstance().Initialize(typeof(SpriteSheetSize), UserPreferences.SpriteSheetSize, "Sprite Sheet Size", false, false);

        showConsoleButtonGameObject.transform.SetAsLastSibling();

        clientRunner.SceneChanged += OnUoSceneChanged;

        //Options that are hidden by default
        optionEnumViewInstance.gameObject.SetActive(false);
        headerTemplate.gameObject.SetActive(false);
    }   

    private void OnUoSceneChanged(bool isGameScene)
    {
        customizeJoystickButtonGameObject.SetActive(isGameScene);
        loginButtonGameObject.SetActive(isGameScene == false);
    }

    private OptionEnumView GetOptionEnumViewInstance()
    {
        var instance = Instantiate(optionEnumViewInstance.gameObject, optionEnumViewInstance.transform.parent).GetComponent<OptionEnumView>();
        optionEnumViews.Add(instance);
        return instance;
    }

    private void AddHeader(string title)
    {
        var header = Instantiate(headerTemplate, optionEnumViewInstance.transform.parent);
        header.text = title;
        header.gameObject.SetActive(true);
        header.transform.SetAsLastSibling();
    }

    private void OnMenuButtonClicked()
    {
        menuOpened = !menuOpened;

        DOTween.Kill(listTransform);

        if (menuOpened)
        {
            listTransform.DOLocalMove(listOpenPosition, listTweenDuration);
        }
        else
        {
            listTransform.DOLocalMove(listClosedPosition, listTweenDuration);
        }
    }
}
