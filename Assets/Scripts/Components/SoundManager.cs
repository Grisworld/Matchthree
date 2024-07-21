using System;
using System.Collections.Generic;
using Events;
using Extensions.Unity.MonoHelper;
using Settings;
using UnityEngine;
using Zenject;

namespace Components
{
    public class SoundManager : EventListenerMono
    {
     
        [Inject] private SoundEvents SoundEvents{get;set;}
        [Inject] private ProjectSettings ProjectSettings{get;set;}
        
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioSource _audioSourceUnneces;
        
        private Settings _mySoundSettings;

        private void Awake()
        {
            _mySoundSettings = ProjectSettings.SoundSettings;
        }

        protected override void RegisterEvents()
        {
            SoundEvents.Play += OnPlaySound;
            SoundEvents.Stop += OnStopSound;
        }
        private void OnPlaySound(int index, float pitch, bool loop,int priority)
        {
            if (index == 1)
            {
                _audioSourceUnneces.clip =  _mySoundSettings.AudioClips[index];
                _audioSourceUnneces.Play();
                return;
            }
            _audioSource.clip = _mySoundSettings.AudioClips[index];
            _audioSource.priority = priority;
            _audioSource.pitch = pitch;
            _audioSource.loop = loop;
            
            _audioSource.Play();
            
        }
        private void OnStopSound()
        {
            _audioSource.Stop();
            _audioSource.clip = null;
        }

        protected override void UnRegisterEvents()
        {
            SoundEvents.Play -= OnPlaySound;
            SoundEvents.Stop -= OnStopSound;
        }
        [Serializable]
        public class Settings
        {
            [SerializeField] private List<AudioClip> _audioClips;
            public List<AudioClip> AudioClips => _audioClips;
        }
    }
}