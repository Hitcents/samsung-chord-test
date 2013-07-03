using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using Android.Gms.Common;
using Android.Gms.AppStates;
using Android.Gms.Games;
using IOnSignOutCompleteListener = Android.Gms.Games.IOnSignOutCompleteListener;
using Android.Gms.Plus;
using Android.Gms.Games.MultiPlayer;

namespace GooglePlayTest
{
    public class GameHelper : Java.Lang.Object, IGooglePlayServicesClientConnectionCallbacks, IGooglePlayServicesClientOnConnectionFailedListener, IOnSignOutCompleteListener
    {

        public interface GameHelperListener
        {
            void OnSignInFailed();
            void OnSignInSucceeded();
        }

        Activity activity = null;

        static readonly int RC_RESOLVE = 9001;
        static readonly int RC_UNUSED = 9002;

        private GamesClient gamesClient = null;
        private PlusClient plusCient = null;
        private AppStateClient appStateClient = null;

        public const int CLIENT_NONE = 0x00;
        public const int CLIENT_GAMES = 0x01;
        public const int CLIENT_PLUS = 0x02;
        public const int CLIENT_APPSTATE = 0x04;
        public const int CLIENT_ALL = CLIENT_GAMES | CLIENT_PLUS | CLIENT_APPSTATE;

        int requestClients = CLIENT_NONE;
        int connectedClients = CLIENT_NONE;
        int clientCurrentlyConnecting = CLIENT_NONE;

        ProgressDialog progressDialog = null;

        bool autoSignIn = true;

        bool userInitiatedSignIn = false;

        ConnectionResult connectionResult = null;

        bool signInError = false;
        bool expectingActivityResult = false;
        bool signedIn;
        bool debugLog = false;

        string debugTag = "BaseGameActivity";

        string signingInMessage = string.Empty;
        string signingOutMessage = string.Empty;
        string unknownErrorMessage = "Unknown error";

        string invitationId;

        GameHelperListener listener = null;

        public GameHelper(Activity activity)
        {
            this.activity = activity;
        }

        public string SigningInMessage
        {
            get
            {
                return signingInMessage;
            }
            set
            {
                signingInMessage = value;
            }
        }

        public string SigningOutMessage
        {
            get
            {
                return signingOutMessage;
            }
            set
            {
                signingOutMessage = value;
            }
        }

        public void SetUnknownErrorMessage(string message)
        {
            unknownErrorMessage = message;
        }

        public void Setup(GameHelperListener listener)
        {
            Setup(listener, CLIENT_GAMES);
        }

        public void Setup(GameHelperListener listener, int clientsToUse)
        {
            this.listener = listener;
            requestClients = clientsToUse;

            List<string> scopesList = new List<string>();
            if (0 != (clientsToUse & CLIENT_GAMES))
            {
                scopesList.Add(Scopes.Games);
            }
            if (0 != (clientsToUse & CLIENT_PLUS))
            {
                scopesList.Add(Scopes.PlusLogin);
            }
            if (0 != (clientsToUse & CLIENT_APPSTATE))
            {
                scopesList.Add(Scopes.AppState);
            }

            if (0 != (clientsToUse & CLIENT_GAMES))
            {
                gamesClient = new GamesClient.Builder(activity, this, this)
                .SetGravityForPopups((int)(GravityFlags.Top | GravityFlags.CenterHorizontal))
                .SetScopes(scopesList.ToArray())
                .Create();
            }

            if (0 != (clientsToUse & CLIENT_PLUS))
            {
                plusCient = new PlusClient.Builder(activity, this, this)
                .SetScopes(scopesList.ToArray())
                .Build();
            }

            if (0 != (clientsToUse & CLIENT_APPSTATE))
            {
                appStateClient = new AppStateClient.Builder(activity, this, this)
                .SetScopes(scopesList.ToArray())
                .Create();
            }
        }

        public GamesClient GamesClient
        {
            get
            {
                if (gamesClient == null)
                    throw new Exception("No GamesClient. Did you request it at setup?");
                else
                    return gamesClient;
            }
        }

        public AppStateClient AppStateClient
        {
            get
            {
                if (appStateClient == null)
                    throw new Exception("No AppStateClient.  Did you request it at setup?");
                else
                    return appStateClient;
            }
        }

