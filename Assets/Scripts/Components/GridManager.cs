using System;
 using System.Collections.Generic;
 using System.Reflection;
 using Events;
using Extensions.System;
using Extensions.Unity;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

namespace Components
{
    public class GridManager : SerializedMonoBehaviour
    {
        [Inject] private InputEvents InputEvents{get;set;}
        [SerializeField] private Camera Camera;
        [BoxGroup(Order = 999)][TableMatrix(SquareCells = true)/*(DrawElementMethod = nameof(DrawTile))*/,OdinSerialize]
        private Tile[,] _grid;
        [SerializeField] private List<GameObject> _tilePrefabs;
        private int _gridSizeX;
        private int _gridSizeY;
        [SerializeField] private List<int> _prefabIds;
        
        private Tile _selectedTile;
        private Vector3 _mouseDownPos;
        private Vector3 _mouseUpPos;
        
        private void OnEnable()
        {
            RegisterEvents();
        }

        private void OnDisable()
        {
            UnRegisterEvents();
        }

        
        static MethodInfo _clearConsoleMethod;
        static MethodInfo clearConsoleMethod {
            get {
                if (_clearConsoleMethod == null) {
                    Assembly assembly = Assembly.GetAssembly (typeof(SceneView));
                    Type logEntries = assembly.GetType ("UnityEditor.LogEntries");
                    _clearConsoleMethod = logEntries.GetMethod ("Clear");
                }
                return _clearConsoleMethod;
            }
        }
        private Tile DrawTile(Rect rect, Tile tile)
        {
            if (tile != null)
            {
                Sprite spriteTile = tile.GetComponent<SpriteRenderer>().sprite;
                Rect textureRect = rect;
                textureRect.Padding(3);
                UnityEditor.EditorGUI.DrawPreviewTexture(textureRect, spriteTile.texture);
            }

            return tile;
        }

        [Button]
        private void CreateGrid(int sizeX, int sizeY)
        {
            _gridSizeX = sizeX;
            _gridSizeY = sizeY;
            Camera.transform.SetPositionAndRotation(new Vector3(_gridSizeX / 2, _gridSizeY / 2, _gridSizeY * (-1f)), Quaternion.identity);
            for(int id = 0; id < _tilePrefabs.Count; id ++) _prefabIds.Add(id);
            if (_grid != null)
            {
                foreach (Tile o in _grid)
                {
                    if (o != null)
                        DestroyImmediate(o.gameObject);
                }
            }

            _grid = new Tile[_gridSizeX, _gridSizeY];

            for (int x = 0; x < _gridSizeX; x++)
            for (int y = 0; y < _gridSizeY; y++)
            {
                Vector2Int coord = new(x, _gridSizeY - y - 1);
                Vector3 pos = new(coord.x, coord.y, 0f);
                while (true)
                {
                    List<int> spawnableIds = new(_prefabIds);
                    int randomId = spawnableIds.Random();
                    int randomIndex = Random.Range(0, _tilePrefabs.Count);
                    GameObject tilePrefabRandom = _tilePrefabs[randomIndex];
                    GameObject tileNew = Instantiate(tilePrefabRandom, pos, Quaternion.identity);
                    Tile tile = tileNew.GetComponent<Tile>();
                    //Debug.Log(tile.ID);
                    //Debug.Log(x + "  " + (_gridSizeY - y - 1));
                    int tileID = tile.ID;
                    bool avail = true;
                    if (x >= 2 && _grid[x - 1, y].ID == tileID && _grid[x - 2, y].ID == tileID)
                        avail = false;
                    if (y >= 2 && _grid[x, y - 1].ID == tileID && _grid[x, y - 2].ID == tileID)
                        avail = false;
                    if (avail)
                    {
                        CreateAndQuit(tile, coord, x, y);
                        break;
                    }
                    DestroyImmediate(tileNew);
                }
            }
            clearConsoleMethod.Invoke (new object (), null);
            Debug.Log("Any Move? "+ ControlMoves(_gridSizeX, _gridSizeY));
            FindAllPossibleMoves(_gridSizeX, _gridSizeY);
            
        }

