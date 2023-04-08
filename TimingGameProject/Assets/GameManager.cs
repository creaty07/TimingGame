
using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Agreement.JPake;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Security.Cryptography;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using VRC.Udon.Serialization.OdinSerializer.Utilities;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class GameManager : UdonSharpBehaviour
{
    const string STATE_READY = "ready";
    const string STATE_RUN = "run";

    [UdonSynced] private byte maxRound; // 최대 라운드
    [UdonSynced] private byte round; // 게임 진행 라운드
    [UdonSynced] private string gameState; // 게임 진행 상태 ready, run, suc, fail
    [UdonSynced] private int nowNumber;
    [UdonSynced] private int maxNumberCnt;
    [UdonSynced] private int life;
    [UdonSynced] private int maxLife;
    private int numberMin;
    private int numberMax;

    [UdonSynced] private int[] joinPlayers;
    [UdonSynced] private int[] playerNumbers;
    [UdonSynced] private int[] playerNumbersCnt;
    [UdonSynced] private bool playerSendNumber;

    public Text textNowNumber;
    public Text textMyLowNumber;
    public Text textRoundAndLife;
    void Start()
    {
        maxRound = 8;
        life = 0;
        maxLife = 8;
        round = 0;
        gameState = STATE_READY;
        numberMin = 1;
        numberMax = 101;
        nowNumber = 0;
        joinPlayers = new int[0];
        playerNumbers = new int[0];
        playerNumbersCnt = new int[0];
        playerSendNumber = false;

        if (Networking.IsOwner(this.gameObject))
        {
            RequestSerialization();
        }
    }
    public override void OnDeserialization()
    {
        base.OnDeserialization();

        if(round > 0)
        {
            SetRoundText(round, life);
            SetMyMinNumberText(GetMyMinNumber());
        }
    }
    public override void OnPlayerJoined(VRCPlayerApi player)
    {

    }
    public override void OnPlayerLeft(VRCPlayerApi player)
    {

    }
    public void PlayerGameJoinToggleInteract()
    {
        SetOwner();

        VRCPlayerApi localPlayer = Networking.LocalPlayer;

        int playerId = localPlayer.playerId;

        bool exists = joinPlayers.Exists(playerId);

        if (!exists)
        {
            joinPlayers = joinPlayers.AppendItem(playerId);
        }
        else
        {
            joinPlayers = joinPlayers.RemoveItem(playerId);
        }
        RequestSerialization();
    }
    public void PlayerSendNumberInteract()
    {
        if (gameState != STATE_RUN || playerSendNumber) return;

        SetOwner();

        playerSendNumber = true;

        RequestSerialization();

        int playerId = Networking.LocalPlayer.playerId;

        int playerIndex = joinPlayers.FindIndex(playerId);

        int number = GetMyMinNumber();

        if (playerNumbersCnt[playerIndex] > 0)
        {
            if (nowNumber < number)
            {
                SetNowNumber(number);
                int numberIndex = playerNumbers.FindIndex(number);

                playerNumbers[numberIndex] = 999;
                playerNumbersCnt[playerIndex]--;

                int lowNumberCnt = GetLowNumberCnt();

                if (lowNumberCnt > 0) 
                { 
                    life--;
                    SetRoundText(round, life);
                }

                SetMyMinNumberText(GetMyMinNumber());
            }

            if (life == 0)
            {
                GameStop();
                return;
            }
            else if (CheckNextRound() == true)
            {
                NextRound();
            }
        }

        playerSendNumber = false;

        RequestSerialization();
    }

    private int GetLowNumberCnt()
    {
        int lowNumberCnt = 0;

        for(int i = 0; i < joinPlayers.Length; i++)
        {
            int[] numbers = new int[round];

            Array.Copy(playerNumbers, i * round, numbers, 0, numbers.Length);

            for(int j = 0; j < numbers.Length; j++)
            {
                int number = numbers[j];
                if (nowNumber > number)
                {
                    int numberIndex = playerNumbers.FindIndex(number);
                    playerNumbers[numberIndex] = 999;
                    playerNumbersCnt[i]--;
                    lowNumberCnt++;
                }
            }
        }

        return lowNumberCnt;
    }

    private bool CheckNextRound()
    {
        int playerNumberCnt = 0;

        for(int i = 0; i < playerNumbersCnt.Length; i++)
        {
            playerNumberCnt += playerNumbersCnt[i];
        }

        return playerNumberCnt == 0;
    }

    public void GameStart()
    {
        if (gameState != STATE_READY || joinPlayers.Length <= 1) return;

        SetOwner();

        life = maxLife;
        round = 0;
        gameState = STATE_RUN;
        playerSendNumber = false;

        playerNumbersCnt = new int[joinPlayers.Length];

        RequestSerialization();

        NextRound();
    }
    private void NextRound()
    {
        SetOwner();

        round++;
        SetRoundText(round, life);

        if (round > maxRound || joinPlayers.Length == 0)
        {
            GameStop();
            return;
        }

        int playerCnt = joinPlayers.Length;
        SetNowNumber(0);
        maxNumberCnt = round * playerCnt;

        playerNumbers = GetRoundNubmers(maxNumberCnt);

        for (int i = 0; i < playerCnt; i++)
        {
            playerNumbersCnt[i] = round;
        }

        SetMyMinNumberText(GetMyMinNumber());

        RequestSerialization();
    }
    private int[] GetRoundNubmers(int numberCnt)
    {
        int[] numbers = new int[numberCnt];

        int numberIndex = 0;

        while(numberIndex < numberCnt)
        {
            bool addNumber = true;
            int inputNumber = UnityEngine.Random.Range(numberMin, numberMax);

            foreach (int number in numbers)
            {
                if(number == inputNumber)
                {
                    addNumber = false;
                    break;
                }
            }

            if (addNumber)
            {
                numbers[numberIndex++] = inputNumber;
            }
        }

        return numbers;
    }
    private void GameStop()
    {
        SetOwner();

        gameState = STATE_READY;

        if (life == 0) textNowNumber.text = "Fail";
        else textNowNumber.text = "Suc";

        RequestSerialization();
    }
    void SetOwner()
    {
        if (!Networking.IsOwner(this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
    }

    void SetNowNumber(int number)
    {
        nowNumber = number;
        textNowNumber.text = number.ToString();
    }

    int GetMyMinNumber()
    {
        int playerId = Networking.LocalPlayer.playerId;

        int playerIndex = joinPlayers.FindIndex(playerId);

        int[] numbers = new int[round];

        Array.Copy(playerNumbers, playerIndex * round, numbers, 0, numbers.Length);

        return numbers.GetMinInt();
    }

    void SetMyMinNumberText(int number)
    {
        textMyLowNumber.text = number < 999 ? number.ToString() : "X";
    }

    void SetRoundText(int round, int life)
    {
        textRoundAndLife.text = $"Roud : {round}, Life : {life}";
    }
}
