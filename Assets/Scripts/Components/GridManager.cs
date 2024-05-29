using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Sirenix.Utilities;
using UnityEngine;

namespace Components
{
    public class GridManager : SerializedMonoBehaviour
    {
        [BoxGroup(Order = 999)] [TableMatrix(SquareCells = true, DrawElementMethod = nameof(DrawTile)), OdinSerialize]
        private Tile[,] _grid;

        [SerializeField] private List<GameObject> _tilePrefabs;

        private int _gridSizeX;
        private int _gridSizeY;

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
            
            Debug.Log("Any MOve? "+ ControlMoves(_gridSizeX, _gridSizeY));
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
    }
}