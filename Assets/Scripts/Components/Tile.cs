using System;
using System.Collections.Generic;
using DG.Tweening;
using Extensions.DoTween;
using Extensions.Unity;
using UnityEngine;

namespace Components
{
    public class Tile : MonoBehaviour, ITileGrid, IPoolObj, ITweenContainerBind
    {
        public Vector2Int Coords => _coords;
        public int ID => _id;

        public List<Vector3> TweenCoords => _tweenCoords;
        private List<Vector3> _tweenCoords;
        
        [SerializeField] private Vector2Int _coords;
        [SerializeField] private int _id;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Transform _transform;
        public bool DidSpawnGun { get; set; }
        public MonoPool MyPool{get;set;}
        public ITweenContainer TweenContainer{get;set;}
        public bool ToBeDestroyed{get;set;}

        public SpriteRenderer GetSprite()
        {
            return _spriteRenderer;
        }
        private void Awake()
        {
            TweenContainer = TweenContain.Install(this);
            _tweenCoords = new List<Vector3>();

        }

        private void OnDisable()
        {
            TweenContainer.Clear();
        }


        void ITileGrid.SetCoord(Vector2Int coord)
        {
            _coords = coord;
        }

        void ITileGrid.SetCoord(int x, int y)
        {
            _coords = new Vector2Int(x, y);
        }

        public void AfterCreate()
        {
        }

        public void BeforeDeSpawn()
        {
        }

        public void TweenDelayedDeSpawn(Func<bool> onComplete) {}

        public void AfterSpawn()
        {
            ToBeDestroyed = false;
            _tweenCoords = new List<Vector3>();

            //RESET METHOD (Resurrect)
            
        }

        public void Teleport(Vector3 worldPos)
        {
            _transform.position = worldPos;
        }

        public void Construct(Vector2Int coords) {_coords = coords;}

        public Tween DoMove(Vector3 worldPos, float dur = 1f, TweenCallback onComplete = null)
        {
            TweenContainer.AddTween = _transform.DOMove(worldPos,dur);
            TweenContainer.AddedTween.onComplete += onComplete;

            return TweenContainer.AddedTween;
        }

        public Sequence DoHint(Vector3 worldPos, TweenCallback onComplete = null)
        {
            _spriteRenderer.sortingOrder = EnvVar.HintSpriteLayer;
            Vector3 lastPos = _transform.position;
            
            TweenContainer.AddSequence = DOTween.Sequence();
            
            TweenContainer.AddedSeq.Append(_transform.DOMove(worldPos, 1f));
            TweenContainer.AddedSeq.Append(_transform.DOMove(lastPos, 1f));

            TweenContainer.AddedSeq.onComplete += onComplete;
            TweenContainer.AddedSeq.onComplete += delegate
            {
                _spriteRenderer.sortingOrder = EnvVar.TileSpriteLayer;
            };
            return TweenContainer.AddedSeq;
        }

        public void AddCoord(Vector3 coord)
        {
            _tweenCoords.Add(coord);
        }

        public void SetCoordsFree()
        {
            _tweenCoords = new List<Vector3>();
        }

        public Tween DoMoveAllPath(List<Vector3> movePoses, float moveSpeed, TweenCallback onComplete = null)
        {
            Vector3 prevPos = _transform.position;
            float totalDist = 0f;
            
            for (int i = 0; i < movePoses.Count; i++)
            {
                Vector3 currPos = movePoses[i];
                totalDist += Vector3.Distance(prevPos, currPos);
                prevPos = currPos;
            }
            
            TweenContainer.AddTween = _transform.DOPath(movePoses.ToArray(), totalDist / moveSpeed,PathType.Linear, PathMode.TopDown2D, 25, Color.red);
            
            float tweenDuration = TweenContainer.AddedTween.Duration();
            
//            Debug.Log(tweenDuration);
            
            TweenContainer.AddedTween.onComplete += onComplete;

            return TweenContainer.AddedTween;
        }
    }

    public interface ITileGrid
    {
        void SetCoord(Vector2Int coord);
        void SetCoord(int x, int y);
    }
}