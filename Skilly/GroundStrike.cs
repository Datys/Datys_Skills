using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Datys_Skills
{
    public class GroundStrikeAbility : Ability
    {
        public GroundStrikeAbility()
        {
            AbilityName = "Ground Strike";
            Cooldown = 15f;
            EitrCost = 20f;
            IconName = "GroundStrike_icon.png";  // Uprav dle názvu souboru s ikonou
        }

        public override IEnumerator Activate(Player player)
        {
            if (player == null)
            {
                Debug.LogError("❌ Hráč není platný!");
                yield break;
            }
            
            // Kontrola, zda je hráč na zemi
            if (!IsPlayerGrounded(player))
            {
                Debug.Log("⚠️ Ability se nedá spustit ve vzduchu!");
                yield break;
            }
            
            Datys_SkillsPlugin.AbilityActive = true;
            Debug.Log($"▶ {AbilityName} spuštěna!");

            // Přidej status efekt "D_Ability_Start" pomocí status effect manageru hráče
            player.GetSEMan().AddStatusEffect("D_Ability_Start".GetStableHashCode());
            Debug.Log("🛡️ Status effect D_Ability_Start přidán hráči!");

            // Spusť animaci "swing_sledge"
            player.m_zanim.SetTrigger("swing_sledge");
            Debug.Log("▶ Animace swing_sledge spuštěna!");

            // Počkej 0.3 s před spuštěním SFX efektu 
            yield return new WaitForSeconds(0.3f);

            // Spusť VFX efekt D_VFX_Attack_Ground v směru, kam hráč kouká
            GameObject vfxPrefab = ZNetScene.instance.GetPrefab("D_VFX_Attack_Ground");
            if (vfxPrefab)
            {
                EffectList vfxEffect = new EffectList
                {
                    m_effectPrefabs = new EffectList.EffectData[]
                    {
                        new EffectList.EffectData { m_prefab = vfxPrefab, m_enabled = true }
                    }
                };
                Quaternion effectRotation = Quaternion.LookRotation(player.transform.forward, Vector3.up);
                vfxEffect.Create(player.transform.position, effectRotation, null, 1f);
                Debug.Log("🔊 VFX efekt D_VFX_Attack_Ground aktivován v směru, kam hráč kouká!");
            }
            else
            {
                Debug.LogError("❌ VFX efekt D_VFX_Attack_Ground nenalezen!");
            }

            // Počkej dalších 0.7 s před spuštěním damage 
            yield return new WaitForSeconds(0.7f);

            // Výpočet poškození dle vybavené zbraně
            float strikeDamage = CalculateWeaponDamage(player);
            Debug.Log($"⚔️ Spočítané poškození: {strikeDamage}");

            // Zjisti směr útoku (pouze horizontální složka)
            Vector3 strikeDirection = player.transform.forward;
            strikeDirection.y = 0;
            strikeDirection.Normalize();

            // Uchovávej již zasažené nepřátele, aby nedošlo k opakovanému damage
            HashSet<Character> hitEnemies = new HashSet<Character>();

            // Definuj rozsah útoku: například od 1 m do 10 m, každý 1 m se damage aplikuje okamžitě
            float totalDistance = 10f;
            float intervalDistance = 1f;

            for (float d = intervalDistance; d <= totalDistance; d += intervalDistance)
            {
                // Výpočet bodu zásahu – využíváme aktuální pozici hráče
                Vector3 strikePoint = player.transform.position + strikeDirection * d;
                Debug.Log($"Strike point pro {d}m: {strikePoint}");

                // Detekuj nepřátele v okruhu kolem strikePoint
                float radius = 1f;
                List<Character> allCharacters = new List<Character>();
                Character.GetCharactersInRange(strikePoint, radius, allCharacters);

                foreach (Character character in allCharacters)
                {
                    if (character == null || character == player || character.IsPlayer() || hitEnemies.Contains(character))
                        continue;

                    HitData hitData = new HitData { m_damage = new HitData.DamageTypes() };
                    hitData.m_damage.m_blunt = strikeDamage;
                    hitData.m_skillLevel = 0f;
                    hitData.m_point = character.transform.position;
                    hitData.m_dir = (character.transform.position - player.transform.position).normalized;
                    hitData.m_pushForce = 20f;

                    character.Damage(hitData);
                    hitEnemies.Add(character);
                    Debug.Log($"💥 {character.m_name} zasažen Ground Strike poškozením {strikeDamage} na pozici {strikePoint}");
                }
            }

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

        // Metoda pro kontrolu, zda je hráč na zemi
        private bool IsPlayerGrounded(Player player)
        {
            Vector3 origin = player.transform.position + Vector3.up * 0.1f;
            return Physics.Raycast(origin, Vector3.down, 0.2f, LayerMask.GetMask("Default", "static_solid", "piece", "terrain"));
        }
    }
}
