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

namespace SamsungChordTest
{
    public class MessageAdapter : BaseAdapter
    {
        public MessageAdapter()
        {
            Logs = new List<string>();
        }

        public List<string> Logs { get; set; }

        public override int Count
        {
            get { return Logs.Count; }
        }

        public override Java.Lang.Object GetItem(int position)
        {
            return Logs[position];
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            if (convertView == null)
            {
                convertView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.datalayout, null);
            }

            var text = convertView.FindViewById<TextView>(Resource.Id.textRow);
            text.Text = Logs[position];

            return convertView;
        }
    }
}