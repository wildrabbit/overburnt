using System.Collections;
using UnityEngine;

public class TransitionScreen : MonoBehaviour
{
    bool _readyToTransition;
    bool _transitioning;
    public GameObject InputText;
    public float Delay;
    public string NextScene;
    public AudioSource ConfirmSound;
    public RectTransform MusicToggle;
    // Start is called before the first frame update
    void Start()
    {
        _readyToTransition = false;
        _transitioning = false;
        InputText.SetActive(false);
        StartCoroutine(AwaitAndInput());
    }

    private void Update()
    {
        if(_readyToTransition && !_transitioning && Input.anyKey)
        {
            bool mouseDown = Input.GetMouseButton(0);
            bool mouseInMusicToggle = RectTransformUtility.RectangleContainsScreenPoint(MusicToggle, Input.mousePosition, Camera.main);
            if (mouseDown && mouseInMusicToggle)
            {
                return;
            }

            _transitioning = true;
            StartCoroutine(AwaitSoundAndNext());
        }
    }

    IEnumerator AwaitSoundAndNext()
    {
        ConfirmSound.Play();
        yield return new WaitForSeconds(ConfirmSound.clip.length);
        UnityEngine.SceneManagement.SceneManager.LoadScene(NextScene);
    }

    // Update is called once per frame
    IEnumerator AwaitAndInput()
    {
        yield return new WaitForSeconds(Delay);
        InputText.SetActive(true);
        _readyToTransition = true;
    }
}
