using System;
using UnityEngine;

namespace SCP096TheShyGuy
{
    internal static class ShyGuyTraits
    {
        internal const string GroupId = "ArvakSCP096_Group";
        internal const string WitnessId = "ArvakSCP096_Witness";
        internal const string RageId = "ArvakSCP096_RageSpeed";
        internal const string Rage2Id = "ArvakSCP096_RageSpeed2";
        internal const string Rage3Id = "ArvakSCP096_RageSpeed3";
        internal const string Rage4Id = "ArvakSCP096_RageSpeed4";
        private static readonly string[] RageStages = { RageId, Rage2Id, Rage3Id, Rage4Id };
        internal static string StageId(int stage) => RageStages[Mathf.Clamp(stage, 0, 3)];
        internal static void ClearRage(WorldBridge world, object scp)
        {
            if (world == null || scp == null) return;
            foreach (string id in RageStages) world.RemoveTrait(scp, id);
        }
        internal static bool SetRageStage(WorldBridge world, object scp, int stage)
        {
            if (world == null || scp == null) return false;
            stage = Mathf.Clamp(stage, 0, 3);
            string active = RageStages[stage];
            if (world.HasTrait(scp, active)) return true;
            // Remove all mutually exclusive speed traits before adding the new one:
            // the bonus must NEVER stack across targets or stages.
            ClearRage(world, scp);
            return world.AddTrait(scp, active);
        }

        private static void RegisterRageStage(string id, float bonus)
        {
            ActorTrait trait = new ActorTrait
            {
                id = id,
                group_id = GroupId,
                path_icon = "ui/icons/scp096_rage",
                can_be_given = false,
                can_be_removed = true,
                can_be_cured = false,
                needs_to_be_explored = false
            };
            AssetManager.traits.add(trait);
            trait.base_stats["speed"] = bonus;
        }

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
                RegisterRageStage(Rage2Id, 105f);
                RegisterRageStage(Rage3Id, 125f);
                RegisterRageStage(Rage4Id, 145f);
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
