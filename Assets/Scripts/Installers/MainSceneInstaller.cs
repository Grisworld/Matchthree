using System.Collections.Generic;
using Events;
using UnityEngine;
using Zenject;

namespace Installers
{
    public class MainSceneInstaller : MonoInstaller<MainSceneInstaller>
    {
        [SerializeField] private Camera _camera;
        
        public override void InstallBindings()
        {
            Container.BindInstance(_camera);
            
        }

        public override void Start()
        {
            GridEvents gridEvents = Container.Resolve<GridEvents>();
            gridEvents.InsPrefab += OnInsPrefab;
        }

        private GameObject OnInsPrefab(GameObject arg)
        {
            return Container.InstantiatePrefab(arg);
        }
    }
}