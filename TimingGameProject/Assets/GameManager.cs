
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class GameManager : UdonSharpBehaviour
{
    [UdonSynced]
    private byte maxRound; // 최대 라운드
    [UdonSynced]
    private byte round; // 게임 진행 라운드
    [UdonSynced]
    private byte[] numbers; // 내 숫자 리스트
    [UdonSynced]
    private GameState gameState; // 게임 진행 상태

    public GameRule rule;

    void Start()
    {

    }
  

}