        public PlusClient PlusClient
        {
            get
            {
                if (PlusClient == null)
                    throw new Exception("No PlusClient. Did you request it at setup?");
                else
                    return PlusClient;
            }
        }

        public bool IsSignedIn
        {
            get
            {
                return signedIn;
            }
        }

        public bool HasSignInError
        {
            get
            {
                return signInError;
            }
        }

        public ConnectionResult SignInError
        {
            get
            {
                return signInError ? connectionResult : null;
            }
        }

        public Dialog ErrorDialog(int errorCode)
        {
            Dialog errorDialog = GooglePlayServicesUtil.GetErrorDialog(errorCode, activity, RC_UNUSED, null);
            if (errorDialog != null)
                return errorDialog;
            return (new AlertDialog.Builder(activity))
                .SetMessage(unknownErrorMessage + " " + errorCode)
                .SetNeutralButton(Android.Resource.String.Ok, (EventHandler<DialogClickEventArgs>)null)
                .Create();
        }

        public void OnStart(Activity activity)
        {
            this.activity = activity;

            if (expectingActivityResult)
            {
                //debuglog
            }
            else if (!autoSignIn)
            {

            }
            else
            {
                StartConnections();
            }
        }

        public void OnStop()
        {
            KillConnections(CLIENT_ALL);

            signedIn = false;
            signInError = false;

            DismissDialog();

            progressDialog = null;

            activity = null;
        }

        public void ShowAlert(string title, string message)
        {
            new AlertDialog.Builder(activity)
                .SetMessage(message)
                .SetNeutralButton(Android.Resource.String.Ok, (EventHandler<DialogClickEventArgs>)null)
                .Create()
                .Show();
        }

        public string InvitationId
        {
            get
            {
                return invitationId;
            }
        }

        public void EnableDebugLog(bool enabled, string tag)
        {
            debugLog = enabled;
            debugTag = tag;
        }

        public string GetScopes()
        {
            StringBuilder scopeStringBuilder = new StringBuilder();
            int clientsToUse = requestClients;
            // GAMES implies PLUS_LOGIN
            if (0 != (clientsToUse & CLIENT_GAMES))
            {
                AddToScope(scopeStringBuilder, Scopes.Games);
            }
            if (0 != (clientsToUse & CLIENT_PLUS))
            {
                AddToScope(scopeStringBuilder, Scopes.PlusLogin);
            }
            if (0 != (clientsToUse & CLIENT_APPSTATE))
            {
                AddToScope(scopeStringBuilder, Scopes.AppState);
            }
            return scopeStringBuilder.ToString();
        }

        void AddToScope(StringBuilder scopeStringBuilder, String scope)
        {
            if (scopeStringBuilder.Length == 0)
            {
                scopeStringBuilder.Append("oauth2:");
            }
            else
            {
                scopeStringBuilder.Append(" ");
            }
            scopeStringBuilder.Append(scope);
        }

        public void SignOut()
        {
            connectionResult = null;
            autoSignIn = false;
            signedIn = false;
            signInError = false;

            if (plusCient != null && plusCient.IsConnected)
            {
                plusCient.ClearDefaultAccount();
            }
            if (gamesClient != null && gamesClient.IsConnected)
            {
                ShowProgressDialog(false);
                gamesClient.SignOut(this);
            }

            // kill connects to all clients but games, which must remain
            // connected til we get onSignOutComplete()
            KillConnections(CLIENT_ALL & ~CLIENT_GAMES);
        }

        public void OnActivityResult(int requestCode, int responseCode, Intent intent)
        {
            if (requestCode == RC_RESOLVE)
            {
                // We're coming back from an activity that was launched to resolve a
                // connection
                // problem. For example, the sign-in UI.
                expectingActivityResult = false;
                //debugLog("onActivityResult, req " + requestCode + " response " + responseCode);
                if (responseCode == (int)Result.Ok)
                {
                    // Ready to try to connect again.
                    //debugLog("responseCode == RESULT_OK. So connecting.");
                    ConnectCurrentClient();
                }
                else
                {
                    // Whatever the problem we were trying to solve, it was not
                    // solved.
                    // So give up and show an error message.
                    //debugLog("responseCode != RESULT_OK, so not reconnecting.");
                    GiveUp();
                }
            }
        }

