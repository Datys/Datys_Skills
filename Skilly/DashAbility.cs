using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Datys_Skills
{
    public class DashAbility : Ability
    {
        public DashAbility()
        {
            AbilityName = "Dash";
            Cooldown = 15f;
            EitrCost = 20f;
            IconName = "Dash.png";
        }
        
        private bool IsTamed(Character character)
        {
            // Předpokládáme, že postava má booleanovou vlastnost m_tamed.
            // Pokud tvůj systém používá jinou logiku, uprav tuto metodu.
            return character.m_tamed;
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

            // Spusť animaci dash
            player.m_zanim.SetTrigger("greatsword_secondary");

            // Počkej 1 s na zahájení dash efektu
            yield return new WaitForSeconds(0.8f);

            // Spusť SFX efekt
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

            // Výpočet damage dashu
            float dashDamage = CalculateWeaponDamage(player);
            Debug.Log($"⚔️ Spočítané poškození: {dashDamage}");

            // Dash: posun vpřed
            Vector3 dashDirection = player.transform.forward;
            dashDirection.y = 0;
            dashDirection.Normalize();

            float dashDistance = 10f; // nastav dle potřeby
            float dashTime = 0.08f;
            float elapsed = 0f;
            HashSet<Character> hitEnemies = new HashSet<Character>();

            while (elapsed < dashTime)
            {
                float step = (dashDistance / dashTime) * Time.deltaTime;
                player.transform.position += dashDirection * step;

                // Aplikuj damage jednou pro každého nepřítele, kterého trefíš
                float radius = 1f;
                List<Character> allCharacters = new List<Character>();
                Character.GetCharactersInRange(player.transform.position, radius, allCharacters);

                foreach (Character character in allCharacters)
                {
                    if (character == null || character == player || character.IsPlayer() || IsTamed(character) || hitEnemies.Contains(character))
                        continue;


                    if (Vector3.Distance(character.transform.position, player.transform.position) <= radius)
                    {
                        HitData hitData = new HitData { m_damage = new HitData.DamageTypes() };
                        hitData.m_damage.m_blunt = dashDamage;
                        hitData.m_skillLevel = 0f;
                        hitData.m_point = character.transform.position;
                        hitData.m_dir = (character.transform.position - player.transform.position).normalized;
                        hitData.m_pushForce = 20f;

                        character.Damage(hitData);
                        hitEnemies.Add(character);
                        Debug.Log($"💥 {character.m_name} zasažen dash poškozením {dashDamage}");
                    }
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            EnsurePlayerLandsProperly(player);

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

        private void EnsurePlayerLandsProperly(Player player)
        {
            Vector3 finalPosition = player.transform.position;
            RaycastHit hit;
            if (Physics.Raycast(finalPosition + Vector3.up, Vector3.down, out hit, 20f,
                LayerMask.GetMask("Default", "static_solid", "piece", "terrain")))
            {
                finalPosition.y = hit.point.y;
                Debug.Log($"✅ Raycast úspěšný! Hráčova nová pozice: {finalPosition}");
            }
            else
            {
                Debug.LogWarning("⚠️ Raycast nenašel podlahu! Hráčova pozice zůstává stejná.");
            }
            player.transform.position = finalPosition;
        }
    }
}
