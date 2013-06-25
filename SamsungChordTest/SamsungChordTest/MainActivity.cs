using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace SamsungChordTest
{
    [Activity(Label = "SamsungChordTest", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            var send = FindViewById<Button>(Resource.Id.Send);
            var host = FindViewById<Button>(Resource.Id.Host);
            var connect = FindViewById<Button>(Resource.Id.Connect);

            var listView = FindViewById<Button>(Resource.Id.dataList);

            var text = FindViewById<EditText>(Resource.Id.sendText);
        }
    }
}

