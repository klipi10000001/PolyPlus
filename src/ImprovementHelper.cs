using HarmonyLib;
using Polytopia.Data;

namespace PolyPlus;

public static class ImprovementHelperExtensions
{
    public static bool HasAbility(this ImprovementData data, string ability)
    {
        return data.improvementAbilities.Contains(EnumCache<ImprovementAbility.Type>.GetType(ability));
    }
}

public static class ImprovementHelper
{
    
}