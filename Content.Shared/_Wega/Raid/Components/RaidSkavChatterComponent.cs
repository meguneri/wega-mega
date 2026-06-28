using Robust.Shared.GameObjects;

namespace Content.Shared._Wega.Raid.Components;

[RegisterComponent]
public sealed partial class RaidSkavChatterComponent : Component
{
    [DataField]
    public List<string> Phrases = new();

    [DataField]
    public List<string> CombatPhrases = new();

    [DataField]
    public float CombatBarkCooldown = 8f;

    [ViewVariables]
    public TimeSpan NextCombatBark;

    [DataField]
    public float MinDelay = 12f;

    [DataField]
    public float MaxDelay = 30f;

    [DataField]
    public float Chance = 0.6f;

    [ViewVariables]
    public TimeSpan NextSpeak;
}
