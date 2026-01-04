namespace GPSrvtTabWrapper
{
    public interface ICheckUpdate
    {
        bool CheckForUpdate();
        
        bool ShouldUpdateOnShutdown();
        
        void UpdateOnShutdown();
    }
}
