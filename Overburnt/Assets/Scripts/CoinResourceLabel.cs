using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CoinResourceLabel : MonoBehaviour
{
    public Image CoinGraphic;
    public TextMeshProUGUI Label;

    public Color AddColour;
    public Color LoseColour;

    public void SetAmount(int amount, int extra = 0)
    {
        if(amount > 0)
        {
            Label.color = LoseColour;
            string amountText = $"{amount}";
            if(extra > 0)
            {
                amountText += $"(+{ extra})";
                Label.text = amountText;
            }
        }
        else
        {
            Label.color = LoseColour;
            Label.text = $"-{amount}";
        }
    }
}
