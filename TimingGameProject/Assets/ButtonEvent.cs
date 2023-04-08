
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ButtonEvent : UdonSharpBehaviour
{
    public UdonBehaviour gameManager;
    public string eventName; 
    public override void Interact()
    {
        gameManager.SendCustomEvent(eventName);
    }
}
