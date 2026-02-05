using System;
using System.Text;
using Newtonsoft.Json;

namespace LootWithFriends
{
    public class NetPackageClientChangedAffinity : NetPackage
    {

        private string AffinityChangeJson;
        
        public NetPackage Setup(string playerName, string itemClassName, AffinityTypes affinityType)
        {
            AffinityChangeJson = JsonConvert.SerializeObject(new AffinityChange(playerName, itemClassName, affinityType));
            return this;
        }
        
        //processed on the SERVER
        public override void ProcessPackage(World _world, GameManager _callbacks)
        {
            var affinityChange = JsonConvert.DeserializeObject<AffinityChange>(AffinityChangeJson);
            Log.Out($"Server Processing NetPackageClientChangedAffinity. {affinityChange.PlayerName}, {affinityChange.ItemClassName}, {affinityChange.AffinityType}");
            Affinity.ServerUpdateAffinitiesForPlayer(affinityChange);
        }

        public override void read(PooledBinaryReader reader)
        {
            AffinityChangeJson = reader.ReadString();
        }

        public override void write(PooledBinaryWriter writer)
        {
            base.write(writer);
            writer.Write(AffinityChangeJson);
        }

        public override int GetLength() => Encoding.UTF8.GetByteCount(AffinityChangeJson ?? "") + 8;

    }
}