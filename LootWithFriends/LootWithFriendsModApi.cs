using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace LootWithFriends
{
    public class LootWithFriendsModApi : IModApi
    {
        public void InitMod(Mod modInstance)
        {
            ModEvents.PlayerSpawnedInWorld.RegisterHandler(PlayerSpawnedInWorld);
            ModEvents.GameUpdate.RegisterHandler(OnGameUpdate);
            new Harmony(this.GetType().ToString()).PatchAll(Assembly.GetExecutingAssembly());
            Log.Error($"LootWithFriends: Mod Initialized");
        }

        private void PlayerSpawnedInWorld(ref ModEvents.SPlayerSpawnedInWorldData data)
        {
            Affinity.PreFetchPlayerAffinity();
            LootWaypointManager.LoadWaypoints();
        }
        
        private void OnGameUpdate(ref ModEvents.SGameUpdateData data)
        {
            LootWaypointManager.Update();
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
                LootWaypointManager.SaveWaypoints();
            }
        }
        
        [HarmonyPatch(typeof(GameManager), nameof(GameManager.OnApplicationQuit))]
        public static class GameManager_OnApplicationQuit_Patch
        {
            private static void Prefix()
            {
                if (!ConnectionManager.Instance.IsClient)
                    return;

                LootWaypointManager.SaveWaypoints();
            }
        }
        
        [HarmonyPatch(typeof(ConnectionManager), nameof(ConnectionManager.Disconnect))]
        public static class ConnectionManager_Disconnect_Patch
        {
            private static void Prefix()
            {
                if (!ConnectionManager.Instance.IsClient)
                    return;

                LootWaypointManager.SaveWaypoints();
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