namespace LootWithFriends
{
    public static class Utilities
    {
        public static EntityPlayer FindNearestOtherPlayer(EntityPlayer self)
        {
            float bestDistSq = float.MaxValue;
            EntityPlayer best = null;

            var players = GameManager.Instance.World.Players.list;

            foreach (var player in players)
            {
                if (player == null || player.entityId == self.entityId)
                    continue;

                float distSq = (player.position - self.position).sqrMagnitude;

                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    best = player;
                }
            }

            return best;
        }
    }
}