using Events;
using UnityEngine.SceneManagement;
using Zenject;
using Settings;
using UnityEngine;

namespace Installers
{
    public class ProjectInstaller : MonoInstaller<ProjectInstaller>
    {
        private ProjectEvents _projectEvents;
        private InputEvents _inputEvents;
        private GridEvents _gridEvents;
        private SoundEvents _soundEvents;
        private ProjectSettings _projectSettings;

        public override void InstallBindings()
        {
            InstallEvents();
            InstallSettings();
        }

        private void InstallEvents()
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

        private void InstallSettings()
        {
            _projectSettings = Resources.Load<ProjectSettings>(EnvVar.ProjectSettingsPath);
            Container.BindInstance(_projectSettings).AsSingle();
        }

        private void Awake()
        {
            RegisterEvents();
        }

        public override void Start()
        {
            _projectEvents.ProjectStarted?.Invoke();
            if (SceneManager.GetActiveScene().name == EnvVar.LoginSceneName)
            {
                LoadScene(EnvVar.MainSceneName);
            }
        }

        private static void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }

        private void RegisterEvents()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene loadedScene, LoadSceneMode arg1)
        {
            /*if(loadedScene.name == EnvVar.LoginSceneName)
            {
                LoadScene(EnvVar.MainSceneName);
            }*/
        }
    }
}