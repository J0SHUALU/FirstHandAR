using System.Collections.Generic;

namespace FirstHand.Data
{
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
