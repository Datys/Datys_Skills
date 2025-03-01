using UnityEngine;
using System.Collections;

namespace Datys_Skills
{
    public abstract class Ability
    {
        public string AbilityName { get; set; } = "Unnamed Ability";
        public float Cooldown { get; set; } = 10f;
        public float LastUsedTime { get; set; } = -999f;
        public float EitrCost { get; set; } = 10f;
        
        // Cesta k ikoně (PNG), abychom ji mohli načíst a zobrazit v UI
        public string IconName { get; set; } = "default_icon.png";

        // Je abilita na cooldownu?
        public bool IsOnCooldown => Time.time < LastUsedTime + Cooldown;

        // Zda je abilita připravená k použití
        public bool CanActivate(Player player)
        {
            // Kontrola Eitru
            if (player.GetEitr() < EitrCost)
            {
                Debug.Log($"❌ {AbilityName}: Nedostatek Eitru!");
                return false;
            }

            // Kontrola cooldownu
            if (IsOnCooldown)
            {
                float zbývá = (LastUsedTime + Cooldown) - Time.time;
                Debug.Log($"❌ {AbilityName}: ještě {zbývá:F1}s cooldown!");
                return false;
            }

            return true;
        }

        // Spuštění ability (obsluha cooldownu, Eitru, coroutine)
        public void TryActivate(Player player)
        {
            if (!CanActivate(player)) return;

            // Odečti Eitr a nastav cooldown
            player.AddEitr(-EitrCost);
            LastUsedTime = Time.time;

            // Spusť logiku ability
            player.StartCoroutine(Activate(player));
        }

        // Samotná logika ability - v potomcích bude konkrétní implementace
        public abstract IEnumerator Activate(Player player);
    }
}