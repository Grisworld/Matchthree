using System;
using System.Collections.Generic;
using DG.Tweening;
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
        private Settings _mySoundSettings;

        private void Awake()
        {
            _mySoundSettings = ProjectSettings.SoundSettings;
        }

        private void OnPlaySound(int index)
        {
            _audioSource.clip = _mySoundSettings.AudioClips[index];
            _audioSource.Play();
        }

        protected override void RegisterEvents()
        {
            SoundEvents.PlaySound += OnPlaySound;
        }

        protected override void UnRegisterEvents()
        {
            SoundEvents.PlaySound -= OnPlaySound;
        }
        [Serializable]
        public class Settings
        {
            [SerializeField] private List<AudioClip> _audioClips;
            public List<AudioClip> AudioClips => _audioClips;
        }
    }
}