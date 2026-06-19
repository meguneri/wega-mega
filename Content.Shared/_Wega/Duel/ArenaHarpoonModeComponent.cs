namespace Content.Shared._Wega.Duel;

/// <summary>
/// Переключатель режима добивания на гарпуне-потрошителе: использование в руке (Use-in-hand)
/// перебирает доступные исходы (<see cref="ArenaHarpoonFinisher"/>). Выбранный режим переопределяет
/// поведение снаряда по умолчанию — его читает <c>ArenaHarpoonSystem</c> в момент зацепа по стрелявшему
/// оружию. Логика чисто серверная.
/// </summary>
[RegisterComponent]
public sealed partial class ArenaHarpoonModeComponent : Component
{
    /// <summary>Доступные режимы, перебираются по кругу. По умолчанию — срыв конечности и обезглавливание.</summary>
    [DataField]
    public List<ArenaHarpoonFinisher> Modes = new() { ArenaHarpoonFinisher.Dismember, ArenaHarpoonFinisher.Behead };

    /// <summary>Индекс текущего режима в <see cref="Modes"/>.</summary>
    [DataField]
    public int Index;

    /// <summary>Текущий выбранный исход добивания.</summary>
    public ArenaHarpoonFinisher Current => Modes.Count == 0 ? ArenaHarpoonFinisher.None : Modes[Index % Modes.Count];
}
