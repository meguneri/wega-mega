namespace Content.Server.Dice;

[RegisterComponent]
public sealed partial class DiceOfFateComponent : Component
{
    public bool Used;

    /// <summary>
    /// Если true — кубик разыгрывает боевой набор исходов под дуэльную арену (только баффы,
    /// оружие, броня, медикаменты, без негатива). Если false — исходный «общесерверный»
    /// кубик судьбы с летальными провалами.
    /// </summary>
    [DataField]
    public bool Arena;
}
