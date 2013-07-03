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
using Com.Google.Android.Gms.Games.Multiplayer.Realtime;

namespace GooglePlayTest
{
    public class RoomFragment : Fragment
    {

        TextView _player1;
        TextView _player2;
        Button _ready;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override View OnCreateView(LayoutInflater p0, ViewGroup p1, Bundle p2)
        {
            var view = p0.Inflate(Resource.Layout.Room, null, false);

            _player1 = view.FindViewById<TextView>(Resource.Id.Player1);
            _player2 = view.FindViewById<TextView>(Resource.Id.Player2);

            _ready = view.FindViewById<Button>(Resource.Id.Ready);
            _ready.Click += delegate
            {
                
            };

            return view;
        }

        public void SetPlayer1(string player1)
        {
            _player1.Text = player1;
        }

        public void SetPlayer2(string player2)
        {
            _player2.Text = player2;
        }

    }
}