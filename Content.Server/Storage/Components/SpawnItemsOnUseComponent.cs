using Content.Shared.Storage;
using Robust.Shared.Audio;

namespace Content.Server.Storage.Components
{
    /// <summary>
    ///     Spawns items when used in hand.
    /// </summary>
    [RegisterComponent]
    public sealed partial class SpawnItemsOnUseComponent : Component
    {
        /// <summary>
        ///     The list of entities to spawn, with amounts and orGroups.
        /// </summary>
        [DataField("items", required: true)]
        public List<EntitySpawnEntry> Items = new();

        /// <summary>
        ///     A sound to play when the items are spawned. For example, gift boxes being unwrapped.
        /// </summary>
        [DataField("sound")]
        public SoundSpecifier? Sound = null;

        /// <summary>
        ///     How many uses before the item should delete itself.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("uses")]
        public int Uses = 1;

        /// <summary>
        ///     Wega: if true, spawned clothing is auto-equipped onto the user (into the first matching empty
        ///     inventory slot) instead of dropping at their feet. Non-clothing (weapons, food) still falls
        ///     through to a free hand or the floor. Lets the arena LARP kits dress the duelist on unwrap.
        ///     Already-worn slots are never stripped. Leave false for vanilla gift boxes / cigarette packs.
        /// </summary>
        [DataField]
        public bool EquipToUser;
    }
}
