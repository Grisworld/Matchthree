using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Components.EffectObjects;
using DG.Tweening;
using Events;
using Extensions.DoTween;
using Extensions.System;
using Extensions.Unity;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using Zenject;
using Settings;
using Random = UnityEngine.Random;

namespace Components
{
    public partial class GridManager : SerializedMonoBehaviour, ITweenContainerBind
    {
        [Inject] private InputEvents InputEvents { get; set; }
        [Inject] private GridEvents GridEvents { get; set; }
        [Inject] private SoundEvents SoundEvents { get; set; }
        [Inject] private ProjectSettings ProjectSettings { get; set; }

        [BoxGroup(Order = 999)]
#if UNITY_EDITOR
        [TableMatrix(SquareCells = true, DrawElementMethod = nameof(DrawTile))]
#endif
        [OdinSerialize]
        private Tile[,] _grid;

        [SerializeField] private int _gridSizeX;
        [SerializeField] private int _gridSizeY;
        [SerializeField] private Bounds _gridBounds;
        [SerializeField] private Transform _transform;
        [SerializeField] private List<GameObject> _tileBGs = new();
        [SerializeField] private List<GameObject> _gridBorders = new();
        [SerializeField] private Transform _bGTrans;

        [SerializeField] private Transform _borderTrans;
        [SerializeField] private int _scoreMulti;
        private Settings _mySettings;

        private Tile _selectedTile;
        private Vector3 _mouseDownPos;
        private Vector3 _mouseUpPos;
        private List<MonoPool> _tilePoolsByPrefabID;
        private MonoPool _tilePool0;
        private MonoPool _tilePool1;
        private MonoPool _tilePool2;
        private MonoPool _tilePool3;
        private Tile[,] _tilesToMove;
        [OdinSerialize] private List<List<Tile>> _lastMatches;
        private Tile _hintTile;
        private GridDir _hintDir;
        private Sequence _hintTween;
        private Coroutine _destroyRoutine;
        private Coroutine _gunSpawnRoutine;
        private const float Mousethreshold = 1.0f;
        private int _bulletTrigger;
        private bool _triggered;
        private bool _checkMatch;
        public ITweenContainer TweenContainer { get; set; }
        private Coroutine _hintRoutine;

        private void Awake()
        {
            _mySettings = ProjectSettings.GridManagerSettings;
            _tilePoolsByPrefabID = new List<MonoPool>();

            for (int prefabId = 0; prefabId < _mySettings.PrefabIDs.Count; prefabId++)
            {
                MonoPool tilePool = new
                (
                    new MonoPoolData
                    (
                        _mySettings.TilePrefabs[prefabId],
                        prefabId == 4 || prefabId == 5 ? 1 : 10,
                        _transform
                    )
                );

                _tilePoolsByPrefabID.Add(tilePool);
            }

            TweenContainer = TweenContain.Install(this);
        }

        private void Start()
        {
            _bulletTrigger = 0;
            _triggered = false;
            _checkMatch = false;
            for (int x = 0; x < _grid.GetLength(0); x++)
            for (int y = 0; y < _grid.GetLength(1); y++)
            {
                Tile tile = _grid[x, y];
                SpawnTile(tile.ID, _grid.CoordsToWorld(_transform, tile.Coords), tile.Coords);
                tile.gameObject.Destroy();
            }

            IsGameOver(out _hintTile, out _hintDir);
            GridEvents.GridLoaded?.Invoke(_gridBounds);
            GridEvents.InputStart?.Invoke();
        }

        private void OnEnable()
        {
            RegisterEvents();
        }

        private void OnDisable()
        {
            UnRegisterEvents();
            TweenContainer.Clear();
        }

        private bool CanMove(Vector2Int tileMoveCoord) => _grid.IsInsideGrid(tileMoveCoord);

        // private bool HasMatch(Tile fromTile, Tile toTile, out List<List<Tile>> matches)
        // {
        //     matches = new List<List<Tile>>();
        //     bool hasMatches = false;
        //
        //     List<Tile> matchesAll = _grid.GetMatchesYAll(toTile);
        //     matchesAll.AddRange(_grid.GetMatchesXAll(toTile));
        //
        //     if(matchesAll.Count > 0)
        //     {
        //         matches.Add(matchesAll);
        //     }
        //
        //     matchesAll = _grid.GetMatchesYAll(fromTile);
        //     matchesAll.AddRange(_grid.GetMatchesXAll(fromTile));
        //
        //     if(matchesAll.Count > 0)
        //     {
        //         matches.Add(matchesAll);
        //     }
        //     
        //     if(matches.Count > 0) hasMatches = true;
        //
        //     return hasMatches;
        // }

