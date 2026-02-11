using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LootWithFriends
{
    public static class Utilities
    {
        public static string ModSaveDir =>
            Path.Combine(ConnectionManager.Instance.IsServer ? GameIO.GetSaveGameDir() : GameIO.GetSaveGameLocalDir(),
                "Mods", "LootWithFriends");
        
        public static string ModInstallDir =>
            Path.Combine(GameIO.GetGameDir(string.Empty), "Mods", "LootWithFriends");

        public static string GetStablePlayerId(EntityPlayer player)
        {
            if (player == null)
                return "";

            var world = GameManager.Instance.World;
            var ppd = world.GetGameManager()
                .GetPersistentPlayerList()
                .GetPlayerDataFromEntityID(player.entityId);

            return ppd?.PlatformData.PrimaryId.CombinedString;
        }

        public static string GetStablePlayerId(int playerEntityId)
        {
            for (int i = 0; i < GameManager.Instance.World.Players.list.Count; i++)
            {
                if (GameManager.Instance.World.Players.list[i].entityId == playerEntityId)
                {
                    return GetStablePlayerId(GameManager.Instance.World.Players.list[i]);
                }
            }
            return null;
        }

        public static EntityPlayer FindPlayerByStableId(string stableId)
        {
            foreach (var p in GameManager.Instance.World.Players.list)
            {
                if (GetStablePlayerId(p) == stableId)
                    return p;
            }

            return null;
        }

        public static bool LocalPlayerExists()
        {
            return GameManager.Instance.myEntityPlayerLocal != null;
        }


        public static EntityPlayer FindNearestAlly(EntityPlayer self)
        {
            EntityPlayer best = null;
            
            if (!self.IsInParty())
                return null;
            
            float bestDistSq = float.MaxValue;

            foreach (var player in GameManager.Instance.World.Players.list)
            {
                if (player == null || player.entityId == self.entityId)
                    continue;

                if (!player.IsInParty())
                    continue;
                
                if (player.Party.PartyID != self.party.PartyID)
                    continue;
                
                var distSq = (player.position - self.position).sqrMagnitude;

                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    best = player;
                }
            }

            return best;
        }

        public static EntityPlayer FindNearestPlayer(Vector3i position)
        {
            World world = GameManager.Instance.World;
            if (world == null)
                return null;

            Vector3 targetPos = position.ToVector3();

            EntityPlayer closestPlayer = null;
            float closestSqrDistance = float.MaxValue;

            // This returns *all* players (local + remote)
            List<EntityPlayer> players = world.Players.list;

            foreach (EntityPlayer player in players)
            {
                if (player == null || player.IsDead())
                    continue;

                float sqrDist = (player.position - targetPos).sqrMagnitude;

                if (sqrDist < closestSqrDistance)
                {
                    closestSqrDistance = sqrDist;
                    closestPlayer = player;
                }
            }

            return closestPlayer;
        }
    }
}