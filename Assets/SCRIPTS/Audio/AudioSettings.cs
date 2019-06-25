using UnityEngine;

[RequireComponent(typeof(AudioController))]
public class AudioSettings : MonoBehaviour {

    [SerializeField] float m_Music = 1f;
    [SerializeField] float m_Sound = 1f;

    float Music { get { return m_Music; } set { } }
    float Sound { get { return m_Sound; } set { } }

    void Start()
    {
        AudioController.SetVolume(Music, Sound);
    }

    void Update()
    {
        AudioController.ManualUpdate(TimeManager.TimeDeltaTime, TimeManager.UnscaledDeltaTime);
    }
    
    void OnEnable()
    {
        AudioController.Change += OnChangeAudio;
    }

    void OnDisable()
    {
        AudioController.Change -= OnChangeAudio;
    }

    void OnChangeAudio()
    {
        Sound = AudioController.SoundVolume;
        Music = AudioController.MusicVolume;
    }
    
}
