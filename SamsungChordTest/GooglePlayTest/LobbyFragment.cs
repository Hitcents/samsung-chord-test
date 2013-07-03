using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.Support.V4.App;
using Android.Gms.Games;
using Com.Google.Android.Gms.Games.Multiplayer.Realtime;
using Android.Gms.Games.MultiPlayer;

namespace GooglePlayTest
{
    public class LobbyFragment : Fragment
    {

        private const int FriendsIntent = 0;
        private const int InboxIntent = 1;

        RoomConfig _config;
        RoomConfig.Builder _builder;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override View OnCreateView(LayoutInflater p0, ViewGroup p1, Bundle p2)
        {
            var view = p0.Inflate(Resource.Layout.Lobby, p1, false);

            _builder = RoomConfig.InvokeBuilder((GameActivity)Activity);
            _builder.SetMessageReceivedListener((GameActivity)Activity);

            var autoMatch = view.FindViewById<Button>(Resource.Id.AutoMatch);
            autoMatch.Click += delegate
            {
                var am = RoomConfig.CreateAutoMatchCriteria(1, 1, 0);
                _builder.SetAutoMatchCriteria(am);
                _config = _builder.Build();
                ((GameActivity)Activity).GamesClient.CreateRoom(_config);
                Activity.Window.AddFlags(WindowManagerFlags.KeepScreenOn);
                autoMatch.Enabled = false;
            };

            var hostGame = view.FindViewById<Button>(Resource.Id.HostGame);
            hostGame.Click += delegate
            {
                var intent = ((GameActivity)Activity).GamesClient.GetSelectPlayersIntent(1, 1);
                StartActivityForResult(intent, FriendsIntent);
            };

            var joinGame = view.FindViewById<Button>(Resource.Id.JoinGame);
            joinGame.Click += delegate
            {
                var intent = ((GameActivity)Activity).GamesClient.InvitationInboxIntent;
                StartActivityForResult(intent, InboxIntent);
            };

            return view;
        }

        public override void OnActivityResult(int request, int response, Intent data)
        {


            if (request == FriendsIntent)
            {
                if (response == (int)Result.Ok)
                {
                    var players = ((Java.Util.ArrayList) data.Extras.Get(GamesClient.ExtraPlayers)).ToArray();

                    var invites = players.Select(x => x.ToString()).ToList();

                    _builder.AddPlayersToInvite(invites);

                    _config = _builder.Build();
                    ((GameActivity)Activity).GamesClient.CreateRoom(_config);
                    Activity.Window.AddFlags(WindowManagerFlags.KeepScreenOn);
                }
            }

            if (request == InboxIntent)
            {
                if (response == (int)Result.Ok)
                {
                    var invitation = (IInvitation)data.Extras.Get(GamesClient.ExtraInvitation);

                    ((GameActivity)Activity).AcceptInvite(invitation.InvitationId);
                }
            }

        }

    }
}