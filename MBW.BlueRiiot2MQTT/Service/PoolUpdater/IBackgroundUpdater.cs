namespace MBW.BlueRiiot2MQTT.Service.PoolUpdater
{
    internal interface IBackgroundUpdater
    {
        void Start();
        void Stop();
        void ForceSync();
    }
}