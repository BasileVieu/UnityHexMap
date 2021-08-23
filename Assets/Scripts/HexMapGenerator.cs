using System.Collections.Generic;
using UnityEngine;

public class HexMapGenerator : MonoBehaviour
{
    struct MapRegion
    {
        public int xMin;
        public int xMax;
        public int zMin;
        public int zMax;
    }

    struct ClimateData
    {
        public float clouds;
        public float moisture;
    }

    struct Biome
    {
        public int terrain;
        public int plant;

        public Biome(int _terrain, int _plant)
        {
            terrain = _terrain;
            plant = _plant;
        }
    }

    public enum HemisphereMode
    {
        Both,
        North,
        South
    }

    public HexGrid grid;

    private HexCellPriorityQueue searchFrontier;

    private List<MapRegion> regions;
    
    List<ClimateData> climate = new List<ClimateData>();
    List<ClimateData> nextClimate = new List<ClimateData>();
    List<HexDirection> flowDirections = new List<HexDirection>();

    private int cellCount;
    private int landCells;
    private int searchFrontierPhase;

    private int temperatureJitterChannel;

    private static float[] temperatureBands = {0.1f, 0.3f, 0.6f};
    private static float[] moistureBands = {0.12f, 0.28f, 0.85f};
    
    static Biome[] biomes =
    {
            new Biome(0, 0), new Biome(4, 0), new Biome(4, 0), new Biome(4, 0),
            new Biome(0, 0), new Biome(2, 0), new Biome(2, 1), new Biome(2, 2),
            new Biome(0, 0), new Biome(1, 0), new Biome(1, 1), new Biome(1, 2),
            new Biome(0, 0), new Biome(1, 1), new Biome(1, 2), new Biome(1, 3)
    };

    public bool useFixedSeed;

    public int seed;

    [Range(0.0f, 0.5f)] public float jitterProbability = 0.25f;

    [Range(20, 200)] public int chunkSizeMin = 30;
    [Range(20, 200)] public int chunkSizeMax = 100;

    [Range(0.0f, 1.0f)] public float highRiseProbability = 0.25f;
    [Range(0.0f, 0.4f)] public float sinkProbability = 0.2f;

    [Range(5, 95)] public int landPercentage = 50;
    [Range(1, 5)] public int waterLevel = 3;

    [Range(-4, 0)] public int elevationMinimum = -2;
    [Range(6, 10)] public int elevationMaximum = 8;

    [Range(0, 10)] public int mapBorderX = 5;
    [Range(0, 10)] public int mapBorderZ = 5;

    [Range(0, 10)] public int regionBorder = 5;

    [Range(1, 4)] public int regionCount = 1;

    [Range(0, 100)] public int erosionPercentage = 50;

    [Range(0.0f, 1.0f)] public float startingMoisture = 0.1f;

    [Range(0.0f, 1.0f)] public float evaporationFactor = 0.5f;

    [Range(0.0f, 1.0f)] public float precipitationFactor = 0.25f;

    [Range(0.0f, 1.0f)] public float runoffFactor = 0.25f;

    [Range(0.0f, 1.0f)] public float seepageFactor = 0.125f;

    public HexDirection windDirection = HexDirection.NW;

    [Range(1.0f, 10.0f)] public float windStrength = 4.0f;

    [Range(0, 20)] public int riverPercentage = 10;

    [Range(0.0f, 1.0f)] public float extraLakeProbability = 0.25f;

    [Range(0.0f, 1.0f)] public float lowTemperature;
    [Range(0.0f, 1.0f)] public float highTemperature = 1.0f;

    public HemisphereMode hemisphere;

    [Range(0.0f, 1.0f)] public float temperatureJitter = 0.1f;

    public void GenerateMap(int _x, int _z, bool _wrapping)
    {
        Random.State originalRandomState = Random.state;

        if (!useFixedSeed)
        {
            seed = Random.Range(0, int.MaxValue);
            seed ^= (int) System.DateTime.Now.Ticks;
            seed ^= (int) Time.unscaledTime;
            seed &= int.MaxValue;
        }

        Random.InitState(seed);

        cellCount = _x * _z;

        grid.CreateMap(_x, _z, _wrapping);

        if (searchFrontier == null)
        {
            searchFrontier = new HexCellPriorityQueue();
        }

        for (var i = 0; i < cellCount; i++)
        {
            grid.GetCell(i).WaterLevel = waterLevel;
        }

        CreateRegion();

        CreateLand();

        ErodeLand();

        CreateClimate();

        CreateRivers();

        SetTerrainType();

        for (var i = 0; i < cellCount; i++)
        {
            grid.GetCell(i).SearchPhase = 0;
        }

        Random.state = originalRandomState;
    }

