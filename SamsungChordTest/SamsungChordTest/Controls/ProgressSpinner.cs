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

namespace SamsungChordTest.Controls
{
    class ProgressSpinner : Dialog
    {
        public static ProgressSpinner Show(Context context, Java.Lang.ICharSequence title, Java.Lang.ICharSequence message)
        {
            return Show(context, title, message);
        }

        public static ProgressSpinner Show(Context context, Java.Lang.ICharSequence title, Java.Lang.ICharSequence message, bool indeterminate)
        {
            return Show(context, title, message, indeterminate);
        }

        public static ProgressSpinner Show(Context context, Java.Lang.ICharSequence title, Java.Lang.ICharSequence message, bool indeterminate, bool cancelable)
        {
            return Show(context, title, message, indeterminate, cancelable, null);
        }

        public static ProgressSpinner Show(Context context, Java.Lang.ICharSequence title, Java.Lang.ICharSequence message, bool indeterminate,
            bool cancelable, IDialogInterfaceOnCancelListener cancelListener)
        {
            ProgressSpinner dialog = new ProgressSpinner(context);
            dialog.SetCancelable(cancelable);
            dialog.SetOnCancelListener(cancelListener);
            /* The next line will add the ProgressBar to the dialog. */
            dialog.AddContentView(new ProgressBar(context), new LinearLayout.LayoutParams(LinearLayout.LayoutParams.WrapContent, LinearLayout.LayoutParams.WrapContent));
            dialog.Show();

            return dialog;
        }

        public ProgressSpinner(Context context)
            : base(context, Resource.Style.ProgressSpinner)
        {

        }
    }
}