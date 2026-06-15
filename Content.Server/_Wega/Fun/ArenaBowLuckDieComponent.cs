using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Wega.Fun;

/// <summary>
/// «Кубик лучника»: при использовании в руке бросает d6 и спавнит в руку владельца лук жёсткого
/// света, залоченный на один тип стрел — по выпавшему числу. Сам кубик после броска исчезает.
/// </summary>
[RegisterComponent]
public sealed partial class ArenaBowLuckDieComponent : Component
{
    /// <summary>
    /// Возможные исходы броска: индекс = (выпавшее число − 1). Длина списка задаёт число граней
    /// (обычно 6) — каждому варианту лука соответствует одно число.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId> Outcomes = new();

    /// <summary>Звук броска кубика.</summary>
    [DataField]
    public SoundSpecifier RollSound = new SoundCollectionSpecifier("Dice");
}
