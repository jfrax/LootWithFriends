using System.Text;
using Newtonsoft.Json;

namespace LootWithFriends
{
    public class NetPackageServerSendClientKnownDroppedBagSaveData : NetPackage
    {

        private string JsonSaveData;
        private bool BeingDeleted;
        
        public NetPackage Setup(KnownDroppedBagSaveData knownDroppedBagSaveData, bool beingDeleted = false)
        {
            JsonSaveData = JsonConvert.SerializeObject(knownDroppedBagSaveData, Formatting.Indented);
            BeingDeleted = beingDeleted;
            Log.Out($"NetPackageServerSendClientKnownDroppedBagSaveData Setup -, {BeingDeleted}, {JsonSaveData}");
            return this;
        }
        
        public override void write(PooledBinaryWriter _writer)
        {
            base.write(_writer);
            _writer.Write(BeingDeleted);
            _writer.Write(JsonSaveData);
        }

        public override void read(PooledBinaryReader _reader)
        {
            BeingDeleted = _reader.ReadBoolean();
            JsonSaveData =  _reader.ReadString();
        }

        public override void ProcessPackage(World _world, GameManager _callbacks)
        {
            var saveData = JsonConvert.DeserializeObject<KnownDroppedBagSaveData>(JsonSaveData);
            Log.Out($"NetPackageServerSendClientKnownDroppedBagSaveData ProcessPackage -, {BeingDeleted}, {JsonSaveData}");
            //let's check if the client knows about the entity by id. if not, we'll create the wp by coordinate
            var byCoordRef = GameManager.Instance.World.GetEntity(saveData.entityId) == null;
            
            Waypoints.AddUpdateOrDeleteWaypointForLocalPlayer(saveData, BeingDeleted, byCoordRef);
        }

        public override int GetLength() => 4 + Encoding.UTF8.GetByteCount(JsonSaveData);
    }
}