using Il2CppSystem.Runtime.CompilerServices;
using Polytopia.Data;

namespace PolyPlus;

public class ImprovementDataPatches
{
    public readonly TerrainRequirementData TerrainRequirements;
    public readonly AdjacencyRequirementData AdjacencyRequirements;
    public ImprovementData ImprovementData;

    public ImprovementDataPatches(ImprovementData improvementData)
    {
        ImprovementData = improvementData;
        TerrainRequirements = new TerrainRequirementData(improvementData);
        AdjacencyRequirements = new AdjacencyRequirementData(improvementData);
    }
}

public static class ImprovementDataPatchTable
{
    public static readonly Dictionary<ImprovementData, ImprovementDataPatches> PatchTable = new();

    public static TerrainRequirementData GetTerrainRequirementData(this ImprovementData improvementData)
    {
        if (PatchTable.TryGetValue(improvementData, out var improvementPatch))
            return improvementPatch.TerrainRequirements;
        var data = new ImprovementDataPatches(improvementData);
        PatchTable.Add(improvementData, data);
        return data.TerrainRequirements;
    }
    
    public static AdjacencyRequirementData GetAdjacencyRequirementData(this ImprovementData improvementData)
    {
        if (PatchTable.TryGetValue(improvementData, out var improvementPatch))
            return improvementPatch.AdjacencyRequirements;
        var data = new ImprovementDataPatches(improvementData);
        PatchTable.Add(improvementData, data);
        return data.AdjacencyRequirements;
    }
}