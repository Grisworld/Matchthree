using Events;
using UnityEngine.SceneManagement;
using Zenject;

namespace Installers
{
    public class ProjectInstaller : MonoInstaller<ProjectInstaller>
    {
        private ProjectEvents _projectEvents;
        private InputEvents _inputEvents;
        private GridEvents _gridEvents;
        private SoundEvents _soundEvents;

        public override void InstallBindings()
        {
            _projectEvents = new ProjectEvents();
            Container.BindInstance(_projectEvents).AsSingle();
            
            _inputEvents = new InputEvents();
            Container.BindInstance(_inputEvents).AsSingle();

            _gridEvents = new GridEvents();
            Container.BindInstance(_gridEvents).AsSingle();
            
            _soundEvents = new SoundEvents();
            Container.BindInstance(_soundEvents).AsSingle();
            
        }

        private void Awake()
        {
            RegisterEvents();
        }

        public override void Start()
        {
            _projectEvents.ProjectStarted?.Invoke();
        }

        private static void LoadScene(string sceneName) {SceneManager.LoadScene(sceneName);}

        private void RegisterEvents()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene loadedScene, LoadSceneMode arg1)
        {
            if(loadedScene.name == EnvVar.LoginSceneName)
            {
                LoadScene(EnvVar.MainSceneName);
            }
        }
    }
}