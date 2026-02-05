namespace LootWithFriends
{
    public class AffinityChange
    {
        public string PlayerName { get; set; }
        public string ItemClassName { get; set; }
        public AffinityTypes AffinityType { get; set; }

        public AffinityChange(string playerName, string itemClassName, AffinityTypes affinityType)
        {
            PlayerName = playerName;
            ItemClassName = itemClassName;
            AffinityType = affinityType;
        }
    }
}