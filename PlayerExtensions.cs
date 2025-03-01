using System.Linq;
using UnityEngine;
using ItemDataManager;
using ItemManager;

namespace Datys_Skills
{
    public static class PlayerExtensions
    {
        public static ItemDrop.ItemData GetEquippedShield(this Player player)
        {
            // Získáme všechny předměty z inventáře
            var allItems = player.GetInventory().GetAllItems();

            // Projdeme je a najdeme ten, který je nasazený a je typu Shield
            foreach (global::ItemDrop.ItemData item in allItems)
            {
                if (item != null
                    && item.m_equipped
                    && item.m_shared.m_itemType == global::ItemDrop.ItemData.ItemType.Shield)
                {
                    return item;
                }
            }

            // Pokud nic nenajdeme, vrátíme null
            return null;
        }
        
    }
}