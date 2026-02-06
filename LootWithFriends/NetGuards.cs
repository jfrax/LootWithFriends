using System;

namespace LootWithFriends
{
    public static class NetGuards
    {
        public static void ClientOnly(string methodName = null)
        {
            if (ConnectionManager.Instance.IsServer)
            {
                Log.Error($"{methodName ?? "Client-only method"} was run on SERVER");
                throw new InvalidOperationException("Client-only method invoked on server");
            }
        }

        public static void ServerOnly(string methodName = null)
        {
            if (!ConnectionManager.Instance.IsServer)
            {
                Log.Error($"{methodName ?? "Server-only method"} was run on CLIENT");
                throw new InvalidOperationException("Server-only method invoked on client");
            }
        }
    }
}