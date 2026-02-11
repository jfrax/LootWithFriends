using UniLinq;

namespace LootWithFriends
{
    public class NetPackageClientRequestingAffinities : NetPackage
    {
        private int requestingPlayerEntityId;

        public NetPackage Setup(int playerEntityId)
        {
            NetGuards.ClientOnly("NetPackageClientRequestingAffinities.Setup");
            requestingPlayerEntityId = playerEntityId;
            return this;
        }
        
        public override void write(PooledBinaryWriter writer)
        {
            base.write(writer);
            writer.Write(requestingPlayerEntityId);
        }

        public override void read(PooledBinaryReader reader)
        {
            requestingPlayerEntityId = reader.ReadInt32();
        }
        
        public override void ProcessPackage(World world, GameManager callbacks)
        {
            NetGuards.ServerOnly("NetPackageClientRequestingAffinities.ProcessPackage");
            //now we (the server) reply to this client
            var player = GameManager.Instance.World.Players.list.FirstOrDefault(x => x.entityId == requestingPlayerEntityId);
            var playerAffinities = Affinity.GetAffinitiesForPlayer(player);

            var pkg = NetPackageManager.GetPackage<NetPackageServerReplyClientAffinities>().Setup(playerAffinities);
            
            ConnectionManager.Instance.SendPackage(
                pkg,
                _onlyClientsAttachedToAnEntity: true,
                _attachedToEntityId: requestingPlayerEntityId
            );
        }

        public override int GetLength() => 8;

    }
}