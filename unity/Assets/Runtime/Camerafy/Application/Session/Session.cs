using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Camerafy
{
    namespace Application
    {
        using User;
        using Network;

        /// <summary>
        /// The session will connect to a message broker to enable signaling with remote clients.
        /// Another task of the session is to create and remove clients. New user objects are
        /// spawned as child object under the session game object.
        /// Note: Session MonoBehaviour execution order must be one below Signaler to ensure proper initialization.
        /// </summary>
        [DefaultExecutionOrder(201)]
        public partial class Session : PeerConnection
        {
            /// <summary>
            /// All currently connected user.
            /// </summary>
            public Dictionary<string, User> ConnectedUser { get; private set; } = new Dictionary<string, User>();

            /// <summary>
            /// The life time (seconds) of a user token. Expired tokens cannot be used for user-session authentication.
            /// </summary>
            public int UserTokenLifetime = 90;

            /// <summary>
            /// Maximum number of concurrent useres per session.
            /// </summary>
            public int MaximumUserSlots = 5;

            /// <summary>
            /// The prefab for session users.
            /// </summary>
            public GameObject UserPrefab = null;

            /// <summary>
            /// True if new user connections are allowed.
            /// </summary>
            public bool NewUserConnectionsAllowed { get { return this.ConnectedUser.Count < this.MaximumUserSlots; } }

            private void Start()
            {
                if (this.UserPrefab == null)
                    throw new Exception("Session.UserPrefab is null.");

                // override Session properties by config
                {
                    this.MaximumUserSlots = Application.Current.Config.MaximumUserSlots;
                }

                // override default session peer id by static id provided by configuration file
                this.Initialize(Application.Current.Config.SessionId);
            }

            internal new void OnDestroy()
            {
                // dispose of peer connection and event-bus connection.
                this.Dispose();
            }

            private void OnApplicationQuit()
            {
                // Singal all clients session termination.
                this.SessionTerminate();
            }

            private void Update()
            {
                // check if any user has timed out.
                var NOW = DateTime.UtcNow;
                foreach (var kvp in this.ConnectedUser)
                {
                    string id = kvp.Key;
                    User user = kvp.Value;

                    if ((NOW - user.LastActivity).TotalSeconds > Application.Current.Config.UserSessionTimout)
                    {
                        Logger.Info("User '{0}' timed out. Connection will be closed.", id);

                        // tell user that he timed out and disconnect.
                        user.Timeout();

                        // remove user object next frame
                        RemoveSessionUser(id);
                    }
                    else
                    {
                        switch (user.UserStatus)
                        {
                            case UserStatus.Failed:
                            {
                                RemoveSessionUser(id);
                                break;
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// We need a coroutine to spawn new user, since unity does not allow it,
            /// to call Instantiate in another thread, except the main one.
            /// </summary>
            /// <returns></returns>
            public void SpawnUser(string InUserId)
            {
                // sanity check
                if (this.ConnectedUser.ContainsKey(InUserId))
                {
                    Logger.Error("Trying to spawn a user ({0}) twice. ", InUserId);
                    return;
                }

                GameObject GO = Instantiate(this.UserPrefab);
                {
                    // add the user object under the session game object
                    GO.transform.SetParent(this.gameObject.transform);

#if UNITY_EDITOR
                    GO.name = InUserId;
#endif

                    // access User component
                    User user = GO.GetComponent<User>();
                    {
                        // cache user object
                        this.ConnectedUser.Add(InUserId, user);

                        user.UserStatus = UserStatus.Initializing;
                        //user.LoginData = LoginData;

                        // Call Initialized method on user
                        user.Initialize(InUserId);
                    }
                }
            }

            private void KillUser(string UserID)
            {
                if (string.IsNullOrWhiteSpace(UserID))
                    return;

                User user;
                if (this.ConnectedUser.TryGetValue(UserID, out user))
                {
                    // destroy the user session gameobject
                    Destroy(user.gameObject);

                    // remove user from cache
                    this.ConnectedUser.Remove(user.Id);
                }
            }

            /// <summary>
            /// Removes an user from active session.
            /// </summary>
            /// <param name="userId"></param>
            public async Task RemoveSessionUser(string UserId)
            {
                await Application.Current.CreateGamethreadTask(delegate { KillUser(UserId); });
            }
        }
    }
}