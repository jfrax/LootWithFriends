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
        
        public override void write(PooledBinaryWriter _writer)
        {
            base.write(_writer);
            _writer.Write(playerEntityId);
            _writer.Write(saveDataJson);
        }

        public override void read(PooledBinaryReader _reader)
        {
            playerEntityId = _reader.ReadInt32();
            saveDataJson = _reader.ReadString();
        }

        public override void ProcessPackage(World _world, GameManager _callbacks)
        {
            var saveData = JsonConvert.DeserializeObject<KnownDroppedBagSaveData>(saveDataJson);
            var deleterStableId = Utilities.GetStablePlayerId(playerEntityId);
            Waypoints.MarkWaypointAsDeleted(saveData, deleterStableId);
        }

        public override int GetLength() => 4 + Encoding.UTF8.GetByteCount(saveDataJson);
    }
}