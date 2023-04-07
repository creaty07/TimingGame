
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
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

public class GameManager : UdonSharpBehaviour
{
    const string STATE_READY = "ready";
    const string STATE_RUN = "run";
    const string STATE_SUC = "suc";
    const string STATE_FAIL = "fail";

    [UdonSynced] private byte maxRound; // 최대 라운드
    [UdonSynced] private byte round; // 게임 진행 라운드
    [UdonSynced] private string gameState; // 게임 진행 상태 ready, run, suc, fail
    [UdonSynced] private int playerCnt;
    [UdonSynced] public int nowNumber;
    [UdonSynced] private int maxNumberCnt;
    [UdonSynced] private int sendNumberCnt;

    private int numberMin;
    private int numberMax;

    public GameObject originalPlayerObj;

    public GameObject[] players;
    public GameObject[] roundPlayers;
    void Start()
    {
        if (Networking.IsOwner(this.gameObject))
        {
            maxRound = 8;
            round = 0;
            gameState = STATE_READY;
            playerCnt = 0;
            numberMin = 1;
            numberMax = 101;
            nowNumber = 0;
            sendNumberCnt = 0;

            RequestSerialization();
        }
    }

    private void FixedUpdate()
    {
        Debug.Log($"nowNumber : {nowNumber}");
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        GameObject playerObj = Instantiate(originalPlayerObj);
        Player playerComponent = playerObj.GetComponent<Player>();
        playerComponent.localPlayer = player;
        playerComponent.gameManager = this;

        if (players == null)
        {
            players = new GameObject[1];
        }
        else
        {
            GameObject[] newPlayers = new GameObject[players.Length + 1];
            players.CopyTo(newPlayers, 0);

            players = newPlayers;
        }

        players[players.Length - 1] = playerObj;

        Debug.Log($"PlayerJoin {player.playerId}!!");
        Debug.Log($"PlayerCnt {players.Length}!!");
    }

    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        int leftPlayerId = player.playerId;

        if (players.Length - 1 == 0)
        {
            Destroy(players[0]);

            players = null;
        }
        else
        {
            GameObject[] newPlayers = new GameObject[players.Length - 1];

            int playerIndex = 0;

            for (int i = 0; i < players.Length; i++)
            {
                int playerId = players[i].GetComponent<Player>().GetPlayerId();

                if (leftPlayerId == playerId)
                {
                    Destroy(players[i]);
                }
                else
                {
                    newPlayers[playerIndex++] = players[i];
                }
            }

            players = newPlayers;
        }

        Debug.Log($"PlayerLeft {player.playerId}!!");
        Debug.Log($"PlayerCnt {players.Length}!!");
    }

    public void PlayerGameJoinToggleInteract()
    {
        if (!Networking.IsOwner(this.gameObject)) Networking.SetOwner(Networking.LocalPlayer, this.gameObject);

        nowNumber++;

        RequestSerialization();

        //int playerId = Networking.LocalPlayer.playerId;
        //
        //for (int i = 0; i < players.Length; i++)
        //{
        //    Player player = players[i].GetComponent<Player>();
        //
        //    if (player.GetPlayerId() == playerId)
        //    {
        //        player.GameJoinToggle();
        //        break;
        //    }
        //}
    }

    public void PlayerGameSendNumberInteract()
    {
        int playerId = Networking.LocalPlayer.playerId;

        for (int i = 0; i < players.Length; i++)
        {
            Player player = players[i].GetComponent<Player>();

            if (player.GetPlayerId() == playerId)
            {
                player.SendNumber();
            }
        }
    }

    public void PlayerGameJoin()
    {
        VRCPlayerApi localPlayer = Networking.LocalPlayer;

        playerCnt++;
    }
    public void PlayerGameLeft()
    {
        VRCPlayerApi localPlayer = Networking.LocalPlayer;

        playerCnt--;
    }
    public void GameStart()
    {
        if (gameState != STATE_READY) return;

        VRCPlayerApi localPlayer = Networking.LocalPlayer;

        round = 0;
        gameState = STATE_RUN;

        roundPlayers = new GameObject[playerCnt];
        int roundPlayerIndex = 0;

        for (int i = 0; i < players.Length; i++)
        {
            Player roundPlayer = players[i].GetComponent<Player>();

            if (roundPlayer.isVaildJoin)
            {
                roundPlayers[roundPlayerIndex++] = players[i];
            }
        }

        Debug.Log($"GameStart RoundPlayerCnt : {roundPlayers.Length}!!");

        NextRound();
    }
    private void NextRound()
    {
        round++;

        if (round > maxRound || playerCnt == 0)
        {
            GameStop();
            return;
        }

        VRCPlayerApi localPlayer = Networking.LocalPlayer;

        nowNumber = 0;
        sendNumberCnt = 0;
        maxNumberCnt = round * playerCnt;

        Debug.Log($"Round {round} Numbers : {maxNumberCnt}");

        int[] roundNumbers = GetRoundNubmers(maxNumberCnt);

        int roundNumberIndex = 0;
        for (int i = 0; i < playerCnt; i++)
        {
            Player roundPlayer = roundPlayers[i].GetComponent<Player>();
            int[] playerNumber = new int[round];

            for(int j = 0; j < playerNumber.Length; j++)
            {
                playerNumber[j] = roundNumbers[roundNumberIndex++];
            }

            roundPlayer.SetNumbers(playerNumber);
        }
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
                Debug.Log($"Add Number : {inputNumber}");
            }
        }

        return numbers;
    }
    private void GameStop()
    {
        gameState = STATE_READY;

        Debug.Log($"GameStop Round {round}");
    }

    public void SendNumber()
    {
        if (gameState != STATE_RUN) return;

        VRCPlayerApi localPlayer = Networking.LocalPlayer;

        int playerId = localPlayer.playerId;

        int number = 0;
        Player roundPlayer = null;

        for (int i = 0; i < playerCnt; i++)
        {
            roundPlayer = roundPlayers[i].GetComponent<Player>();

            if(roundPlayer.GetPlayerId() == playerId)
            {
                number = roundPlayer.GetFirstNumber();
                break;
            }
        }

        if(number > 0)
        {
            if(nowNumber < number)
            {
                sendNumberCnt++;
                nowNumber = number;
                roundPlayer.RemoveFirstNumber();
            }
            else
            {

            }
        }

        Debug.Log($"NowNumber : {nowNumber}");
    }
}