    HexCell GetRandomCell(MapRegion _region) => grid.GetCell(Random.Range(_region.xMin, _region.xMax),
                                                             Random.Range(_region.zMin, _region.zMax));

    void CreateRegion()
    {
        if (regions == null)
        {
            regions = new List<MapRegion>();
        }
        else
        {
            regions.Clear();
        }

        int borderX = grid.wrapping ? regionBorder : mapBorderX;

        MapRegion region;

        switch (regionCount)
        {
            default:
                if (grid.wrapping)
                {
                    borderX = 0;
                }
                
                region.xMin = borderX;
                region.xMax = grid.cellCountX - borderX;
                region.zMin = mapBorderZ;
                region.zMax = grid.cellCountZ - mapBorderZ;

                regions.Add(region);
                
                break;

            case 2:
                if (Random.value < 0.5f)
                {
                    region.xMin = borderX;
                    region.xMax = grid.cellCountX / 2 - regionBorder;
                    region.zMin = mapBorderZ;
                    region.zMax = grid.cellCountZ - mapBorderZ;

                    regions.Add(region);

                    region.xMin = grid.cellCountX / 2 + regionBorder;
                    region.xMax = grid.cellCountX - borderX;

                    regions.Add(region);
                }
                else
                {
                    if (grid.wrapping)
                    {
                        borderX = 0;
                    }
                    
                    region.xMin = borderX;
                    region.xMax = grid.cellCountX - borderX;
                    region.zMin = mapBorderZ;
                    region.zMax = grid.cellCountZ / 2 - regionBorder;

                    regions.Add(region);

                    region.zMin = grid.cellCountZ / 2 + regionBorder;
                    region.zMax = grid.cellCountZ - mapBorderZ;

                    regions.Add(region);
                }

                break;
            
            case 3:
                region.xMin = borderX;
                region.xMax = grid.cellCountX / 3 - regionBorder;
                region.zMin = mapBorderZ;
                region.zMax = grid.cellCountZ - mapBorderZ;
                
                regions.Add(region);
                
                region.xMin = grid.cellCountX / 3 + regionBorder;
                region.xMax = grid.cellCountX * 2 / 3 - regionBorder;
                
                regions.Add(region);
                
                region.xMin = grid.cellCountX * 2 / 3 + regionBorder;
                region.xMax = grid.cellCountX - borderX;
                
                regions.Add(region);

                break;
            
            case 4:
                region.xMin = borderX;
                region.xMax = grid.cellCountX / 2 - regionBorder;
                region.zMin = mapBorderZ;
                region.zMax = grid.cellCountZ / 2 - regionBorder;
                
                regions.Add(region);
                
                region.xMin = grid.cellCountX / 2 + regionBorder;
                region.xMax = grid.cellCountX - borderX;
                
                regions.Add(region);
                
                region.zMin = grid.cellCountZ / 2 + regionBorder;
                region.zMax = grid.cellCountZ - mapBorderZ;
                
                regions.Add(region);
                
                region.xMin = borderX;
                region.xMax = grid.cellCountX / 2 - regionBorder;
                
                regions.Add(region);
                
                break;
            
        }
    }

    void CreateLand()
    {
        int landBudget = Mathf.RoundToInt(cellCount * landPercentage * 0.01f);

        landCells = landBudget;

        for (var guard = 0; guard < 10000; guard++)
        {
            bool sink = Random.value < sinkProbability;

            foreach (MapRegion region in regions)
            {
                int chunkSize = Random.Range(chunkSizeMin, chunkSizeMax - 1);

                if (sink)
                {
                    landBudget = SinkTerrain(chunkSize, landBudget, region);
                }
                else
                {
                    landBudget = RaiseTerrain(chunkSize, landBudget, region);

                    if (landBudget == 0)
                    {
                        return;
                    }
                }
            }
        }

        if (landBudget > 0)
        {
            Debug.LogWarning("Failed to use up " + landBudget + " land budget.");

            landCells -= landBudget;
        }
    }

