using System.Text;
using Newtonsoft.Json;

namespace LootWithFriends
{
    public class NetPackageServerReplyClientAffinities : NetPackage
    {
        
        private string AffinityJson;
        
        public NetPackage Setup(Affinity affinity)
        {
            NetGuards.ServerOnly("NetPackageServerReplyClientAffinities.Setup");
            AffinityJson = JsonConvert.SerializeObject(affinity);
            return this;
        }

        public override void write(PooledBinaryWriter writer)
        {
            base.write(writer);
            writer.Write(AffinityJson);
        }
        
        public override void read(PooledBinaryReader reader)
        {
            AffinityJson = reader.ReadString();
        }
        
        public override void ProcessPackage(World _world, GameManager _callbacks)
        {
            NetGuards.ClientOnly("NetPackageServerReplyClientAffinities.ProcessPackage");
            var affinity = JsonConvert.DeserializeObject<Affinity>(AffinityJson);
            Affinity.ClientSetAffinitiesForPlayer(GameManager.Instance.myEntityPlayerLocal, affinity);
        }

        public override int GetLength() => Encoding.UTF8.GetByteCount(AffinityJson ?? "") + 8;
        
    }
}