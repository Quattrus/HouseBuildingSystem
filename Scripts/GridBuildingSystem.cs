using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Utils;
using System;

public class GridBuildingSystem : MonoBehaviour
{
    public static GridBuildingSystem Instance { get; private set; }
    private GridMap<GridObject> grid;
    #region EventHandlers
    public event EventHandler OnSelectedChanged;
    public event EventHandler OnObjectPlaced;
    #endregion

    #region PlacedObject Variables
    [SerializeField] private List<PlacedObjectTypeSO> placedObjectTypeSOList = null;
    private PlacedObjectTypeSO.Dir dir;
    private PlacedObjectTypeSO placedObjectTypeSO;
    #endregion

    #region Grid Setup Variables
    [SerializeField] int gridWidth = 10;
    [SerializeField] int gridHeight = 10;
    [SerializeField] float cellSize = 10f;
    #endregion


    private void Awake()
    {
        Instance = this;
        grid = new GridMap<GridObject>(gridWidth, gridHeight, cellSize, new Vector3(0, 0, 0), (GridMap<GridObject> g, int x, int z) => new GridObject(g,x,z));

        placedObjectTypeSO = null;// placedObjectTypeSOList[0];

    }


    public class GridObject
    {
        private GridMap<GridObject> grid;
        private int x;
        private int z;
        public PlacedObject placedObject;

        public GridObject(GridMap<GridObject> grid, int x, int z)
        {
            this.grid = grid;
            this.x = x;
            this.z = z;
            placedObject = null;
        }
        public void SetPlacedObject(PlacedObject placedObject)
        {
            this.placedObject = placedObject;
            grid.TriggerGridObjectChanged(x, z);
        }

        public PlacedObject GetPlacedObject()
        {
            return placedObject;
        }

        public void ClearPlacedObject()
        {
            placedObject = null;
            grid.TriggerGridObjectChanged(x, z);
        }

        public bool CanBuild()
        {
            return placedObject == null;
        }
        public override string ToString()
        {
            return x + ", " + z + "\n" + placedObject;
        }
    }

    private void Update()
    {
        if(Input.GetMouseButtonDown(0) && placedObjectTypeSO != null)
        {
            Build();
        }

        if(Input.GetMouseButtonDown(1))
        {
            DestroyPlacedObject();
        }

        GetBuildingValue();

        if(Input.GetKeyDown(KeyCode.R))
        {
            Rotate();
        }
    }

    private void DeselectObjectType()
    {
        placedObjectTypeSO = null; RefreshSelectedObjectType();
    }

    private void RefreshSelectedObjectType()
    {
        OnSelectedChanged?.Invoke(this, EventArgs.Empty);
    }


    public Vector2Int GetGridPosition(Vector3 worldPosition)
    {
        grid.GetXZ(worldPosition, out int x, out int z);
        return new Vector2Int(x, z);
    }

    public Vector3 GetMouseWorldSnappedPosition()
    {
        Vector3 mousePosition = Mouse3D.GetMouseWorldPosition();
        grid.GetXZ(mousePosition, out int x, out int z);

        if (placedObjectTypeSO != null)
        {
            Vector2Int rotationOffset = placedObjectTypeSO.GetRotationOffset(dir);
            Vector3 placedObjectWorldPosition = grid.GetWorldPosition(x, z) + new Vector3(rotationOffset.x, 0, rotationOffset.y) * grid.GetCellSize();
            return placedObjectWorldPosition;
        }
        else
        {
            return mousePosition;
        }
    }

    public Quaternion GetPlacedObjectRotation()
    {
        if (placedObjectTypeSO != null)
        {
            return Quaternion.Euler(0, placedObjectTypeSO.GetRotationAngle(dir), 0);
        }
        else
        {
            return Quaternion.identity;
        }
    }

    public PlacedObjectTypeSO GetPlacedObjectTypeSO()
    {
        return placedObjectTypeSO;
    }

    private void Build()
    {
        Vector3 mousePosition = Mouse3D.GetMouseWorldPosition();
        grid.GetXZ(mousePosition, out int x, out int z);

        Vector2Int placedObjectOrigin = new Vector2Int(x, z);
        placedObjectOrigin = grid.ValidateGridPosition(placedObjectOrigin);

        //Test can build
        List<Vector2Int> gridPositionList = placedObjectTypeSO.GetGridPositionList(placedObjectOrigin, dir);
        bool canBuild = true;
        foreach (Vector2Int gridPosition in gridPositionList)
        {
            if (!grid.GetGridObject(gridPosition.x, gridPosition.y).CanBuild())
            {
                //can't build
                canBuild = false;
                break;
            }
        }
        if (canBuild)
        {
            Vector2Int rotationOffset = placedObjectTypeSO.GetRotationOffset(dir);
            Vector3 placedObjectWorldPosition = grid.GetWorldPosition(placedObjectOrigin.x, placedObjectOrigin.y) + new Vector3(rotationOffset.x, 0, rotationOffset.y) * grid.GetCellSize();

            PlacedObject placedObject = PlacedObject.Create(placedObjectWorldPosition, placedObjectOrigin, dir, placedObjectTypeSO);

            foreach (Vector2Int gridPosition in gridPositionList)
            {
                grid.GetGridObject(gridPosition.x, gridPosition.y).SetPlacedObject(placedObject);
            }
            OnObjectPlaced?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            GameUtilities.CreateWorldTextPopup("Cannot build here!", mousePosition);
        }
    }

    private void Rotate()
    {
        dir = PlacedObjectTypeSO.GetNextDir(dir);
        GameUtilities.CreateWorldTextPopup("" + dir, Mouse3D.GetMouseWorldPosition());
    }

    private void GetBuildingValue()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) { placedObjectTypeSO = placedObjectTypeSOList[0]; }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { placedObjectTypeSO = placedObjectTypeSOList[1]; }
        if (Input.GetKeyDown(KeyCode.Alpha0)) { DeselectObjectType(); }
    }

    private void DestroyPlacedObject()
    {
        Vector3 mousePosition = Mouse3D.GetMouseWorldPosition();
        if (grid.GetGridObject(mousePosition) != null)
        {
            PlacedObject placedObject = grid.GetGridObject(mousePosition).GetPlacedObject();
            if (placedObject != null)
            {
                placedObject.DestroySelf();
                List<Vector2Int> gridPositionList = placedObject.GetGridPositionList();

                foreach (Vector2Int gridPosition in gridPositionList)
                {
                    grid.GetGridObject(gridPosition.x, gridPosition.y).ClearPlacedObject();
                }
            }
        }
    }
}
