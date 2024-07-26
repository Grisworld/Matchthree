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
        private GameObject _lastGas;

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

        private bool SummonExplosion(float xOffSet, float yOffSet, bool flip)
        {
            var position = _transform.position;
            _lastGas = Instantiate(
                flip ? _myGunSettings.ExplosionGasRotatedZ : _myGunSettings.ExplosionGas,
                new Vector3(position.x + xOffSet,position.y + yOffSet ,0f),
                Quaternion.identity
            );
            SoundEvents.Play?.Invoke(1,0.65f,false,128);
            return false;
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

            sequence.onPlay += delegate
            {         
                SoundEvents.Play?.Invoke(5,1.0f,false,128);
            };
            
            Tween takeBackGunTween = _transform.DOMove(toLocation, 1f);
            Tween whiteFadeOut = _spriteRenderer.DOColor(new Color(1f,1f,1f,0f), 0.75f);

            sequence.Append(takeBackGunTween);
            sequence.Join(whiteFadeOut);
            
            TweenContainer.AddSequence = sequence;
            TweenContainer.AddedSeq.onComplete += delegate
            {
                _transform.gameObject.Destroy();
                if (_lastGas != null)
                {
                    _lastGas.Destroy();
                }
            };
            return TweenContainer.AddedSeq;

        }

        public Tween Whirl()
        {
            
            TweenContainer.AddTween = _transform.DORotate(new Vector3(0f, 0f, 1080f), 0.1f, RotateMode.FastBeyond360);
            TweenContainer.AddedTween.SetLoops(15);
            
            TweenContainer.AddedTween.onPlay += delegate
            {         
                SoundEvents.Play?.Invoke(4,1.0f,true,128);
            };
            
            TweenContainer.AddedTween.onComplete += delegate
            {         
                SoundEvents.Stop?.Invoke();
            };
            
            return TweenContainer.AddedTween;
        }

        public Sequence ShakeGun(float xOffSet,float yOffSet,bool flip)
        {
            Sequence shakeSeq = DOTween.Sequence();
            
            shakeSeq.onPlay += delegate
            {                 
                SoundEvents.Play?.Invoke(6,0.35f,false,100);
    
            };
            
            Tween shakeTween = _transform.DOShakeScale(0.75f,1f,100);
            Tween redColorTween = _spriteRenderer.DOColor(Color.red, 0.75f);
            
            shakeSeq.Append(shakeTween);
            shakeSeq.Join(redColorTween);
            
            TweenContainer.AddSequence = shakeSeq;
            TweenContainer.AddedSeq.onComplete += delegate
            {
                SummonExplosion(xOffSet, yOffSet, flip);
            };
            
            
            return TweenContainer.AddedSeq;
        }
        
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