using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace Extensions.DoTween
{
    public class TweenContain : ITweenContainer
    {
        public event UnityAction OnAllTweensCompleted;
        public Tween AddTween
        {
            set
            {
                AddedTween = value;
                AddTweenMethod(value);
            }
        }

        public Tween AddedTween { get; private set; }
        public Sequence AddSequence {
            set
            {
                AddedSeq = value;
                AddTweenMethod(value);
            }
        }
        public Sequence AddedSeq { get; private set; }

        private List<Tween> _activeTweens = new List<Tween>();
        private ITweenContainerBind _myBind;

        private TweenContain(ITweenContainerBind myBind)
        {
            _myBind = myBind;
        }

        public static ITweenContainer Install(ITweenContainerBind tweenContainerBind)
        {
            TweenContain newTweenContain = new TweenContain(tweenContainerBind);
            tweenContainerBind.TweenContainer = newTweenContain;
            return newTweenContain;
        }

        private void AddTweenMethod(Tween tween)
        {
            _activeTweens.Add(tween);

            tween.onComplete += delegate
            {
                OnComplete(tween);
            };

            tween.onKill += delegate
            {
                OnKill(tween);
            };
        }

        private void OnKill(Tween tween)
        {
            if (_activeTweens.Contains(tween) == false) return;

            List<Tween> activeTweens = new List<Tween>(_activeTweens);
            activeTweens.Remove(tween);

            _activeTweens = activeTweens.Where(at => at != null).ToList();
            
            if (_activeTweens.Count == 0)
            {
                OnAllTweensCompleted?.Invoke();
            }
        }

        private void OnComplete(Tween tween)
        {
            if (_activeTweens.Contains(tween) == false) return;

            List<Tween> activeTweens = new List<Tween>(_activeTweens);
            activeTweens.Remove(tween);

            _activeTweens = activeTweens.Where(at => at != null).ToList();

            if (_activeTweens.Count == 0)
            {
                OnAllTweensCompleted?.Invoke();
            }
        }

        public void Clear()
        {
            foreach (Tween activeTween in _activeTweens)
            {
                activeTween.Kill();
            }

            _activeTweens = new List<Tween>();
        }

        private IEnumerator WaitForAllTweensEndedRoutine()
        {
            while (_activeTweens.Count > 0)
            {
                yield return null;
            }
            
        }

        public YieldInstruction WaitForAllTweensEnded()
        {
            return _myBind.StartCoroutine(WaitForAllTweensEndedRoutine());
            
        }
    }

    public interface ITweenContainerBind
    {
        ITweenContainer TweenContainer { get; set; }

        Coroutine StartCoroutine(IEnumerator coroutine);
    }

    public interface ITweenContainer
    {
        Tween AddTween { set; }
        Tween AddedTween { get; }
        Sequence AddSequence { set; }
        Sequence AddedSeq { get; }
        void Clear();

        YieldInstruction WaitForAllTweensEnded();
    }
}