using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameAudioManager : MonoBehaviour
{
    public AudioSource AudioSourceMusic;
    public AudioSource Sfx; // TODO: Add more

    public AudioClip MainTrack;
    public AudioClip LevelWonTrack;
    public AudioClip LevelLostTrack;
    public AudioClip GameWonTrack;

    public GameController GameController;

    public void Awake()
    {
        GameController.GameFinished += OnFinished;
        GameController.GameStarted += OnGameReady;
        GameController.GameBeaten += OnGameBeaten;
    }

    private void OnGameBeaten(float obj)
    {
        AudioSourceMusic.loop = false;
        AudioSourceMusic.clip = GameWonTrack;
        AudioSourceMusic.Play();
    }

    private void OnGameReady(int arg1, LevelConfig arg2, float arg3)
    {
        AudioSourceMusic.loop = true;
        AudioSourceMusic.clip = MainTrack;
        AudioSourceMusic.Play();
    }

    private void OnFinished(Result result, int arg2, int arg3, float arg4)
    {
        bool won = result == Result.WonBase || result == Result.WonGood || result == Result.WonGreat;
        bool lost = !won && result != Result.Running;
        if(!won && !lost)
        {
            return;
        }

        AudioSourceMusic.loop = false;
        AudioSourceMusic.clip = won ? LevelWonTrack : LevelLostTrack;
        AudioSourceMusic.Play();
    }
}
