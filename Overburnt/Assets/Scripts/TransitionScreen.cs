using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransitionScreen : MonoBehaviour
{
    public GameObject InputText;
    public float Delay;
    public string NextScene;
    public AudioSource ConfirmSound;
    // Start is called before the first frame update
    void Start()
    {
        InputText.SetActive(false);
        StartCoroutine(AwaitAndInput());
    }

    // Update is called once per frame
    IEnumerator AwaitAndInput()
    {
        yield return new WaitForSeconds(Delay);
        InputText.SetActive(true);
        while(!Input.anyKey)
        {
            yield return null;
        }
        ConfirmSound.Play();
        yield return new WaitForSeconds(ConfirmSound.clip.length);
        UnityEngine.SceneManagement.SceneManager.LoadScene(NextScene);
    }
}