    int RaiseTerrain(int _chunkSize, int _budget, MapRegion _region)
    {
        searchFrontierPhase += 1;

        HexCell firstCell = GetRandomCell(_region);
        firstCell.SearchPhase = searchFrontierPhase;
        firstCell.Distance = 0;
        firstCell.SearchHeuristic = 0;

        searchFrontier.Enqueue(firstCell);

        HexCoordinates center = firstCell.coordinates;

        int rise = Random.value < highRiseProbability ? 2 : 1;

        var size = 0;

        while (size < _chunkSize
               && searchFrontier.Count > 0)
        {
            HexCell current = searchFrontier.Dequeue();

            int originalElevation = current.Elevation;

            int newElevation = originalElevation + rise;

            if (newElevation > elevationMaximum)
            {
                continue;
            }

            current.Elevation = newElevation;

            if (originalElevation < waterLevel
                && newElevation >= waterLevel
                && --_budget == 0)
            {
                break;
            }

            size += 1;

            for (var direction = HexDirection.NE; direction <= HexDirection.NW; direction++)
            {
                HexCell neighbor = current.GetNeighbor(direction);

                if (neighbor
                    && neighbor.SearchPhase < searchFrontierPhase)
                {
                    neighbor.SearchPhase = searchFrontierPhase;
                    neighbor.Distance = neighbor.coordinates.DistanceTo(center);
                    neighbor.SearchHeuristic = Random.value < jitterProbability ? 1 : 0;

                    searchFrontier.Enqueue(neighbor);
                }
            }
        }

        searchFrontier.Clear();

        return _budget;
    }

    int SinkTerrain(int _chunkSize, int _budget, MapRegion _region)
    {
        searchFrontierPhase += 1;

        HexCell firstCell = GetRandomCell(_region);
        firstCell.SearchPhase = searchFrontierPhase;
        firstCell.Distance = 0;
        firstCell.SearchHeuristic = 0;

        searchFrontier.Enqueue(firstCell);

        HexCoordinates center = firstCell.coordinates;

        int sink = Random.value < highRiseProbability ? 2 : 1;

        var size = 0;

        while (size < _chunkSize
               && searchFrontier.Count > 0)
        {
            HexCell current = searchFrontier.Dequeue();

            int originalElevation = current.Elevation;

            int newElevation = originalElevation - sink;

            if (newElevation < elevationMinimum)
            {
                continue;
            }

            current.Elevation = newElevation;

            if (originalElevation >= waterLevel
                && newElevation < waterLevel)
            {
                _budget += 1;
            }

            size += 1;

            for (var direction = HexDirection.NE; direction <= HexDirection.NW; direction++)
            {
                HexCell neighbor = current.GetNeighbor(direction);

                if (neighbor
                    && neighbor.SearchPhase < searchFrontierPhase)
                {
                    neighbor.SearchPhase = searchFrontierPhase;
                    neighbor.Distance = neighbor.coordinates.DistanceTo(center);
                    neighbor.SearchHeuristic = Random.value < jitterProbability ? 1 : 0;

                    searchFrontier.Enqueue(neighbor);
                }
            }
        }

        searchFrontier.Clear();

        return _budget;
    }

    bool IsErodible(HexCell _cell)
    {
        int erodibleElevation = _cell.Elevation - 2;

        for (var direction = HexDirection.NE; direction <= HexDirection.NW; direction++)
        {
            HexCell neighbor = _cell.GetNeighbor(direction);

            if (neighbor
                && neighbor.Elevation <= erodibleElevation)
            {
                return true;
            }
        }

        return false;
    }

    HexCell GetErosionTarget(HexCell _cell)
    {
        List<HexCell> candidates = ListPool<HexCell>.Get();

        int erodibleElevation = _cell.Elevation - 2;

        for (var direction = HexDirection.NE; direction <= HexDirection.NW; direction++)
        {
            HexCell neighbor = _cell.GetNeighbor(direction);

            if (neighbor
                && neighbor.Elevation <= erodibleElevation)
            {
                candidates.Add(neighbor);
            }
        }

        HexCell target = candidates[Random.Range(0, candidates.Count)];

        ListPool<HexCell>.Add(candidates);

        return target;
    }

