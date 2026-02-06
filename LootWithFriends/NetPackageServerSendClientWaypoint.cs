using System.Collections;
using UnityEngine;

namespace LootWithFriends
{
    public class NetPackageServerSendClientWaypoint : NetPackage
    {
        private int containerEntityId;
        private int droppingPlayerEntityId;

        public NetPackage Setup(EntityLootContainer container, EntityPlayer droppingPlayer)
        {
            containerEntityId = container.entityId;
            droppingPlayerEntityId = droppingPlayer.entityId;
            return this;
        }

        public override void write(PooledBinaryWriter writer)
        {
            base.write(writer);
            writer.Write(containerEntityId);
            writer.Write(droppingPlayerEntityId);
        }

        public override void read(PooledBinaryReader reader)
        {
            containerEntityId = reader.ReadInt32();
            droppingPlayerEntityId = reader.ReadInt32();
        }

        public override void ProcessPackage(World world, GameManager callbacks)
        {
            // CLIENT ONLY
            if (!ConnectionManager.Instance.IsClient)
                return;

            var container = world.GetEntity(containerEntityId) as EntityLootContainer;
            if (container == null)
            {
                // entity may not have arrived yet — see below
                ScheduleRetry(containerEntityId, droppingPlayerEntityId);
                return;
            }

            
            var player = world.GetEntity(droppingPlayerEntityId) as EntityPlayer;
            LootWaypointManager.AddForLocalPlayer(container, player?.PlayerDisplayName ?? "A Friend");
        }

        public override int GetLength() => 8;
        
        private static void ScheduleRetry(int containerId, int playerId)
        {
            GameManager.Instance.StartCoroutine(RetryFind(containerId, playerId));
        }

        private static IEnumerator RetryFind(int containerId, int playerId)
        {
            for (int i = 0; i < 20; i++) // ~2 seconds
            {
                yield return new WaitForSeconds(0.1f);

                var world = GameManager.Instance.World;
                var container = world.GetEntity(containerId) as EntityLootContainer;
                var player = world.GetEntity(playerId) as EntityPlayer;

                if (container != null && player != null)
                {
                    LootWaypointManager.AddForLocalPlayer(container, player.PlayerDisplayName);
                    yield break;
                }
            }

            Log.Warning($"Failed to resolve loot container {containerId}");
        }

    }
}