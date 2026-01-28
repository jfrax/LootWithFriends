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
            //ModEvents.UnityUpdate.RegisterHandler(ItemDropHelper.ProcessPendingDrops);
            //ModEvents.PlayerSpawnedInWorld.RegisterHandler(PlayerSpawnedInWorld);
            new Harmony(this.GetType().ToString()).PatchAll(Assembly.GetExecutingAssembly());
            Log.Error($"InitMod Done");
        }

        // [HarmonyPatch(typeof(XUiC_ItemActionList), "Init")]
        // private static class XUiC_ItemActionList_Init_Patch
        // {
        //     private static void Postfix(XUiC_ItemActionList __instance)
        //     {
        //         Log.Error($"XUiC_ItemActionList_Init_Patch called");
        //         // var entry = new XUiC_ItemActionEntry();
        //         // entry.lblName.text = "HEY CHECK IT!!";
        //         // __instance.entryList.Add(entry);
        //     }
        // }

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

                // Optional filtering
                if (stack.ItemStack.IsEmpty())
                    return;

                __instance.AddActionListEntry(
                    new ItemActionEntryLootAffinity(itemController)
                );

                __instance.RefreshActionList();
            }
        }

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.SaveWorld))]
        public static class GameManager_SaveWorld_Patch
        {
            private static void Postfix()
            {
                Affinities.FlushToDisk();
            }
        }
    }
}