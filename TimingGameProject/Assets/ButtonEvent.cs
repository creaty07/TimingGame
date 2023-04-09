
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ButtonEvent : UdonSharpBehaviour
{
    AudioSource interactSoundEffect;
    public UdonBehaviour gameManager;
    public string eventName;

    private void Start()
    {
        interactSoundEffect = GetComponent<AudioSource>();
    }

    public override void Interact()
    {
        interactSoundEffect.Play();
        gameManager.SendCustomEvent(eventName);
    }
}