        private void FindAllPossibleMoves(int gridSizeX, int gridSizeY)
        {
            List<Tuple<int,int,String>> positions = new List<Tuple<int,int,String>>();
            for (int x = 0; x < gridSizeX; x++)
            for (int y = 0; y < gridSizeY; y++)
            {
                if (y < gridSizeY - 3 && _grid[x, y + 2].ID == _grid[x, y].ID && _grid[x, y + 3].ID == _grid[x, y].ID)
                    positions.Add(new Tuple<int, int,String>(x,y,"YPos"));
                if (y >= 3 && _grid[x, y - 2].ID == _grid[x, y].ID && _grid[x, y - 3].ID == _grid[x, y].ID)
                    positions.Add(new Tuple<int, int,String>(x,y,"YPos"));
                if (y < gridSizeY - 1 && x >= 2 && _grid[x - 1, y + 1].ID == _grid[x, y].ID && _grid[x - 2, y + 1].ID == _grid[x, y].ID)
                    positions.Add(new Tuple<int, int,String>(x,y,"YPos"));
                if (y < gridSizeY - 1 && x < gridSizeX - 2 && _grid[x + 1, y + 1].ID == _grid[x, y].ID && _grid[x + 2, y + 1].ID == _grid[x, y].ID)
                    positions.Add(new Tuple<int, int,String>(x,y,"YPos"));
                if (y >= 1 && x >= 2 && _grid[x - 1, y - 1].ID == _grid[x, y].ID && _grid[x - 2, y - 1].ID == _grid[x, y].ID)
                    positions.Add(new Tuple<int, int,String>(x,y,"YPos"));
                if (y >= 1 && x < gridSizeX - 2 && _grid[x + 1, y - 1].ID == _grid[x, y].ID && _grid[x + 2, y - 1].ID == _grid[x, y].ID)
                    positions.Add(new Tuple<int, int,String>(x,y,"YPos"));
                if (y < gridSizeY - 1 && x < gridSizeX - 1 && x >= 1 && _grid[x + 1, y + 1].ID == _grid[x, y].ID && _grid[x - 1, y + 1].ID == _grid[x, y].ID)
                    positions.Add(new Tuple<int, int,String>(x,y,"YPos"));
                if (y >= 1 && x < gridSizeX - 1 && x >= 1 && _grid[x + 1, y - 1].ID == _grid[x, y].ID && _grid[x - 1, y - 1].ID == _grid[x, y].ID)
                    positions.Add(new Tuple<int, int,String>(x,y,"YPos"));
                
                if (x < gridSizeX - 3 && _grid[x + 2, y].ID == _grid[x, y].ID && _grid[x + 3, y].ID == _grid[x, y].ID)
                    positions.Add(new Tuple<int, int,String>(x,y,"XPos"));
                if (x >= 3 && _grid[x - 2, y].ID == _grid[x, y].ID && _grid[x - 3, y].ID == _grid[x, y].ID)
                    positions.Add(new Tuple<int, int,String>(x,y,"XPos"));
                if (x >= 1 && y < gridSizeY - 2 && _grid[x - 1, y + 1].ID == _grid[x, y].ID && _grid[x - 1, y + 2].ID == _grid[x, y].ID)
                    positions.Add(new Tuple<int, int,String>(x,y,"XPos"));
                if (x >= 1 && y >= 2 && _grid[x - 1, y - 1].ID == _grid[x, y].ID && _grid[x - 1, y - 2].ID == _grid[x, y].ID)
                    positions.Add(new Tuple<int, int,String>(x,y,"XPos"));
                if (x < gridSizeX - 1 && y < gridSizeY - 2 && _grid[x + 1, y + 1].ID == _grid[x, y].ID && _grid[x + 1, y + 2].ID == _grid[x, y].ID)
                    positions.Add(new Tuple<int, int,String>(x,y,"XPos"));
                if (x < gridSizeX - 1 && y >= 2 && _grid[x + 1, y - 1].ID == _grid[x, y].ID && _grid[x + 1, y - 2].ID == _grid[x, y].ID)
                    positions.Add(new Tuple<int, int,String>(x,y,"XPos"));
                if (x < gridSizeX - 1 && y < gridSizeY - 1 && y >= 1 && _grid[x + 1, y + 1].ID == _grid[x, y].ID && _grid[x + 1, y - 1].ID == _grid[x, y].ID)
                    positions.Add(new Tuple<int, int,String>(x,y,"XPos"));
                if (x >= 1 && y < gridSizeY - 1 && y >= 1 && _grid[x - 1, y + 1].ID == _grid[x, y].ID && _grid[x - 1, y - 1].ID == _grid[x, y].ID)
                    positions.Add(new Tuple<int, int,String>(x,y,"XPos"));
                
            }

            int count = 0;
            int temp = -1;
            int temp2 = -1;
            foreach (var tuples in positions)
            {
                count = temp == tuples.Item1 && temp2 == tuples.Item2 ? count + 1 : 0;
                Debug.Log("Positions= "+ tuples.Item1 + " "+tuples.Item2 + " " +tuples.Item3);
                temp = tuples.Item1;
                temp2 = tuples.Item2;
                if(count == 6)
                    Debug.LogError("Max "+ tuples.Item1+ " "+tuples.Item2 + " " +tuples.Item3);
            }
        }

