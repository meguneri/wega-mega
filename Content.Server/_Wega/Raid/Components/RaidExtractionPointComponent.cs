using Robust.Shared.Audio;
using Robust.Shared.GameObjects;

namespace Content.Server._Wega.Raid.Components;

/// <summary>
/// Точка экстракта на карте рейда. Пока живой рейдер непрерывно стоит в радиусе <see cref="Range"/>
/// в течение <see cref="ExtractTime"/> секунд — он эвакуируется на хаб (выжил со своим снаряжением).
/// Если рейдер вышел из зоны или упал в крит — прогресс сбрасывается.
///
/// Размещается при маппинге (обычно на краю/в укрытии локации). Несколько точек экстракта на одной
/// карте поддерживаются.
/// </summary>
[RegisterComponent]
public sealed partial class RaidExtractionPointComponent : Component
{
    /// <summary>Радиус зоны экстракта в тайлах.</summary>
    [DataField]
    public float Range = 1.5f;

    /// <summary>Сколько секунд нужно непрерывно простоять в зоне для эвакуации.</summary>
    [DataField]
    public float ExtractTime = 10f;

    /// <summary>Звук, проигрываемый рейдеру в момент начала эвакуации (вход в зону).</summary>
    [DataField]
    public SoundSpecifier? StartSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");

    /// <summary>Звук успешной эвакуации.</summary>
    [DataField]
    public SoundSpecifier? ExtractSound = new SoundPathSpecifier("/Audio/_Wega/Duel/duel_start.ogg");

    /// <summary>
    /// Накопленный прогресс эвакуации по каждому рейдеру в зоне (UID → секунды). Сбрасывается, когда
    /// рейдер покидает зону. Рантайм-состояние, не сериализуется.
    /// </summary>
    [ViewVariables]
    public Dictionary<EntityUid, float> Progress = new();
}
