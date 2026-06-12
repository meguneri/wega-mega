using Robust.Shared.GameStates;

namespace Content.Shared._Wega.Aphrodisiac;

/// <summary>
///     Статус-эффект афродизиака: розовый оверлей с сердечками на клиенте.
///     Вешается на сущность статус-эффекта (новая система StatusEffectNew),
///     как <c>SeeingRainbowsStatusEffectComponent</c>. Портировано из lust-station,
///     адаптировано под новую систему статус-эффектов.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class LoveVisionStatusEffectComponent : Component;
