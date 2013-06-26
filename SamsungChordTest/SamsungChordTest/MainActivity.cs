using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using SamsungChordTest.Controls;
using System.Collections.Generic;

namespace SamsungChordTest
{
    [DataContract]
    public class TestMessage
    {
        [DataMember]
        public string Text { get; set; }
    }

    [Activity(Label = "SamsungChordTest", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        private MultiplayerService _service;
        private ProgressSpinner _progress;

        public MainActivity()
        {
            _service = new SamsungMultiplayerService(this)
            {
                MessageTypes = new Dictionary<string, Type>
                {
                    { "T", typeof(TestMessage) },
                },
            };
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            var context = TaskScheduler.FromCurrentSynchronizationContext();

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            var send = FindViewById<Button>(Resource.Id.Send);
            var host = FindViewById<Button>(Resource.Id.Host);
            var connect = FindViewById<Button>(Resource.Id.Connect);
            var listView = FindViewById<ListView>(Resource.Id.dataList);
            var text = FindViewById<EditText>(Resource.Id.sendText);
            var adapter = new MessageAdapter();
            listView.Adapter = adapter;

            host.Click += (sender, e) =>
            {
                _progress = ProgressSpinner.Show(this, null, null, true, false);

                _service.Host(new MultiplayerGame
                {
                    Name = Build.Manufacturer.Trim() + " " + Build.Model.Trim(),
                })
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        ShowPopUp("Error", t.Exception.InnerExceptions.First().Message);
                    }
                    else
                    {
                        ShowPopUp("Success", "You have hosted a game.");
                    }

                }, context);
            };

            connect.Click += (sender, e) =>
            {
                _progress = ProgressSpinner.Show(this, null, null, true, false);
                _service.FindGames()
                    .ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                        {
                            ShowPopUp("Error", t.Exception.InnerExceptions.First().Message);
                            return;
                        }

                        AlertDialog dialog = null;
                        var games = t.Result.Select(g => g.Name).ToArray();
                        var builder = new AlertDialog.Builder(this);
                        builder.SetTitle("Select Connection");
                        builder.SetSingleChoiceItems(games, -1, (s, o) =>
                        {
                            var game = t.Result.ElementAtOrDefault(o.Which);
                            if (game != null)
                            {
                                _progress = ProgressSpinner.Show(this, null, null, true, false);
                                dialog.Dismiss();
                                _service.Join(game).ContinueWith(c =>
                                {
                                    if (c.IsFaulted)
                                    {
                                        ShowPopUp("Error", c.Exception.InnerExceptions.First().Message);
                                    }
                                    else
                                    {
                                        ShowPopUp("Success", "You have connected to the game.");
                                    }
                                }, context);
                            }
                        });
                        dialog = builder.Create();
                        dialog.Show();
                        _progress.Dismiss();
                    }, context);
            };

            send.Click += (sender, e) =>
            {
                var message = new TestMessage { Text = text.Text };

                _service.Send("T", message).ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        ShowPopUp("Error", t.Exception.InnerExceptions.First().Message);
                        return;
                    }

                    adapter.Logs.Add("SENT: " + message.Text);
                    adapter.NotifyDataSetChanged();

                }, context);
            };

            _service.Received += (sender, e) =>
            {
                var message = e.Message as TestMessage;
                adapter.Logs.Add("RECEIVED: " + message.Text);
                adapter.NotifyDataSetChanged();
            };
        }

        protected override void OnStop()
        {
            base.OnStop();

            _service.Stop();
        }

        private void ShowPopUp(string title, string message)
        {
            if (_progress != null && _progress.IsShowing)
            {
                _progress.Dismiss();
            }

            var dialog = new AlertDialog.Builder(this)
                .SetTitle(title)
                .SetMessage(message)
                .SetPositiveButton("OK", (sender, e) => { })
                .Create();
            dialog.Show();
        }
    }
}

