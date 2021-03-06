using UnityEngine;
using UnityEngine.EventSystems;

public class HexGameUI : MonoBehaviour
{
    public HexGrid grid;

    private HexCell currentCell;

    private HexUnit selectedUnit;

    public void SetEditMode(bool _toggle)
    {
        enabled = !_toggle;
        grid.ShowUI(!_toggle);
        grid.ClearPath();

        if (_toggle)
        {
            Shader.EnableKeyword("HEX_MAP_EDIT_MODE");
        }
        else
        {
            Shader.DisableKeyword("HEX_MAP_EDIT_MODE");
        }
    }

    bool UpdateCurrentCell()
    {
        HexCell cell = grid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));

        if (cell != currentCell)
        {
            currentCell = cell;

            return true;
        }

        return false;
    }

    void DoSelection()
    {
        grid.ClearPath();

        UpdateCurrentCell();

        if (currentCell)
        {
            selectedUnit = currentCell.Unit;
        }
    }

    void DoPathFinding()
    {
        if (UpdateCurrentCell())
        {
            if (currentCell
                && selectedUnit.IsValidDestination(currentCell))
            {
                grid.FindPath(selectedUnit.Location, currentCell, selectedUnit);
            }
            else
            {
                grid.ClearPath();
            }
        }
    }

    void DoMove()
    {
        if (grid.HasPath)
        {
            selectedUnit.Travel(grid.GetPath());
            grid.ClearPath();
        }
    }

    void Update()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            if (Input.GetMouseButtonDown(0))
            {
                DoSelection();
            }
            else if (selectedUnit)
            {
                if (Input.GetMouseButtonDown(1))
                {
                    DoMove();
                }
                else
                {
                    DoPathFinding();
                }
            }
        }
    }
}