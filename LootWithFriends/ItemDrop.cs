using UniLinq;
using System.Collections.Generic;
using UnityEngine;

namespace LootWithFriends
{
    public static class ItemDrop
    {

        public static void PerformDrop(EntityPlayer requestorPlayer)
        {
            if (requestorPlayer == null)
            {
                return;
            }
            
            if (ConnectionManager.Instance.IsServer)
            {
                var (toDrop, stacksToDrop) = ServerWhatShouldBeDropped(ItemDropRequestInfo.FromServerPlayer(requestorPlayer));
                
                if (toDrop.Any(x => x))
                {
                    if (!ServerTryDropLootBag(requestorPlayer, stacksToDrop.ToArray()))
                        return;
                    
                    DropItemsAtSlots(toDrop);
                }
            }
            else
            {
                var pkg = NetPackageManager.GetPackage<NetPackageClientWantsToDropStuff>().Setup(requestorPlayer);
                ConnectionManager.Instance.SendToServer(pkg);
            }
        }


        public static (bool[], ItemStack[]) ServerWhatShouldBeDropped(ItemDropRequestInfo itemDropRequestInfo)
        {
            NetGuards.ServerOnly(nameof(ServerWhatShouldBeDropped));
            var toDrop = new bool[itemDropRequestInfo.ItemSlotNames.Length];
            var itemsToPutInDroppedLootBag = new List<ItemStack>();

            var nearestPlayer = Utilities.FindNearestOtherPlayer(itemDropRequestInfo.RequestorPlayer);

            ItemStack CreateItemStack(string className, int count)
            {
                ItemClass itemClass = ItemClass.GetItemClass(className);
                if (itemClass == null)
                {
                    Log.Error($"Invalid item class name: {className}");
                    return null;
                }
                ItemValue itemValue = new ItemValue(itemClass.Id, false);
                ItemStack stack = new ItemStack(itemValue, count);
                return stack;
            }
            
            for (int i = 0; i < itemDropRequestInfo.ItemSlotNames.Length; i++)
            {
                if (itemDropRequestInfo.LockedSlots[i])
                    continue;
                
                var className = itemDropRequestInfo.ItemSlotNames[i];
                if (string.IsNullOrEmpty(className))
                    continue;
                
                var count = itemDropRequestInfo.StackCounts[i];
                if (count > 0)
                {
                    var newStack = CreateItemStack(className, count);
                    if (newStack == null)
                        continue;
                    toDrop[i] = Affinity.ShouldDropItemStack(itemDropRequestInfo.RequestorPlayer, nearestPlayer, newStack);
                    if (toDrop[i])
                    {
                        itemsToPutInDroppedLootBag.Add(newStack);
                    }
                }
            }

            return (toDrop, itemsToPutInDroppedLootBag.ToArray());

        }
        
        public static bool ServerTryDropLootBag(EntityPlayer playerDropping, ItemStack[] items)
        {
            NetGuards.ServerOnly(nameof(ServerTryDropLootBag));

            var dropPosition = playerDropping.GetDropPosition();

            foreach (EntityLootContainer container in GameManager.Instance.DropContentInLootContainerServer(
                         -1,
                         "DroppedLootContainer",
                         dropPosition,
                         items,
                         false,
                         null
                     ))
            {
                container.SetVelocity(Vector3.zero);
                
                //On the server, we will always create waypoint for the local player.
                LootWaypointManager.AddForLocalPlayer(container, playerDropping);

                var nearestPlayer = Utilities.FindNearestOtherPlayer(playerDropping);
                if (nearestPlayer != null)
                {
                    //also need to let the client know to create their own waypoint
                    var pkg = NetPackageManager.GetPackage<NetPackageServerSendClientWaypoint>().Setup(container, playerDropping);

                    ConnectionManager.Instance.SendPackage(
                        pkg,
                        _onlyClientsAttachedToAnEntity: true,
                        _attachedToEntityId: nearestPlayer.entityId
                    );
                }
            }

            return true;
        }

        public static void DropItemsAtSlots(bool[] itemsToDrop)
        {
            var localPlayer = GameManager.Instance.myEntityPlayerLocal;
            for (int i = 0; i < itemsToDrop.Length; i++)
            {
                if (itemsToDrop[i])
                {
                    Log.Out($"CLIENT IS DROPPING ITEM AT INDEX {i}");
                    localPlayer.bag.SetSlot(i, ItemStack.Empty.Clone());    
                }
            }
        }
    }
}