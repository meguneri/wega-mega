using Robust.Shared.GameStates;

namespace Content.Shared._Wega.Barricade;

/// <summary>
/// Служебный компонент, навешиваемый на снаряд при первом столкновении с баррикадой. Кеширует
/// решение «проходит/перехвачен» по каждой баррикаде, чтобы один и тот же снаряд получал
/// стабильный результат для каждой баррикады (физический контакт может опрашиваться несколько раз
/// за полёт). Решение принимается на сервере и сетится клиенту через <see cref="CollideBarricades"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedBarricadeSystem))]
public sealed partial class PassBarricadeComponent : Component
{
    /// <summary>Баррикада -> проходит ли снаряд сквозь неё (true) или перехвачен (false).</summary>
    [ViewVariables, AutoNetworkedField]
    public Dictionary<EntityUid, bool> CollideBarricades = new();
}
