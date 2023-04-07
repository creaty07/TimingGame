
using UdonSharp;
using UnityEngine;
using UnityEngine.Events;
using VRC.SDKBase;
using VRC.Udon;

public class Player : UdonSharpBehaviour
{
    public UdonSharpBehaviour gameManager;
    public VRCPlayerApi localPlayer;
    [UdonSynced]
    public int[] numbers;
    [UdonSynced]
    public bool isVaildJoin;

    void Start()
    {
        isVaildJoin = false;
    }

    public int GetPlayerId()
    {
        return localPlayer.playerId;
    }

    public void GameJoin()
    {
        isVaildJoin = true;
        gameManager.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "PlayerGameJoin");
    }
    public void GameLeft()
    {
        isVaildJoin = false;
        gameManager.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "PlayerGameLeft");
    }
    public void SetNumbers(int[] numbers)
    {
        this.numbers = numbers;

        Debug.Log($"Player {GetPlayerId()} Numbers : {numbers.Length}");
    }
    public void SendNubmer()
    {
        if (numbers.Length == 0) return;
    }

    public int GetFirstNumber()
    {
        if (numbers.Length == 0) return 0;

        return numbers[0];
    }
}
