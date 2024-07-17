using System;
using System.Threading;
using Components;
using DG.Tweening;
using Extensions.DoTween;
using UnityEngine;

namespace Components.EffectObjects
{
    public class EyeMovement : MonoBehaviour, ITweenContainerBind
    {
        [SerializeField] private Tile _myTile;
        [SerializeField] private Transform _eyeTransform;
        public ITweenContainer TweenContainer{get;set;}
        private Sequence _eyeSeq;
        private int _milliSeconds;
        private float _xOffSet;
        private float _posX;
        
        private void Awake()
        {
            TweenContainer = TweenContain.Install(this);
            _eyeSeq = DOTween.Sequence();
            _milliSeconds = 2250;
            _xOffSet = 0.305f;
            _posX = _eyeTransform.position.x;

        }

        private void OnDisable()
        {
            TweenContainer.Clear();
            
        }

        private void Start()
        {
            _eyeSeq.SetLoops(-1);
            TweenContainer.AddTween = _eyeTransform.DOMoveX(_eyeTransform.position.x + (_myTile.ID == EnvVar.
                TileLeftArrow ? _xOffSet : _xOffSet * -1 ), 1.5f);
            TweenContainer.AddedTween.onComplete += delegate
            {
                _posX = _eyeTransform.position.x;
            };
            _eyeSeq.Append(TweenContainer.AddedTween);
            TweenContainer.AddTween = _eyeTransform.DOMoveX(_eyeTransform.position.x + (_myTile.ID == EnvVar.
                TileLeftArrow ? _xOffSet * -1 : _xOffSet ), 1.5f);
            _eyeSeq.Append(TweenContainer.AddedTween);
            _eyeSeq.onComplete += delegate
            {
                _posX = _eyeTransform.position.x;
            };
            
            //

        }

        private void WaitForOneAndQuarterMin()
        {
            Thread.Sleep(_milliSeconds);
        }
    }
}

