using HarmonyLib;
using UnityEngine;

namespace Datys_Skills
{
    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    public class WeaponAbilityPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance)
        {
            if (__instance == null || !__instance.IsPlayer() || __instance != Player.m_localPlayer)
                return;

            // Načti položku z hlavní ruky
            ItemDrop.ItemData mainItem = __instance.GetCurrentWeapon();
            // Načti off-hand položku (štít) pomocí naší extension metody
            ItemDrop.ItemData offHandItem = __instance.GetEquippedShield();

            Ability mainAbility1 = null;
            Ability mainAbility2 = null;
            Ability offHandAbility = null;

            // Pokud máme položku v hlavní ruce, načti schopnosti z AbilityManageru
            if (mainItem != null && mainItem.m_dropPrefab != null &&
                AbilityManager.weaponAbilities.TryGetValue(mainItem.m_dropPrefab.name, out var mainAbilities))
            {
                mainAbility1 = mainAbilities.ability1;
                mainAbility2 = mainAbilities.ability2;
            }

            // Pokud je vybaven štít, načti jeho schopnosti (očekáváme, že je definována ve slotu ability2)
            if (offHandItem != null && offHandItem.m_dropPrefab != null &&
                AbilityManager.weaponAbilities.TryGetValue(offHandItem.m_dropPrefab.name, out var offAbilities))
            {
                // Pokud je u štítu definovaná ability2, použij ji, jinak případně fallback na ability1
                offHandAbility = offAbilities.ability2 ?? offAbilities.ability1;
            }

            // Sloty pro schopnosti:
            // - Slot1 bude vždy z hlavní ruky (ability1).
            // - Slot2 bude z off-hand (štít) pokud existuje, jinak z hlavní ruky (ability2).
            Ability slot1 = mainAbility1;
            Ability slot2 = offHandItem != null ? offHandAbility : mainAbility2;

            // Aktivace schopností dle stisknutých kláves
            if (!Datys_SkillsPlugin.AbilityActive)
            {
                if (Input.GetKeyDown(Datys_SkillsPlugin.ability1Key.Value.MainKey) && slot1 != null)
                {
                    slot1.TryActivate(__instance);
                }
                if (Input.GetKeyDown(Datys_SkillsPlugin.ability2Key.Value.MainKey) && slot2 != null)
                {
                    slot2.TryActivate(__instance);
                }
            }
        }
    }
}
