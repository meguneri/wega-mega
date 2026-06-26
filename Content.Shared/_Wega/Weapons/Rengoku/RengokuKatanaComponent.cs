using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Wega.Weapons.Rengoku;

/// <summary>
/// Гранит катане Рэнгоку два приёма Дыхания Пламени.
/// Действия выдаются носителю через ActionGrant/ItemActionGrant и
/// обрабатываются на стороне сервера в RengokuKatanaSystem.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RengokuKatanaComponent : Component
{
    // === Первая форма: Неведомый огонь (炎の呼吸 壹ノ型 知ル火) ===
    // Веерный взмах: поджигает и наносит урон всем в конусе перед носителем.

    /// <summary>Радиус поражения первой формы (в тайлах).</summary>
    [DataField]
    public float FirstFormRadius = 3.5f;

    /// <summary>
    /// Полуугол конуса первой формы в градусах. 180 = круговой взмах,
    /// меньше — узкий веер строго перед носителем.
    /// </summary>
    [DataField]
    public float FirstFormHalfAngle = 75f;

    /// <summary>Урон первой формы каждой цели в конусе.</summary>
    [DataField]
    public DamageSpecifier FirstFormDamage = new()
    {
        DamageDict =
        {
            ["Slash"] = 20,
            ["Heat"] = 15,
        },
    };

    /// <summary>Сколько стаков огня вешается на каждую цель.</summary>
    [DataField]
    public float FirstFormFireStacks = 4f;

    [DataField]
    public SoundSpecifier FirstFormSound = new SoundPathSpecifier("/Audio/Effects/fire.ogg");

    /// <summary>Опциональный визуальный эффект, спавнится в позиции носителя.</summary>
    [DataField]
    public EntProtoId? FirstFormEffect;

    // === Девятая форма: Рэнгоку (玖ノ型 煉獄) ===
    // Коронный приём — огненный рывок сквозь врагов с мощным взрывом пламени.

    /// <summary>Максимальная дальность рывка (в тайлах).</summary>
    [DataField]
    public float NinthFormRange = 6f;

    /// <summary>Скорость рывка.</summary>
    [DataField]
    public float NinthFormSpeed = 13f;

    /// <summary>Радиус огненного взрыва в конце рывка.</summary>
    [DataField]
    public float NinthFormRadius = 3f;

    /// <summary>Урон девятой формы каждой задетой цели.</summary>
    [DataField]
    public DamageSpecifier NinthFormDamage = new()
    {
        DamageDict =
        {
            ["Slash"] = 35,
            ["Heat"] = 30,
        },
    };

    /// <summary>Сколько стаков огня вешается на каждую цель.</summary>
    [DataField]
    public float NinthFormFireStacks = 10f;

    [DataField]
    public SoundSpecifier NinthFormSound = new SoundPathSpecifier("/Audio/Effects/explosion1.ogg");

    /// <summary>Звук в момент старта рывка (рёв пламени).</summary>
    [DataField]
    public SoundSpecifier NinthFormChargeSound = new SoundPathSpecifier("/Audio/Effects/fire.ogg");

    /// <summary>Главный эффект взрыва в центре удара.</summary>
    [DataField]
    public EntProtoId? NinthFormEffect;

    /// <summary>Мелкий эффект пламени для огненного следа вдоль рывка.</summary>
    [DataField]
    public EntProtoId? NinthFormTrailEffect;

    /// <summary>Эффект, спавнящийся НА каждой задетой девятой формой цели.</summary>
    [DataField]
    public EntProtoId? NinthFormHitEffect;

    /// <summary>Сколько вспышек пламени оставляет след за время рывка.</summary>
    [DataField]
    public int NinthFormTrailCount = 5;

    /// <summary>Сила тряски экрана у тех, кто рядом с ударом.</summary>
    [DataField]
    public float NinthFormShakeStrength = 1.2f;
}
