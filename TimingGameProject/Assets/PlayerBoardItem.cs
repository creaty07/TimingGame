
using UdonSharp;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class PlayerBoardItem : UdonSharpBehaviour
{
    public int index;
    public Text playerName;
    public Text dataCnt;
    public Sprite[] sprites;
    Image image;
    void Start()
    {
        image = GetComponent<Image>();

        if (index % 2 == 0) 
        {
            image.sprite = sprites[0];
        }
        else
        {
            image.sprite = sprites[1];
        }
    }

    public void SetPlayerName(string playerName)
    {
        this.playerName.text = playerName;
    }

    public void SetDataCnt(int dataCnt)
    {
        this.dataCnt.text = dataCnt.ToString();
    }
}