        private bool HasAnyMatches(out List<List<Tile>> matches)
        {
            matches = new List<List<Tile>>();
            _checkMatch = true;
            foreach (Tile tile in _grid)
            {
                List<Tile> matchesAll = _grid.GetMatchesXAll(tile);
                matchesAll.AddRange(_grid.GetMatchesYAll(tile));

                if (matchesAll.Count > 0)
                {
                    matches.Add(matchesAll);
                }
            }

            matches = matches.OrderByDescending(e => e.Count).ToList();

            for (int i = 0; i < matches.Count; i++)
            {
                List<Tile> match = matches[i];
                matches[i] = match.Where(e => e.ToBeDestroyed == false).DoToAll(e => e.ToBeDestroyed = true).ToList();
            }

            matches = matches.Where(e => e.Count > 2).ToList();

            return matches.Count > 0;
        }

        private bool IsGameOver(out Tile hintTile, out GridDir hintDir)
        {
            hintDir = GridDir.Null;
            hintTile = null;

            List<Tile> matches = new();
            int maxMatch = 0;
            int temp = 0;
            foreach (Tile fromTile in _grid)
            {
                if (GridF.ControlImmovableIds(fromTile)) continue;
                Vector2Int thisCoord = fromTile.Coords;

                Vector2Int leftCoord = thisCoord + Vector2Int.left;
                Vector2Int topCoord = thisCoord + Vector2Int.up;
                Vector2Int rightCoord = thisCoord + Vector2Int.right;
                Vector2Int botCoord = thisCoord + Vector2Int.down;
                if (_grid.IsInsideGrid(leftCoord))
                {
                    Tile toTile = _grid.Get(leftCoord);
                    if (!GridF.ControlImmovableIds(toTile))
                    {
                        _grid.Swap(fromTile, toTile);

                        matches = _grid.GetMatchesX(fromTile);
                        matches.AddRange(_grid.GetMatchesY(fromTile));

                        _grid.Swap(toTile, fromTile);

                        if (matches.Count > 0)
                        {
                            if (FindMaxHint(maxMatch, matches))
                            {
                                maxMatch = matches.Count;
                                hintDir = GridDir.Left;
                            }
                        }
                    }
                }

                if (_grid.IsInsideGrid(topCoord))
                {
                    Tile toTile = _grid.Get(topCoord);
                    if (!GridF.ControlImmovableIds(toTile))
                    {
                        _grid.Swap(fromTile, toTile);

                        matches = _grid.GetMatchesX(fromTile);
                        matches.AddRange(_grid.GetMatchesY(fromTile));

                        _grid.Swap(toTile, fromTile);

                        if (matches.Count > 0)
                        {
                            if (FindMaxHint(maxMatch, matches))
                            {
                                maxMatch = matches.Count;
                                hintDir = GridDir.Up;
                            }
                        }
                    }
                }

                if (_grid.IsInsideGrid(rightCoord))
                {
                    Tile toTile = _grid.Get(rightCoord);
                    if (!GridF.ControlImmovableIds(toTile))
                    {
                        _grid.Swap(fromTile, toTile);

                        matches = _grid.GetMatchesX(fromTile);
                        matches.AddRange(_grid.GetMatchesY(fromTile));

                        _grid.Swap(toTile, fromTile);

                        if (matches.Count > 0)
                        {
                            if (FindMaxHint(maxMatch, matches))
                            {
                                maxMatch = matches.Count;
                                hintDir = GridDir.Right;
                            }
                        }
                    }
                }

                if (_grid.IsInsideGrid(botCoord))
                {
                    Tile toTile = _grid.Get(botCoord);
                    if (!GridF.ControlImmovableIds(toTile))
                    {
                        _grid.Swap(fromTile, toTile);

                        matches = _grid.GetMatchesX(fromTile);
                        matches.AddRange(_grid.GetMatchesY(fromTile));

                        _grid.Swap(toTile, fromTile);

                        if (matches.Count > 0)
                        {
                            if (FindMaxHint(maxMatch, matches))
                            {
                                maxMatch = matches.Count;
                                hintDir = GridDir.Down;
                            }
                        }
                    }
                }

                if (maxMatch > temp) hintTile = fromTile;
                //Debug.Log("maxMatch "+ maxMatch + " hintDir  "+ hintDir+ " cords " + thisCoord);
                if (maxMatch >= 7) return false;
                temp = maxMatch;
            }

            return maxMatch == 0;
        }

