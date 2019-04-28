using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

[Serializable]
public class ResultData
{
    public Result resultKey;
    public string text;
    public bool showNext;
    public Color textColour;
}

public class GameHUD : MonoBehaviour
{
    public List<ResultData> Results;

    public GameController _gameController;
    public TextMeshProUGUI _timeLeft;
    public TextMeshProUGUI _income;
    public TextMeshProUGUI _fatigue;

    public GameObject _endPanel;
    public GameObject _nextRoot;
    public TextMeshProUGUI _result;
    public TextMeshProUGUI _revenue;
    public TextMeshProUGUI _nextBestRevenue;

    // Start is called before the first frame update
    void Start()
    {
        _timeLeft.text = FormatTime(_gameController.TimeLeft);
        _income.text = _gameController.Earnings.ToString();
        _gameController.OnEarningsChanged += RefreshIncome;
        _gameController.OnFatigueChanged += RefreshFatigue;
        _gameController.GameReset += OnReset;
        _gameController.GameFinished += OnFinished ;
        _endPanel.SetActive(false);
    }

    void RefreshFatigue(int percent)
    {
        _fatigue.text = $"{percent}%";
    }

    private void OnFinished(Result arg1, int arg2, int arg3)
    {
        if (!_endPanel.activeInHierarchy)
        {
            _endPanel.SetActive(true);
        }
        ResultData res = Results.Find(x => x.resultKey == arg1);
        if(res == null)
        {
            return;
        }

        _nextRoot.SetActive(res.showNext);
        _result.color = res.textColour;
        _result.text = res.text;
        _revenue.text = arg2.ToString();
        _nextBestRevenue.text = arg3.ToString();
    }

    void OnReset()
    {
        if(_endPanel.activeInHierarchy)
        {
            _endPanel.SetActive(false);
        }
        RefreshFatigue(_gameController.FatiguePercent);
        RefreshTime(_gameController.TimeLeft);
        RefreshIncome(_gameController.Earnings);
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
        RefreshTime(_gameController.TimeLeft);
    }

    void RefreshTime(float timeLeft)
    {
        _timeLeft.text = FormatTime(timeLeft);
    }
}
