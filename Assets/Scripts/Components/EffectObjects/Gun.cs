using System;
using DG.Tweening;
using UnityEngine;
using Extensions.DoTween;
namespace Components.EffectObjects
{
    
    public class Gun : MonoBehaviour, ITweenContainerBind
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Transform _transform;
        public ITweenContainer TweenContainer{get;set;}
        private void Awake()
        {
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
    }
    

}