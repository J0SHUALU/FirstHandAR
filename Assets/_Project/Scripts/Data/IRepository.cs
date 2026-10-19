using System.Collections.Generic;

namespace FirstHand.Data
{
    /// <summary>
    /// Repository pattern: the rest of the app asks for "profiles" or "sessions for this trainee" and never
    /// sees file paths, JSON or encryption. Swapping storage only changes the implementation class.
    /// </summary>
    public interface IRepository
    {
        ProfilesFile LoadProfiles();
        void SaveProfiles(ProfilesFile file);

        SessionsFile LoadSessions(string traineeId);
        void SaveSessions(SessionsFile file);
        void DeleteSessions(string traineeId);

        IEnumerable<string> AllTraineeIdsWithSessions();
        string WriteExport(string fileName, string contents);
        void PurgeOldExports(double maxAgeHours);
    }
}
