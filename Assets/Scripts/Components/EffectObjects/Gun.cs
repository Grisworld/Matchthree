using System;
using DG.Tweening;
using Events;
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
        public ITweenContainer TweenContainer{get;set;}

        [Inject] private ProjectSettings ProjectSettings{get;set;}
        [Inject] private SoundEvents SoundEvents { get; set; }
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Transform _transform;
        private Settings _myGunSettings;
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


        public Sequence MoveAndSpawn(Vector3 toLocation)
        {
            Sequence sequence = DOTween.Sequence();
            
            Tween moveGunTween = _transform.DOMove(toLocation, 1f);
            Tween fadeTween = _spriteRenderer.DOFade(1f, 1f);
            //TweenContainer.AddTween = _transform.DORotate(new Vector3(0f, 0f, 360f), 1f, RotateMode.WorldAxisAdd);
            
            sequence.Append(moveGunTween);
            sequence.Join(fadeTween);
            TweenContainer.AddSequence = sequence;
            
            return sequence;
        }

        public Sequence DestroyGun(Vector3 toLocation)
        {
            Sequence sequence = DOTween.Sequence();
            
            Tween takeBackGunTween = _transform.DOMove(toLocation, 1f);
            Tween fadeGun = _spriteRenderer.DOFade(0f, 1f);

            sequence.Append(takeBackGunTween);
            sequence.Join(fadeGun);
            
            TweenContainer.AddSequence = sequence;
            TweenContainer.AddedSeq.onComplete += delegate
            {
                _transform.gameObject.Destroy();
                _myGunSettings.ExplosionGas.transform.gameObject.Destroy();
                _myGunSettings.ExplosionGasRotatedZ.transform.gameObject.Destroy();
            };
            return TweenContainer.AddedSeq;

        }

        public Tween Whirl(float xOffSet,float yOffSet,bool flip)
        {
            TweenContainer.AddTween = _transform.DORotate(new Vector3(0f, 0f, 1080f), 0.1f, RotateMode.FastBeyond360);
            TweenContainer.AddedTween.SetLoops(15);
            TweenContainer.AddedTween.onComplete += delegate
            {
                GameObject gas = Instantiate(
                    flip ? _myGunSettings.ExplosionGasRotatedZ : _myGunSettings.ExplosionGas,
                    new Vector3(_transform.position.x + xOffSet,_transform.position.y + yOffSet ,0f),
                    Quaternion.identity
                    );
                SoundEvents.PlaySound?.Invoke(1);
            };
            return TweenContainer.AddedTween;
        }

        public Tween ShakeGun()
        {
            TweenContainer.AddTween = _transform.DOShakeScale(1f);

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
            [SerializeField] private GameObject _explosionGasRotatedZ;
            public GameObject ExplosionGas => _explosionGas;
            
            public GameObject ExplosionGasRotatedZ => _explosionGasRotatedZ;
        }

        
    }
    

}