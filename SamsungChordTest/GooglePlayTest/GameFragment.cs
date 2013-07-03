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
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace GooglePlayTest
{
    public class GameFragment : Fragment
    {

        GamesClient _client;
        IRoom _room;
        TextView _chat;

        public GameFragment(IRoom room)
        {
            _room = room;
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            _client = ((GameActivity)Activity).GamesClient;
        }

        public override View OnCreateView(LayoutInflater p0, ViewGroup p1, Bundle p2)
        {
            var view = p0.Inflate(Resource.Layout.Game, p1, false);

            var message = view.FindViewById<EditText>(Resource.Id.Message);
            var send = view.FindViewById<Button>(Resource.Id.Send);
            _chat = view.FindViewById<TextView>(Resource.Id.MessageBox);

            send.Click += delegate
            {

                string myId = string.Empty;
                foreach (var player in _room.Participants)
                {
                    if (player.DisplayName.Equals(((GameActivity)Activity).GamesClient.CurrentPlayer.DisplayName))
                        myId = player.ParticipantId;
                }

                foreach (var player in _room.Participants)
                {
                    //dont send message to self
                    var stampedMessage = new TimeStampedMessage
                    {
                        Message = message.Text,
                        TimeStamp = DateTime.Now,
                        ShouldEcho = true,
                        PlayerId = myId,
                    };
                    if( !player.DisplayName.Equals(((GameActivity)Activity).GamesClient.CurrentPlayer.DisplayName))
                    {
                        _client.SendReliableRealTimeMessage(
                            (GameActivity)Activity,
                            Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(stampedMessage).ToCharArray()),
                            _room.RoomId,
                            player.ParticipantId);
                    }
                }
                /*_chat.Text += "You: " + message.Text + "\n";
                message.Text = "";*/
            };

            return view;
        }

        public void ReceiveMessage(byte[] s)
        {
            TimeStampedMessage stampedMessage = JsonConvert.DeserializeObject<TimeStampedMessage>(Encoding.Unicode.GetString(s));
            if (stampedMessage.ShouldEcho)
            {
                _chat.Text += "Them: " + stampedMessage.Message + "\n";
                stampedMessage.ShouldEcho = false;
                _client.SendReliableRealTimeMessage((GameActivity)Activity, Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(stampedMessage).ToCharArray()), _room.RoomId, stampedMessage.PlayerId);
            }
            else 
            {
                _chat.Text += "You: " + stampedMessage.Message + " Ping: " + DateTime.Now.Subtract(stampedMessage.TimeStamp).ToString() + "\n";
            }
        }
    }
}