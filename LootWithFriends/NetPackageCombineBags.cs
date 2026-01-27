using System;

namespace LootWithFriends
{
    public class NetPackageCombineBags : NetPackage
    {
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
