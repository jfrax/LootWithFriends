using System.Text;
using Newtonsoft.Json;

namespace LootWithFriends
{
    public class NetPackageServerSendClientKnownDroppedBagSaveData : NetPackage
    {

        private string jsonSaveData;
        private bool beingDeleted;
        
        public NetPackage Setup(KnownDroppedBagSaveData knownDroppedBagSaveData, bool isBeingDeleted = false)
        {
            jsonSaveData = JsonConvert.SerializeObject(knownDroppedBagSaveData, Formatting.Indented);
            beingDeleted = isBeingDeleted;
            return this;
        }
        
        public override void write(PooledBinaryWriter writer)
        {
            base.write(writer);
            writer.Write(beingDeleted);
            writer.Write(jsonSaveData);
        }

        public override void read(PooledBinaryReader reader)
        {
            beingDeleted = reader.ReadBoolean();
            jsonSaveData =  reader.ReadString();
        }

        public override void ProcessPackage(World world, GameManager callbacks)
        {
            var saveData = JsonConvert.DeserializeObject<KnownDroppedBagSaveData>(jsonSaveData);
            
            if (beingDeleted)
            {
                Waypoints.DeleteLocalWaypoint(saveData);
            }
            else
            {
                Waypoints.ClientApplyBagState(saveData);
            }
        }

        public override int GetLength() => 4 + Encoding.UTF8.GetByteCount(jsonSaveData);
    }
}