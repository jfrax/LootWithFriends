using System;
using JetBrains.Annotations;
using UniLinq;

namespace LootWithFriends
{
    public class ItemDropRequestInfo
    {
        public EntityPlayer RequestorPlayer { get; set; }
        public string[] ItemSlotNames { get; set; }
        public int[] StackCounts {get;set;}
        public PackedBoolArray LockedSlots {get;set;}
        
        public ItemDropRequestInfo([NotNull] EntityPlayer requestorPlayer, [NotNull] string[] itemSlotNames,
            [NotNull] int[] stackCounts, [NotNull] PackedBoolArray lockedSlots)
        {
            RequestorPlayer = requestorPlayer ?? throw new ArgumentNullException(nameof(requestorPlayer));
            ItemSlotNames = itemSlotNames ?? throw new ArgumentNullException(nameof(itemSlotNames));
            StackCounts = stackCounts ?? throw new ArgumentNullException(nameof(stackCounts));
            LockedSlots = lockedSlots ?? throw new ArgumentNullException(nameof(lockedSlots));
        }

        private ItemDropRequestInfo()
        {
        }

        public static ItemDropRequestInfo FromServerPlayer([NotNull] EntityPlayer requestorPlayer)
        {
            NetGuards.ServerOnly(nameof(FromServerPlayer));

            return new ItemDropRequestInfo()
            {
                RequestorPlayer = requestorPlayer,
                ItemSlotNames = requestorPlayer.bag.items.Select(x => x.itemValue?.ItemClass?.Name ?? string.Empty).ToArray(),
                StackCounts = requestorPlayer.bag.items.Select(x => x.count).ToArray(),
                LockedSlots = requestorPlayer.bag.LockedSlots
            };
        }
    }
}