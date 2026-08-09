using Polytopia.Data;

namespace PolyPlus;

public class TerrainRequirementData
{
    public Dictionary<TileType, List<TerrainRequirements>> requirements = new();

    public TerrainRequirementData()
    {
        foreach (var tileType in Enum.GetValues<TileType>())
        {
            requirements.Add(tileType, new List<TerrainRequirements>());
        }
    }

    public TerrainRequirementData (ImprovementData improvement) : this()
    {
        foreach (var requirement in improvement.terrainRequirements)
        {
            if (requirement.resource != null && requirement.resource.type != ResourceData.Type.None)
            {
               requirements[TileType.Resource].Add(requirement); 
            }

            if (requirement.terrain != null  && requirement.terrain.type != TerrainData.Type.None)
            {
                requirements[TileType.Terrain].Add(requirement);
            }
        }
    }

    public List<TerrainRequirements> this[TileType tileType]
    {
        get => requirements[tileType];
        set => requirements[tileType] = value;
    }
}