        private static bool FindMaxHint(int maxMatch, List<Tile> matches)
        {
            return maxMatch < matches.Count;
        }

        private void SpawnAndAllocateTiles()
        {
            _tilesToMove = new Tile[_gridSizeX, _gridSizeY];
            for (int y = 0; y < _gridSizeY; y++)
            {
                int spawnStartY = 0;
                for (int x = 0; x < _gridSizeX; x++)
                {
                    Vector2Int thisCoord = new(x, y);
                    Tile thisTile = _grid.Get(thisCoord);

                    if (thisTile) continue;

                    int spawnPoint = _gridSizeY;

                    for (int y1 = y; y1 <= spawnPoint; y1++)
                    {
                        Vector2Int emptyCoords = new(x, y1);
                        if (y1 == spawnPoint)
                        {
                            if (spawnStartY == 0)
                            {
                                spawnStartY = thisCoord.y;
                            }

                            MonoPool randomPool;
                            int index = Random.Range(0, _tilePoolsByPrefabID.ToList().Count - 2);
                            randomPool = _tilePoolsByPrefabID[index];

                            Tile newTile = SpawnTile
                            (
                                randomPool,
                                _grid.CoordsToWorld(_transform, new Vector2Int(x, spawnPoint)),
                                thisCoord
                            );

                            _tilesToMove[thisCoord.x, thisCoord.y] = newTile;
                            break;
                        }

                        Tile mostTopTile = _grid.Get(emptyCoords);
                        if (mostTopTile)
                        {
                            _grid.Set(null, mostTopTile.Coords);
                            _grid.Set(mostTopTile, thisCoord);

                            _tilesToMove[thisCoord.x, thisCoord.y] = mostTopTile;

                            break;
                        }
                    }
                }
            }

            StartCoroutine(RainDownRoutine());
        }

        private Tile SpawnTile(MonoPool randomPool, Vector3 spawnWorldPos, Vector2Int spawnCoords)
        {
            Tile newTile = randomPool.Request<Tile>();

            newTile.Teleport(spawnWorldPos);

            _grid.Set(newTile, spawnCoords);

            return newTile;
        }

        private Tile SpawnTile(int id, Vector3 worldPos, Vector2Int coords) =>
            SpawnTile(_tilePoolsByPrefabID[id], worldPos, coords);

        private IEnumerator RainDownRoutine()
        {
            int longestDistY = 0;
            Tween longestTween = null;

            for (int y = 0; y < _gridSizeY; y++) // TODO: Should start from first tile that we are moving
            {
                bool shouldWait = false;

                for (int x = 0; x < _gridSizeX; x++)
                {
                    Tile thisTile = _tilesToMove[x, y];

                    if (thisTile == false) continue;

                    Tween thisTween = thisTile.DoMove(_grid.CoordsToWorld(_transform, thisTile.Coords));

                    shouldWait = true;

                    if (longestDistY < y)
                    {
                        longestDistY = y;
                        longestTween = thisTween;
                    }
                }

                if (shouldWait)
                {
                    yield return new WaitForSeconds(0.1f);
                }
            }

            if (longestTween != null)
            {
                longestTween.onComplete += delegate
                {
                    //SHOULD BE WRITTEN HERE
                    if (_bulletTrigger == 0 && !_triggered)
                    {
                        _triggered = true;
                        StartSpawnGunRoutine();
                        return;
                    }
                    if(_checkMatch)
                        if (HasAnyMatches(out _lastMatches))
                        {
                            StartDestroyRoutine();
                        }
                        else
                        {
                            IsGameOver(out _hintTile, out _hintDir);
                            GridEvents.InputStart?.Invoke();
                        }
                };
            }
            else
            {
                Debug.LogWarning("This should not have happened!");
                GridEvents.InputStart?.Invoke();
            }
        }

