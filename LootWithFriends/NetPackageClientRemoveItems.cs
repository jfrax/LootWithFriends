using System.Collections.Generic;

namespace LootWithFriends
{
    public class NetPackageClientRemoveItems : NetPackage
    {
        private bool[] slotsToDrop;

        public NetPackage Setup(bool[] _slotsToDrop)
        {
            slotsToDrop = _slotsToDrop;
            return this;
        }

        public override void write(PooledBinaryWriter writer)
        {
            base.write(writer);
            writer.Write((ushort)slotsToDrop.Length);
            foreach (var slot in slotsToDrop)
            {
                writer.Write(slot);
            }
        }

        public override void read(PooledBinaryReader reader)
        {
            int length = reader.ReadUInt16();
            slotsToDrop =  new bool[length];
            for (int i = 0; i < length; i++)
                slotsToDrop[i] = reader.ReadBoolean();
        }

        public override void ProcessPackage(World _world, GameManager _callbacks)
        {
            for (int i = 0; i < slotsToDrop.Length; i++)
            {
                if (slotsToDrop[i])
                    GameManager.Instance.myEntityPlayerLocal.bag.SetSlot(i, ItemStack.Empty.Clone());    
            }
        }

        public override int GetLength() => slotsToDrop.Length + 8;
        
    }

}