    void ErodeLand()
    {
        List<HexCell> erodibleCells = ListPool<HexCell>.Get();

        for (var i = 0; i < cellCount; i++)
        {
            HexCell cell = grid.GetCell(i);

            if (IsErodible(cell))
            {
                erodibleCells.Add(cell);
            }
        }

        var targetErodibleCount = (int) (erodibleCells.Count * (100 - erosionPercentage) * 0.01f);

        while (erodibleCells.Count > targetErodibleCount)
        {
            int index = Random.Range(0, erodibleCells.Count);

            HexCell cell = erodibleCells[index];

            HexCell targetCell = GetErosionTarget(cell);

            cell.Elevation -= 1;

            targetCell.Elevation += 1;

            if (!IsErodible(cell))
            {
                erodibleCells[index] = erodibleCells[erodibleCells.Count - 1];
                erodibleCells.RemoveAt(erodibleCells.Count - 1);
            }

            for (var direction = HexDirection.NE; direction <= HexDirection.NW; direction++)
            {
                HexCell neighbor = cell.GetNeighbor(direction);

                if (neighbor
                    && neighbor.Elevation == cell.Elevation + 2
                    && !erodibleCells.Contains(neighbor))
                {
                    erodibleCells.Add(neighbor);
                }
            }

            if (IsErodible(targetCell)
                && !erodibleCells.Contains(targetCell))
            {
                erodibleCells.Add(targetCell);
            }

            for (var direction = HexDirection.NE; direction <= HexDirection.NW; direction++)
            {
                HexCell neighbor = targetCell.GetNeighbor(direction);

                if (neighbor
                    && neighbor != cell
                    && neighbor.Elevation == targetCell.Elevation + 1
                    && !IsErodible(neighbor))
                {
                    erodibleCells.Remove(neighbor);
                }
            }
        }

        ListPool<HexCell>.Add(erodibleCells);
    }

    void CreateClimate()
    {
        climate.Clear();
        nextClimate.Clear();

        var initialData = new ClimateData
        {
                moisture = startingMoisture
        };

        var clearData = new ClimateData();

        for (var i = 0; i < cellCount; i++)
        {
            climate.Add(initialData);
            nextClimate.Add(clearData);
        }

        for (var cycle = 0; cycle < 40; cycle++)
        {
            for (var i = 0; i < cellCount; i++)
            {
                EvolveClimate(i);
            }

            List<ClimateData> swap = climate;
            climate = nextClimate;
            nextClimate = swap;
        }
    }

    void EvolveClimate(int _cellIndex)
    {
        HexCell cell = grid.GetCell(_cellIndex);

        ClimateData cellClimate = climate[_cellIndex];

        if (cell.IsUnderwater)
        {
            cellClimate.moisture = 1.0f;
            cellClimate.clouds += evaporationFactor;
        }
        else
        {
            float evaporation = cellClimate.moisture * evaporationFactor;
            cellClimate.moisture -= evaporation;
            cellClimate.clouds += evaporation;
        }

        float precipitation = cellClimate.clouds * precipitationFactor;
        cellClimate.clouds -= precipitation;
        cellClimate.moisture += precipitation;

        float cloudMaximum = 1.0f - cell.ViewElevation / (elevationMaximum + 1.0f);

        if (cellClimate.clouds > cloudMaximum)
        {
            cellClimate.moisture += cellClimate.clouds - cloudMaximum;
            cellClimate.clouds = cloudMaximum;
        }

        HexDirection mainDispersalDirection = windDirection.Opposite();

        float cloudDispersal = cellClimate.clouds * (1.0f / (5.0f + windStrength));

        float runoff = cellClimate.moisture * runoffFactor * (1.0f / 6.0f);

        float seepage = cellClimate.moisture * seepageFactor * (1.0f / 6.0f);

        for (var direction = HexDirection.NE; direction <= HexDirection.NW; direction++)
        {
            HexCell neighbor = cell.GetNeighbor(direction);

            if (!neighbor)
            {
                continue;
            }

            ClimateData neighborClimate = nextClimate[neighbor.Index];

            if (direction == mainDispersalDirection)
            {
                neighborClimate.clouds += cloudDispersal * windStrength;
            }
            else
            {
                neighborClimate.clouds += cloudDispersal;
            }

            int elevationDelta = neighbor.ViewElevation - cell.ViewElevation;

            if (elevationDelta < 0)
            {
                cellClimate.moisture -= runoff;
                neighborClimate.moisture += runoff;
            }
            else if (elevationDelta == 0)
            {
                cellClimate.moisture -= seepage;
                neighborClimate.moisture += seepage;
            }

            nextClimate[neighbor.Index] = neighborClimate;
        }

        ClimateData nextCellClimate = nextClimate[_cellIndex];
        nextCellClimate.moisture += cellClimate.moisture;

        if (nextCellClimate.moisture > 1.0f)
        {
            nextCellClimate.moisture = 1.0f;
        }
        
        nextClimate[_cellIndex] = nextCellClimate;

        climate[_cellIndex] = new ClimateData();
    }

