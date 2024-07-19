using System;
using DG.Tweening;
using UnityEngine;
using Extensions.DoTween;
using Extensions.Unity;
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
            _myGunSettings = ProjectSettings.GunSettings;
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


        public Tween MoveAndSpawn(Vector3 toLocation)
        {
            TweenContainer.AddTween = _transform.DOMove(toLocation, 1.85f);
            TweenContainer.AddTween = _spriteRenderer.DOFade(1f, 1.85f);
            //TweenContainer.AddTween = _transform.DORotate(new Vector3(0f, 0f, 360f), 1f, RotateMode.WorldAxisAdd);
            
            return TweenContainer.AddedTween;
        }

        public Tween DestroyGun(Vector3 toLocation)
        {
            Tween takeBackGunTween = _transform.DOMove(toLocation, 1f);
            Tween fadeGun = _spriteRenderer.DOFade(0f, 1f);
            return fadeGun;

        }

        public Tween Whirl()
        {
            TweenContainer.AddTween = _transform.DORotate(new Vector3(0f, 0f, 1080f), 0.1f, RotateMode.FastBeyond360);
            TweenContainer.AddedTween.SetLoops(15);
            return TweenContainer.AddedTween;
        }
        /*public Sequence DestroyGun(Vector3 toLocation)
        {
            if (TweenContainer.AddedTween != null)
            {
                Debug.Log("still continues...");
            }
            Sequence sequence = DOTween.Sequence();
            sequence.Append(_transform.DOMove(toLocation, 1f));
            sequence.Append(_spriteRenderer.DOFade(0f, 1f));
            TweenContainer.AddSequence = sequence;
            TweenContainer.AddedSeq.onComplete += delegate { _transform.gameObject.Destroy(); };
            return TweenContainer.AddedSeq;
        }*/
        [Serializable]
        public class Settings
        {
            [SerializeField] private GameObject _explosionGas;
            public GameObject ExplosionGas => _explosionGas;
        }

        
    }
    

}