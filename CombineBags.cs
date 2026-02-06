using System;

public class CombineBags : NetPackage
{
    public int fromEntityId;
    public int toEntityId;

    public override void ProcessPackage(World world, GameManager.Callbacks callbacks)
    {
        if (!ConnectionManager.Instance.IsServer)
            return;

        EntityPlayer fromPlayer = world.GetEntity(fromEntityId) as EntityPlayer;
        EntityPlayer toPlayer = world.GetEntity(toEntityId) as EntityPlayer;

        if (fromPlayer == null || toPlayer == null)
            return;

        // SAFE server-side call
        CombineBags(fromPlayer, toPlayer);
    }

    public override void read(PooledBinaryReader br)
    {
        fromEntityId = br.ReadInt32();
        toEntityId = br.ReadInt32();
    }

    public override void write(PooledBinaryWriter bw)
    {
        bw.Write(fromEntityId);
        bw.Write(toEntityId);
    }

    public static void MoveItemStack(
EntityPlayer fromPlayer,
EntityPlayer toPlayer,
int fromSlotIndex,
int amount)
    {
        Bag fromBag = fromPlayer.bag;
        Bag toBag = toPlayer.bag;

        ItemStack sourceStack = fromBag.GetSlots()[fromSlotIndex];

        if (sourceStack.IsEmpty())
            return;

        int moveAmount = Math.Min(amount, sourceStack.count);

        // Create a temp stack
        ItemStack movingStack = sourceStack.Clone();
        movingStack.count = moveAmount;

        // Try to add FIRST (no removal yet)
        bool added = toBag.AddItem(movingStack);

        if (!added)
        {
            // Could not add safely — abort
            return;
        }

        // Now remove from source
        sourceStack.count -= moveAmount;
        if (sourceStack.count <= 0)
            fromBag.GetSlots()[fromSlotIndex] = ItemStack.Empty;
    }

    public static void MoveAllOfItem(
EntityPlayer fromPlayer,
EntityPlayer toPlayer,
string itemName)
    {
        Bag fromBag = fromPlayer.bag;

        for (int i = 0; i < fromBag.GetSlots().Length; i++)
        {
            ItemStack stack = fromBag.GetSlots()[i];

            if (stack.IsEmpty())
                continue;

            if (stack.itemValue.ItemClass.GetItemName() != itemName)
                continue;

            MoveItemStack(fromPlayer, toPlayer, i, stack.count);
        }
    }
}
