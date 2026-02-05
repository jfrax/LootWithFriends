using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace LootWithFriends
{
    public class LootWithFriendsModApi : IModApi
    {
        public void InitMod(Mod modInstance)
        {
            ModEvents.PlayerSpawnedInWorld.RegisterHandler(PlayerSpawnedInWorld);
            new Harmony(this.GetType().ToString()).PatchAll(Assembly.GetExecutingAssembly());
            Log.Error($"InitMod Done");
        }

        private void PlayerSpawnedInWorld(ref ModEvents.SPlayerSpawnedInWorldData data)
        {
            Affinity.PreFetchClientPlayerAffinity();
        }

        [HarmonyPatch(typeof(XUiC_ItemActionList), "SetCraftingActionList")]
        private static class AddLootAffinityAction_Patch
        {
            private static void Postfix(
                XUiC_ItemActionList __instance,
                XUiC_ItemActionList.ItemActionListTypes _actionListType,
                XUiController itemController
            )
            {
                // Only for actual items
                if (_actionListType != XUiC_ItemActionList.ItemActionListTypes.Item)
                    return;

                // Only item stacks
                if (!(itemController is XUiC_ItemStack stack))
                    return;

                if (!((XUiC_ItemStack)itemController).itemClass.CanStack())
                    return;
                
                // Optional filtering
                if (stack.ItemStack.IsEmpty())
                    return;

                var player = GameManager.Instance.myEntityPlayerLocal;
                Log.Out("About to call Affinity.GetAffinity");
                var aff = Affinity.GetAffinity(player, stack.itemClass.Name);
                
                Log.Out("Finished calling Affinity.GetAffinity");
                
                __instance.AddActionListEntry(
                    new ItemActionEntryLootAffinity(itemController, ItemActionEntryLootAffinity.GetTextForAffinity(aff))
                );

                Log.Out("About to RefreshActionList from AddLootAffinityAction_Patch");
                
                __instance.RefreshActionList();
            }
        }

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.SaveWorld))]
        public static class GameManager_SaveWorld_Patch
        {
            private static void Postfix()
            {
                Affinity.FlushToDisk();
            }
        }
        
        [HarmonyPatch(typeof(XUiC_BackpackWindow), nameof(XUiC_BackpackWindow.Init))]
        public static class BackpackWindow_Init_Patch
        {
            static void Postfix(XUiC_BackpackWindow __instance)
            {
                var btn = __instance.GetChildById("btnDropWithAffinities");
                if (btn == null)
                {
                    Log.Out("[LootWithFriends] btnDropWithAffinities not found");
                    return;
                }

                btn.OnPress += OnDropWithAffinitiesPressed;
                Log.Out("[LootWithFriends] Drop button wired");
            }

            private static void OnDropWithAffinitiesPressed(XUiController sender, int mouseButton)
            {
                var player = sender?.xui?.playerUI?.entityPlayer;
                if (player == null)
                    return;

                Log.Out("[LootWithFriends] Backpack drop button pressed");
                
                ItemDrop.PerformDrop(player);

                // LootWithFriendsDropper.DropUsingAffinities(player);
            }
        }

    }
}