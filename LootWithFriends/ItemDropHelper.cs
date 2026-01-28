namespace LootWithFriends
{
    using System.Collections.Generic;
    using UnityEngine;

    public static class ItemDropHelper
    {
        private class PendingDrop
        {
            public ItemStack Stack;
            public Vector3 Position;
            public float DropTime;
        }

        private static List<PendingDrop> pendingDrops = new List<PendingDrop>();

        /// <summary>
        /// Queue a group of items to drop in a line.
        /// </summary>
        /// <param name="player">Player dropping the items</param>
        /// <param name="stacks">Item stacks to drop</param>
        /// <param name="direction">Normalized direction to lay out the line (e.g., player.forward)</param>
        /// <param name="spacing">Distance between each drop</param>
        /// <param name="startDelay">Optional initial delay before first drop</param>
        /// <param name="delayBetween">Optional delay between drops</param>
        public static void QueueLineDrops(
            EntityPlayer player,
            List<ItemStack> stacks,
            Vector3 direction,
            float spacing = 0.5f,
            float startDelay = 0.05f,
            float delayBetween = 0.05f
        )
        {
            Vector3 basePos = player.position + Vector3.up; // start slightly above ground
            direction.Normalize();

            for (int i = 0; i < stacks.Count; i++)
            {
                pendingDrops.Add(new PendingDrop
                {
                    Stack = stacks[i],
                    Position = basePos + direction * spacing * i,
                    DropTime = Time.time + startDelay + delayBetween * i
                });
            }
        }

        /// <summary>
        /// Call every frame from GameManager.Update (or via Harmony postfix) to process queued drops.
        /// </summary>
        public static void ProcessPendingDrops()
        {
            float now = Time.time;
            for (int i = pendingDrops.Count - 1; i >= 0; i--)
            {
                if (pendingDrops[i].DropTime <= now)
                {
                    var pd = pendingDrops[i];
                    GameManager.Instance.ItemDropServer(pd.Stack, pd.Position, Vector3.zero, -1, 60f, false);
                    pendingDrops.RemoveAt(i);
                }
            }
        }
    }
}