using System.Text;
using Newtonsoft.Json;

namespace LootWithFriends
{
    public class NetPackageClientOnTrackedWaypointRemoved : NetPackage
    {
        private string saveDataJson;
        private int playerEntityId;

        public NetPackage Setup(KnownDroppedBagSaveData saveData, EntityPlayer deletingPlayer)
        {
            saveDataJson = JsonConvert.SerializeObject(saveData);
            playerEntityId = deletingPlayer.entityId;
            return this;
        }
        
        public override void write(PooledBinaryWriter writer)
        {
            base.write(writer);
            writer.Write(playerEntityId);
            writer.Write(saveDataJson);
        }

        public override void read(PooledBinaryReader reader)
        {
            playerEntityId = reader.ReadInt32();
            saveDataJson = reader.ReadString();
        }

        public override void ProcessPackage(World world, GameManager callbacks)
        {
            var saveData = JsonConvert.DeserializeObject<KnownDroppedBagSaveData>(saveDataJson);
            var deleterStableId = Utilities.GetStablePlayerId(playerEntityId);
            Waypoints.MarkWaypointAsDeleted(saveData, deleterStableId);
        }

        public override int GetLength() => 4 + Encoding.UTF8.GetByteCount(saveDataJson);
    }
}