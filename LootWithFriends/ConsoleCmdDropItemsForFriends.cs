using System.Collections.Generic;

namespace LootWithFriends
{
    public class ConsoleCmdDropItemsForFriends : ConsoleCmdAbstract
    {
        public override string[] getCommands()
        {
            return new[] { "lwf" };
        }

        public override string getDescription()
        {
            return "Loot With Friends Mod: Drop items from your backpack intended for your nearest ally.";
        }

        public override string GetHelp()
        {
            return
                "Usage:\n" +
                "lwf";
        }

        public override void Execute(List<string> parameters, CommandSenderInfo senderInfo)
        {
            ItemDrop.PerformDrop(GameManager.Instance.myEntityPlayerLocal);
        }
    }
}