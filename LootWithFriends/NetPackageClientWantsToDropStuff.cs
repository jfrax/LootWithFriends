using System.Text;
using UniLinq;

namespace LootWithFriends
{
    public class NetPackageClientWantsToDropStuff : NetPackage
    {
        private int requestingPlayerEntityId;
        private string[] itemSlotNames;
        private int[] stackCounts;
        private PackedBoolArray lockedSlots;

        public NetPackage Setup(EntityPlayer player)
        {
            NetGuards.ClientOnly("NetPackageClientWantsToDropStuff.Setup");
            requestingPlayerEntityId = player.entityId;
            itemSlotNames = player.bag.items.Select(x => x.IsEmpty() ? string.Empty : x.itemValue.ItemClass.Name).ToArray();

            stackCounts = player.bag.items.Select(x => x.count).ToArray();
            lockedSlots = player.bag.LockedSlots;
            return this;
        }

        public override void write(PooledBinaryWriter writer)
        {
            base.write(writer);
            writer.Write(requestingPlayerEntityId);
            writer.Write(itemSlotNames.Length);

            for (int i = 0; i < itemSlotNames.Length; i++)
            {
                writer.Write(itemSlotNames[i]);
                writer.Write(stackCounts[i]);
            }

            writer.Write(lockedSlots?.Length ?? 0);
            if (lockedSlots != null && lockedSlots.Length > 0)
            {
                for (int i = 0; i < lockedSlots.Length; i++)
                {
                    writer.Write(lockedSlots[i]);
                }
            }
        }

        public override void read(PooledBinaryReader reader)
        {
            requestingPlayerEntityId = reader.ReadInt32();

            var slotCount = reader.ReadInt32();
            
            itemSlotNames = new string[slotCount];
            stackCounts = new  int[slotCount];
            lockedSlots = new  PackedBoolArray(slotCount);
            for (int i = 0; i < itemSlotNames.Length; i++)
            {
                itemSlotNames[i] = reader.ReadString();
                stackCounts[i] = reader.ReadInt32();
            }

            var lsLength = reader.ReadInt32();
            if (lsLength > 0)
            {
                for (int i = 0; i < lsLength; i++)
                {
                    lockedSlots[i] = reader.ReadBoolean();
                }
            }
        }

        public override void ProcessPackage(World world, GameManager callbacks)
        {
            NetGuards.ServerOnly("NetPackageClientWantsToDropStuff.ProcessPackage");
            var player =
                GameManager.Instance.World.Players.list.FirstOrDefault(x => x.entityId == requestingPlayerEntityId);

            if (player == null)
            {
                Log.Warning($"NetPackageClientWantsToDropStuff unable to find requestingPlayerEntityId {requestingPlayerEntityId}");
                return;
            }

            var (toDrop, itemsToPutInDroppedLootBag) = 
                ItemDrop.ServerWhatShouldBeDropped(new ItemDropRequestInfo(player, itemSlotNames, stackCounts, lockedSlots));
            
            if (toDrop.Any((x => x)))
            {
                //now actually drop the loot bag
                if (!ItemDrop.ServerTryDropLootBag(player, itemsToPutInDroppedLootBag.ToArray()))
                    return;

                //then tell the client which things to delete
                var pkg = NetPackageManager.GetPackage<NetPackageServerReplyItemsToDelete>().Setup(toDrop);

                ConnectionManager.Instance.SendPackage(
                    pkg,
                    _onlyClientsAttachedToAnEntity: true,
                    _attachedToEntityId: requestingPlayerEntityId
                );
            }
        }

        private ItemStack CreateItemStack(string className, int count)
        {
            // Get the ItemClass
            ItemClass itemClass = ItemClass.GetItemClass(className);
            if (itemClass == null)
            {
                Log.Error($"Invalid item class name: {className}");
                return null;
            }
            ItemValue itemValue = new ItemValue(itemClass.Id, false);
            Log.Out($"Creating ItemStack for {className}");
            ItemStack stack = new ItemStack(itemValue, count);
            Log.Out("Done Creating ItemStack");
            return stack;
        }

        public override int GetLength() => 16 
                                        + itemSlotNames.Select(x => Encoding.UTF8.GetByteCount(x)).Sum() 
                                        + stackCounts.Length * 4
                                        + (lockedSlots?.Length ?? 1) * 4;
    }
}