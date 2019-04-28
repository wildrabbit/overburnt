using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class GameHUD : MonoBehaviour
{
    public GameController _gameController;
    public TextMeshProUGUI _timeLeft;
    public TextMeshProUGUI _income;

    // Start is called before the first frame update
    void Start()
    {
        _timeLeft.text = FormatTime(_gameController.TimeLeft);
        _income.text = _gameController.Earnings.ToString();
        _gameController.OnEarningsChanged += RefreshIncome;
    }

    void RefreshIncome(int newValue)
    {
        _income.text = newValue.ToString();
    }

    private string FormatTime(float timeLeft)
    {
        TimeSpan t = TimeSpan.FromSeconds(timeLeft);
        return string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);
    }

    // Update is called once per frame
    void Update()
    {
        _timeLeft.text = FormatTime(_gameController.TimeLeft);
    }
}