        private void StartDestroyRoutine()
        {
            if (_destroyRoutine != null)
            {
                StopCoroutine(_destroyRoutine);
            }

            _destroyRoutine = StartCoroutine(DestroyRoutine());
        }

        private IEnumerator DestroyRoutine()
        {
            foreach (List<Tile> matches in _lastMatches)
            {
                IncScoreMulti();
                matches.DoToAll(DespawnTile);
                SoundEvents.PlaySound?.Invoke();
                //TODO: Show score multi text in ui as PunchScale

                GridEvents.MatchGroupDespawn?.Invoke(matches.Count * _scoreMulti, _scoreMulti);

                yield return new WaitForSeconds(0.1f);
            }

            SpawnAndAllocateTiles();
        }

        private void StartHintRoutine()
        {
            if (_hintRoutine != null)
            {
                StopCoroutine(_hintRoutine);
            }

            _hintRoutine = StartCoroutine(HintRoutineUpdate());
        }

        private void StopHintRoutine()
        {
            if (_hintTile)
            {
                _hintTile.Teleport(_grid.CoordsToWorld(_transform, _hintTile.Coords));
            }

            if (_hintRoutine != null)
            {
                StopCoroutine(_hintRoutine);
                _hintRoutine = null;
            }
        }

        private void ResetScoreMulti()
        {
            _scoreMulti = 0;
        }

        private void IncScoreMulti()
        {
            _scoreMulti++;
        }

        private IEnumerator HintRoutineUpdate()
        {
            while (true)
            {
                yield return new WaitForSeconds(3f);
                TryShowHint();
            }
        }

        private void DespawnTile(Tile e)
        {
            _grid.Set(null, e.Coords);
            _tilePoolsByPrefabID[e.ID].DeSpawn(e);
        }

        private void DoTileMoveAnim(Tile fromTile, Tile toTile, TweenCallback onComplete = null)
        {
            Vector3 fromTileWorldPos = _grid.CoordsToWorld(_transform, fromTile.Coords);
            fromTile.DoMove(fromTileWorldPos);
            Vector3 toTileWorldPos = _grid.CoordsToWorld(_transform, toTile.Coords);
            toTile.DoMove(toTileWorldPos, onComplete);
        }

        private void TryShowHint()
        {
            if (_hintTile)
            {
                Vector2Int gridMoveDir = _hintDir.ToVector();
                Vector3 gridMoveEase = gridMoveDir.ToVector3XY() * 0.66f;

                Vector3 moveCoords = _grid.CoordsToWorld(_transform, _hintTile.Coords + gridMoveDir) - gridMoveEase;

                _hintTween = _hintTile.DoHint(moveCoords);
            }
        }

        private void RegisterEvents()
        {
            InputEvents.MouseDownGrid += OnMouseDownGrid;
            InputEvents.MouseUpGrid += OnMouseUpGrid;
            GridEvents.InputStart += OnInputStart;
            GridEvents.InputStop += OnInputStop;
        }

        private void OnInputStop()
        {
            StopHintRoutine();
        }

        private void OnInputStart()
        {
            StartHintRoutine();
            ResetScoreMulti();
        }

        private void OnMouseDownGrid(Tile clickedTile, Vector3 dirVector)
        {
            _selectedTile = clickedTile;
            _mouseDownPos = dirVector;

            if (_hintTween.IsActive())
            {
                _hintTween.Complete();
            }
        }

        private void OnMouseUpGrid(Vector3 mouseUpPos)
        {
            _mouseUpPos = mouseUpPos;

            Vector3 dirVector = mouseUpPos - _mouseDownPos;
            _checkMatch = true;
            if (dirVector.magnitude < Mousethreshold) return;
            if (_selectedTile)
            {
                if (GridF.ControlImmovableIds(_selectedTile)) return;
                Vector2Int tileMoveCoord = _selectedTile.Coords + GridF.GetGridDirVector(dirVector);
                if (!CanMove(tileMoveCoord)) return;
                Tile toTile = _grid.Get(tileMoveCoord);
                if (GridF.ControlImmovableIds(toTile)) return;

                _grid.Swap(_selectedTile, toTile);

                if (!HasAnyMatches(out _lastMatches))
                {
                    GridEvents.InputStop?.Invoke();
                    if (_hintTween.IsActive())
                    {
                        _hintTween.Complete();
                    }

                    DoTileMoveAnim(_selectedTile, toTile,
                        delegate
                        {
                            _grid.Swap(toTile, _selectedTile);

                            DoTileMoveAnim(_selectedTile, toTile,
                                delegate { GridEvents.InputStart?.Invoke(); });
                        });
                }
                else
                {
                    GridEvents.InputStop?.Invoke();
                    _bulletTrigger++;
                    _bulletTrigger %= 3;
                    _triggered = false;
                    DoTileMoveAnim
                    (
                        _selectedTile,
                        toTile,
                        StartDestroyRoutine
                    );
                }
            }
        }

