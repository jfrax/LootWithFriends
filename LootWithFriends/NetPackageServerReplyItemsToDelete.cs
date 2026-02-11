namespace LootWithFriends
{
    public class NetPackageServerReplyItemsToDelete : NetPackage
    {
        private bool[] itemsToDrop;

        public NetPackage Setup(bool[] toDrop)
        {
            NetGuards.ServerOnly("NetPackageServerReplyItemsToDelete.Setup");
            itemsToDrop = toDrop;
            return this;
        }

        public override void write(PooledBinaryWriter writer)
        {
            base.write(writer);
            writer.Write(itemsToDrop.Length);
            foreach (bool item in itemsToDrop)
            {
                writer.Write(item);
            }
        }

        public override void read(PooledBinaryReader reader)
        {
            var slotLength = reader.ReadInt32();
            itemsToDrop = new bool[slotLength];
            for (int i = 0; i < slotLength; i++)
            {
                itemsToDrop[i] = reader.ReadBoolean();
            }
        }

        public override void ProcessPackage(World world, GameManager callbacks)
        {
            NetGuards.ClientOnly("NetPackageServerReplyItemsToDelete.ProcessPackage");
            ItemDrop.DropItemsAtSlots(itemsToDrop);
        }

        public override int GetLength() => 4 + itemsToDrop.Length;

    }
}