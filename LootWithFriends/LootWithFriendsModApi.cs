using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace LootWithFriends
{
    public class LootWithFriendsModApi : IModApi
    {
        public void InitMod(Mod modInstance)
        {
            ModEvents.GameStartDone.RegisterHandler(GameStartDone);
            ModEvents.PlayerSpawnedInWorld.RegisterHandler(PlayerSpawnedInWorld);
            new Harmony(this.GetType().ToString()).PatchAll(Assembly.GetExecutingAssembly());
            Log.Out($"LootWithFriends: Mod Initialized");
            
        }

        private void GameStartDone(ref ModEvents.SGameStartDoneData data)
        {
            if (ConnectionManager.Instance.IsServer)
            {
                Affinity.PreFetchPlayerAffinity();
                Waypoints.LoadWaypoints();
            }
        }

        private void PlayerSpawnedInWorld(ref ModEvents.SPlayerSpawnedInWorldData data)
        {
            Affinity.PreFetchPlayerAffinity();

            if (ConnectionManager.Instance.IsServer)
            {
                var playerThatSpawned = Utilities.FindNearestPlayer(data.Position);
                if (playerThatSpawned != null)
                {
                    Waypoints.FetchPlayerWaypoints(playerThatSpawned);    
                }
                else
                {
                    Log.Warning("Unable to locate player by coordinates in PlayerSpawnedInWorld!");
                }
            }
                
        }

        [HarmonyPatch(typeof(EntityLootContainer), "removeBackpack")]
        private static class EntityLootContainer_RemoveBackpack_Patch
        {
            private static void Postfix(EntityLootContainer __instance)
            {
                if (!ConnectionManager.Instance.IsServer)
                    return;
                
                Waypoints.LootContainerRemoved(__instance);
            }
        }
        
        [HarmonyPatch(typeof(Entity), "OnAddedToWorld")]
        private static class Entity_OnAddedToWorld_Patch
        {
            private static void Postfix(Entity __instance)
            {
                //Clients need to check, when an entityitem gets created on their side, if they should update their positional waypoints into waypoints on the container itself
                if (__instance is EntityLootContainer lootContainer)
                {
                    Log.Out("Entity_OnAddedToWorld_Patch - EntityLootContainer added to world!");
                    Waypoints.UpdateWaypointWithBagReference(lootContainer);
                }
            }
        }

        [HarmonyPatch(typeof(Entity), "OnEntityUnload")]
        private static class Entity_OnEntityUnload_Patch
        {
            private static void Postfix(Entity __instance)
            {
                if (__instance is EntityLootContainer container)
                {
                    Log.Out("Entity_OnEntityUnload_Patch - EntityLootContainer unloaded!");
                    Waypoints.UpdateWaypointWithCoordinateReference(container);
                }
            }
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
                Waypoints.SaveWaypoints();
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