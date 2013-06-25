using System;
using System.Linq;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using System.Threading.Tasks;
using SamsungChordTest.Controls;

namespace SamsungChordTest
{
    [Activity(Label = "SamsungChordTest", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        private MultiplayerService _service;
        private ProgressSpinner _progress;

        public MainActivity()
        {
            _service = new MultiplayerService();
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

            connect.Click += (sender, e) =>
                {
                    _progress = ProgressSpinner.Show(this, null, null, true, false);
                    _service.FindGames()
                        .ContinueWith(t =>
                        {
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
                                    _service.Connect(game).ContinueWith(c =>
                                        {
                                            if (c.IsFaulted)
                                            {
                                                Console.Write(c.Exception);
                                                ShowPopUp("Error", c.Exception.Message);
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
        }

        private void ShowPopUp(string title, string message)
        {
            if(_progress != null && _progress.IsShowing)
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

