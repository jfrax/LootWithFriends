using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Audio;
using UnityEngine;

namespace LootWithFriends
{
    internal class BagOperations
    {
        public static void CombineBags(EntityPlayer fromPlayer, EntityPlayer toPlayer)
        {
            SdtdConsole.Instance.Output("About to move bags: " + fromPlayer.PlayerDisplayName + " => " + toPlayer.PlayerDisplayName);
            SendMaterialsSafely(fromPlayer, toPlayer);
        }


    /// <summary>
    /// Sends items from source bag to target bag, but only if target already has
    /// equal or more of that item type than the source.
    /// </summary>
    public static void SendMaterialsSafely(EntityPlayer fromPlayer, EntityPlayer toPlayer)
    {
        var sourceBag = fromPlayer.bag;
        var targetBag = toPlayer.bag;
        
        if (sourceBag == null || targetBag == null)
            return;

        var sourceItems = GroupItemsByType(sourceBag);
        var targetItems = GroupItemsByType(targetBag);

        foreach (var kvp in sourceItems)
        {
            int itemType = kvp.Key;
            var sourceStacks = kvp.Value;

            int sourceCount = sourceStacks.Sum(s => s.count);
            int targetCount = targetItems.TryGetValue(itemType, out var targetStacks)
                ? targetStacks.Sum(s => s.count)
                : 0;

            // Only send if target already has >=
            if (targetCount < sourceCount)
                continue;

            foreach (var stack in sourceStacks.ToArray())
            {
                if (stack == null || stack.count <= 0)
                    continue;
                
                int moveCount = stack.count;

                // 🔑 CLONE the stack
                ItemStack transfer = new ItemStack(stack.itemValue.Clone(), moveCount);

                // Add FIRST
                if (targetBag.AddItem(transfer))
                {
                    // Remove explicitly
                    sourceBag.DecItem(stack.itemValue, moveCount);
                }
            }
        }
    }
    
    private void DropItem(ItemStack stack, EntityPlayer player)
    {
        GameManager instance = GameManager.Instance;
        if ((bool) (UnityEngine.Object) instance)
        {
            instance.ItemDropServer(stack, player.GetDropPosition(), Vector3.zero, player.entityId, 60f, false);
            Manager.BroadcastPlay("itemdropped");
        }
        //this.xui.CollectedItemList?.RemoveItemStack(stack);
    }



    /// <summary>
    /// Helper: collects all non-empty stacks grouped by item ID
    /// </summary>
    private static Dictionary<int, List<ItemStack>> GroupItemsByType(Bag bag)
    {
        Dictionary<int, List<ItemStack>> dict = new Dictionary<int, List<ItemStack>>();

        foreach (var stack in bag.GetSlots())
        {
            if (stack == null || stack.count <= 0) continue;

            if (!dict.TryGetValue(stack.itemValue.type, out var list))
            {
                list = new List<ItemStack>();
                dict[stack.itemValue.type] = list;
            }

            list.Add(stack);
        }

        return dict;
    }
        
        
        
        /// <summary>
        /// /////////////////////////////////////
        /// </summary>
        /// <param name="fromPlayer"></param>
        /// <param name="toPlayer"></param>
        /// <param name="fromSlotIndex"></param>
        /// <param name="amount"></param>
        public static void MoveItemStack(EntityPlayer fromPlayer, EntityPlayer toPlayer, int fromSlotIndex, int amount)
        {
            Bag fromBag = fromPlayer.bag;
            Bag toBag = toPlayer.bag;

            ItemStack sourceStack = fromBag.GetSlots()[fromSlotIndex];

            if (sourceStack.IsEmpty())
                return;

            int moveAmount = Math.Min(amount, sourceStack.count);

            // Create a temp stack
            ItemStack movingStack = sourceStack.Clone();
            movingStack.count = moveAmount;

            // Try to add FIRST (no removal yet)
            bool added = toBag.AddItem(movingStack);

            if (!added)
            {
                // Could not add safely — abort
                return;
            }

            // Now remove from source
            sourceStack.count -= moveAmount;
            if (sourceStack.count <= 0)
                fromBag.GetSlots()[fromSlotIndex] = ItemStack.Empty;
        }

        public static void MoveAllOfItem(EntityPlayer fromPlayer, EntityPlayer toPlayer, string itemName)
        {
            Bag fromBag = fromPlayer.bag;

            for (int i = 0; i < fromBag.GetSlots().Length; i++)
            {
                ItemStack stack = fromBag.GetSlots()[i];

                if (stack.IsEmpty())
                    continue;

                if (stack.itemValue.ItemClass.GetItemName() != itemName)
                    continue;

                MoveItemStack(fromPlayer, toPlayer, i, stack.count);
            }
        }

    }
}
