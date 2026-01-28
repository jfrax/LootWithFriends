using System;

namespace LootWithFriends
{
    public class NetPackageCombineBags : NetPackage
    {
        
        /*
         * i want to drop stuff (msg to server w client player entity id)
         * if already on server, just perform logic directly - DropItemsForNearbyPlayer, otherwise send netpackage request, "NetPackageIWantToDropStuff"
         *
         * on server:
         * find player with entity id sent
         * lookup that player's affinity FROM THE SERVER (server will hold world-scoped configuration for all players' affinities. We want different builds in different playthroughs etc)
         *
         * between the affinity and the server's own inspection of the requesting / nearest players' inventories, figure out which item TYPES should be dropped by the requesting player
         * 
         * 
         * send a net package, "NetPackagePleaseDropThese"
         * 1st int is player id
         * 2nd int is number of item types that will follow
         * rest of ints are item types
         * (or maybe just serialized string or something)
         *
         * if player id matches current player id, then drop all stacks of that item type
         * 
         * 
         * 
         */



        public static void DropItemsForNearbyPlayer(int otherPlayerEntityId)
        {
            //look up affinities for both players etc
            
            
        }
        
        private int fromEntityId;
        private int toEntityId;

        public NetPackageCombineBags(int fromEntityId, int toEntityId)
        {
            this.fromEntityId = fromEntityId;
            this.toEntityId = toEntityId;
        }

        public override void ProcessPackage(World world, GameManager gameManager)
        {
            if (!ConnectionManager.Instance.IsServer)
                return;
            
            EntityPlayer fromPlayer = world.GetEntity(fromEntityId) as EntityPlayer;
            EntityPlayer toPlayer = world.GetEntity(toEntityId) as EntityPlayer;

            Log.Warning($"We are in NetPackageCombineBags.ProcessPackage. From {fromPlayer?.PlayerDisplayName} To {toPlayer?.PlayerDisplayName} ");
            
            if (fromPlayer == null || toPlayer == null)
                return;

            BagOperations.CombineBags(fromPlayer, toPlayer);
        }

        public override void read(PooledBinaryReader reader)
        {
            fromEntityId = reader.ReadInt32();
            toEntityId = reader.ReadInt32();
        }

        public override void write(PooledBinaryWriter writer)
        {
            
            // Cast the original writer to our compat class
            if (!(writer is PooledBinaryWriterCompat compat))
            {
                throw new Exception("Writer must be PooledBinaryWriterCompat");
            }

            compat.WriteIntCompat(fromEntityId);
            compat.WriteIntCompat(toEntityId);
        }

        public override int GetLength()
        {
            // 2 ints = 8 bytes
            return 8;
        }
    }
}
