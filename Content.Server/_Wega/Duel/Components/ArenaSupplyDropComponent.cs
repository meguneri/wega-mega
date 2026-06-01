using Robust.Shared.GameObjects;

namespace Content.Server._Wega.Duel.Components;

/// <summary>
/// Метка дуэльного ящика снаряжения (Supply Drop). При спавне ящик и весь его лут
/// рекурсивно помечаются <see cref="ArenaIssuedItemComponent"/>, чтобы очистка арены
/// (<see cref="Systems.DuelArenaCleanupSystem"/>) убрала их после окончания раунда —
/// где бы предметы ни оказались (пол, руки, надетое, внутри сумок).
/// </summary>
[RegisterComponent]
public sealed partial class ArenaSupplyDropComponent : Component
{
}
