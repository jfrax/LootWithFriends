using System.Collections.Generic;
using UnityEngine;

namespace LootWithFriends
{
    public class ConsoleCmdCombineBags : ConsoleCmdAbstract
    {
        public override string[] getCommands()
        {
            return new[] { "combinebags", "cb" };
        }

        public override string getDescription()
        {
            return "Combine backpack items from one player into another.";
        }

        public override string GetHelp()
        {
            return
                "Usage:\n" +
                "cb (nearest player → you)";
        }

        public void DropLootBag(EntityPlayer player, ItemStack[] items)
        {
            Vector3 position = GetGroundPositionInFrontOfPlayer(player);

            foreach (EntityLootContainer container in GameManager.Instance.DropContentInLootContainerServer(
                         -1,
                         "DroppedVehicleContainer",
                         position,
                         items,
                         false,
                         null 
                     ))
            {
                container.SetVelocity(Vector3.zero);
            }
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

        public override void Execute(List<string> parameters, CommandSenderInfo senderInfo)
        {
            Log.Out($"IsServer: {ConnectionManager.Instance.IsServer}");

            EntityPlayer fromPlayer = ConnectionManager.Instance.IsServer
                ? GameManager.Instance.myEntityPlayerLocal
                : GameManager.Instance.World.Players.dict[senderInfo.RemoteClientInfo.entityId];

            Log.Out($"FromPlayer: {fromPlayer.entityId}");

            if (ConnectionManager.Instance.IsServer)
            {
                var stacksToDrop = new List<ItemStack>();

                //testing to make sure we can drop like this
                foreach (var stack in fromPlayer.bag.items)
                {
                    if (stack == null || stack.count <= 0)
                        continue;

                    int dropCount = stack.count;
                    stacksToDrop.Add(stack.Clone());
                    
                    fromPlayer.bag.DecItem(stack.itemValue, dropCount);
                }

                DropLootBag(fromPlayer, stacksToDrop.ToArray());

                //var toDrop = ItemDrop.WhatShouldIDrop(fromPlayer.entityId);
            }
            else
            {
                var pkg = new NetPackageClientWantsToDropStuff(senderInfo.RemoteClientInfo.entityId);
                ConnectionManager.Instance.SendToServer(pkg);
            }
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