namespace Rapr.Utils
{
    public static class DriverStoreFactory
    {
        public static IDriverStore CreateOnlineDriverStore()
        {
            if (DSEFormHelper.IsWin8OrNewer && DismUtil.IsDismAvailable)
            {
                return new DismUtil();
            }
            else
            {
                return new PnpUtil();
            }
        }

        public static IDriverStore CreateOfflineDriverStore(string imagePath)
        {
            return new DismUtil(imagePath);
        }

        public static IDriverStore CreateRemoteDriverStore(string computerName)
        {
            return new RemoteDriverStore(computerName);
        }
    }
}
