using Robust.Shared.GameStates;

namespace Content.Shared._Wega.Duel;

/// <summary>
/// Помечает сущность, к которой сейчас «тянется» трос арена-гарпуна. Хранит якорь (стрелок или
/// точка зацепа), к которому трос привязан вторым концом. Клиентский <c>ArenaHarpoonRopeOverlay</c>
/// каждый кадр рисует трос между этой сущностью и якорём по их живым позициям, поэтому он «приклеен»
/// к модельке и плавно укорачивается по мере сближения — без рывков и исчезновения кусками.
/// Навешивается/снимается сервером из <c>ArenaHarpoonSystem</c> вместе с подмоткой.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ArenaHarpoonRopeComponent : Component
{
    /// <summary>Сущность-якорь, к которой привязан второй конец троса (стрелок либо стена/конструкция).</summary>
    [DataField, AutoNetworkedField]
    public NetEntity? Anchor;
}
