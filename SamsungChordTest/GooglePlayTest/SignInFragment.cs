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
using SignInButton = Android.Gms.Common.SignInButton;

namespace GooglePlayTest
{
    public class SignInFragment : Fragment
    {

        SignInButton _signInButton;
        //Button _signOutButton;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override View OnCreateView(LayoutInflater p0, ViewGroup p1, Bundle p2)
        {
            var view = p0.Inflate(Resource.Layout.Signin, p1, false);

            _signInButton = view.FindViewById<SignInButton>(Resource.Id.SignInButton);
            _signInButton.Click += delegate
            {
                ((BaseGameActivity)Activity).BeginUserInitiatedSignIn();
            };

            ((BaseGameActivity)Activity).RequestedClients = BaseGameActivity.CLIENT_PLUS;

            return view;
        }
    }
}