        public void BeginUserInitiatedSignIn()
        {
            if (signedIn)
            {
                return;
            }

            autoSignIn = true;

            int result = GooglePlayServicesUtil.IsGooglePlayServicesAvailable(activity);

            if (result != ConnectionResult.Success)
            {
                Dialog errorDialog = ErrorDialog(result);
                errorDialog.Show();
                if (listener != null)
                    listener.OnSignInFailed();
                return;
            }

            userInitiatedSignIn = true;
            if (connectionResult != null)
            {
                ShowProgressDialog(true);
                ResolveConnectionResult();
            }
            else
            {
                StartConnections();
            }
        }

        void StartConnections()
        {
            connectedClients = CLIENT_NONE;
            invitationId = null;
            ConnectNextClient();
        }

        void ShowProgressDialog(bool signIn)
        {
            string message = signIn ? signingInMessage : signingOutMessage;

            if (progressDialog == null)
            {
                if (activity == null)
                {
                    return;
                }
                progressDialog = new ProgressDialog(activity);
            }
            progressDialog.SetMessage(message == null ? "" : message);
            progressDialog.Indeterminate = true;
            progressDialog.Show();
        }

        void DismissDialog()
        {
            if (progressDialog != null)
                progressDialog.Dismiss();
            progressDialog = null;
        }

        void ConnectNextClient()
        {
            int pendingClients = requestClients & ~connectedClients;
            if (pendingClients == 0)
            {
                SucceedSignIn();
                return;
            }

            ShowProgressDialog(true);

            // which client should be the next one to connect?
            if (gamesClient != null && (0 != (pendingClients & CLIENT_GAMES)))
            {
                //debugLog("Connecting GamesClient.");
                clientCurrentlyConnecting = CLIENT_GAMES;
            }
            else if (plusCient != null && (0 != (pendingClients & CLIENT_PLUS)))
            {
                //debugLog("Connecting PlusClient.");
                clientCurrentlyConnecting = CLIENT_PLUS;
            }
            else if (appStateClient != null && (0 != (pendingClients & CLIENT_APPSTATE)))
            {
                //debugLog("Connecting AppStateClient.");
                clientCurrentlyConnecting = CLIENT_APPSTATE;
            }
            else
            {
                throw new Exception("Not all clients connected, yet no one is next. R="
                        + requestClients + ", C=" + connectedClients);
            }

            ConnectCurrentClient();

        }

        void ConnectCurrentClient()
        {
            switch (clientCurrentlyConnecting)
            {
                case CLIENT_GAMES:
                    gamesClient.Connect();
                    break;
                case CLIENT_APPSTATE:
                    appStateClient.Connect();
                    break;
                case CLIENT_PLUS:
                    plusCient.Connect();
                    break;
            }
        }

        void KillConnections(int whatClients)
        {
            if ((whatClients & CLIENT_GAMES) != 0 && gamesClient != null
                    && gamesClient.IsConnected)
            {
                connectedClients &= ~CLIENT_GAMES;
                gamesClient.Disconnect();
            }
            if ((whatClients & CLIENT_PLUS) != 0 && plusCient != null
                    && plusCient.IsConnected)
            {
                connectedClients &= ~CLIENT_PLUS;
                plusCient.Disconnect();
            }
            if ((whatClients & CLIENT_APPSTATE) != 0 && appStateClient != null
                    && appStateClient.IsConnected)
            {
                connectedClients &= ~CLIENT_APPSTATE;
                appStateClient.Disconnect();
            }
        }

        public void ReconnectClients(int whatClients)
        {
            ShowProgressDialog(true);

            if ((whatClients & CLIENT_GAMES) != 0 && gamesClient != null
                    && gamesClient.IsConnected)
            {
                connectedClients &= ~CLIENT_GAMES;
                gamesClient.Reconnect();
            }
            if ((whatClients & CLIENT_APPSTATE) != 0 && appStateClient != null
                    && appStateClient.IsConnected)
            {
                connectedClients &= ~CLIENT_APPSTATE;
                appStateClient.Reconnect();
            }
            if ((whatClients & CLIENT_PLUS) != 0 && plusCient != null
                    && plusCient.IsConnected)
            {
                connectedClients &= ~CLIENT_PLUS;
                plusCient.Disconnect();
                plusCient.Connect();
            }
        }


