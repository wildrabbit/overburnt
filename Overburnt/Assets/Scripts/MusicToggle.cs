using UnityEngine;
using UnityEngine.UI;

public class MusicToggle : MonoBehaviour
{
    public Image ButtonImage;
    public Sprite SpriteOn;
    public Sprite SpriteOff;

    public void Start()
    {
        RefreshMusicToggle();
    }

    public void RefreshMusicToggle()
    {
        ButtonImage.sprite = (Mathf.Approximately(AudioListener.volume, 0)) ? SpriteOff : SpriteOn;
    }

    public void OnMusicToggleClicked()
    {
        //AudioListener.pause = !AudioListener.pause;

        if(Mathf.Approximately(AudioListener.volume, 0))
        {
            AudioListener.volume = 1;
        }
        else
        {
            AudioListener.volume = 0;
        }
        RefreshMusicToggle();
    }
}
