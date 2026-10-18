using System;
using UnityEngine;

namespace SCP096TheShyGuy
{
    internal static class ShyGuyTraits
    {
        internal const string GroupId = "ArvakSCP096_Group";
        internal const string WitnessId = "ArvakSCP096_Witness";
        internal const string RageId = "ArvakSCP096_RageSpeed";

        internal static bool Register()
        {
            try
            {
                // API identical to the working The Unrelenting installation.
                AssetManager.trait_groups.add(new ActorTraitGroupAsset
                {
                    id = GroupId,
                    name = "trait_group_ArvakSCP096",
                    color = "#D7E0EA"
                });
                AssetManager.traits.add(new ActorTrait
                {
                    id = WitnessId,
                    group_id = GroupId,
                    path_icon = "ui/icons/scp096_witness",
                    can_be_given = true,
                    can_be_removed = true,
                    can_be_cured = false,
                    needs_to_be_explored = false
                });
                ActorTrait rage = new ActorTrait
                {
                    id = RageId,
                    group_id = GroupId,
                    path_icon = "ui/icons/scp096_rage",
                    can_be_given = false,
                    can_be_removed = true,
                    can_be_cured = false,
                    needs_to_be_explored = false
                };
                AssetManager.traits.add(rage);
                rage.base_stats["speed"] = 85f;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SCP096] Registro das traits falhou: " + e);
                return false;
            }
        }
    }
}
