using SmartBackup.Model;

namespace SmartBackup.Services
{
    public interface IConfReader
    {
        Config Configuration { get; }
        void Load();
    }
}