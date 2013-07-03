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
using Android.Support.V4.App;
using Android.Gms.Games;
using Android.Gms.AppStates;
using Android.Gms.Common;
using Android.Gms.Games;
using Android.Gms.Plus;

namespace GooglePlayTest
{
    public abstract class BaseGameActivity : FragmentActivity, GameHelper.GameHelperListener
    {

        protected GameHelper helper;

        public const int CLIENT_GAMES = GameHelper.CLIENT_GAMES;
        public const int CLIENT_APPSTATE = GameHelper.CLIENT_APPSTATE;
        public const int CLIENT_PLUS = GameHelper.CLIENT_PLUS;
        public const int CLIENT_ALL = GameHelper.CLIENT_ALL;

        protected int requestedClients = CLIENT_GAMES;
        public int RequestedClients
        {
            get
            {
                return requestedClients;
            }
            set
            {
                requestedClients = value;
            }
        }

        protected BaseGameActivity()
            :base()
        {
            helper = new GameHelper(this);
        }

        protected BaseGameActivity(int requestedClients)
            :base()
        {
            RequestedClients = requestedClients;
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            helper = new GameHelper(this);
            helper.Setup(this, requestedClients);
        }

        protected override void OnStart()
        {
            base.OnStart();
            helper.OnStart(this);
        }

        protected override void OnStop()
        {
            base.OnStop();
            helper.OnStop();
        }

        protected override void OnActivityResult(int request, Result response, Intent data)
        {
            base.OnActivityResult(request, response, data);
            helper.OnActivityResult(request, (int)response, data);
        }

        public GamesClient GamesClient
        {
            get
            {
                return helper.GamesClient;
            }
        }

        public AppStateClient AppStateClient
        {
            get
            {
                return helper.AppStateClient;
            }
        }

        public PlusClient PlusClient
        {
            get
            {
                return helper.PlusClient;
            }
        }

        protected bool IsSignedIn
        {
            get
            {
                return helper.IsSignedIn;
            }
        }

        public void BeginUserInitiatedSignIn() {
            helper.BeginUserInitiatedSignIn() ;
        }

        public void SignOut() {
            helper.SignOut();
        }

        protected void ShowAlert(String title, String message) {
            helper.ShowAlert(title, message);
        }

        protected void ShowAlert(String message) {
            helper.ShowAlert(string.Empty, message);
        }

        protected void EnableDebugLog(bool enabled, String tag) {
            helper.EnableDebugLog(enabled, tag);
        }

        protected String InvitationId
        {
            get
            {
                return helper.InvitationId;
            }
        }

        protected void ReconnectClients(int whichClients) {
            helper.ReconnectClients(whichClients);
        }

        protected String Scopes
        {
            get
            {
                return helper.GetScopes();
            }
        }

        protected bool HasSignInError
        {
            get
            {
                return helper.HasSignInError;
            }
        }

        protected ConnectionResult SignInError
        {
            get
            {
                return helper.SignInError;
            }
        }

        protected void SetSignInMessages(String signingInMessage, String signingOutMessage) {
            helper.SigningInMessage = signingInMessage;
            helper.SigningOutMessage = signingOutMessage;
        }

        public virtual void OnSignInFailed()
        {
            throw new NotImplementedException();
        }

        public virtual void OnSignInSucceeded()
        {
            throw new NotImplementedException();
        }
    }
}