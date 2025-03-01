using UnityEngine;
using BepInEx;


namespace Datys_Skills
{
    public class AbilityIconOverlay : MonoBehaviour
    {
        public Vector2 ability1Pos;
        public Vector2 ability2Pos;
        private bool dragging1 = false;
        private bool dragging2 = false;
        private Vector2 dragOffset1;
        private Vector2 dragOffset2;
        public bool showIcons = true; // zda zobrazovat ikony
        private float iconSize = 64f;
        
        void Start()
        {
            // Načti uložené pozice z konfigu
            ability1Pos = new Vector2(Datys_SkillsPlugin.ability1PosX.Value, Datys_SkillsPlugin.ability1PosY.Value);
            ability2Pos = new Vector2(Datys_SkillsPlugin.ability2PosX.Value, Datys_SkillsPlugin.ability2PosY.Value);
        }

        void OnGUI()
        {
            // Pokud není lokální hráč, nic nevykresluj
            if (Player.m_localPlayer == null) return;
            if (!showIcons) return;

            // Zjisti, zda držíme nějakou zbraň s abilitami
            Player player = Player.m_localPlayer;

// Získej položku z hlavní ruky (např. meč)
            ItemDrop.ItemData mainItem = player.GetCurrentWeapon();
// Získej off-hand položku (štít) – metoda GetEquippedShield musí být implementovaná (viz PlayerExtensions.cs)
            ItemDrop.ItemData offHandItem = player.GetEquippedShield();

// Pokud hráč nedrží ani jednu položku, ukonči vykreslování
            if (mainItem == null && offHandItem == null) return;

// Inicializuj proměnné pro sloty schopností
            Ability slot1 = null;
            Ability slot2 = null;

// Zpracuj hlavní ruku
            if (mainItem != null && mainItem.m_dropPrefab != null &&
                AbilityManager.weaponAbilities.TryGetValue(mainItem.m_dropPrefab.name, out var mainAbilities))
            {
                // Slot1 se bere z hlavní ruky (ability1)
                slot1 = mainAbilities.ability1;
    
                // Pokud hráč nemá off-hand položku, slot2 doplň z hlavní ruky (ability2)
                if (offHandItem == null)
                {
                    slot2 = mainAbilities.ability2;
                }
            }

// Zpracuj off-hand (štít)
            if (offHandItem != null && offHandItem.m_dropPrefab != null &&
                AbilityManager.weaponAbilities.TryGetValue(offHandItem.m_dropPrefab.name, out var offAbilities))
            {
                // Štít obvykle definuje schopnost ve slotu ability2, případně fallback na ability1, pokud ability2 není
                slot2 = offAbilities.ability2 ?? offAbilities.ability1;
            }

            // Definuj obdélníky ikon na základě aktuálních pozic
            Rect rect1 = new Rect(ability1Pos.x, ability1Pos.y, iconSize, iconSize);
            Rect rect2 = new Rect(ability2Pos.x, ability2Pos.y, iconSize, iconSize);

            // Detekce přetažení – funguje pouze pokud držíš Ctrl
            Event e = Event.current;
            bool ctrlHeld = e.control;

            // Pro Ability F
            if (ctrlHeld)
            {
                if (e.type == EventType.MouseDown && rect1.Contains(e.mousePosition))
                {
                    dragging1 = true;
                    dragOffset1 = e.mousePosition - ability1Pos;
                    e.Use();
                }
            }
            if (dragging1 && e.type == EventType.MouseDrag)
            {
                ability1Pos = e.mousePosition - dragOffset1;
                e.Use();
            }
            if (e.type == EventType.MouseUp && dragging1)
            {
                dragging1 = false;
                // Ulož novou pozici do konfigu
                Datys_SkillsPlugin.ability1PosX.Value = ability1Pos.x;
                Datys_SkillsPlugin.ability1PosY.Value = ability1Pos.y;
                // Možná chceš Config.Save() zavolat zde nebo nechat OnDestroy, který se zavolá při ukončení
                e.Use();
            }

            // Pro Ability G
            if (ctrlHeld)
            {
                if (e.type == EventType.MouseDown && rect2.Contains(e.mousePosition))
                {
                    dragging2 = true;
                    dragOffset2 = e.mousePosition - ability2Pos;
                    e.Use();
                }
            }
            if (dragging2 && e.type == EventType.MouseDrag)
            {
                ability2Pos = e.mousePosition - dragOffset2;
                e.Use();
            }
            if (e.type == EventType.MouseUp && dragging2)
            {
                dragging2 = false;
                Datys_SkillsPlugin.ability2PosX.Value = ability2Pos.x;
                Datys_SkillsPlugin.ability2PosY.Value = ability2Pos.y;
                e.Use();
            }

            // Vykresli ikony pomocí aktuálních pozic
            if (slot1 != null)
            {
                DrawAbilityIcon(slot1, rect1, Datys_SkillsPlugin.ability1Key.Value.MainKey.ToString());
            }
            if (slot2 != null)
            {
                DrawAbilityIcon(slot2, rect2, Datys_SkillsPlugin.ability2Key.Value.MainKey.ToString());
            }
        }

        private void DrawAbilityIcon(Ability ability, Rect rect, string keyLabel)
        {
            // Načti texturu podle jména
            Texture2D tex = LoadTexture(ability.IconName);
            if (tex == null) return;

            // Vykresli ikonu
            GUI.DrawTexture(rect, tex);

            // Vykresli název ability nad ikonou
            Rect nameRect = new Rect(rect.x, rect.y - 20, rect.width, 20);
            GUIStyle nameStyle = new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                normal = { textColor = Color.white }
            };
            GUI.Label(nameRect, ability.AbilityName, nameStyle);

            // Pokud je ability na cooldownu, vykresli overlay s odpočtem
            if (ability.IsOnCooldown)
            {
                float zbývá = (ability.LastUsedTime + ability.Cooldown) - Time.time;
                // Vykreslení progress baru uvnitř ikony:
                float percent = zbývá / ability.Cooldown;
                Rect progressRect = new Rect(rect.x, rect.y + rect.height * (1 - percent), rect.width, rect.height * percent);
    
                // Vytvoření pulzujícího efektu:
                float flashAlpha = Mathf.Lerp(0.3f, 0.7f, Mathf.PingPong(Time.time * 2f, 1f));
                GUI.color = new Color(0, 0, 0, flashAlpha);
                GUI.DrawTexture(progressRect, Texture2D.whiteTexture);
                GUI.color = Color.white;

                // Vykreslení textu s odpočtem:
                GUIStyle style = new GUIStyle
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 18,
                    normal = { textColor = Color.white }
                };
                GUI.Label(rect, $"{zbývá:F0}s", style);
            }

            // Vykresli label s klávesou
            GUIStyle keyStyle = new GUIStyle
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 14,
                normal = { textColor = Color.yellow }
            };
            GUI.Label(rect, keyLabel, keyStyle);
        }


        private Texture2D LoadTexture(string iconName)
        {
            // Tady načti texturu z disku nebo asset bundlu
            // Pro ukázku předpokládáme, že ikony jsou ve složce "BepInEx/plugins/icons"
            string path = System.IO.Path.Combine(Paths.PluginPath, "icons", iconName);
            if (!System.IO.File.Exists(path))
            {
                // Debug.LogError($"❌ Ikona {iconName} nebyla nalezena na cestě: {path}");
                return null;
            }
            byte[] data = System.IO.File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(data);
            return tex;
        }
    }
}
