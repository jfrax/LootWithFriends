using UnityEngine;

namespace LootWithFriends
{
    public class KnownDroppedBagSaveData
    {
        public int entityId;

        public float posX;
        public float posY;
        public float posZ;
        
        public string droppedByStableId;
        public string droppedForStableId;
        public string droppedByDisplayName;

        public bool droppedByDeletedWaypoint;
        public bool droppedForDeletedWaypoint;
    }
}