        public void OnConnected(Android.OS.Bundle connectionHint)
        {
            //debugLog("onConnected: connected! client=" + mClientCurrentlyConnecting);

            // Mark the current client as connected
            connectedClients |= clientCurrentlyConnecting;

            // If this was the games client and it came with an invite, store it for
            // later retrieval.
            if (clientCurrentlyConnecting == CLIENT_GAMES && connectionHint != null)
            {
                //debugLog("onConnected: connection hint provided. Checking for invite.");
                IInvitation inv = (IInvitation)connectionHint.GetParcelable(GamesClient.ExtraInvitation);
                if (inv != null && inv.InvitationId != null)
                {
                    // accept invitation
                    //debugLog("onConnected: connection hint has a room invite!");
                    invitationId = inv.InvitationId;
                    //debugLog("Invitation ID: " + mInvitationId);
                }
            }
            // connect the next client in line, if any.
            ConnectNextClient();
        }

        void SucceedSignIn()
        {
            //debugLog("All requested clients connected. Sign-in succeeded!");
            signedIn = true;
            signInError = false;
            autoSignIn = true;
            userInitiatedSignIn = false;
            DismissDialog();
            if (listener != null)
            {
                listener.OnSignInSucceeded();
            }
        }

        public void OnConnectionFailed(ConnectionResult result)
        {
            // save connection result for later reference
            connectionResult = result;
            //debugLog("onConnectionFailed: result " + result.getErrorCode());
            DismissDialog();

            if (!userInitiatedSignIn)
            {
                // If the user didn't initiate the sign-in, we don't try to resolve
                // the connection problem automatically -- instead, we fail and wait
                // for the user to want to sign in. That way, they won't get an
                // authentication (or other) popup unless they are actively trying
                // to
                // sign in.
                //debugLog("onConnectionFailed: since user didn't initiate sign-in, failing now.");
                connectionResult = result;
                if (listener != null)
                {
                    listener.OnSignInFailed();
                }
                return;
            }

            //debugLog("onConnectionFailed: since user initiated sign-in, trying to resolve problem.");

            // Resolve the connection result. This usually means showing a dialog or
            // starting an Activity that will allow the user to give the appropriate
            // consents so that sign-in can be successful.
            ResolveConnectionResult();
        }

        void ResolveConnectionResult()
        {
            // Try to resolve the problem
            //debugLog("resolveConnectionResult: trying to resolve result: " + mConnectionResult);
            if (connectionResult.HasResolution)
            {
                // This problem can be fixed. So let's try to fix it.
                //debugLog("result has resolution. Starting it.");
                try
                {
                    // launch appropriate UI flow (which might, for example, be the
                    // sign-in flow)
                    expectingActivityResult = true;
                    connectionResult.StartResolutionForResult(activity, RC_RESOLVE);
                }
                catch (Exception e)
                {
                    // Try connecting again
                    //debugLog("SendIntentException.");
                    ConnectCurrentClient();
                }
            }
            else
            {
                // It's not a problem what we can solve, so give up and show an
                // error.
                //debugLog("resolveConnectionResult: result has no resolution. Giving up.");
                GiveUp();
            }
        }

        void GiveUp()
        {
            signInError = true;
            autoSignIn = false;
            DismissDialog();
            //debugLog("giveUp: giving up on connection. " +
            //        ((mConnectionResult == null) ? "(no connection result)" :
            //                ("Status code: " + mConnectionResult.getErrorCode())));

            Dialog errorDialog = null;
            if (connectionResult != null)
            {
                // get error dialog for that specific problem
                errorDialog = ErrorDialog(connectionResult.ErrorCode);
                errorDialog.Show();
                if (listener != null)
                {
                    listener.OnSignInFailed();
                }
            }
            else
            {
                // this is a bug
                //Log.e("GameHelper", "giveUp() called with no mConnectionResult");
            }
        }

        public void OnDisconnected()
        {
            connectionResult = null;
            autoSignIn = false;
            signedIn = false;
            signInError = false;
            invitationId = null;
            connectedClients = CLIENT_NONE;
            if (listener != null)
            {
                listener.OnSignInFailed();
            }
        }

        void IOnSignOutCompleteListener.OnSignOutComplete()
        {
            DismissDialog();
            if (gamesClient.IsConnected)
                gamesClient.Disconnect();
        }
    }
}