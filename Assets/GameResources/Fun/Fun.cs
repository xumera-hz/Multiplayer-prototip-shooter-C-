using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fun : MonoBehaviour {

    [SerializeField]
    GameObject m_Audio;
    [SerializeField]
    GameObject m_Fun;
    [SerializeField]
    GameObject m_Explosion;

    public void ExitGame()
    {
        m_Explosion.SetActive(true);
        Invoke("Next", 1f);
    }

    void PlayAudio()
    {
        if (m_Audio != null) m_Audio.SetActive(true);
    }

    void Next()
    {
        PlayAudio();
        Time.timeScale = 0f;
        m_Fun.SetActive(true);
        StartCoroutine(ExitWait(8f));
    }

    IEnumerator ExitWait(float timer)
    {
        while (timer > 0f)
        {
            yield return null;
            timer -= Time.unscaledDeltaTime;
        }
        Application.Quit();
    }
}
