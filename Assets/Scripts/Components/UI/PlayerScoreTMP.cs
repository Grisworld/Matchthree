﻿using DG.Tweening;
using Events;
using Extensions.DoTween;
using Extensions.Unity.MonoHelper;
using TMPro;
using UnityEngine;
using Zenject;

namespace Components.UI
{
    public class PlayerScoreTMP : UITMP, ITweenContainerBind
    {
        [Inject] private GridEvents GridEvents { get; set; }
        [Inject] private SoundEvents SoundEvents { get; set; }
        
        [SerializeField] private TextMeshProUGUI _multTMP;
        [SerializeField] private RectTransform _multRectTrans;
        private Tween _counterTween;
        private Tween _multTween;
        public ITweenContainer TweenContainer { get; set; }
        private int _currCounterVal;

        private int _playerScore;

        //DOPUNCH SCALE 5
        private void Awake()
        {
            TweenContainer = TweenContain.Install(this);
        }

        protected override void RegisterEvents()
        {
            GridEvents.MatchGroupDespawn += OnMatchGroupDespawn;
        }

        private void OnMatchGroupDespawn(int arg0, int mult)
        {
            

            _playerScore += arg0;
            
            if (_counterTween.IsActive()) _counterTween.Kill();
            if (_multTween.IsActive()) _multTween.Kill();
            _multRectTrans.localScale = Vector3.one;
            
            _counterTween = DOVirtual.Int
            (
                _currCounterVal,
                _playerScore,
                1f,
                OnCounterUpdate
            );

            TweenContainer.AddTween = _counterTween;
            
            
            if (mult > 1)
            {
                //counterTween.onComplete += delegate
                //{
                _multTMP.enabled = true;
                _multTMP.text = $"X{mult}";
                //};
                
                _multTween = _multRectTrans.DOPunchScale(Vector3.one * 1.3f, 1.5f);
                TweenContainer.AddTween = _multTween;
                
                if(mult == 2)
                    SoundEvents.Play?.Invoke(3,1.0f,false,128);
                else if(mult == 3)
                    SoundEvents.Play?.Invoke(3,1.20f,false,128);
                else if(mult == 4)
                    SoundEvents.Play?.Invoke(3,1.40f,false,128);
                else
                    SoundEvents.Play?.Invoke(3,1.6f,false,128);
            }

            _counterTween.onComplete += delegate
            {
                _multRectTrans.localScale = Vector3.one;
                _multTMP.enabled = false;
            };
        }


        private void OnCounterUpdate(int val)
        {
            _currCounterVal = val;
            _myTMP.text = $"Score: {_currCounterVal}";
        }

        protected override void UnRegisterEvents()
        {
            GridEvents.MatchGroupDespawn -= OnMatchGroupDespawn;
            TweenContainer.Clear();
        }
        
    }
}