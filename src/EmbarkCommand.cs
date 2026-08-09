using Il2CppInterop.Runtime.Injection;

namespace PolyPlus;

public class EmbarkCommand : CommandBase
{
    public WorldCoordinates Coordinates { get; protected set; }

    public EmbarkCommand(IntPtr intPtr) : base(intPtr) {}
    public EmbarkCommand() : base(ClassInjector.DerivedConstructorPointer<EmbarkCommand>())
    {
        ClassInjector.DerivedConstructorBody(this);
    }

    public static EmbarkCommand CreateCommand(byte playerId, WorldCoordinates coordinates)
    {
        if (!ClassInjector.IsTypeRegisteredInIl2Cpp<EmbarkCommand>())
            ClassInjector.RegisterTypeInIl2Cpp<EmbarkCommand>();
        var embarkCommand = new EmbarkCommand
        {
            PlayerId = playerId,
            Coordinates = coordinates
        };
        return embarkCommand;
    }

    public override bool IsValid(GameState state, out string validationError)
    {
        if (!PassesBasicValidation(state, out validationError))
        {
            return false;
        }
        var tile = state.Map.GetTile(Coordinates);
        if (tile == null || tile.unit == null)
        {
            validationError = "Tile is missing unit";
            return false;
        }
        if (!tile.unit.CanEmbark(state))
        {
            validationError = "cant embark here";
            return false;
        }
        return true;
    }

    public override void Execute(GameState gameState)
    {
        gameState.ActionStack.Add(new EmbarkAction(PlayerId, Coordinates));
    }

    public override CommandType GetCommandType()
    {
        return EnumCache<CommandType>.GetType("embarkcommand");
    }

    public override void Serialize(Il2CppSystem.IO.BinaryWriter writer, int version)
    {
        base.Serialize(writer, version);
        Coordinates.Serialize(writer, version);
    }

    public override void Deserialize(Il2CppSystem.IO.BinaryReader reader, int version)
    {
        base.Deserialize(reader, version);
        Coordinates = new WorldCoordinates(reader, version);
    }
}