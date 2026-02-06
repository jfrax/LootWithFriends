using System.Collections.Generic;
using UnityEngine;

namespace LootWithFriends
{
    public static class Utilities
    {
        public static EntityPlayer FindNearestOtherPlayer(EntityPlayer self)
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
        
        public static void ShowDropFailureMessage(EntityPlayer player)
        {
            // server-side only
            if (!ConnectionManager.Instance.IsServer) return;

            // _senderEntityId = server entity? or player entity
            int senderEntityId = -1;

            // optional: use EnumGameMessages.Chat or PlainTextLocal (if you want only on local)
            EnumGameMessages messageType = EnumGameMessages.Chat;

            // localization key for clients
            string messageKey = "lwf.drop.no_space";
            
            
            

            // 7DTD doesn't take arbitrary strings for GameMessageServer; you send the key via chat
            GameManager.Instance.ChatMessageServer(
                null,                       // ClientInfo (null = send to all?)
                EChatType.Global,            // chat type, pick whatever makes sense
                senderEntityId,
                messageKey,                  // localization key
                new List<int> { player.entityId }, // recipients
                EMessageSender.Server
            );
        }

        
        public static bool TryGetDropPositionInFrontOfPlayer(
            EntityPlayer player,
            out Vector3 dropPos,
            float forward = 1.5f,
            int verticalSearch = 3)
        {
            dropPos = Vector3.zero;

            World world = player.world;

            Vector3 basePos = player.position + player.GetForwardVector() * forward;

            int x = Utils.Fastfloor(basePos.x);
            int z = Utils.Fastfloor(basePos.z);

            int playerY = Utils.Fastfloor(player.position.y);

            // Search a small vertical window around player height
            for (int y = playerY + verticalSearch; y >= playerY - verticalSearch; y--)
            {
                Vector3i below = new Vector3i(x, y - 1, z);
                Vector3i at = new Vector3i(x, y, z);
                Vector3i above = new Vector3i(x, y + 1, z);

                BlockValue belowBlock = world.GetBlock(below);
                BlockValue atBlock = world.GetBlock(at);
                BlockValue aboveBlock = world.GetBlock(above);

                // Require: solid floor + 2 blocks of air
                if (!belowBlock.isair && atBlock.isair && aboveBlock.isair)
                {
                    dropPos = new Vector3(
                        x + 0.5f,
                        y + 0.05f,
                        z + 0.5f
                    );
                    return true;
                }
            }

            // No valid spot found
            return false;
        }
    }
}