using System;

namespace LootWithFriends
{
    public class NetPackageClientWantsToDropStuff : NetPackage
    {
        private int requestingPlayerEntityId;

        public NetPackageClientWantsToDropStuff(int clientEntityId)
        {
            requestingPlayerEntityId = clientEntityId;
        }

        public override void read(PooledBinaryReader reader)
        {
            requestingPlayerEntityId = reader.ReadInt32();
        }

        public override void write(PooledBinaryWriter writer)
        {
            // Cast the original writer to our compat class
            if (!(writer is PooledBinaryWriterCompat compat))
            {
                throw new Exception("Writer must be PooledBinaryWriterCompat");
            }

            compat.WriteIntCompat(requestingPlayerEntityId);
        }

        public override void ProcessPackage(World world, GameManager callbacks)
        {
            if (!ConnectionManager.Instance.IsServer)
            {
                Log.Warning("NetPackageClientWantsToDropStuff was being processed on an instance that wasn't the server");
                return;
            }
            
            
            
        }

        public override int GetLength()
        {
            // 1 ints = 4 bytes
            return 4;
        }
    }
}