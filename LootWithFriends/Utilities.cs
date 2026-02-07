using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LootWithFriends
{
    public static class Utilities
    {
        
        public static string ModSaveDir =>
            Path.Combine(ConnectionManager.Instance.IsServer ? GameIO.GetSaveGameDir() : GameIO.GetSaveGameLocalDir(), "Mods", "LootWithFriends");

        public static string GetStablePlayerId(EntityPlayer player)
        {
            var world = GameManager.Instance.World;
            var ppd = world.GetGameManager()
                .GetPersistentPlayerList()
                .GetPlayerDataFromEntityID(player.entityId);

            return ppd?.PlatformData.PrimaryId.CombinedString;
        }

        
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
    }
}