    void CreateRivers()
    {
        List<HexCell> riverOrigins = ListPool<HexCell>.Get();

        for (var i = 0; i < cellCount; i++)
        {
            HexCell cell = grid.GetCell(i);

            if (cell.IsUnderwater)
            {
                continue;
            }

            ClimateData data = climate[i];

            float weight = data.moisture * (cell.Elevation - waterLevel) / (elevationMaximum - waterLevel);

            if (weight > 0.75f)
            {
                riverOrigins.Add(cell);
                riverOrigins.Add(cell);
            }

            if (weight > 0.5f)
            {
                riverOrigins.Add(cell);
            }

            if (weight > 0.25f)
            {
                riverOrigins.Add(cell);
            }
        }

        int riverBudget = Mathf.RoundToInt(landCells * riverPercentage * 0.01f);

        while (riverBudget > 0
               && riverOrigins.Count > 0)
        {
            int index = Random.Range(0, riverOrigins.Count);

            int lastIndex = riverOrigins.Count - 1;

            HexCell origin = riverOrigins[index];

            riverOrigins[index] = riverOrigins[lastIndex];
            riverOrigins.RemoveAt(lastIndex);

            if (!origin.HasRiver)
            {
                var isValidOrigin = true;

                for (var direction = HexDirection.NE; direction <= HexDirection.NW; direction++)
                {
                    HexCell neighbor = origin.GetNeighbor(direction);

                    if (neighbor
                        && (neighbor.HasRiver
                            || neighbor.IsUnderwater))
                    {
                        isValidOrigin = false;

                        break;
                    }
                }

                if (isValidOrigin)
                {
                    riverBudget -= CreateRiver(origin);
                }
            }
        }

        if (riverBudget > 0)
        {
            Debug.LogWarning("Failed to use up river budget.");
        }

        ListPool<HexCell>.Add(riverOrigins);
    }

    int CreateRiver(HexCell _origin)
    {
        var length = 1;

        HexCell cell = _origin;

        var direction = HexDirection.NE;

        while (!cell.IsUnderwater)
        {
            var minNeighborElevation = int.MaxValue;
            
            flowDirections.Clear();

            for (var d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = cell.GetNeighbor(d);

                if (!neighbor)
                {
                    continue;
                }

                if (neighbor.Elevation < minNeighborElevation)
                {
                    minNeighborElevation = neighbor.Elevation;
                }
                
                if (neighbor == _origin
                    || neighbor.HasIncomingRiver)
                {
                    continue;
                }

                int delta = neighbor.Elevation - cell.Elevation;

                if (delta > 0)
                {
                    continue;
                }

                if (neighbor.HasOutgoingRiver)
                {
                    cell.SetOutgoingRiver(d);

                    return length;
                }

                if (delta < 0)
                {
                    flowDirections.Add(d);
                    flowDirections.Add(d);
                    flowDirections.Add(d);
                }

                if (length == 1
                    || d != direction.Next2()
                    && d != direction.Previous2())
                {
                    flowDirections.Add(d);
                }

                flowDirections.Add(d);
            }

            if (flowDirections.Count == 0)
            {
                if (length == 1)
                {
                    return 0;
                }

                if (minNeighborElevation >= cell.Elevation)
                {
                    cell.WaterLevel = minNeighborElevation;

                    if (minNeighborElevation == cell.Elevation)
                    {
                        cell.Elevation = minNeighborElevation - 1;
                    }
                }

                break;
            }

            direction = flowDirections[Random.Range(0, flowDirections.Count)];

            cell.SetOutgoingRiver(direction);

            length += 1;

            if (minNeighborElevation >= cell.Elevation
                && Random.value < extraLakeProbability)
            {
                cell.WaterLevel = cell.Elevation;
                cell.Elevation -= 1;
            }

            cell = cell.GetNeighbor(direction);
        }

        return length;
    }

