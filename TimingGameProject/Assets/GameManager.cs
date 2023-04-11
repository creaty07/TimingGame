
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
    [UdonSynced] private string[] joinPlayerNames;
    [UdonSynced] private int[] playerNumbers;
    [UdonSynced] private int[] playerNumbersCnt;
    [UdonSynced] private bool playerSendNumber;

    [UdonSynced] private int maxItemCnt;
    [UdonSynced] private int itemCnt;
    [UdonSynced] private int useItemCnt;
    [UdonSynced] private int totalItemTouchCnt;
    [UdonSynced] private bool useItem;
    [UdonSynced] private bool usingItemClick;

    public GameObject uiView;
    public Text textGameJoin;
    public Text textNowNumber;
    public Text textMyLowNumber;
    public Text textRoundAndLife;
    public Text textItemCnt;
    public Text textPlayerNumberCnt;
    public GameObject playerInteractBoard;
    public GameObject defaultCanvas;
    public GameObject itemCanvas;
    public GameObject headObj;
    public Slider maxLifeSlider;
    public Slider maxRoundSlider;
    public Slider maxItemSlider;
    VRCPlayerApi localPlayer;
    void Start()
    {
        maxRound = 3;
        life = 0;
        maxLife = 3;
        round = 0;
        gameState = STATE_READY;
        numberMin = 1;
        numberMax = 101;
        nowNumber = 0;
        joinPlayers = new int[0];
        joinPlayerNames = new string[0];
        playerNumbers = new int[0];
        playerNumbersCnt = new int[0];
        playerSendNumber = false;
        maxItemCnt = 0;
        itemCnt = 0;
        useItemCnt = 0;
        totalItemTouchCnt = 0;
        useItem = false;
        usingItemClick = false;
        localPlayer = Networking.LocalPlayer;
        playerInteractBoard.SetActive(false);
        uiView.SetActive(false);

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
            SetUi();

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
        itemCnt = maxItemCnt;

        playerNumbersCnt = new int[joinPlayers.Length];

        RequestSerialization();

        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetGameStartUi");

        NextRound();
    }
    public void SetGameStartUi()
    {
        uiView.SetActive(true);
        playerInteractBoard.SetActive(true);
        defaultCanvas.SetActive(true);
        itemCanvas.SetActive(false);
        SetUi();
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
        SetTextPlayerNumberCnt();
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
        if (life == 0) SetNowNumberText("실패");
        else SetNowNumberText("성공");
        textRoundAndLife.text = "";
        textMyLowNumber.text = "";
        playerInteractBoard.SetActive(false);
        uiView.SetActive(false);
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
            joinPlayerNames = joinPlayerNames.AppendItem(localPlayer.displayName);
            textGameJoin.text = "게임 미참가";
        }
        else
        {
            joinPlayers = joinPlayers.RemoveItem(playerId);
            joinPlayerNames = joinPlayerNames.RemoveItem(localPlayer.displayName);
            textGameJoin.text = "가임 참가";
        }
        RequestSerialization();
    }
    public void PlayerSendNumberInteract()
    {
        if (gameState != STATE_RUN || playerSendNumber || useItem) return;

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
                SetTextPlayerNumberCnt();
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
    // Item
    public void EnableItemInteract()
    {
        if (itemCnt == 0) return;

        SetOwner();

        useItem = true;
        useItemCnt = 0;
        totalItemTouchCnt = 0;

        RequestSerialization();

        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetItemCanvas");
    }
    public void UseItemInteract()
    {
        if (usingItemClick) return;

        SetOwner();
        
        usingItemClick = true;

        RequestSerialization();

        useItemCnt++;
        totalItemTouchCnt++;

        usingItemClick = false;

        CheckUseItem();
    }
    public void NotUseItemInteract()
    {
        if (usingItemClick) return;

        SetOwner();

        usingItemClick = true;

        RequestSerialization();

        totalItemTouchCnt++;

        usingItemClick = false;

        CheckUseItem();
    }
    private void CheckUseItem()
    {
        if (totalItemTouchCnt == joinPlayers.Length)
        {
            if (totalItemTouchCnt == useItemCnt)
            {
                itemCnt--;
                for(int i = 0; i< joinPlayers.Length; i++)
                {
                    int playerId = joinPlayers[i];
                    int[] numbers = GetPlayerNumbers(playerId);
                    int minNumber = numbers.GetMinInt();

                    if(minNumber < 999)
                    {
                        int findIndex = playerNumbers.FindIndex(minNumber);
                        playerNumbers[findIndex] = 999;
                        playerNumbersCnt[i]--;
                    }
                }
                SetItemCntText();
                SetMyMinNumberText(GetMyNumbers());
                SetTextPlayerNumberCnt();
            }

            useItem = false;
        }

        RequestSerialization();

        SetDefaultCanvas();
    }
    void SetItemCntText()
    {
        textItemCnt.text = $"남은 아이템 수 : {itemCnt}";
    }
    public void SetItemCanvas()
    {
        defaultCanvas.SetActive(false);
        itemCanvas.SetActive(true);
    }
    public void SetDefaultCanvas()
    {
        defaultCanvas.SetActive(true);
        itemCanvas.SetActive(false);
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
    public void MaxLifeChanged()
    {
        SetOwner();
        maxLife = (byte)maxLifeSlider.value;
        RequestSerialization();
    }
    public void MaxRoundChanged()
    {
        SetOwner();
        maxRound = (byte)maxRoundSlider.value;
        RequestSerialization();
    }
    public void MaxItemChanged()
    {
        SetOwner();
        maxItemCnt = (byte)maxItemSlider.value;
        RequestSerialization();
    }
    public void SetTextPlayerNumberCnt()
    {
        string text = "";

        for(int i = 0; i < joinPlayers.Length; i++)
        {
            if (text.Length > 0) text += "\n\n";
            text += $"{joinPlayerNames[i]} : {playerNumbersCnt[i]}";
        }

        textPlayerNumberCnt.text = text;
    }
    public void SetUi()
    {
        SetNowNumber(nowNumber);
        SetRoundText(round, life);
        SetMyMinNumberText(GetMyNumbers());
        SetItemCntText();
        SetTextPlayerNumberCnt();
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
