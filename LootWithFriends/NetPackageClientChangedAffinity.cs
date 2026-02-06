using System.Text;
using Newtonsoft.Json;

namespace LootWithFriends
{
    public class NetPackageClientChangedAffinity : NetPackage
    {
        private string affinityChangeJson;
        
        public NetPackage Setup(string playerName, string itemClassName, AffinityTypes affinityType)
        {
            NetGuards.ClientOnly("NetPackageClientChangedAffinity.Setup");
            affinityChangeJson = JsonConvert.SerializeObject(new AffinityChange(playerName, itemClassName, affinityType));
            return this;
        }
        
        public override void write(PooledBinaryWriter writer)
        {
            base.write(writer);
            writer.Write(affinityChangeJson);
        }
        
        public override void read(PooledBinaryReader reader)
        {
            affinityChangeJson = reader.ReadString();
        }
        
        public override void ProcessPackage(World world, GameManager callbacks)
        {
            NetGuards.ServerOnly("NetPackageClientChangedAffinity.ProcessPackage");
            var affinityChange = JsonConvert.DeserializeObject<AffinityChange>(affinityChangeJson);
            Affinity.ServerUpdateAffinitiesForPlayer(affinityChange);
        }

        public override int GetLength() => Encoding.UTF8.GetByteCount(affinityChangeJson ?? "") + 8;

    }
}