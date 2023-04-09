
using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Agreement.JPake;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Security.Cryptography;
using System.Text;
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
    [UdonSynced] private string gameState; // 게임 진행 상태 ready, run
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

    [UdonSynced] private int totalPlayerHintCnt;
    [UdonSynced] private int playerHintCnt;
    [UdonSynced] private int useHintCnt;
    [UdonSynced] private int totalHintTouchCnt;
    [UdonSynced] private bool useHint;

    public Text textNowNumber;
    public Text textMyLowNumber;
    public Text textRoundAndLife;
    public GameObject playerInteractBoard;
    public GameObject defaultCanvas;
    public GameObject hintCanvas;
    public GameObject headObj;
    VRCPlayerApi localPlayer;
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
        totalPlayerHintCnt = 3;
        playerHintCnt = 0;
        useHintCnt = 0;
        totalHintTouchCnt = 0;
        useHint = false;
        localPlayer = Networking.LocalPlayer;
        playerInteractBoard.SetActive(false);

        if (Networking.IsOwner(this.gameObject))
        {
            RequestSerialization();
        }
    }
    private void FixedUpdate()
    {
        float distance = 1.5f;
        var transform = playerInteractBoard.transform;

        var forward = localPlayer.GetRotation() * Vector3.forward;
        // Player의 위치와 바라보는 방향을 기반으로 새로운 위치 계산
        Vector3 newPosition = localPlayer.GetPosition() + forward * distance;

        // 계산된 위치로 GameObject의 위치를 설정
        transform.position = new Vector3(newPosition.x, newPosition.y + 0.65f, newPosition.z);

        var head = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);

        headObj.transform.position = head.position + forward * (distance + 0.5f);
        headObj.transform.rotation = head.rotation;

        transform.LookAt(headObj.transform);
    }
    public override void OnDeserialization()
    {
        base.OnDeserialization();

        if(round > 0)
        {
            SetNowNumber(nowNumber);
            SetRoundText(round, life);
            SetMyMinNumberText(GetMyNumbers());
        }
    }
    // game rul
    public void GameStart()
    {
        if (gameState != STATE_READY || joinPlayers.Length <= 1) return;

        SetOwner();

        life = maxLife;
        round = 0;
        gameState = STATE_RUN;
        playerSendNumber = false;
        playerHintCnt = totalPlayerHintCnt;

        playerNumbersCnt = new int[joinPlayers.Length];

        RequestSerialization();

        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetGameStartUi");

        NextRound();
    }
    public void SetGameStartUi()
    {
        playerInteractBoard.SetActive(true);
        defaultCanvas.SetActive(true);
        hintCanvas.SetActive(false);
    }
    private void NextRound()
    {
        SetOwner();

        round++;
        SetRoundText(round, life);

        if (round > maxRound || joinPlayers.Length == 0)
        {
            GameOver();
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

        SetMyMinNumberText(GetMyNumbers());

        RequestSerialization();
    }
    private int[] GetRoundNubmers(int numberCnt)
    {
        int[] numbers = new int[numberCnt];

        int numberIndex = 0;

        while (numberIndex < numberCnt)
        {
            bool addNumber = true;
            int inputNumber = UnityEngine.Random.Range(numberMin, numberMax);

            foreach (int number in numbers)
            {
                if (number == inputNumber)
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
    private void GameOver()
    {
        SetOwner();

        gameState = STATE_READY;

        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetGameOverUi");

        RequestSerialization();
    }
    public void SetGameOverUi()
    {
        if (life == 0) SetNowNumberText("Fail");
        else SetNowNumberText("Suc");
        textRoundAndLife.text = "";
        textMyLowNumber.text = "";
        playerInteractBoard.SetActive(false);
    }
    // player interact
    public void PlayerGameJoinToggleInteract()
    {
        if (gameState != STATE_READY) return;

        SetOwner();

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
        if (gameState != STATE_RUN || playerSendNumber || useHint) return;

        SetOwner();

        playerSendNumber = true;

        RequestSerialization();

        int playerId = localPlayer.playerId;

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

                SetMyMinNumberText(GetMyNumbers());
            }

            if (life == 0)
            {
                GameOver();
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
    // hint
    public void EnableHintInteract()
    {
        if (playerHintCnt == 0) return;

        SetOwner();

        useHint = true;

        RequestSerialization();

        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetHintCanvas");
    }
    public void UseHintInteract()
    {
        SetOwner();

        useHintCnt++;
        totalHintTouchCnt++;

        CheckUseHint();
    }
    public void NotUseHintInteract()
    {
        SetOwner();

        totalHintTouchCnt++;

        CheckUseHint();
    }
    private void CheckUseHint()
    {
        if (totalHintTouchCnt == joinPlayers.Length)
        {
            if (totalHintTouchCnt == useHintCnt)
            {
                playerHintCnt--;
                int setMaxNumber = 0;

                for(int i = 0; i< joinPlayers.Length; i++)
                {
                    int playerId = joinPlayers[i];
                    int[] numbers = GetPlayerNumbers(playerId);
                    int minNumber = numbers.GetMinInt();

                    if(minNumber != 999)
                    {
                        if(setMaxNumber < minNumber)
                        {
                            setMaxNumber = minNumber;
                        }

                        int findIndex = playerNumbers.FindIndex(minNumber);
                        playerNumbers[findIndex] = 999;
                        playerNumbersCnt[i]--;
                    }
                }

                nowNumber = setMaxNumber;
                SetNowNumber(nowNumber);
                SetMyMinNumberText(GetMyNumbers());
            }

            useHint = false;

            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetDefaultCanvas");
        }

        RequestSerialization();
    }
    public void SetHintCanvas()
    {
        defaultCanvas.SetActive(false);
        hintCanvas.SetActive(true);
    }
    public void SetDefaultCanvas()
    {
        defaultCanvas.SetActive(true);
        hintCanvas.SetActive(false);
    }
    // utils
    // set
    void SetOwner()
    {
        if (!Networking.IsOwner(this.gameObject)) Networking.SetOwner(localPlayer, this.gameObject);
    }
    void SetNowNumber(int number)
    {
        nowNumber = number;
        SetNowNumberText(number.ToString());
    }
    void SetNowNumberText(string text)
    {
        textNowNumber.text = text;
    }
    void SetMyMinNumberText(int[] numbers)
    {
        string numberText = "";

        numbers.SortArray();

        for (int i = 0; i < numbers.Length; i++)
        {
            if (numbers[i] != 999)
            {
                if (i != 0) numberText += ", ";

                numberText += numbers[i].ToString();
            }
        }

        textMyLowNumber.text = numberText.Length != 0 ? numberText : "X";
    }
    void SetRoundText(int round, int life)
    {
        textRoundAndLife.text = $"Roud : {round}, Life : {life}";
    }
    // get
    int GetMyMinNumber()
    {
        int[] numbers = GetMyNumbers();

        return numbers.GetMinInt();
    }

    int[] GetMyNumbers()
    {
        int playerId = localPlayer.playerId;

        return GetPlayerNumbers(playerId);
    }

    int[] GetPlayerNumbers(int playerId)
    {
        int playerIndex = joinPlayers.FindIndex(playerId);

        int[] numbers = new int[round];

        Array.Copy(playerNumbers, playerIndex * round, numbers, 0, numbers.Length);

        return numbers;
    }
    private int GetLowNumberCnt()
    {
        int lowNumberCnt = 0;

        for (int i = 0; i < joinPlayers.Length; i++)
        {
            int[] numbers = new int[round];

            Array.Copy(playerNumbers, i * round, numbers, 0, numbers.Length);

            for (int j = 0; j < numbers.Length; j++)
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
    // check
    private bool CheckNextRound()
    {
        int playerNumberCnt = 0;

        for (int i = 0; i < playerNumbersCnt.Length; i++)
        {
            playerNumberCnt += playerNumbersCnt[i];
        }

        return playerNumberCnt == 0;
    }
}
