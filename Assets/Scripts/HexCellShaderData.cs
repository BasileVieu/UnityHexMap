using System.Collections.Generic;
using UnityEngine;

public class HexCellShaderData : MonoBehaviour
{
    private Texture2D cellTexture;

    private Color32[] cellTextureData;
    
    List<HexCell> transitioningCells = new List<HexCell>();

    private const float transitionSpeed = 255.0f;

    private bool needsVisibilityReset;
    
    public bool ImmediateMode { get; set; }
    
    public HexGrid Grid { get; set; }

    public void Initialize(int _x, int _z)
    {
        if (cellTexture)
        {
            cellTexture.Resize(_x, _z);
        }
        else
        {
            cellTexture = new Texture2D(_x, _z, TextureFormat.RGBA32, false, true)
            {
                    filterMode = FilterMode.Point,
                    wrapModeU = TextureWrapMode.Repeat,
                    wrapModeV = TextureWrapMode.Clamp
            };
            
            Shader.SetGlobalTexture("_HexCellData", cellTexture);
        }

        Shader.SetGlobalVector("_HexCellData_TexelSize", new Vector4(1.0f / _x, 1.0f / _z, _x, _z));

        if (cellTextureData == null
            || cellTextureData.Length != _x * _z)
        {
            cellTextureData = new Color32[_x * _z];
        }
        else
        {
            for (var i = 0; i < cellTextureData.Length; i++)
            {
                cellTextureData[i] = new Color32(0, 0, 0, 0);
            }
        }

        transitioningCells.Clear();

        enabled = true;
    }

    public void RefreshTerrain(HexCell _cell)
    {
        cellTextureData[_cell.Index].a = (byte) _cell.TerrainTypeIndex;
        enabled = true;
    }

    public void RefreshVisibility(HexCell _cell)
    {
        int index = _cell.Index;

        if (ImmediateMode)
        {
            cellTextureData[index].r = _cell.IsVisible ? (byte) 255 : (byte) 0;
            cellTextureData[index].g = _cell.IsExplored ? (byte) 255 : (byte) 0;
        }
        else if (cellTextureData[index].b != 255)
        {
            cellTextureData[index].b = 255;
            
            transitioningCells.Add(_cell);
        }

        enabled = true;
    }

    bool UpdateCellData(HexCell _cell, int _delta)
    {
        int index = _cell.Index;

        Color32 data = cellTextureData[index];

        var stillUpdating = false;

        if (_cell.IsExplored
            && data.g < 255)
        {
            stillUpdating = true;

            int t = data.g + _delta;

            data.g = t >= 255 ? (byte) 255 : (byte) t;
        }

        if (_cell.IsVisible)
        {
            if (data.r < 255)
            {
                stillUpdating = true;

                int t = data.r + _delta;

                data.r = t >= 255 ? (byte) 255 : (byte) t;
            }
        }
        else if (data.r > 0)
        {
            stillUpdating = true;

            int t = data.r - _delta;

            data.r = t < 0 ? (byte) 0 : (byte) t;
        }

        if (!stillUpdating)
        {
            data.b = 0;
        }

        cellTextureData[index] = data;

        return stillUpdating;
    }

    public void ViewElevationChanged()
    {
        needsVisibilityReset = true;

        enabled = true;
    }

    public void SetMapData(HexCell _cell, float _data)
    {
        cellTextureData[_cell.Index].b = _data < 0.0f ? (byte) 0 : _data < 1.0f ? (byte) (_data * 254.0f) : (byte) 254;

        enabled = true;
    }

    void LateUpdate()
    {
        if (needsVisibilityReset)
        {
            needsVisibilityReset = false;

            Grid.ResetVisibility();
        }
        
        var delta = (int) (Time.deltaTime * transitionSpeed);

        if (delta == 0)
        {
            delta = 1;
        }

        for (var i = 0; i < transitioningCells.Count; i++)
        {
            if (!UpdateCellData(transitioningCells[i], delta))
            {
                transitioningCells[i--] = transitioningCells[transitioningCells.Count - 1];
                transitioningCells.RemoveAt(transitioningCells.Count - 1);
            }
        }
        
        cellTexture.SetPixels32(cellTextureData);
        cellTexture.Apply();
        enabled = transitioningCells.Count > 0;
    }
}