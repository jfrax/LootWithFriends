using System.Text;
using Newtonsoft.Json;

namespace LootWithFriends
{
    public class NetPackageServerReplyClientAffinities : NetPackage
    {
        
        private string affinityJson;
        
        public NetPackage Setup(Affinity affinity)
        {
            NetGuards.ServerOnly("NetPackageServerReplyClientAffinities.Setup");
            affinityJson = JsonConvert.SerializeObject(affinity);
            return this;
        }

        public override void write(PooledBinaryWriter writer)
        {
            base.write(writer);
            writer.Write(affinityJson);
        }
        
        public override void read(PooledBinaryReader reader)
        {
            affinityJson = reader.ReadString();
        }
        
        public override void ProcessPackage(World world, GameManager callbacks)
        {
            NetGuards.ClientOnly("NetPackageServerReplyClientAffinities.ProcessPackage");
            var affinity = JsonConvert.DeserializeObject<Affinity>(affinityJson);
            Affinity.ClientSetAffinitiesForPlayer(GameManager.Instance.myEntityPlayerLocal, affinity);
        }

        public override int GetLength() => Encoding.UTF8.GetByteCount(affinityJson ?? "") + 8;
        
    }
}