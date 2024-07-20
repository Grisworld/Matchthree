using System;
using DG.Tweening;
using Extensions.DoTween;
using Extensions.Unity;
using UnityEngine;

namespace Components.EffectObjects
{
    public class Bullet : MonoBehaviour, ITweenContainerBind
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Transform _transform;
        public ITweenContainer TweenContainer { get; set; }

        private void Awake()
        {
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
        public Transform GetTransform()
        {
            return _transform;
        }
        
        public Tween DestroyBullet()
        {
            TweenContainer.AddTween = _spriteRenderer.DOFade(0f, 1.85f);
            TweenContainer.AddedTween.onComplete += delegate { _transform.gameObject.Destroy(); };
            return TweenContainer.AddedTween;
        }
    }

    
}