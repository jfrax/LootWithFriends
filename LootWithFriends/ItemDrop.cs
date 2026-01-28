using UniLinq;
using System.Linq;
using System.Collections.Generic;

namespace LootWithFriends
{
    public static class ItemDrop
    {

        public static bool[] WhatShouldIDrop(int requestorPlayerEntityId)
        {
            EntityPlayer requestorPlayer = null;
            foreach (var player in GameManager.Instance.World.Players.list)
            {
                if (player.entityId == requestorPlayerEntityId)
                {
                    requestorPlayer = player;
                    break;
                }
            }
                
            if (requestorPlayer == null)
            {
                Log.Error($"Unable to find requestor player by entity id: {requestorPlayerEntityId} in WhatShouldIDrop");
                return new bool[]{};
            }
                
            // var nearestPlayer = FindNearestOtherPlayer(requestorPlayer);
            // if (nearestPlayer == null)
            // {
            //     Log.Warning($"No other players found nearby");
            //     return new bool[]{};
            // }

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
                    //item.itemValue.ItemClass.Name
                    ReflectionDumper.DumpObject(item.itemValue.ItemClass, "itemclass_dump.txt");
                    break;
                }
            }
            
            return dropInfo;
        }
        
        private static EntityPlayer FindNearestOtherPlayer(EntityPlayer self)
        {
            float bestDistSq = float.MaxValue;
            EntityPlayer best = null;

            var players = GameManager.Instance.World.Players.list;

            foreach (var player in players)
            {
                if (player == null || player.entityId == self.entityId)
                    continue;

                float distSq = (player.position - self.position).sqrMagnitude;

                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    best = player;
                }
            }

            return best;
        }
        
    }
}