        private void StartSpawnGunRoutine()
        {
            if (_gunSpawnRoutine != null)
            {
                StopCoroutine(_gunSpawnRoutine);
            }

            _gunSpawnRoutine = StartCoroutine(SpawnGunAndDestroyThoseLines());
        }

        private IEnumerator SpawnGunAndDestroyThoseLines()
        {
            //ONLY ONE COURUTINE
            _checkMatch = false;
            for (int x = 0; x < _gridSizeX; x++)
            {
                for (int y = 0; y < _gridSizeY; y++)
                {
                    Tile thisTile = _grid[x, y];
                    int bulletOffSet = 8;
                    Sequence fireSeq = DOTween.Sequence();
                    if (thisTile.ID == EnvVar.TileRightArrow) //RIGHT
                    {
                        Vector3 pos = _grid.CoordsToWorld(_transform, thisTile.Coords);
                        float bulletLocOffSet = _grid[_gridSizeX - 1, y].ID == EnvVar.TileLeftArrow
                            ? pos.x + bulletOffSet
                            : pos.x + bulletOffSet + 1;

                        GameObject gunNew = GridEvents.InsPrefab
                        (
                            _mySettings.Gun
                        );
                        GameObject bulletNew = Instantiate
                        (
                            _mySettings.Bullet
                        );
                        bulletNew.transform.position = pos;
                        Vector3 bulletLocPos = new Vector3(bulletLocOffSet, pos.y, pos.z);
                        //x = +-0.606 for summon and +-0.426 to put
                        pos.y -= 0.22f;
                        pos.x -= 0.606f;
                        gunNew.transform.position = pos;
                        Vector3 tLoc = new Vector3(pos.x + 0.18f, pos.y, pos.z);
                        Gun gun = gunNew.GetComponent<Gun>();
                        Tween gunSpawnTween = gun.MoveAndSpawn(tLoc);
                        fireSeq.Append(gunSpawnTween);
                        fireSeq.Append(gun.Whirl());
                        Bullet bullet = bulletNew.GetComponent<Bullet>();
                        fireSeq.Append(bullet.GetSprite().DOFade(1f, 1.5f));
                        fireSeq.Append(SendTheBullet(bulletLocPos, bullet));
                        fireSeq.Append(bullet.DestroyBullet());
                        fireSeq.Append(gun.DestroyGun(new Vector3(pos.x - 0.18f, pos.y,pos.z)));
                        fireSeq.onComplete += SpawnAndAllocateTiles;
                        yield return new WaitForSeconds(10f);
                    }
                    else if (thisTile.ID == EnvVar.TileLeftArrow) //LEFT
                    {
                        Vector3 pos = _grid.CoordsToWorld(_transform, thisTile.Coords);
                        float bulletLocOffSet = _grid[0, y].ID == EnvVar.TileRightArrow
                            ? pos.x - bulletOffSet
                            : pos.x - (bulletOffSet + 1);

                        GameObject gunNew = GridEvents.InsPrefab
                        (
                            _mySettings.Gun
                        );

                        GameObject bulletNew = Instantiate
                        (
                            _mySettings.Bullet
                        );

                        bulletNew.transform.position = pos;
                        Vector3 bulletLocPos =
                            new Vector3(bulletLocOffSet, pos.y, pos.z);
                        pos.y -= 0.22f;
                        pos.x += 0.606f;
                        gunNew.transform.position = pos;
                        Vector3 tLoc = new Vector3(pos.x - 0.18f, pos.y, pos.z);
                        Gun gun = gunNew.GetComponent<Gun>();
                        Tween gunSpawnTween = gun.MoveAndSpawn(tLoc);
                        fireSeq.Append(gunSpawnTween);
                        fireSeq.Append(gun.Whirl());
                        gun.GetSprite().flipX = true;
                        Bullet bullet = bulletNew.GetComponent<Bullet>();
                        fireSeq.Append(bullet.GetSprite().DOFade(1f, 1.5f));
                        bullet.GetSprite().flipX = true;
                        fireSeq.Append(SendTheBullet(bulletLocPos, bullet));
                        fireSeq.Append(bullet.DestroyBullet());
                        fireSeq.Append(gun.DestroyGun(new Vector3(pos.x + 0.18f, pos.y,pos.z)));
                        fireSeq.onComplete += SpawnAndAllocateTiles;
                        yield return new WaitForSeconds(10f);
                    }
                }
            }

            if (HasAnyMatches(out _lastMatches))
            {
                StartDestroyRoutine();
            }
            else
            {
                GridEvents.InputStart?.Invoke();
            }
            //yield return RainDownRoutine();// GOES INTO THIS ROUTINE AND RETURN THIS ONE
        }

//After 0.7 difference Rain that Column
        private Tween SendTheBullet(Vector3 bulletLocPos, Bullet bullet)
        {
            
            Tween bulletPosTw = bullet.GetTransform().DOMove(bulletLocPos, 1.85f);
            Tile lastTile = null;
            bulletPosTw.onUpdate += delegate
            {
                Vector3 currPosOfBullet = bullet.GetTransform().position;
                Tile shottedTile = _grid[(int)currPosOfBullet.x, (int)currPosOfBullet.y];
                if (shottedTile != null && !GridF.ControlImmovableIds(shottedTile))
                {
                    if (_grid.CoordsToWorld(_transform, shottedTile.Coords).x - currPosOfBullet.x < 0.550001f)
                    {
                        GridEvents.MatchGroupDespawn?.Invoke(1,_scoreMulti);
                        DespawnTile(shottedTile);
                    }
                }
                //LAST PART
                else if (shottedTile == null)
                {
                    if (currPosOfBullet.x - _grid.CoordsToWorld(_transform, lastTile.Coords).x < 0.750001f)
                    {
                        Debug.Log("Tile is avaible after destroyed?");
                    }
                }

                lastTile = shottedTile;
            };
            return bulletPosTw;
        }