        private bool ControlMoves(int gridSizeX, int gridSizeY)
        {
            
            for (int x = 0; x < gridSizeX; x++)
            for (int y = 0; y < gridSizeY; y++)
            {
                //Y CASES
                if (y < gridSizeY - 3 && _grid[x, y + 2].ID == _grid[x, y].ID && _grid[x, y + 3].ID == _grid[x, y].ID)
                    return true;
                if (y >= 3 && _grid[x, y - 2].ID == _grid[x, y].ID && _grid[x, y - 3].ID == _grid[x, y].ID)
                    return true;
                if (y < gridSizeY - 1 && x >= 2 && _grid[x - 1, y + 1].ID == _grid[x, y].ID && _grid[x - 2, y + 1].ID == _grid[x, y].ID)
                    return true;
                if (y < gridSizeY - 1 && x < gridSizeX - 2 && _grid[x + 1, y + 1].ID == _grid[x, y].ID && _grid[x + 2, y + 1].ID == _grid[x, y].ID)
                    return true;
                if (y >= 1 && x >= 2 && _grid[x - 1, y - 1].ID == _grid[x, y].ID && _grid[x - 2, y - 1].ID == _grid[x, y].ID)
                    return true;
                if (y >= 1 && x < gridSizeX - 2 && _grid[x + 1, y - 1].ID == _grid[x, y].ID && _grid[x + 2, y - 1].ID == _grid[x, y].ID)
                    return true;
                if (y < gridSizeY - 1 && x < gridSizeX - 1 && x >= 1 && _grid[x + 1, y + 1].ID == _grid[x, y].ID && _grid[x - 1, y + 1].ID == _grid[x, y].ID)
                    return true;
                if (y >= 1 && x < gridSizeX - 1 && x >= 1 && _grid[x + 1, y - 1].ID == _grid[x, y].ID && _grid[x - 1, y - 1].ID == _grid[x, y].ID)
                    return true;
                
                //X CASES
                if (x < gridSizeX - 3 && _grid[x + 2, y].ID == _grid[x, y].ID && _grid[x + 3, y].ID == _grid[x, y].ID)
                    return true;
                if (x >= 3 && _grid[x - 2, y].ID == _grid[x, y].ID && _grid[x - 3, y].ID == _grid[x, y].ID)
                    return true;
                if (x >= 1 && y < gridSizeY - 2 && _grid[x - 1, y + 1].ID == _grid[x, y].ID && _grid[x - 1, y + 2].ID == _grid[x, y].ID)
                    return true;
                if (x >= 1 && y >= 2 && _grid[x - 1, y - 1].ID == _grid[x, y].ID && _grid[x - 1, y - 2].ID == _grid[x, y].ID)
                    return true;
                if (x < gridSizeX - 1 && y < gridSizeY - 2 && _grid[x + 1, y + 1].ID == _grid[x, y].ID && _grid[x + 1, y + 2].ID == _grid[x, y].ID)
                    return true;
                if (x < gridSizeX - 1 && y >= 2 && _grid[x + 1, y - 1].ID == _grid[x, y].ID && _grid[x + 1, y - 2].ID == _grid[x, y].ID)
                    return true;
                if (x < gridSizeX - 1 && y < gridSizeY - 1 && y >= 1 && _grid[x + 1, y + 1].ID == _grid[x, y].ID && _grid[x + 1, y - 1].ID == _grid[x, y].ID)
                    return true;
                if (x >= 1 && y < gridSizeY - 1 && y >= 1 && _grid[x - 1, y + 1].ID == _grid[x, y].ID && _grid[x - 1, y - 1].ID == _grid[x, y].ID)
                    return true;
                
            }

            return false;
        }

        private void CreateAndQuit(Tile tile, Vector2Int coord, int x, int y)
        {
            tile.Construct(coord);
            _grid[x, y] = tile;
        }
        private void RegisterEvents()
        {
            InputEvents.MouseDownGrid += OnMouseDownGrid;
            InputEvents.MouseUpGrid += OnMouseUpGrid;
        }

        private void OnMouseDownGrid(Tile arg0, Vector3 arg1)
        {
            _selectedTile = arg0;
            _mouseDownPos = arg1;
            EDebug.Method();

        }

        private void OnMouseUpGrid(Vector3 arg0)
        {
            _mouseUpPos = arg0;

            if(_selectedTile)
            {
                EDebug.Method();
    
                Debug.DrawLine(_mouseDownPos, _mouseUpPos, Color.blue, 2f);
            }
        }

        private void UnRegisterEvents()
        {
            InputEvents.MouseDownGrid -= OnMouseDownGrid;
            InputEvents.MouseUpGrid -= OnMouseUpGrid;
        }
    }
}