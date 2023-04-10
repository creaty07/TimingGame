
using System.Media;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class InteractSoundEffect : UdonSharpBehaviour
{
    AudioSource source;
    void Start()
    {
        source = GetComponent<AudioSource>();
    }

    public override void Interact()
    {
        if(source != null) source.Play();
    }
}
