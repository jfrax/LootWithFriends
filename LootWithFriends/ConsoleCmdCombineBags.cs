using System.Collections.Generic;

namespace LootWithFriends
{
    public class ConsoleCmdCombineBags : ConsoleCmdAbstract
    {
        public override string[] getCommands()
        {
            return new[] { "combinebags", "cb" };
        }

        public override string getDescription()
        {
            return "Combine backpack items from one player into another.";
        }

        public override string GetHelp()
        {
            return
                "Usage:\n" +
                "cb (nearest player → you)";
        }

        public override void Execute(List<string> parameters, CommandSenderInfo senderInfo)
        {
            EntityPlayer fromPlayer = ConnectionManager.Instance.IsServer
                ? GameManager.Instance.myEntityPlayerLocal
                : GameManager.Instance.World.Players.dict[senderInfo.RemoteClientInfo.entityId];

            EntityPlayer nearest = FindNearestOtherPlayer(fromPlayer);

            if (nearest == null)
            {
                Log.Out("No other players nearby.");
                return;
            }

            if (ConnectionManager.Instance.IsServer)
            {
                // We are host/server
                BagOperations.CombineBags(fromPlayer, nearest);
                Log.Out("Combine Bags executed directly on server.");
            }
            else if (senderInfo.RemoteClientInfo != null)
            {
                // We are client: send NetPackage to server
                SendPackage(fromPlayer.entityId, nearest.entityId);
                Log.Out("Combine Bags package sent to server.");
            }
            else
            {
                Log.Error("Not executed on server, but senderInfo.RemoteClientInfo is null.");
                return;
            }

            SdtdConsole.Instance.Output(
                $"Finished cb command for: {fromPlayer.EntityName} → {nearest.EntityName}");
        }

        private static void SendPackage(int fromId, int toId)
        {
            var pkg = new NetPackageCombineBags(fromId, toId);
            ConnectionManager.Instance.SendToServer(pkg);
        }

        private static EntityPlayer FindNearestOtherPlayer(EntityPlayer self)
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