namespace tmkbCompanion.MVVM.Core
{
    public interface IUpdateService
    {
        /// <summary>
        /// Checks for updates.
        /// </summary>
        /// <param name="isManualCheck">True if triggered by user from settings, false if automatically checked on launch.</param>
        void CheckForUpdates(bool isManualCheck);
    }
}
