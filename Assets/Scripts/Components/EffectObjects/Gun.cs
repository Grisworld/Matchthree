using System;
using DG.Tweening;
using UnityEngine;
using Extensions.DoTween;
using Extensions.Unity.MonoHelper;
using Settings;
using Sirenix.OdinInspector;
using Zenject;

namespace Components.EffectObjects
{
    
    public class Gun : MonoBehaviour, ITweenContainerBind
    {
        [Inject] private ProjectSettings ProjectSettings{get;set;}

        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Transform _transform;
        private Settings _myGunSettings;
        public ITweenContainer TweenContainer{get;set;}
        private void Awake()
        {
            if (ProjectSettings == null)
            {
                Debug.Log("Its null!!");
            }
//            _myGunSettings = ProjectSettings.GunSettings;
            var color = _spriteRenderer.color;
            color.a = 0f;
            _spriteRenderer.color = color;
            TweenContainer = TweenContain.Install(this);
        }
        private void OnDisable()
        {
            TweenContainer.Clear();
        }

       

        public SpriteRenderer GetSprite()
        {
            return _spriteRenderer;
        }


        public void MoveAndSpawn(Vector3 toLocation)
        {
            TweenContainer.AddTween = _transform.DOMove(toLocation, 1.85f);
            TweenContainer.AddTween = _spriteRenderer.DOFade(1f, 1.85f);
        }

        [Serializable]
        public class Settings
        {
            [SerializeField] private GameObject _explosionGas;
            public GameObject ExplosionGas => _explosionGas;
        }
    }
    

}