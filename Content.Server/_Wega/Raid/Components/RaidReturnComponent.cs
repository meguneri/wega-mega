using Robust.Shared.GameObjects;

namespace Content.Server._Wega.Raid.Components;

/// <summary>
/// Невидимый маркер точки возврата на хаб. Сюда телепортируется рейдер при успешном экстракте, а
/// также все, кого принудительно эвакуировало по истечении таймера рейда. Ставится на хаб-карту
/// (рядом с контроллером рейда). Если маркера нет — возврат идёт на позицию самого контроллера.
/// </summary>
[RegisterComponent]
public sealed partial class RaidReturnComponent : Component
{
}
