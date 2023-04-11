
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class SyncSlider : UdonSharpBehaviour
{
    [UdonSynced] private float syncedValue;
    //private float localValue;
    private bool deserializing;
    private VRCPlayerApi localPlayer;
    private Slider slider;

    public UdonBehaviour gameManager;
    public string eventName;
    public Text textSliderValue;
    void Start()
    {
        slider = transform.GetComponent<Slider>();
        localPlayer = Networking.LocalPlayer;
        syncedValue = slider.value;
        deserializing = false;
    }

    public override void OnPreSerialization()
    {
        syncedValue = slider.value;
    }

    public override void OnDeserialization()
    {
        deserializing = true;

        if (!Networking.IsOwner(gameObject))
        {
            slider.value = syncedValue;
            if (textSliderValue != null) textSliderValue.text = slider.value.ToString();
        }

        deserializing = false;  
    }

    public void SliderUpdate()
    {
        if (!Networking.IsOwner(gameObject) && !deserializing) Networking.SetOwner(localPlayer, gameObject);

         syncedValue = slider.value;

        if(textSliderValue != null) textSliderValue.text = slider.value.ToString();

        gameManager.SendCustomEvent(eventName);
    }
}
