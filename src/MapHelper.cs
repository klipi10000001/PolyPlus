using Polytopia.Data;

namespace PolyPlus;

public static class MapHelper
{
    public static int GetTileIndex(this MapData map, int x, int y)
    {
        if (x < 0 || x >= map.Width || y < 0 || y >= map.Height)
        {
            return -1;
        }
        var num = x + y * map.Width;
        if (num < 0 || num >= map.Tiles.Length)
        {
            return -1;
        }
        return num;
    }

    public static TileData? GetTile(this MapData map, int x, int y)
    {
        var tileIndex = map.GetTileIndex(x, y);
        if (tileIndex == -1)
        {
            return null;
        }
        return map.Tiles[tileIndex];
    }

    public static List<TileData> GetTilesAtDistance(MapData map, WorldCoordinates coordinates, int distance)
    {
        var finalTiles = new List<TileData>();
        if (distance <= 0) return finalTiles;
        
        var startX = coordinates.X - distance;
        var minusX = startX >= 0;
        
        var startY = coordinates.Y - distance;
        var minusY = startY >= 0;
        
        var endX = coordinates.X + distance;
        var plusX = endX < map.Width;
        
        var endY = coordinates.Y + distance;
        var plusY = endY < map.Height;
        
        if (minusX && minusY) 
        {
            finalTiles.Add(map.GetTile(startX, startY)!);
        }
        if (minusX && plusY) 
        {
            finalTiles.Add(map.GetTile(startX, endY)!);
        }
        if (plusX && minusY) 
        {
            finalTiles.Add(map.GetTile(endX, startY)!);
        }
        if (plusX && plusY) 
        {
            finalTiles.Add(map.GetTile(endX, endY)!);
        }
        for (var i = -distance + 1; i <= distance - 1; i++)
        {
            var newX = coordinates.X + i;
            var newY = coordinates.Y + i;
            var newXValid = newX >= 0 && newX < map.Width;
            var newYValid = newY >= 0 && newY < map.Height;
            
            if (minusX && newYValid) 
            {
                finalTiles.Add(map.GetTile(startX, newY)!);
            }
            if (plusX && newYValid) 
            {
                finalTiles.Add(map.GetTile(endX, newY)!);
            }
            if (minusY && newXValid) 
            {
                finalTiles.Add(map.GetTile(newX, startY)!);
            }
            if (plusY && newXValid) 
            {
                finalTiles.Add(map.GetTile(newX, endY)!);
            }
        }
        
        return finalTiles;
    }

    public static List<TileData> GetTilesWithinDistance(MapData map, WorldCoordinates coordinates, int distance, bool ignoreCenter = false)
    {
        var x = coordinates.X;
        var y = coordinates.Y;

        var finalTiles = new List<TileData>();
        if (distance <= 0) return finalTiles;
        var startX = -Math.Min(distance, x);
        var endX = Math.Min(map.Width - x - 1, distance);
        var startY = -Math.Min(distance, y);
        var endY = Math.Min(map.Height - y - 1, distance);
        for (var j = startY; j <= endY; j++)
        {
            for (var i = startX; i <= endX; i++)
            {
                if (ignoreCenter && j == 0 && i == 0) continue;
                finalTiles.Add(map.GetTile(i + x, j + y)!);
            }
        }

        return finalTiles;
    }
    
    public static List<TileData> GetTilesWithinCardinalDistance(MapData map, WorldCoordinates coordinates, int distance, bool ignoreCenter)
    {
        var x = coordinates.X;
        var y = coordinates.Y;

        var finalTiles = new List<TileData>();
        if (distance <= 0) return finalTiles;
        var startX = -Math.Min(distance, x);
        var endX = Math.Min(map.Width - x - 1, distance);
        var startY = -Math.Min(distance, y);
        var endY = Math.Min(map.Height - y - 1, distance);

        var iMax = distance + startY;
        for (var j = startY; j <= 0; j++, iMax++)
        {
            var endPos = Math.Min(iMax, endX);
            for (var i = Math.Max(-iMax, startX); i <= endPos; i++)
            {
                if (ignoreCenter && j == 0 && i == 0) continue;
                finalTiles.Add(map.GetTile(i + x, j + y)!);
            }
        }
        iMax = distance - 1;
        for (var j = 1; j <= endY; j++, iMax--)
        {
            var endPos = Math.Min(iMax, endX);
            for (var i = Math.Max(-iMax, startX); i <= endPos; i++)
            {
                if (ignoreCenter && j == 0 && i == 0) continue;
                finalTiles.Add(map.GetTile(i + x, j + y)!);
            }
        }

        return finalTiles;
    }

    public static void PostTerrainVillages(MapGenerator generator, MapData map, int maxCityCount)
    {
        var allValidCitySpotIndices = generator.GetAllValidCitySpotIndices(map);
        var cityTotal = 0;
        var playerCapitals = map.Tiles.Where(it => it.improvement?.type == ImprovementData.Type.City && it.capitalOf != 0).ToList();
        var distances = new Dictionary<TileData, int>();
        foreach (var capital in playerCapitals) distances[capital] = 1;
        var cityIndex = -1;
        while (allValidCitySpotIndices.Count > 0 && cityTotal < maxCityCount)
        {
            cityIndex++;
            if (cityIndex >= playerCapitals.Count) cityIndex = 0;
            var currentCity = playerCapitals[cityIndex];
            while (true)
            {
                var tiles = GetTilesAtDistance(map, currentCity.coordinates, distances[currentCity] + 2)
                    .Where(it => allValidCitySpotIndices.Contains(map.GetTileIndex(it.coordinates)))
                    .ToList();
                if (tiles.Count <= 0) 
                {
                    distances[currentCity]++;
                    continue;
                }
                var newCityTile = tiles[generator.random.Range(0, tiles.Count)];
                generator.SetTileAsCity(newCityTile);
                cityTotal++;
                foreach (var tile in GetTilesWithinDistance(map, newCityTile.coordinates, 2))
                {
                    allValidCitySpotIndices.Remove(map.GetTileIndex(tile.coordinates));
                }
                break;
            }
        }
    }
}