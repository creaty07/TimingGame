using System.Collections;
using System.Collections.Generic;
using UdonSharp;
using UnityEngine;
/// <summary>
/// 게임 상태값
/// </summary>
public enum GameState
{
    Ready,
    Running,
    Suc,
    Fail
}
public class RoundPlayer
{
    public int playerId;
    public byte[] numbers;

}

public class GameRule : MonoBehaviour
{
    public byte maxRound; // 최대 라운드
    private byte round; // 게임 진행 라운드
    private byte[] numbers; // 숫자 리스트
    private GameState gameState; // 게임 진행 상태
    private byte playerCnt; // 플레이어 수
    private int[] playerIds; // 플레이어 아이디 리스트
    public RoundPlayer[] roundPlayers; // 플레이어가 가지고 있는 숫자, key 플레이어 아이디, value 플레이어가 가지고 있는 숫자
    void Start()
    {
        maxRound = 8;
        round = 0;
        gameState = GameState.Ready;
        playerCnt = 0;
        playerIds = new int[6] { -1, -1, -1, -1, -1, -1 }; // 플레이어 아이디 저장 할 곳 최대 6명
        numbers = new byte[100]; // 숫자 리스트 1 ~ 100 까지 저장
        for (byte i = 0; i < numbers.Length; i++) // 숫자 1 ~ 100까지 생성
        {
            numbers[i] = (byte)(i + 1);
        }
    }
    /// <summary>
    /// 플레이어 들어옴
    /// </summary>
    /// <param name="playerId">VRCPlayerApi에 PlayerId</param>
    public void JoinPlayer(int playerId)
    {
        if (playerCnt >= playerIds.Length) return;

        playerIds[playerCnt++] = playerId;
    }
    /// <summary>
    /// 플레이어 떠남
    /// </summary>
    /// <param name="playerId">VRCPlayerApi에 PlayerId</param>
    public void LeavePlayer(int playerId)
    {
        if (playerCnt == 0) return;

        bool leave = false;
        int length = playerIds.Length;

        for (int i = 0; i < length; i++)
        {
            if (playerIds[i] == playerId)
            {
                playerIds[i] = -1;
                leave = true;
            }

            if (leave && i + 1 <= length - 1)
            {
                int tmp = playerIds[i];
                playerIds[i] = playerIds[i + 1];
                playerIds[i + 1] = tmp;
            }
        }
        playerCnt--;
    }

    public void GameStart()
    {
        if (gameState != GameState.Ready) return;

        round = 0;
        gameState = GameState.Running;

        roundPlayers = new RoundPlayer[playerCnt];
        
        for (int i = 0; i < playerCnt; i++)
        {
            RoundPlayer roundPlayer = new RoundPlayer();
            roundPlayer.playerId = playerIds[i];
            roundPlayers[i] = roundPlayer;
        }

        NextRound();
    }

    private void NextRound()
    {
        round++;

        if (round > maxRound)
        {
            GameStop();
            return;
        }

        byte[] roundNumbers = GetRoundNubmers(round * playerCnt);

        for (int i = 0; i < playerCnt; i++)
        {
            byte[] playerNumber = new byte[round];

            roundNumbers.CopyTo(playerNumber, i * round);

            roundPlayers[i].numbers = playerNumber;
        }
    }

    private byte[] GetRoundNubmers(int numberCnt)
    {
        int[] useNumberIndexs = new int[numberCnt];
        byte[] roundNumbers = new byte[numberCnt];

        for (int i = 0; i < numberCnt; i++)
        {
            retry:
            int index = UnityEngine.Random.Range(0, numbers.Length);

            foreach (byte useNumberIndex in useNumberIndexs)
            {
                if(useNumberIndex == index) goto retry;
            }

            byte number = numbers[index];

            roundNumbers[i] = number;
            useNumberIndexs[i] = index;
        }

        return roundNumbers;
    }

    private void GameStop()
    {
        gameState = GameState.Ready;
    }
}
