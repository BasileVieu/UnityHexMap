using UnityEngine;

public class NewMapMenu : MonoBehaviour
{
    public HexGrid hexGrid;

    public HexMapGenerator mapGenerator;

    private bool generateMaps = true;
    private bool wrapping = true;

    void CreateMap(int _x, int _z)
    {
        if (generateMaps)
        {
            mapGenerator.GenerateMap(_x, _z, wrapping);
        }
        else
        {
            hexGrid.CreateMap(_x, _z, wrapping);
        }

        HexMapCamera.ValidatePosition();

        Close();
    }

    public void Open()
    {
        gameObject.SetActive(true);
        HexMapCamera.Locked = true;
    }

    public void Close()
    {
        gameObject.SetActive(false);
        HexMapCamera.Locked = false;
    }

    public void CreateSmallMap()
    {
        CreateMap(20, 15);
    }

    public void CreateMediumMap()
    {
        CreateMap(40, 30);
    }

    public void CreateLargeMap()
    {
        CreateMap(80, 60);
    }

    public void ToggleMapGeneration(bool _toggle)
    {
        generateMaps = _toggle;
    }

    public void ToggleWrapping(bool _toggle)
    {
        wrapping = _toggle;
    }
}