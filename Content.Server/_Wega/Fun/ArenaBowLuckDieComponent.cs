using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Wega.Fun;

/// <summary>
/// «Кубик лучника»: при использовании в руке бросает d8 и спавнит в руку владельца лук жёсткого
/// света — по выпавшему числу. Сам кубик после броска исчезает.
/// </summary>
[RegisterComponent]
public sealed partial class ArenaBowLuckDieComponent : Component
{
    /// <summary>
    /// Возможные исходы броска: индекс = (выпавшее число − 1). Длина списка задаёт число граней
    /// (обычно 8) — каждому варианту лука соответствует одно число.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId> Outcomes = new();

    /// <summary>
    /// Дополнительный дроп под ноги владельца по выпавшему числу: ключ = выпавшее число, значение —
    /// список прототипов, которые спавнятся вдобавок к луку. Пустой ключ = ничего лишнего не падает.
    /// </summary>
    [DataField]
    public Dictionary<int, List<EntProtoId>> BonusDrops = new();

    /// <summary>Звук броска кубика.</summary>
    [DataField]
    public SoundSpecifier RollSound = new SoundCollectionSpecifier("Dice");
}
