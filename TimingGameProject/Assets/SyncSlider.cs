
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class SyncSlider : UdonSharpBehaviour
{
    [UdonSynced]
    private float syncedValue;
    private float localValue;
    private bool deserializing;
    private VRCPlayerApi localPlayer;
    private Slider slider;

    void Start()
    {
        slider = transform.GetComponent<Slider>();
        localPlayer = Networking.LocalPlayer;
        syncedValue = localValue = slider.value;
        deserializing = false;
    }

    public override void OnPreSerialization()
    {
        syncedValue = localValue;
    }

    public override void OnDeserialization()
    {
        deserializing = true;

        if (!Networking.IsOwner(gameObject))
        {
            slider.value = syncedValue;
        }

        deserializing = false;  
    }

    public void SliderUpdate()
    {
        if (!Networking.IsOwner(gameObject) && !deserializing) Networking.SetOwner(localPlayer, gameObject);

        localValue = syncedValue = slider.value;
    }
}
