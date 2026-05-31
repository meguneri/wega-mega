using Robust.Shared.GameObjects;

namespace Content.Server._Wega.Duel.Components;

/// <summary>
/// Метка предмета, выданного дуэльным ящиком-арсеналом (<see cref="SurplusBundle"/> с
/// <c>markIssuedItems: true</c>). Очистка арены (<see cref="Systems.DuelArenaCleanupSystem"/>)
/// удаляет только помеченные предметы, где бы они ни находились (пол, руки, надетое,
/// внутри рюкзаков/ящиков), а вещи игроков и предметы карты не трогает.
/// </summary>
[RegisterComponent]
public sealed partial class ArenaIssuedItemComponent : Component
{
}
