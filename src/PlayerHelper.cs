using Polytopia.Data;

namespace PolyPlus;

public static class PlayerHelper
{
    public static bool HasAbility(this PlayerState playerState, string ability, GameState gameState)
    {
        var abilityType = EnumCache<PlayerAbility.Type>.GetType(ability);
        return playerState.HasAbility(abilityType, gameState);
    } 
}