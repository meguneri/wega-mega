namespace Content.Server.Weapons.ChangeTemperatureOnHit;

/// <summary>
/// Changes the temperature of entities hit by this melee weapon.
/// Ported from Goobstation (arcane-station).
/// </summary>
[RegisterComponent]
public sealed partial class ChangeTemperatureOnHitComponent : Component
{
    [DataField]
    public float Heat;

    [DataField]
    public bool IgnoreResistances = true;
}
