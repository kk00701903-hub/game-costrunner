using UnityEngine;

/// One pure tone that must never share a bus with anything else. The counter
/// window is taught by ear as much as by colour, so this cue stays mandatory
/// even while the rest of the mix is silent.
public class CounterCueTone : MonoBehaviour
{
    public static CounterCueTone Instance { get; private set; }

    [SerializeField] private float frequency = 1760f;
    [SerializeField] private float duration = 0.18f;
    [SerializeField] private float volume = 0.55f;
    [SerializeField] private float missFrequency = 1864.66f;

    private AudioSource _source;
    private AudioClip _openClip;
    private AudioClip _missClip;

    private void Awake()
    {
        Instance = this;
        _source = gameObject.AddComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.spatialBlend = 0f;
        _source.loop = false;
        _source.priority = 0;
        _openClip = BuildTone(frequency, duration, volume);
        _missClip = BuildTone(missFrequency, duration * 0.7f, volume * 0.8f);
    }

    public void PlayOpen()
    {
        if (_source == null || _openClip == null)
            return;
        _source.PlayOneShot(_openClip, 1f);
    }

    public void PlayMiss()
    {
        if (_source == null || _missClip == null)
            return;
        _source.PlayOneShot(_missClip, 1f);
    }

    private static AudioClip BuildTone(float hz, float seconds, float amp)
    {
        int sampleRate = 44100;
        int samples = Mathf.Max(64, Mathf.CeilToInt(sampleRate * seconds));
        float[] data = new float[samples];
        float attack = 0.01f;
        float release = 0.05f;

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = 1f;
            if (t < attack)
                envelope = t / attack;
            else if (t > seconds - release)
                envelope = Mathf.Max(0f, (seconds - t) / release);

            data[i] = Mathf.Sin(2f * Mathf.PI * hz * t) * amp * envelope;
        }

        AudioClip clip = AudioClip.Create("CounterCue_" + Mathf.RoundToInt(hz), samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
