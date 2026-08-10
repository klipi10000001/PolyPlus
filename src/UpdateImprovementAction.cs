using Il2CppInterop.Runtime.Injection;

namespace PolyPlus;

public class UpdateImprovementAction : ActionBase
{
    public UpdateImprovementAction(IntPtr ptr) : base(ptr) { }

    private UpdateImprovementAction() : base(ClassInjector.DerivedConstructorPointer<UpdateImprovementAction>())
    {
        ClassInjector.DerivedConstructorBody(this);
    }

    public static UpdateImprovementAction CreateUpgradeImprovementAction(byte playerId, TileData tile)
    {
        if (!ClassInjector.IsTypeRegisteredInIl2Cpp<UpdateImprovementAction>())
            ClassInjector.RegisterTypeInIl2Cpp<UpdateImprovementAction>();
        var action = new UpdateImprovementAction
        {
            PlayerId = playerId,
            WorldCoordinates = tile.coordinates
        };
        return action;
    }

    public WorldCoordinates WorldCoordinates;

    public override void Execute(GameState state)
    {
        ActionUtils.UpdateImprovementLevel(state, PlayerId, state.Map.GetTile(WorldCoordinates));
    }
}