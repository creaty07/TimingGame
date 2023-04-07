
using System;
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
        Debug.Log($"{GetPlayerId()} GameJoin Click");
        gameManager.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "PlayerGameJoin");
    }
    public void GameLeft()
    {
        isVaildJoin = false;
        Debug.Log($"{GetPlayerId()} GameLeft Click");
        gameManager.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "PlayerGameLeft");
    }
    public void SetNumbers(int[] numbers)
    {
        this.numbers = new int[numbers.Length];

        int temp = 0;
        for (int i = 0; i < numbers.Length - 1; i++)
        {
            for(int j = i + 1; j < numbers.Length; j++)
            {
                if (numbers[i] > numbers[j])
                {
                    temp = numbers[i];
                    numbers[i] = numbers[i];
                    numbers[j] = temp;
                }
            }
        }

        this.numbers = numbers;

        Debug.Log($"Player {GetPlayerId()} Numbers : {numbers.Length}");
    }
    public void SendNumber()
    {
        if (numbers == null || numbers.Length == 0) return;
        gameManager.SendCustomEvent("SendNumber");
    }

    public int GetFirstNumber()
    {
        if (numbers == null || numbers.Length == 0) return 0;

        return numbers[0];
    }

    public void RemoveFirstNumber()
    {
        if(numbers.Length > 0)
        {
            if(numbers.Length - 1 == 0)
            {
                numbers = null;
            }
            else
            {
                int[] newNumbers = new int[numbers.Length - 1];
                numbers.CopyTo(newNumbers, 1);

                numbers = newNumbers;
            }
        }
    }

    public int NumberLength()
    {
        if (numbers == null) return 0;

        return numbers.Length;
    }
}
