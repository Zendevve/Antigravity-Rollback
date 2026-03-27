using System.Threading.Tasks;

namespace AGRollbackTool.Services
{
    /// <summary>
    /// Interface for a service that modifies Google Antigravity's settings.json to block updates.
    /// </summary>
    public interface ISettingsInjectorService
    {
        /// <summary>
        /// Modifies the Antigravity settings.json file to block updates.
        /// </summary>
        /// <returns>A SettingsChangeResult indicating what was changed and whether the operation succeeded.</returns>
        Task<SettingsChangeResult> InjectSettingsAsync();
    }
}
