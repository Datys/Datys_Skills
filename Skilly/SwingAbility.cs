using System.Collections;
using UnityEngine;
using System.Collections.Generic;

namespace Datys_Skills
{
    public class SwingAbility : Ability
    {
        public SwingAbility()
        {
            AbilityName = "Swing";
            Cooldown = 15f;
            EitrCost = 20f;
            IconName = "Swing.png";
        }

        public override IEnumerator Activate(Player player)
        {
            if (player == null)
            {
                Debug.LogError("❌ Hráč není platný!");
                yield break;
            }
            
            Datys_SkillsPlugin.AbilityActive = true;

            Debug.Log($"▶ {AbilityName} spuštěna!");

            // 1) Spuštění animace
            player.m_zanim.SetTrigger("atgeir_secondary");

            // 2) Počkej na dokončení animace
            yield return new WaitForSeconds(0.5f);

            // 3) Spusť SFX efekt
            var sfxPrefab = ZNetScene.instance.GetPrefab("D_sfx_Start_Ability");
            if (sfxPrefab)
            {
                EffectList sfxEffect = new EffectList
                {
                    m_effectPrefabs = new EffectList.EffectData[]
                    {
                        new EffectList.EffectData { m_prefab = sfxPrefab, m_enabled = true }
                    }
                };
                sfxEffect.Create(player.transform.position, Quaternion.identity, player.transform, 1f);
                Debug.Log("🔊 Zvukový efekt aktivován!");
            }
            else
            {
                Debug.LogError("❌ Zvukový efekt D_sfx_Start_Ability nenalezen!");
            }

            // 4) Výpočet poškození podle zbraně
            float swingDamage = CalculateWeaponDamage(player);
            Debug.Log($"⚔️ Spočítané poškození: {swingDamage}");

            // 5) Aplikace poškození v okruhu (damage se aplikuje např. v poloměru 3 metrů)
            DealSwingDamage(player, 4f, swingDamage);

            Debug.Log($"✅ {AbilityName}: dokončena.");
            
            Datys_SkillsPlugin.AbilityActive = false;
            yield break;
        }

        private float CalculateWeaponDamage(Player player)
        {
            ItemDrop.ItemData weapon = player.GetCurrentWeapon();
            if (weapon != null)
            {
                float totalDamage = weapon.m_shared.m_damages.m_blunt +
                                    weapon.m_shared.m_damages.m_slash +
                                    weapon.m_shared.m_damages.m_pierce;
                return totalDamage;
            }
            Debug.LogWarning("⚠️ Hráč nemá vybavenou zbraň! Poškození nastaveno na 0.");
            return 0f;
        }
        
        private bool IsTamed(Character character)
        {
            return character.m_tamed;
        }

        private void DealSwingDamage(Player player, float radius, float damage)
        {
            Debug.Log($"⚔️ Spouštím DealSwingDamage s damage: {damage} v okruhu {radius}");

            List<Character> allCharacters = new List<Character>();
            Character.GetCharactersInRange(player.transform.position, radius, allCharacters);

            foreach (Character character in allCharacters)
            {
                if (character == null || character == player || character.IsPlayer() || IsTamed(character))
                {
                    Debug.Log($"🚫 Přeskakuji {character?.m_name ?? "NULL"} (hráč nebo neplatný cíl)");
                    continue;
                }

                float distance = Vector3.Distance(character.transform.position, player.transform.position);
                Debug.Log($"🎯 Cíl: {character.m_name}, Vzdálenost: {distance}");

                if (distance <= radius)
                {
                    HitData hitData = new HitData { m_damage = new HitData.DamageTypes() };
                    hitData.m_damage.m_blunt = damage;
                    hitData.m_skillLevel = 0f;
                    hitData.m_point = character.transform.position;
                    hitData.m_dir = (character.transform.position - player.transform.position).normalized;
                    hitData.m_pushForce = 20f;

                    Debug.Log($"💥 Aplikuji poškození {damage} na {character.m_name}");
                    character.Damage(hitData);
                }
                else
                {
                    Debug.Log($"❌ {character.m_name} je mimo dosah swing!");
                }
                
                
            }
        }
    }
}
