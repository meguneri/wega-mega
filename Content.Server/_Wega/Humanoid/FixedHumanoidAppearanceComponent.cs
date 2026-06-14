using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;

namespace Content.Server._Wega.Humanoid;

/// <summary>
/// Forces a fixed humanoid appearance (height and/or markings) on map init,
/// after any randomization has already run. Used for named NPCs like
/// Сатору Годжо who must always look the same.
/// </summary>
[RegisterComponent]
public sealed partial class FixedHumanoidAppearanceComponent : Component
{
    /// <summary>
    /// Height in cm to force. Null leaves the height untouched.
    /// </summary>
    [DataField]
    public float? Height;

    /// <summary>
    /// Markings to apply, keyed by organ category and then visual layer.
    /// Only the listed layers are overridden; everything else is left as-is.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> Markings = new();
}
