using System;

namespace LootWithFriends
{
    public static class NetGuards
    {
        public static void ClientOnly(string methodName = null)
        {
            if (ConnectionManager.Instance.IsServer)
            {
                Log.Error($"[LootWithFriends] {methodName ?? "Client-only method"} was run on SERVER");
                throw new InvalidOperationException("[LootWithFriends] Client-only method invoked on server");
            }
        }

        public static void ServerOnly(string methodName = null)
        {
            if (!ConnectionManager.Instance.IsServer)
            {
                Log.Error($"[LootWithFriends] {methodName ?? "Server-only method"} was run on CLIENT");
                throw new InvalidOperationException("[LootWithFriends] Server-only method invoked on client");
            }
        }
    }
}