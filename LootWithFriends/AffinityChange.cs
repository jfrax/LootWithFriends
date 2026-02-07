namespace LootWithFriends
{
    public class AffinityChange
    {
        public string PlayerPlatformId { get; set; }
        public string ItemClassName { get; set; }
        public AffinityTypes AffinityType { get; set; }

        public AffinityChange(string playerPlatformId, string itemClassName, AffinityTypes affinityType)
        {
            PlayerPlatformId = playerPlatformId;
            ItemClassName = itemClassName;
            AffinityType = affinityType;
        }
    }
}