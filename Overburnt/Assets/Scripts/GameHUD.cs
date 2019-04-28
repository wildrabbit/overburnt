using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

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

    public List<Sprite> FatigueMappings;

    public Image _characterExpression;

    public GameController _gameController;
    public TextMeshProUGUI _timeLeft;
    public TextMeshProUGUI _income;
    public TextMeshProUGUI _fatigue;
    [Header("Stage end")]
    public GameObject _endPanel;
    public GameObject _nextRoot;
    public TextMeshProUGUI _result;
    public TextMeshProUGUI _revenue;
    public TextMeshProUGUI _nextBestRevenue;
    public GameObject LevelEndInputHint;
    public TextMeshProUGUI LevelEndHintText;
    [Header("Stage start")]
    public GameObject StartPanel;
    public TextMeshProUGUI StartLevelText;
    public TextMeshProUGUI StartLevelDesc;
    public GameObject InputHint;
    public List<Image> Hints;


    [Header("Game over")]
    public GameObject GameBeaten;
    public GameObject GameBeatenInputHint;



    // Start is called before the first frame update
    void Start()
    {
        _gameController.OnEarningsChanged += RefreshIncome;
        _gameController.OnFatigueChanged += RefreshFatigue;
        _gameController.GameStarted += OnLevelStarted;
        _gameController.GameFinished += OnFinished ;
        _gameController.GameReset += OnGameReset;
        _gameController.GameReady += OnGameReady;
        _gameController.GameBeaten += OnGameBeaten;
        _endPanel.SetActive(false);
    }

    private void OnGameReady()
    {
        StartPanel.gameObject.SetActive(false);
    }

    private void OnGameBeaten(float delay)
    {
        GameBeaten.gameObject.SetActive(true);
        GameBeatenInputHint.gameObject.SetActive(false);
        StartCoroutine(DelayedShow(GameBeatenInputHint, delay));
    }

    void RefreshFatigue(int percent)
    {
        _fatigue.text = $"{percent}%";
        if (FatigueMappings.Count > 1)
        {
            float threshold = 100 / (FatigueMappings.Count - 1);
            float accumThreshold = threshold;
            int idx = 0;
            while(percent > accumThreshold && accumThreshold <= 100)
            {
                accumThreshold += threshold;
                idx++;
            }
            _characterExpression.sprite = FatigueMappings[idx];
        }
        else if (FatigueMappings.Count > 0)
        {
            _characterExpression.sprite = FatigueMappings[0];
        }
        else _characterExpression.sprite = null;
    }
        

    private void OnFinished(Result result, int revenue, int nextRevenue, float delay)
    {
        if (!_endPanel.activeInHierarchy)
        {
            _endPanel.SetActive(true);
            LevelEndInputHint.SetActive(false);
        }
        ResultData res = Results.Find(x => x.resultKey == result);
        if(res == null)
        {
            return;
        }

        _nextRoot.SetActive(res.showNext);
        _result.color = res.textColour;
        _result.text = res.text;
        _revenue.text = revenue.ToString();
        _nextBestRevenue.text = nextRevenue.ToString();
        bool continueText = result == Result.WonBase || result == Result.WonGood || result == Result.WonGreat;
        LevelEndHintText.text = continueText ? "Press any key to continue" : "Press any key to restart";
        StartCoroutine(DelayedShow(LevelEndInputHint, delay));
    }

    void OnGameReset()
    {

    }

    void OnLevelStarted(int levelIdx, LevelConfig leveConfig, float startDelay)
    {
        if(_endPanel.activeInHierarchy)
        {
            _endPanel.SetActive(false);
        }
        RefreshFatigue(_gameController.FatiguePercent);
        RefreshTime(_gameController.TimeLeft);
        RefreshIncome(_gameController.Earnings);

        if(GameBeaten.activeInHierarchy)
        {
            GameBeaten.SetActive(false);
        }

        StartPanel.SetActive(true);
        StartLevelText.text = $"Level {levelIdx + 1}";
        StartLevelDesc.text = leveConfig.DescriptionStart;
        foreach(var hint in Hints)
        {
            hint.gameObject.SetActive(false);
        }
        bool immediate = Mathf.Approximately(startDelay, 0f);
        InputHint.SetActive(immediate);
        if(!immediate)
        {

            StartCoroutine(DelayedShow(InputHint, startDelay));
        }
    }

    IEnumerator DelayedShow(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        obj.SetActive(true);
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
