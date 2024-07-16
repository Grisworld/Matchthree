using Components;
using Components.EffectObjects;
using UnityEngine;

namespace Settings
{
    [CreateAssetMenu(fileName = nameof(ProjectSettings), menuName = EnvVar.ProjectSettingsPath, order = 0)]
    public class ProjectSettings : ScriptableObject
    {
        [SerializeField] private GridManager.Settings _gridManagerSettings;
        [SerializeField] private Gun.Settings _gunSettings;
        public GridManager.Settings GridManagerSettings => _gridManagerSettings;
        public Gun.Settings GunSettings => _gunSettings;
    }
}