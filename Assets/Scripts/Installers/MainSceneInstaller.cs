using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Installers
{
    public class MainSceneInstaller : MonoInstaller<MainSceneInstaller>
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private List<AudioClip> _audioClips;
        public override void InstallBindings()
        {
            Container.BindInstance(_camera);
            Container.BindInstance(_audioSource);
            Container.BindInstance(_audioClips);
        }
    }
}