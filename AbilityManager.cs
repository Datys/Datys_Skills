using System.Collections.Generic;
using UnityEngine;

namespace Datys_Skills
{
    public static class AbilityManager
    {
        // Klíč: jméno zbraně (prefabu), Hodnota: (ability1, ability2)
        public static Dictionary<string, (Ability ability1, Ability ability2)> weaponAbilities 
            = new Dictionary<string, (Ability, Ability)>();

        // Metoda pro inicializaci
        public static void Init()
        {
            // Pro meč "D_Sword_Baldur" přiřaď dvě ability:
            weaponAbilities["D_Sword_Baldur"] = (new SwingAbility(), new DashAbility());
            weaponAbilities["D_Sword_Snake"] = (new SwingAbility(), null);
            
            // Pro meč "SwordIron": nastavíme ability1 na Swing, ability2 necháme null.
            weaponAbilities["SwordIron"] = (new SwingAbility(), null);
    
            // Pro štít "ShieldBanded": nastavíme ability1 na null a ability2 na Dash.
            weaponAbilities["ShieldBanded"] = (null, new DashAbility());
        }
        
        
    }
}