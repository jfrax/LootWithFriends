using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using UniLinq;

namespace LootWithFriends
{
    public class NetPackageServerReplyClientAffinities : NetPackage
    {
        
        private string AffinityJson;
        
        public NetPackage Setup(Affinity affinity)
        {
            AffinityJson = JsonConvert.SerializeObject(affinity);
            return this;
        }

        //This one is processed on the CLIENT
        public override void ProcessPackage(World _world, GameManager _callbacks)
        {
            var affinity = JsonConvert.DeserializeObject<Affinity>(AffinityJson);
            Affinity.ClientSetAffinitiesForPlayer(GameManager.Instance.myEntityPlayerLocal, affinity);
        }

        public override void read(PooledBinaryReader reader)
        {
            AffinityJson = reader.ReadString();
        }

        public override void write(PooledBinaryWriter writer)
        {
            base.write(writer);
            writer.Write(AffinityJson);
        }

        public override int GetLength() => Encoding.UTF8.GetByteCount(AffinityJson ?? "") + 8;
        
    }
}