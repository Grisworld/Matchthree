using System.Collections.Generic;
using Events;
using Extensions.Unity.MonoHelper;
using UnityEngine;
using Zenject;

namespace Components
{
    public class SoundManager : EventListenerMono
    {
     
        [Inject] private SoundEvents SoundEvents{get;set;}

        [SerializeField] private AudioSource AudioSource;
        [SerializeField] private List<AudioClip> AudioClips;
        
        
        private void OnPlaySound()
        {
            AudioSource.Play();
        }

        protected override void RegisterEvents()
        {
            SoundEvents.PlaySound += OnPlaySound;
        }

        protected override void UnRegisterEvents()
        {
            SoundEvents.PlaySound -= OnPlaySound;
        }
    }
}