using System;
using UniLinq;
using UnityEngine;

namespace LootWithFriends
{
    public class NetPackageClientRequestingAffinities : NetPackage
    {
        
        private int requestingPlayerEntityId;

        public NetPackage Setup(int playerEntityId)
        {
            Log.Out($"Requesting client affinities Setup for {playerEntityId}");
            requestingPlayerEntityId = playerEntityId;
            return this;
        }
        


        public override void ProcessPackage(World _world, GameManager _callbacks)
        {
            Log.Out("Server processing package NetPackageClientRequestingAffinities");
            Log.Out("The entity id being processed is: " + requestingPlayerEntityId);
            //now we (the server) reply to this client
            var player = GameManager.Instance.World.Players.list.FirstOrDefault(x => x.entityId == requestingPlayerEntityId);
            Log.Out("The player being processed is: " + player.PlayerDisplayName);
            var playerAffinities = Affinity.GetAffinitiesForPlayer(player);

            var pkg = NetPackageManager.GetPackage<NetPackageServerReplyClientAffinities>().Setup(playerAffinities);
            
            
            ConnectionManager.Instance.SendPackage(
                pkg,
                _onlyClientsAttachedToAnEntity: true,
                _attachedToEntityId: requestingPlayerEntityId
            );
            
        }

        public override void read(PooledBinaryReader reader)
        {
            Log.Out("Server reading NetPackageClientRequestingAffinities");
            requestingPlayerEntityId = reader.ReadInt32();
        }

        public override void write(PooledBinaryWriter writer)
        {
            Log.Out("Client Writing NetPackageClientRequestingAffinities");
            base.write(writer);
            Log.Out("Base write done");
            writer.Write(requestingPlayerEntityId);
            Log.Out("My write done");
        }

        public override int GetLength() => 8;


    }
}