        private bool Tryings()
        {
            return true;
        }


        private void UnRegisterEvents()
        {
            InputEvents.MouseDownGrid -= OnMouseDownGrid;
            InputEvents.MouseUpGrid -= OnMouseUpGrid;
            GridEvents.InputStart -= OnInputStart;
            GridEvents.InputStop -= OnInputStop;
        }

        public class GridManagerNested
        {
            public void Main(GridManager gridManager)
            {
                gridManager.UnRegisterEvents();
            }
        }

        [Serializable]
        public class Settings
        {
            public List<GameObject> TilePrefabs => _tilePrefabs;
            public List<int> PrefabIDs => _prefabIds;
            public GameObject TileBGPrefab => _tileBGPrefab;
            [SerializeField] private GameObject _tileBGPrefab;
            [SerializeField] private List<int> _prefabIds;
            [SerializeField] private List<GameObject> _tilePrefabs;
            [SerializeField] private GameObject _borderTopLeft;
            [SerializeField] private GameObject _borderTopRight;
            [SerializeField] private GameObject _borderBotLeft;
            [SerializeField] private GameObject _borderBotRight;
            [SerializeField] private GameObject _borderLeft;
            [SerializeField] private GameObject _borderRight;
            [SerializeField] private GameObject _borderTop;
            [SerializeField] private GameObject _borderBot;
            [SerializeField] private GameObject _gun;
            [SerializeField] private GameObject _bullet;
            public GameObject BorderTopLeft => _borderTopLeft;
            public GameObject BorderTopRight => _borderTopRight;
            public GameObject BorderBotLeft => _borderBotLeft;
            public GameObject BorderBotRight => _borderBotRight;
            public GameObject BorderLeft => _borderLeft;
            public GameObject BorderRight => _borderRight;
            public GameObject BorderTop => _borderTop;
            public GameObject BorderBot => _borderBot;
            public GameObject Gun => _gun;
            public GameObject Bullet => _bullet;
        }
    }
}