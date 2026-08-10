using Polytopia.Data;

namespace PolyPlus;
public static class DefenceHelper
{
    internal static int ComputeDefenceBonus(UnitState unit, GameState gameState)
    {
        gameState.TryGetPlayer(unit.owner, out var playerState);
        int bonus;
        var tile = gameState.Map.GetTile(unit.coordinates);

        if (tile == null || !unit.HasAbility(UnitAbility.Type.Fortify))
            bonus = DefenceBonus.Base;
        else
        {
            if (tile.HasImprovement(ImprovementData.Type.City))
            {
                bonus = DefenceBonus.Fortified;
                if (tile.improvement.HasReward(CityReward.CityWall))
                    bonus = DefenceBonus.CityWall;
            }
            else
            {
                if (unit.owner != 0 && playerState != null)
                {
                    bonus = playerState.GetDefenceBonus(tile.terrain, gameState);
                    if (bonus == 1) bonus = DefenceBonus.Base;
                }
                else
                {
                    bonus = DefenceBonus.Base;
                }
            }
        }

        if (bonus == DefenceBonus.Base && unit.HasEffect(UnitEffect.Bubble) &&
            playerState != null && playerState.HasAbility("bubbledefense", gameState))
        {
            bonus = DefenceBonus.Fortified;
        }

        if (unit.HasEffect(UnitEffect.Poisoned))
            bonus /= 2;

        return bonus;
    }

    private static class DefenceBonus
    {
        public const int Base = 10;
        public const int Fortified = 15;
        public const int CityWall = 40;
    }
}