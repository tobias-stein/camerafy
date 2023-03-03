using System.Threading.Tasks;

namespace Camerafy.Application
{
    using Event;

    public partial class Session
    {
        [CamerafyEvent(CamerafyEventProperty.Client | CamerafyEventProperty.NoReponse)]
        private async Task<bool> Connect(string InUserId)
        {
            // stop right here, if no new connections are allowed
            if (!Application.Current.Session.NewUserConnectionsAllowed)
                return false;

            //User.UserLoginData ULD = Serialization.EntityManager.FromString<User.UserLoginData>(InUserLoginData);
            //if (ULD == null || !ULD.Valid())
            //    return false;

            // if annonymous, check if allowed
            //if (Application.Current.Config.UserLogin && !ULD.Authenticated())
            //    return false;

            // spawn new player
            await Application.Current.CreateGamethreadTask(delegate 
            {
                // make user groups lower-case
                //for (int i = 0; i < ULD.Groups.Count; ++i) { ULD.Groups[i] = ULD.Groups[i].ToLower(); }
                Application.Current.Session.SpawnUser(InUserId);
            });

            return true;
        }

        [CamerafyEvent(CamerafyEventProperty.Server)]
        void SessionTerminate()
        {
            // Signal all connected client that this session is terminating.
            EventBus.Current.SendServerEventData(null);
        }
    }
}
