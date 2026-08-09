using Polytopia.Data;

namespace PolyPlus;

public class AdjacencyRequirementData
{
    public Dictionary<TileType, List<AdjacencyRequirements>> requirements = new();

    public AdjacencyRequirementData()
    {
        foreach (var tileType in Enum.GetValues<TileType>())
        {
            requirements.Add(tileType, new List<AdjacencyRequirements>());
        }
    }

    public AdjacencyRequirementData (ImprovementData improvement)  : this()
    {
        foreach (var requirement in improvement.adjacencyRequirements)
        {
            if (requirement.resource != null && requirement.resource.type != ResourceData.Type.None)
            {
               requirements[TileType.Resource].Add(requirement); 
            }
            
            if (requirement.terrain != null  && requirement.terrain.type != TerrainData.Type.None)
            {
                requirements[TileType.Terrain].Add(requirement);
            }

            if (requirement.improvement != null && requirement.improvement.type != ImprovementData.Type.None)
            {
                requirements[TileType.Improvement].Add(requirement);
            }

            if (false)
            {
                
            }
        }
    }
}