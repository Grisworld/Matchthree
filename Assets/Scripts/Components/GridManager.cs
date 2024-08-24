using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
        private List<Tile>[] _movementSequences;
        
        private MonoPool _tilePool0;
        private MonoPool _tilePool1;
        private MonoPool _tilePool2;
        private MonoPool _tilePool3;
        private Tile[,] _tilesToMove;
        [OdinSerialize] private List<List<Tile>> _lastMatches;
        private Tile _hintTile;
        private GridDir _hintDir;
        private Sequence _hintTween;
        private const float Mousethreshold = 1.0f;
        private int _matchCounter;
        public ITweenContainer TweenContainer { get; set; }
        private Coroutine _hintRoutine;
        private List<Tile> _gunTiles = new();
        private Coroutine _gridCheckRoutine;
        private bool _canSpawnGuns;

        private void Awake()
        {
            _mySettings = ProjectSettings.GridManagerSettings;
            _tilePoolsByPrefabID = new List<MonoPool>();

            for (var prefabId = 0; prefabId < _mySettings.PrefabIDs.Count; prefabId++)
            {
                MonoPool tilePool = new
                (
                    new MonoPoolData
                    (
                        _mySettings.TilePrefabs[prefabId],
                        10,
                        _transform
                    )
                );

                _tilePoolsByPrefabID.Add(tilePool);
            }

            TweenContainer = TweenContain.Install(this);
        }

        private void Start()
        {
            _matchCounter = 0;
            for (var x = 0; x < _grid.GetLength(0); x++)
            for (var y = 0; y < _grid.GetLength(1); y++)
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

        private bool CanMove(Vector2Int tileMoveCoord)
        {
            return _grid.IsInsideGrid(tileMoveCoord);
        }

        private bool HasAnyMatchesToDestroy(out List<List<Tile>> matches)
        {
            matches = new List<List<Tile>>();
            foreach (Tile tile in _grid)
            {
                if (GridF.ControlImmovableIds(tile)) continue;
                var matchesAll = _grid.GetMatchesXAll(tile);
                matchesAll.AddRange(_grid.GetMatchesYAll(tile));

                if (matchesAll.Count > 0) matches.Add(matchesAll);
            }

            matches = matches.OrderByDescending(e => e.Count).ToList();

            for (var i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                matches[i] = match.Where(e => e.ToBeDestroyed == false).DoToAll(e => e.ToBeDestroyed = true).ToList();
            }

            List<List<Tile>> makeRestfalse = matches.Where(e => e.Count <= 2).ToList();
            //makeRestfalse.Where(e => e.Count <= 2).DoToAll(e => e.DoToAll(x => x.ToBeDestroyed = false));

            makeRestfalse.DoToAll(e => e.DoToAll(x => x.ToBeDestroyed = false));

            matches = matches.Where(e => e.Count > 2).ToList();
            return matches.Count > 0;
        }

        private bool HasAnyMatches()
        {
            List<List<Tile>> matches = new List<List<Tile>>();
            foreach (Tile tile in _grid)
            {
                if (GridF.ControlImmovableIds(tile)) continue;
                var matchesAll = _grid.GetMatchesXAll(tile);
                matchesAll.AddRange(_grid.GetMatchesYAll(tile));

                if (matchesAll.Count > 0) matches.Add(matchesAll);
            }

            matches = matches.Where(e => e.Count > 2).ToList();

            return matches.Count > 0;
        }

        private bool IsGameOver(out Tile hintTile, out GridDir hintDir)
        {
            hintDir = GridDir.Null;
            hintTile = null;

            List<Tile> matches = new();
            var maxMatch = 0;
            var temp = 0;
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
                            if (FindMaxHint(maxMatch, matches))
                            {
                                maxMatch = matches.Count;
                                hintDir = GridDir.Left;
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
                            if (FindMaxHint(maxMatch, matches))
                            {
                                maxMatch = matches.Count;
                                hintDir = GridDir.Up;
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
                            if (FindMaxHint(maxMatch, matches))
                            {
                                maxMatch = matches.Count;
                                hintDir = GridDir.Right;
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
                            if (FindMaxHint(maxMatch, matches))
                            {
                                maxMatch = matches.Count;
                                hintDir = GridDir.Down;
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
            for (var y = 0; y < _gridSizeY; y++)
            {
                var spawnStartY = 0;
                for (var x = 0; x < _gridSizeX; x++)
                {
                    Vector2Int thisCoord = new(x, y);
                    Tile thisTile = _grid.Get(thisCoord);

                    if (thisTile) continue;

                    var spawnPoint = _gridSizeY;

                    for (var y1 = y; y1 <= spawnPoint; y1++)
                    {
                        Vector2Int emptyCoords = new(x, y1);
                        if (y1 == spawnPoint)
                        {
                            if (spawnStartY == 0) spawnStartY = thisCoord.y;

                            MonoPool randomPool;
                            var index = Random.Range(0, _tilePoolsByPrefabID.ToList().Count - 2);
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
        }

        private void SpawnAndAllocateTilesForNow()
        {
            _tilesToMove = new Tile[_gridSizeX, _gridSizeY];
            _movementSequences = new List<Tile>[_gridSizeX];
            bool allFilled;

            do
            {
                allFilled = true;

                for (int y = 0; y < _gridSizeY; y++)
                {
                    for (int x = 0; x < _gridSizeX; x++)
                    {
                        Vector2Int thisCoord = new(x, y);
                        Tile thisTile = _grid.Get(thisCoord);

                        if (thisTile) continue;

                        allFilled = false;

                        if (TryFillFromAbove(thisCoord, true)) continue;

                        TryFillFromDiagonal(thisCoord);
                        continue;

                        // If we reach here, we couldn't fill the tile from above or diagonally
                        // We'll spawn a new tile in the next iteration
                    }
                }

                // Spawn new tiles for any remaining empty spaces
                for (int x = 0; x < _gridSizeX; x++)
                {
                    for (int y = _gridSizeY - 1; y >= 0; y--)
                    {
                        Vector2Int thisCoord = new(x, y);

                        if (TryFillFromAbove(thisCoord, false)) continue;

                        if (_grid.Get(thisCoord) == null)
                        {
                            MonoPool randomPool = _tilePoolsByPrefabID.Random(0, 4);
                            Tile newTile = SpawnTile(
                                randomPool,
                                _grid.CoordsToWorld(_transform, new Vector2Int(x, _gridSizeY)),
                                thisCoord
                            );
                            
                            _tilesToMove[thisCoord.x, thisCoord.y] = newTile;
                            
                            newTile.AddCoord(new Vector2Int(thisCoord.x,thisCoord.y));

                            if (_movementSequences[x] == null) _movementSequences[x] = new List<Tile>();
                            _movementSequences[x].Add(newTile);
                            
                        }
                    }
                }
            } while (!allFilled);
        }


        private bool TryFillFromAbove(Vector2Int coord, bool fill)
        {
            for (int y1 = coord.y + 1; y1 < _gridSizeY; y1++)
            {
                Vector2Int aboveCoords = new(coord.x, y1);
                Tile aboveTile = _grid.Get(aboveCoords);

                if (aboveTile != null)
                {
                    if (aboveTile.ID == EnvVar.TileRightArrow || aboveTile.ID == EnvVar.TileLeftArrow) return false;
                    if (fill)
                    {
                        _grid.Set(null, aboveTile.Coords);
                        _grid.Set(aboveTile, coord);
                        _tilesToMove[coord.x, coord.y] = aboveTile;
                        return true;
                    }
                }
            }

            return false;
        }

        private void TryFillFromDiagonal(Vector2Int coord)
        {
            Vector2Int carriedCoord = new Vector2Int();
            int spawnTilePosX = 0;
            int difference = 1;
            
            for (int y1 = coord.y; y1 < _gridSizeY; y1++)
            {
                Vector2Int tempCoord = new Vector2Int(coord.x, y1);
                carriedCoord = new Vector2Int(tempCoord.x, tempCoord.y);

                Tile tempTile = _grid.IsInsideGrid(tempCoord) ? _grid.Get(tempCoord) : null;


                if (tempTile != null && (tempTile.ID == EnvVar.TileRightArrow || tempTile.ID == EnvVar.TileLeftArrow))
                {
                    if (tempTile.ID == EnvVar.TileRightArrow)
                        spawnTilePosX = coord.x + 1;
                    else
                        spawnTilePosX = coord.x - 1;
                    
                    difference = y1 - coord.y;
                    
                    break;
                }
            }

            carriedCoord.y--;

            if (spawnTilePosX != 0)
            {
                for (int offset = 1; offset < _gridSizeY; offset++)
                {
                    if (TryGetDiagonalTile(carriedCoord, offset, out Tile diagonalTile, spawnTilePosX))
                    {
                        _grid.Set(null, diagonalTile.Coords);
                        _grid.Set(diagonalTile, coord);
                        _tilesToMove[coord.x, coord.y] = diagonalTile;
                        
                        diagonalTile.AddCoord(new Vector2Int(coord.x,coord.y));
                        
                        if (difference > 1)
                        {
                            diagonalTile.AddCoord(new Vector2Int(carriedCoord.x,carriedCoord.y));
                        }
                        
                        return;
                    }
                }


                Debug.Log("loc of carriedCord " + spawnTilePosX);
                //WE REACHED TOP AND STILL NOTHING FOUND 
                MonoPool randomPool = _tilePoolsByPrefabID.Random(0, 4);

                Tile newTile = SpawnTile(
                    randomPool,
                    _grid.CoordsToWorld(_transform, new Vector2Int(spawnTilePosX, _gridSizeY)),
                    coord
                );
                
                _tilesToMove[coord.x, coord.y] = newTile;
                
                newTile.AddCoord(new Vector2Int(coord.x,coord.y));
                newTile.AddCoord(new Vector2Int(carriedCoord.x, carriedCoord.y));
                newTile.AddCoord(new Vector2Int(spawnTilePosX,carriedCoord.y + 1));
                
                if (_movementSequences[spawnTilePosX] == null) _movementSequences[spawnTilePosX] = new List<Tile>();
                _movementSequences[spawnTilePosX].Add(newTile);
            }
        }

        private bool TryGetDiagonalTile(Vector2Int coord, int offset, out Tile diagonalTile, int spawnTilePosX)
        {
            diagonalTile = null;

            Vector2Int diagonalCoord = new Vector2Int(spawnTilePosX, coord.y + offset);
            /*Vector2Int[] diagonalCoords = new[]
            {
                new Vector2Int(coord.x - 1, coord.y + offset),
                new Vector2Int(coord.x + 1, coord.y + offset)
            };


            Vector2Int topCoordinate = new Vector2Int(coord.x, coord.y + 1);

            if (_grid.IsInsideGrid(topCoordinate) == false) return false;

            Tile topTile = _grid.Get(topCoordinate);

            if (topTile == null) return false;*/

            if (_grid.IsInsideGrid(diagonalCoord))
            {
                Tile tile = _grid.Get(diagonalCoord);
                if (tile != null)
                {
                    diagonalTile = tile;
                    return true;
                }
            }


            return false;
        }

        //
        private Tile SpawnTile(MonoPool randomPool, Vector3 spawnWorldPos, Vector2Int spawnCoords)
        {
            var newTile = randomPool.Request<Tile>();

            newTile.Teleport(spawnWorldPos);

            _grid.Set(newTile, spawnCoords);

            if (newTile.ID > 3) _gunTiles.Add(newTile);

            return newTile;
        }

        private Tile SpawnTile(int id, Vector3 worldPos, Vector2Int coords)
        {
            return SpawnTile(_tilePoolsByPrefabID[id], worldPos, coords);
        }

       
        private IEnumerator RainDownRoutine()
        {
            var longestDistY = 0;
            Tween longestTween = null;

            for (var y = 0; y < _gridSizeY; y++) // TODO: Should start from first tile that we are moving
            {
                var shouldWait = false;

                for (var x = 0; x < _gridSizeX; x++)
                {
                    Tile thisTile = _tilesToMove[x, y];

                    if (thisTile == false) continue;

                    Tween thisTween = thisTile.DoMove(_grid.CoordsToWorld(_transform, thisTile.Coords));

                    TweenContainer.AddTween = thisTween;

                    thisTween.onComplete += delegate { SoundEvents.Play?.Invoke(7, 1.0f, false, 128); };
                    shouldWait = true;

                    if (longestDistY < y)
                    {
                        longestDistY = y;
                        longestTween = thisTween;
                    }
                    
                    
                }

                if (shouldWait) yield return new WaitForSeconds(0.1f);
            }

            if (longestTween != null)
            {
                yield return longestTween.WaitForCompletion();
            }
            else
            {
                Debug.LogWarning("This should not have happened!");
            }

            if (_gunTiles.All(e => e.DidSpawnGun)) ResetGunTiles();
        }

        private bool IsStillSpawningGuns()
        {
            return _gunTiles.Any(e => e.DidSpawnGun) && _gunTiles.All(e => e.DidSpawnGun) == false;
        }

        private void ResetGunTiles()
        {
            _gunTiles.DoToAll(e => e.DidSpawnGun = false);
        }

        private IEnumerator DestroyRoutine(List<List<Tile>> lastMatches)
        {
            foreach (List<Tile> matches in lastMatches)
            {
                IncScoreMulti();
                matches.DoToAll(DespawnTile);
                SoundEvents.Play?.Invoke(0, 1.0f, false, 128);
                //TODO: Show score multi text in ui as PunchScale

                GridEvents.MatchGroupDespawn?.Invoke(matches.Count * _scoreMulti, _scoreMulti);

                yield return new WaitForSeconds(0.1f);
            }
        }

        private void StartHintRoutine()
        {
            if (_hintRoutine != null) StopCoroutine(_hintRoutine);

            _hintRoutine = StartCoroutine(HintRoutineUpdate());
        }

        private void StopHintRoutine()
        {
            if (_hintTile) _hintTile.Teleport(_grid.CoordsToWorld(_transform, _hintTile.Coords));

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
            if (e.ID > 3) _gunTiles.Remove(e);

            _grid.Set(null, e.Coords);
            _tilePoolsByPrefabID[e.ID].DeSpawn(e);
            if (_tilePoolsByPrefabID[e.ID].ActiveCount <= 0)
            {
                Debug.Log("Is that even possible?");
            }
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

                TweenContainer.AddTween = _hintTween;
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

            if (_hintTween.IsActive()) _hintTween.Complete();
        }

        private void OnMouseUpGrid(Vector3 mouseUpPos)
        {
            _mouseUpPos = mouseUpPos;
            foreach (Tile tile in _grid)
            {
                if (tile.ToBeDestroyed)
                {
                    bool xmatch = _grid.GetMatchesX(tile).Count == 0;
                    bool ymatch = _grid.GetMatchesY(tile).Count == 0;
                    Debug.Log("bug available? " + tile.Coords + "  X found? " + xmatch + " Y found? " + ymatch);
                }
            }

            Vector3 dirVector = mouseUpPos - _mouseDownPos;
            if (dirVector.magnitude < Mousethreshold) return;

            if (_selectedTile)
            {
                if (GridF.ControlImmovableIds(_selectedTile)) return;
                Vector2Int tileMoveCoord = _selectedTile.Coords + GridF.GetGridDirVector(dirVector);
                if (!CanMove(tileMoveCoord)) return;
                Tile toTile = _grid.Get(tileMoveCoord);
                if (GridF.ControlImmovableIds(toTile)) return;

                _grid.Swap(_selectedTile, toTile);

                if (!HasAnyMatches())
                {
                    GridEvents.InputStop?.Invoke();
                    if (_hintTween.IsActive()) _hintTween.Complete();

                    SoundEvents.Play?.Invoke(2, 1.0f, false, 128);
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
                    if (_hintTween.IsActive()) _hintTween.Complete();

                    _matchCounter++;
                    if (_matchCounter % 3 == 0) _canSpawnGuns = true;

                    DoTileMoveAnim
                    (
                        _selectedTile,
                        toTile,
                        delegate { StartCoroutine(GridCheckRoutine()); }
                    );
                }
            }
        }

        private IEnumerator GridCheckRoutine()
        {
            while (HasAnyMatchesToDestroy(out _lastMatches))
            {
                yield return StartCoroutine(DestroyRoutine(_lastMatches));
                SpawnAndAllocateTilesForNow();
                yield return StartCoroutine(RainDownRoutine());

                if (_canSpawnGuns)
                {
                    _canSpawnGuns = false;

                    if (_gunTiles.All(e => e.DidSpawnGun == false))
                    {
                        foreach (Tile gunTile in _gunTiles)
                        {
                            yield return StartCoroutine(SpawnGunAndDestroyThoseLines(gunTile));
                            SpawnAndAllocateTilesForNow();
                            yield return StartCoroutine(RainDownRoutine());
                        }
                    }
                }
            }

            GridEvents.PlayerMoved?.Invoke();
            IsGameOver(out _hintTile, out _hintDir);
            GridEvents.InputStart?.Invoke();
        }

        private IEnumerator SpawnGunAndDestroyThoseLines(Tile gunTile)
        {
            //ONLY ONE COURUTINE
            ResetScoreMulti();

            var bulletOffSet = _gridSizeY - 3;
            Vector3 pos = default;
            Vector3 bulletLocPos = default;
            if (gunTile.ID == EnvVar.TileRightArrow) //RIGHT
            {
                pos = _grid.CoordsToWorld(_transform, gunTile.Coords);
                pos.x += 1f;
                var bulletLocOffSet = _grid[_gridSizeX - 1, gunTile.Coords.y].ID == EnvVar.TileLeftArrow
                    ? pos.x + bulletOffSet
                    : pos.x + bulletOffSet + 1;
                bulletLocPos = new Vector3(bulletLocOffSet, pos.y, pos.z);
            }
            else if (gunTile.ID == EnvVar.TileLeftArrow) //LEFT
            {
                pos = _grid.CoordsToWorld(_transform, gunTile.Coords);
                pos.x -= 1f;
                var bulletLocOffSet = _grid[0, gunTile.Coords.y].ID == EnvVar.TileRightArrow
                    ? pos.x - bulletOffSet
                    : pos.x - (bulletOffSet + 1);
                bulletLocPos =
                    new Vector3(bulletLocOffSet, pos.y, pos.z);
            }

            yield return GunInstantiate(gunTile, pos, bulletLocPos, gunTile.ID);
        }

        private YieldInstruction GunInstantiate(Tile gunTile, Vector3 pos, Vector3 bulletLocPos, int id)
        {
            gunTile.DidSpawnGun = true;

            GameObject gunNew = GridEvents.InsPrefab
            (
                _mySettings.Gun
            );

            GameObject bulletNew = Instantiate
            (
                _mySettings.Bullet
            );
            bulletNew.SetActive(false);
            bulletNew.transform.position = pos;

            //x = +-0.606 for summon and +-0.426 to put
            pos.x -= 1.606f;
            pos.y -= 0.22f;


            var tLoc = new Vector3(pos.x + 0.18f, pos.y, pos.z);
            var gun = gunNew.GetComponent<Gun>();
            var bullet = bulletNew.GetComponent<Bullet>();

            if (id == EnvVar.TileLeftArrow)
            {
                bullet.GetSprite().flipX = true;
                gun.GetSprite().flipX = true;
                pos.x += 3.212f;
                tLoc.x = pos.x - 0.18f;
            }

            gunNew.transform.position = pos;

            Sequence fireSeq = DOTween.Sequence();
            fireSeq.Append(gun.MoveAndSpawn(tLoc));
            fireSeq.Append(gun.Whirl());

            Tween shakeTween = gun.ShakeGun(id == EnvVar.TileLeftArrow ? -2.491f : 2.491f, -1.455f,
                id == EnvVar.TileLeftArrow);
            shakeTween.onComplete += delegate { bulletNew.SetActive(true); };

            fireSeq.Append(shakeTween);
            fireSeq.Append(SendTheBullet(bulletLocPos, bullet));
            fireSeq.Append(bullet.DestroyBullet());
            fireSeq.Append(gun.DestroyGun(new Vector3(id == EnvVar.TileLeftArrow ? pos.x + 0.18f : pos.x - 0.18f, pos.y,
                pos.z)));

            TweenContainer.AddSequence = fireSeq;

            return fireSeq.WaitForCompletion();
            ;
        }

        //After 0.7 difference Rain that Column
        private Tween SendTheBullet(Vector3 bulletLocPos, Bullet bullet)
        {
            Tween bulletPosTw = bullet.GetTransform().DOMove(bulletLocPos, 1.5f);
            bulletPosTw.onUpdate += delegate
            {
                Vector3 currPosOfBullet = bullet.GetTransform().position;
                Tile shottedTile = _grid[(int)Math.Round(currPosOfBullet.x), (int)Math.Round(currPosOfBullet.y)];

                if (shottedTile != null && !GridF.ControlImmovableIds(shottedTile))
                    if (Math.Abs(_grid.CoordsToWorld(_transform, shottedTile.Coords).x - currPosOfBullet.x) < 0.550001f)
                    {
                        GridEvents.MatchGroupDespawn?.Invoke(1, _scoreMulti);
                        SoundEvents.Play?.Invoke(8, 1.0f, false, 128);
                        DespawnTile(shottedTile);
                    }
                //LAST PART
            };
            return bulletPosTw;
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