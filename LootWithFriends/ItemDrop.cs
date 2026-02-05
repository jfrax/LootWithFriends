using System;
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
                var stacksToDrop = new List<ItemStack>();

                var toDrop = ItemDrop.ServerWhatShouldIDrop(requestorPlayer);

                for (int i = 0; i < requestorPlayer.bag.items.Length; i++)
                {
                    var stack = requestorPlayer.bag.items[i];
                    if (stack == null || stack.IsEmpty())
                        continue;

                    if (toDrop[i])
                    {
                        stacksToDrop.Add(stack.Clone());
                        if (requestorPlayer is EntityPlayerLocal)
                        {
                            //we are the server player; just drop our items directly
                            requestorPlayer.bag.SetSlot(i, ItemStack.Empty.Clone());
                        }
                    }
                }

                if (stacksToDrop.Count > 0)
                {
                    DropLootBag(requestorPlayer, stacksToDrop.ToArray());
                }
            }
            else
            {
                var pkg = NetPackageManager.GetPackage<NetPackageClientWantsToDropStuff>().Setup(requestorPlayer);
                ConnectionManager.Instance.SendToServer(pkg);
            }
        }

        private static bool[] ServerWhatShouldIDrop(EntityPlayer requestorPlayer)
        {
            if (requestorPlayer == null)
            {
                Log.Error($"Requestor player null in WhatShouldIDrop");
            }
                
            var nearestPlayer = Utilities.FindNearestOtherPlayer(requestorPlayer);

            //todo: in final version, if nearest player is null, prob don't want to proceed with the drop.
            //or it can be a config option ideally (could still be useful for solo play to quickly drop nonsense)
            
            var slotCount = requestorPlayer.bag.items.Length;
            var dropInfo = new bool[slotCount];
            
            for (int i = 0; i < slotCount; i++)
            {
                if(requestorPlayer.bag.LockedSlots != null && requestorPlayer.bag.LockedSlots[i])
                {
                    dropInfo[i] = false;
                    continue;
                }
                
                var item = requestorPlayer.bag.items[i];
                if (item.count > 0)
                {
                    Log.Out($"Server thinks player has {item.count} x {item.itemValue.ItemClass.Name} in slot [{i}]");
                    dropInfo[i] = Affinity.ShouldDropItemStack(requestorPlayer, nearestPlayer, item) ;
                }
            }
            
            return dropInfo;
        }
        
        public static void DropLootBag(EntityPlayer player, ItemStack[] items)
        {
            Vector3 position = GetGroundPositionInFrontOfPlayer(player);

            foreach (EntityLootContainer container in GameManager.Instance.DropContentInLootContainerServer(
                         -1,
                         "DroppedLootContainer",
                         position,
                         items,
                         false,
                         null
                     ))
            {
                container.SetVelocity(Vector3.zero);
                
                AddWayPoint(container, player, position);
            }
        }

        private static void AddWayPoint(EntityLootContainer container, EntityPlayer player, Vector3 position)
        {
            // var wp = new Waypoint()
            // {
            //     pos = container.pos,
            //     //icon = this.icon,
            //     name = AuthoredText.Clone(new AuthoredText(
            //     {
            //         Text = $"{player.name}'s Loot Drop"
            //     })),
            //     bTracked = this.bTracked,
            //     ownerId = this.ownerId,
            //     lastKnownPositionEntityId = this.lastKnownPositionEntityId,
            //     lastKnownPositionEntityType = this.lastKnownPositionEntityType,
            //     navObject = this.navObject,
            //     hiddenOnCompass = this.hiddenOnCompass,
            //     bIsAutoWaypoint = this.bIsAutoWaypoint,
            //     bUsingLocalizationId = this.bUsingLocalizationId,
            //     IsSaved = this.IsSaved,
            //     inviterEntityId = this.inviterEntityId,
            //     hiddenOnMap = this.hiddenOnMap
            // };
            //
            // player.Waypoints.Collection.Add(new Waypoint()
            // {
            //     
            // });
        }

        private static Vector3 GetGroundPositionInFrontOfPlayer(EntityPlayer player, float forward = 1.5f)
        {
            Vector3 pos = player.position + player.GetForwardVector() * forward;

            World world = player.world;

            int x = Utils.Fastfloor(pos.x);
            int z = Utils.Fastfloor(pos.z);

            // Get terrain height
            var y = world.GetHeightAt(x, z);

            // Walk upward until we find air above solid (handles snow, shapes, etc.)
            while (y < 255)
            {
                BlockValue block = world.GetBlock(new Vector3i(x, y, z));
                if (!block.isair)
                    y++;
                else
                    break;
            }

            return new Vector3(pos.x + 0.5f, y + 0.05f, pos.z + 0.5f);
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