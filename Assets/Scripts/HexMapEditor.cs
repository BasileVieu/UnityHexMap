using UnityEngine;
using UnityEngine.EventSystems;
using System.IO;

public class HexMapEditor : MonoBehaviour
{
    private enum OptionalToggle
    {
        Ignore,
        Yes,
        No
    }

    public HexGrid hexGrid;

    public Material terrainMaterial;

    private int activeTerrainTypeIndex;
    private int activeElevation;
    private int activeWaterLevel;
    private int activeUrbanLevel;
    private int activeFarmLevel;
    private int activePlantLevel;
    private int activeSpecialIndex;

    private int brushSize;

    private bool applyElevation;
    private bool applyWaterLevel;
    private bool applyUrbanLevel;
    private bool applyFarmLevel;
    private bool applyPlantLevel;
    private bool applySpecialIndex;
    private bool isDrag;

    private OptionalToggle riverMode;
    private OptionalToggle roadMode;
    private OptionalToggle walledMode;

    private HexDirection dragDirection;

    private HexCell previousCell;

    void Awake()
    {
        terrainMaterial.DisableKeyword("GRID_ON");

        Shader.EnableKeyword("HEX_MAP_EDIT_MODE");

        SetEditMode(true);
    }

    private void Update()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            if (Input.GetMouseButton(0))
            {
                HandleInput();

                return;
            }

            if (Input.GetKeyDown(KeyCode.U))
            {
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    DestroyUnit();
                }
                else
                {
                    CreateUnit();
                }

                return;
            }
        }

        previousCell = null;
    }

    HexCell GetCellUnderCursor() => hexGrid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));

    void CreateUnit()
    {
        HexCell cell = GetCellUnderCursor();

        if (cell
            && !cell.Unit)
        {
            hexGrid.AddUnit(Instantiate(HexUnit.unitPrefab), cell, Random.Range(0.0f, 360.0f));
        }
    }

    void DestroyUnit()
    {
        HexCell cell = GetCellUnderCursor();

        if (cell
            && cell.Unit)
        {
            hexGrid.RemoveUnit(cell.Unit);
        }
    }

    private void HandleInput()
    {
        HexCell currentCell = GetCellUnderCursor();

        if (currentCell)
        {
            if (previousCell
                && previousCell != currentCell)
            {
                ValidateDrag(currentCell);
            }
            else
            {
                isDrag = false;
            }

            EditCells(currentCell);

            previousCell = currentCell;
        }
        else
        {
            previousCell = null;
        }
    }

    private void ValidateDrag(HexCell _currentCell)
    {
        for (dragDirection = HexDirection.NE; dragDirection <= HexDirection.NW; dragDirection++)
        {
            if (previousCell.GetNeighbor(dragDirection) == _currentCell)
            {
                isDrag = true;

                return;
            }
        }

        isDrag = false;
    }

    private void EditCells(HexCell _center)
    {
        int centerX = _center.coordinates.X;
        int centerZ = _center.coordinates.Z;

        for (int r = 0, z = centerZ - brushSize; z <= centerZ; z++, r++)
        {
            for (int x = centerX - r; x <= centerX + brushSize; x++)
            {
                EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }

        for (int r = 0, z = centerZ + brushSize; z > centerZ; z--, r++)
        {
            for (int x = centerX - brushSize; x <= centerX + r; x++)
            {
                EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }
    }

    private void EditCell(HexCell _cell)
    {
        if (_cell)
        {
            if (activeTerrainTypeIndex >= 0)
            {
                _cell.TerrainTypeIndex = activeTerrainTypeIndex;
            }

            if (applyElevation)
            {
                _cell.Elevation = activeElevation;
            }

            if (applyWaterLevel)
            {
                _cell.WaterLevel = activeWaterLevel;
            }

            if (applySpecialIndex)
            {
                _cell.SpecialIndex = activeSpecialIndex;
            }

            if (applyUrbanLevel)
            {
                _cell.UrbanLevel = activeUrbanLevel;
            }

            if (applyFarmLevel)
            {
                _cell.FarmLevel = activeFarmLevel;
            }

            if (applyPlantLevel)
            {
                _cell.PlantLevel = activePlantLevel;
            }

            if (riverMode == OptionalToggle.No)
            {
                _cell.RemoveRiver();
            }

            if (roadMode == OptionalToggle.No)
            {
                _cell.RemoveRoads();
            }

            if (walledMode != OptionalToggle.Ignore)
            {
                _cell.Walled = walledMode == OptionalToggle.Yes;
            }

            if (isDrag)
            {
                HexCell otherCell = _cell.GetNeighbor(dragDirection.Opposite());

                if (otherCell)
                {
                    if (riverMode == OptionalToggle.Yes)
                    {
                        otherCell.SetOutgoingRiver(dragDirection);
                    }

                    if (roadMode == OptionalToggle.Yes)
                    {
                        otherCell.AddRoad(dragDirection);
                    }
                }
            }
        }
    }

    public void SetTerrainTypeIndex(int _index)
    {
        activeTerrainTypeIndex = _index;
    }

    public void SetElevation(float _elevation)
    {
        activeElevation = (int) _elevation;
    }

    public void SetApplyElevation(bool _toggle)
    {
        applyElevation = _toggle;
    }

    public void SetBrushSize(float _size)
    {
        brushSize = (int) _size;
    }

    public void SetRiverMode(int _mode)
    {
        riverMode = (OptionalToggle) _mode;
    }

    public void SetRoadMode(int _mode)
    {
        roadMode = (OptionalToggle) _mode;
    }

    public void SetApplyWaterLevel(bool _toggle)
    {
        applyWaterLevel = _toggle;
    }

    public void SetWaterLevel(float _level)
    {
        activeWaterLevel = (int) _level;
    }

    public void SetApplyUrbanLevel(bool _toggle)
    {
        applyUrbanLevel = _toggle;
    }

    public void SetUrbanLevel(float _level)
    {
        activeUrbanLevel = (int) _level;
    }

    public void SetApplyFarmLevel(bool _toggle)
    {
        applyFarmLevel = _toggle;
    }

    public void SetFarmLevel(float _level)
    {
        activeFarmLevel = (int) _level;
    }

    public void SetApplyPlantLevel(bool _toggle)
    {
        applyPlantLevel = _toggle;
    }

    public void SetPlantLevel(float _level)
    {
        activePlantLevel = (int) _level;
    }

    public void SetWalledMode(int _mode)
    {
        walledMode = (OptionalToggle) _mode;
    }

    public void SetApplySpecialIndex(bool _toggle)
    {
        applySpecialIndex = _toggle;
    }

    public void SetSpecialIndex(float _index)
    {
        activeSpecialIndex = (int) _index;
    }

    public void ShowGrid(bool _visible)
    {
        if (_visible)
        {
            terrainMaterial.EnableKeyword("GRID_ON");
        }
        else
        {
            terrainMaterial.DisableKeyword("GRID_ON");
        }
    }

    public void SetEditMode(bool _toggle)
    {
        enabled = _toggle;
    }
}