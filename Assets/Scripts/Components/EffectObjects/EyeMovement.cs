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
        private float _xOffSet;
        
        private void Awake()
        {
            TweenContainer = TweenContain.Install(this);
            _xOffSet = 0.305f;

        }

        private void OnDisable()
        {
            TweenContainer.Clear();
            
        }

        private void Start()
        {
            Vector3 initPos = _eyeTransform.position;
            
            Sequence eyeSeq = DOTween.Sequence();

            eyeSeq.SetLoops(-1);
            
            Tween moveX = _eyeTransform.DOMoveX(initPos.x + (_myTile.ID == EnvVar.
                TileLeftArrow ? _xOffSet : _xOffSet * -1 ), 1.5f);
            eyeSeq.Append(moveX);
            Tween moveXTwo = _eyeTransform.DOMoveX(initPos.x + (_myTile.ID == EnvVar.
                TileLeftArrow ? _xOffSet * -1 : _xOffSet ), 1.5f);
            eyeSeq.Append(moveXTwo);
            Tween moveXThree =  _eyeTransform.DOMoveX( initPos.x, 1.5f);
            eyeSeq.Append(moveXThree);
            Tween delayTween = DOVirtual.DelayedCall(1.5f,null);
            eyeSeq.Append(delayTween);

            TweenContainer.AddSequence = eyeSeq;
            //

        }

        
    }
}

