using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class SaveLoadMenu : MonoBehaviour
{
    public HexGrid hexGrid;

    public Text menuLabel;
    public Text actionButtonLabel;

    public InputField nameInput;

    public RectTransform listContent;

    public SaveLoadItem itemPrefab;

    private bool saveMode;

    private const int mapFileVersion = 5;

    string GetSelectedPath()
    {
        string mapName = nameInput.text;

        return mapName.Length == 0 ? null : Path.Combine(Application.dataPath, mapName + ".map");
    }

    void FillList()
    {
        for (var i = 0; i < listContent.childCount; i++)
        {
            Destroy(listContent.GetChild(i).gameObject);
        }

        string[] paths = Directory.GetFiles(Application.dataPath, "*.map");

        Array.Sort(paths);

        foreach (string path in paths)
        {
            SaveLoadItem item = Instantiate(itemPrefab, listContent, false);
            item.menu = this;
            item.MapName = Path.GetFileNameWithoutExtension(path);
        }
    }

    public void SelectItem(string _name)
    {
        nameInput.text = _name;
    }

    public void Delete()
    {
        string path = GetSelectedPath();

        if (path == null)
        {
            return;
        }

        if (File.Exists(path))
        {
            File.Delete(path);
        }

        nameInput.text = "";

        FillList();
    }

    public void Action()
    {
        string path = GetSelectedPath();

        if (path == null)
        {
            return;
        }

        if (saveMode)
        {
            Save(path);
        }
        else
        {
            Load(path);
        }

        Close();
    }

    public void Open(bool _saveMode)
    {
        saveMode = _saveMode;

        if (saveMode)
        {
            menuLabel.text = "Save Map";
            actionButtonLabel.text = "Save";
        }
        else
        {
            menuLabel.text = "Load Map";
            actionButtonLabel.text = "Load";
        }

        FillList();

        gameObject.SetActive(true);

        HexMapCamera.Locked = true;
    }

    public void Close()
    {
        gameObject.SetActive(false);

        HexMapCamera.Locked = false;
    }

    void Save(string _path)
    {
        using (var writer = new BinaryWriter(File.Open(_path, FileMode.Create)))
        {
            writer.Write(mapFileVersion);

            hexGrid.Save(writer);
        }
    }

    public void Load(string _path)
    {
        if (!File.Exists(_path))
        {
            Debug.LogError("File does not exist " + _path);

            return;
        }

        using (var reader = new BinaryReader(File.OpenRead(_path)))
        {
            int header = reader.ReadInt32();

            if (header <= mapFileVersion)
            {
                hexGrid.Load(reader, header);

                HexMapCamera.ValidatePosition();
            }
            else
            {
                Debug.LogWarning("Unknown map format " + header);
            }
        }
    }
}