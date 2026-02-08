namespace LootWithFriends
{
    //no real content to this one -it's just a message so the server knows
    public class NetPackageClientRequestingWaypoints : NetPackage
    {
        public override void read(PooledBinaryReader _reader)
        {
            throw new System.NotImplementedException();
        }

        public override void ProcessPackage(World _world, GameManager _callbacks)
        {
            throw new System.NotImplementedException();
        }

        public override int GetLength()
        {
            throw new System.NotImplementedException();
        }
    }
}