using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

using Android.Gms.AppStates;
using Android.Gms.Common;
using Android.Gms.Games;
using Android.Gms.Games.MultiPlayer;
using Android.Gms.Plus;
using SignInButton = Android.Gms.Common.SignInButton;
using Android.Support.V4.App;
using Com.Google.Android.Gms.Games.Multiplayer.Realtime;
using System.Text;

namespace GooglePlayTest
{
    [Activity(Label = "GooglePlayServicesExampleProject", MainLauncher = true, Icon = "@drawable/icon")]
    public class GameActivity : BaseGameActivity, IRoomUpdateListener, IRealTimeMessageReceivedListener, IRealTimeReliableMessageSentListener
    {

        SignInFragment _signInFragment;
        public RoomFragment _roomFragment;
        GameFragment _gameFragment;
        FrameLayout _fragmentContainer;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main);

            _fragmentContainer = FindViewById<FrameLayout>(Resource.Id.FragmentContainer);

            _signInFragment = new SignInFragment();
            SupportFragmentManager.BeginTransaction()
                .Add(Resource.Id.FragmentContainer, new SignInFragment())
                .Commit();

        }

        protected override void OnResume()
        {
            base.OnResume();
        }

        public override void OnSignInSucceeded()
        {
            //_signInFragment.View.FindViewById<SignInButton>(Resource.Id.SignInButton).Visibility = ViewStates.Gone;
            //_signInFragment.View.FindViewById<Button>(Resource.Id.SignOutButton).Visibility = ViewStates.Visible;
            Toast.MakeText(this, "Signin Succeeded: entering lobby", ToastLength.Short).Show();
            SupportFragmentManager.BeginTransaction()
                .Replace(Resource.Id.FragmentContainer, new LobbyFragment())
                .Commit();
            //GamesClient.RegisterInvitationListener(this);
        }

        public override void OnSignInFailed()
        {
            //_signInFragment.View.FindViewById<SignInButton>(Resource.Id.SignInButton).Visibility = ViewStates.Visible;
            //_signInFragment.View.FindViewById<Button>(Resource.Id.SignOutButton).Visibility = ViewStates.Gone; ;
            Toast.MakeText(this, "Signin Failed", ToastLength.Short).Show();
        }
        
        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
        }

        public void OnJoinedRoom(int p0, IRoom p1)
        {
            Toast.MakeText(this, "On Joined Room", ToastLength.Short).Show();
            switch (p0)
            {
                case 0:
                    _roomFragment.SetPlayer1(p1.ParticipantIds[p0]);
                    break;
                case 1:
                    _roomFragment.SetPlayer2(p1.ParticipantIds[p0]);
                    break;
                default:
                    Toast.MakeText(this, p0.ToString(), ToastLength.Short).Show();
                    break;
            }
            /*
            _gameFragment = new GameFragment(p1);
            SupportFragmentManager.BeginTransaction()
                .Replace(Resource.Id.FragmentContainer,  _gameFragment)
                .Commit();
            */
        }

        public void OnLeftRoom(int p0, string p1)
        {
            Toast.MakeText(this, "On Left Room", ToastLength.Short).Show();
        }

        public void OnRoomConnected(int p0, IRoom room)
        {
            Toast.MakeText(this, "On Room Connected", ToastLength.Short).Show();
            for (int i = 0; i < room.ParticipantIds.Count; i++)
            {
                try
                {
                    switch (i)
                    {
                        case 0:
                            _roomFragment.SetPlayer1(room.Participants[0].Player.DisplayName);
                            break;
                        case 1:
                            _roomFragment.SetPlayer2(room.Participants[1].Player.DisplayName);
                            break;
                        default:
                            Toast.MakeText(this, p0, ToastLength.Short).Show();
                            break;
                    }
                }
                catch (Exception e)
                {

                }
            }


            //var builder = RoomConfig.InvokeBuilder(room.

            
            //GamesClient.JoinRoom(RoomConfig.InvokeBuilder(this).SetMessageReceivedListener(this).Build());

            _gameFragment = new GameFragment(room);
            SupportFragmentManager.BeginTransaction()
                .Replace(Resource.Id.FragmentContainer, _gameFragment)
                .Commit();
            

            //p1.Participants[0].Player

            //var game = new MultiplayerGame();



        }

        public void OnRoomCreated(int p0, IRoom room)
        {
            Toast.MakeText(this, "On Room Created", ToastLength.Short).Show();
            _roomFragment = new RoomFragment();
            SupportFragmentManager.BeginTransaction()
                .Replace(Resource.Id.FragmentContainer, _roomFragment)
                .Commit();
            
            for (int i = 0; i < room.ParticipantIds.Count; i++)
            {
                try
                {
                    switch (i)
                    {
                        case 0:
                            _roomFragment.SetPlayer1(room.Participants[0].Player.DisplayName);
                            break;
                        case 1:
                            _roomFragment.SetPlayer2(room.Participants[1].Player.DisplayName);
                            break;
                        default:
                            Toast.MakeText(this, p0, ToastLength.Short).Show();
                            break;
                    }
                }
                catch (Exception e)
                {

                }
            }
            /*
            _gameFragment = new GameFragment(room);
            SupportFragmentManager.BeginTransaction()
                .Replace(Resource.Id.FragmentContainer, _gameFragment)
                .Commit();
            */
        }

        
        public void AcceptInvite(string invitationId)
        {

            //auto accept
            _roomFragment = new RoomFragment();
            SupportFragmentManager.BeginTransaction()
                .Replace(Resource.Id.FragmentContainer, _roomFragment)
                .Commit();
            var builder = RoomConfig.InvokeBuilder(this);
            builder.SetInvitationIdToAccept(invitationId);
            builder.SetMessageReceivedListener(this);
            GamesClient.JoinRoom(builder.Build());
            Window.AddFlags(WindowManagerFlags.KeepScreenOn);
        }
        

        public void OnRealTimeMessageSent(int p0, int p1, string p2)
        {
            Toast.MakeText(this, "Message  Sent", ToastLength.Short).Show();
        }

        public void OnRealTimeMessageReceived(RealTimeMessage p0)
        {
            Toast.MakeText(this, "Message Received", ToastLength.Short).Show();
            _gameFragment.ReceiveMessage(p0.GetMessageData());
        }
    }
}