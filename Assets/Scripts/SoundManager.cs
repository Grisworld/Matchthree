using System;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _audioClip;
    public static SoundManager _instance;
    private void Start()
    {
        if (_instance == null)
        {
            _instance = this;
        }
    }

    public void PlaySound()
    {
        _audioSource.Play();
    }
}