    float DetermineTemperature(HexCell _cell)
    {
        float latitude = (float) _cell.coordinates.Z / grid.cellCountZ;

        if (hemisphere == HemisphereMode.Both)
        {
            latitude *= 2.0f;

            if (latitude > 1.0f)
            {
                latitude = 2.0f - latitude;
            }
        }
        else if (hemisphere == HemisphereMode.North)
        {
            latitude = 1.0f - latitude;
        }

        float temperature = Mathf.LerpUnclamped(lowTemperature, highTemperature, latitude);

        temperature *= 1.0f - (_cell.ViewElevation - waterLevel) / (elevationMaximum - waterLevel + 1.0f);

        float jitter = HexMetrics.SampleNoise(_cell.Position * 0.1f)[temperatureJitterChannel];
        
        temperature += (jitter * 2.0f - 1.0f) * temperatureJitter;

        return temperature;
    }

    void SetTerrainType()
    {
        temperatureJitterChannel = Random.Range(0, 4);

        int rockDesertElevation = elevationMaximum - (elevationMaximum - waterLevel) / 2;
        
        for (var i = 0; i < cellCount; i++)
        {
            HexCell cell = grid.GetCell(i);

            float temperature = DetermineTemperature(cell);

            float moisture = climate[i].moisture;

            if (!cell.IsUnderwater)
            {
                var t = 0;
                
                for (; t < temperatureBands.Length; t++)
                {
                    if (temperature < temperatureBands[t])
                    {
                        break;
                    }
                }
                
                var m = 0;
                
                for (; m < moistureBands.Length; m++)
                {
                    if (moisture < moistureBands[m])
                    {
                        break;
                    }
                }

                Biome cellBiome = biomes[t * 4 + m];

                if (cellBiome.terrain == 0)
                {
                    if (cell.Elevation >= rockDesertElevation)
                    {
                        cellBiome.terrain = 3;
                    }
                }
                else if (cell.Elevation == elevationMaximum)
                {
                    cellBiome.terrain = 4;
                }

                if (cellBiome.terrain == 4)
                {
                    cellBiome.plant = 0;
                }
                else if (cellBiome.plant < 3
                         && cell.HasRiver)
                {
                    cellBiome.plant += 1;
                }
                
                cell.TerrainTypeIndex = cellBiome.terrain;
                cell.PlantLevel = cellBiome.plant;
            }
            else
            {
                int terrain;

                if (cell.Elevation == waterLevel - 1)
                {
                    var cliffs = 0;
                    var slopes = 0;

                    for (var direction = HexDirection.NE; direction <= HexDirection.NW; direction++)
                    {
                        HexCell neighbor = cell.GetNeighbor(direction);

                        if (!neighbor)
                        {
                            continue;
                        }

                        int delta = neighbor.Elevation - cell.WaterLevel;

                        if (delta == 0)
                        {
                            slopes += 1;
                        }
                        else if (delta > 0)
                        {
                            cliffs += 1;
                        }
                    }

                    if (cliffs + slopes > 3)
                    {
                        terrain = 1;
                    }
                    else if (cliffs > 0)
                    {
                        terrain = 3;
                    }
                    else if (slopes > 0)
                    {
                        terrain = 0;
                    }
                    else
                    {
                        terrain = 1;
                    }
                }
                else if (cell.Elevation >= waterLevel)
                {
                    terrain = 1;
                }
                else if (cell.Elevation < 0)
                {
                    terrain = 3;
                }
                else
                {
                    terrain = 2;
                }

                if (terrain == 1
                    && temperature < temperatureBands[0])
                {
                    terrain = 2;
                }
                
                cell.TerrainTypeIndex = terrain;
            }
        }
    }
}