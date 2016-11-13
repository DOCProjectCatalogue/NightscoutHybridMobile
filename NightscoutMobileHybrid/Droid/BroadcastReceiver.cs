using System;
using System.Collections.Generic;
using System.Text;
using Android.App;
using Android.Content;
using Android.Util;
using Gcm.Client;
using WindowsAzure.Messaging;
using NightscoutMobileHybrid.Droid;
using Xamarin.Forms;

[assembly: Permission(Name = "@PACKAGE_NAME@.permission.C2D_MESSAGE")]
[assembly: UsesPermission(Name = "@PACKAGE_NAME@.permission.C2D_MESSAGE")]
[assembly: UsesPermission(Name = "com.google.android.c2dm.permission.RECEIVE")]

//GET_ACCOUNTS is needed only for Android versions 4.0.3 and below
[assembly: UsesPermission(Name = "android.permission.GET_ACCOUNTS")]
[assembly: UsesPermission(Name = "android.permission.INTERNET")]
[assembly: UsesPermission(Name = "android.permission.WAKE_LOCK")]

[BroadcastReceiver(Permission = Gcm.Client.Constants.PERMISSION_GCM_INTENTS)]
[IntentFilter(new string[] { Gcm.Client.Constants.INTENT_FROM_GCM_MESSAGE }, Categories = new string[] { "@PACKAGE_NAME@" })]
[IntentFilter(new string[] { Gcm.Client.Constants.INTENT_FROM_GCM_REGISTRATION_CALLBACK }, Categories = new string[] { "@PACKAGE_NAME@" })]
[IntentFilter(new string[] { Gcm.Client.Constants.INTENT_FROM_GCM_LIBRARY_RETRY }, Categories = new string[] { "@PACKAGE_NAME@" })]
public class BroadcastReceiver : GcmBroadcastReceiverBase<PushHandlerService>
{
    public static string[] SENDER_IDS = new string[] { NightscoutMobileHybrid.Constants.SenderID };

    //TODO: Use URL without the http://?
    public const string TAG = "nstest-server.azurewebsites.net";
}

[Service] // Must use the service tag
public class PushHandlerService : GcmServiceBase
{
    public static string RegistrationID { get; private set; }
    private NotificationHub Hub { get; set; }

    public PushHandlerService() : base(BroadcastReceiver.SENDER_IDS)
    {

    }

    protected override void OnMessage(Context context, Intent intent)
    {
        var msg = new StringBuilder();

        if (intent != null && intent.Extras != null)
        {
            foreach (var key in intent.Extras.KeySet())
                msg.AppendLine(key + "=" + intent.Extras.Get(key).ToString());
        }

        string messageText = intent.Extras.GetString("message");
        if (!string.IsNullOrEmpty(messageText))
        {
            createNotification("New hub message!", messageText);
        }
        else
        {
            createNotification("Unknown message details", msg.ToString());
        }
    }

    protected override void OnError(Context context, string errorId)
    {
        Log.Error(BroadcastReceiver.TAG, "GCM Error: " + errorId);
    }

    protected override void OnRegistered(Context context, string registrationId)
    {
        RegistrationID = registrationId;

        Hub = new NotificationHub(NightscoutMobileHybrid.Constants.NotificationHubPath, NightscoutMobileHybrid.Constants.ConnectionString, context);

        try
        {
            Hub.UnregisterAll(registrationId);
        }
        catch (Exception ex)
        {
            Log.Error(BroadcastReceiver.TAG, ex.Message);
        }

        //TODO: Get from URL
        var tags = new List<string>() { BroadcastReceiver.TAG };

        try
        {
            const string templateBodyGCM = "{\"data\":{\"message\":\"$(message)\",\"eventName\":\"$(eventName)\",\"group\":\"$(group)\",\"key\":\"$(key)\",\"level\":\"$(level)\",\"sound\":\"$(sound)\",\"title\":\"$(title)\"}}";

            var hubRegistration = Hub.RegisterTemplate(registrationId, "nightscout", templateBodyGCM, tags.ToArray());
        }
        catch (Exception ex)
        {
            Log.Error(BroadcastReceiver.TAG, ex.Message);
        }
    }

    protected override void OnUnRegistered(Context context, string registrationId)
    {
        createNotification("GCM Unregistered...", "The device has been unregistered!");
    }

    void createNotification(string title, string desc)
    {
        //Create notification
        var notificationManager = GetSystemService(Context.NotificationService) as NotificationManager;

        //Create an intent to show UI
        var uiIntent = new Intent(this, typeof(MainActivity));

        //Create the notification
        var notification = new Notification(Android.Resource.Drawable.SymActionEmail, title);

        //Auto-cancel will remove the notification once the user touches it
        notification.Flags = NotificationFlags.AutoCancel;

        //Set the notification info
        //we use the pending intent, passing our ui intent over, which will get called
        //when the notification is tapped.
        //notification.SetLatestEventInfo(Forms.Context, title, desc, PendingIntent.GetActivity(this, 0, uiIntent, 0));

        //Show the notification
        notificationManager.Notify(1, notification);
        dialogNotify(title, desc);
    }

    protected void dialogNotify(String title, String message)
    {
        var ctx = Forms.Context;
        Xamarin.Forms.Device.BeginInvokeOnMainThread(() => {
            AlertDialog.Builder dlg = new AlertDialog.Builder(ctx);
            AlertDialog alert = dlg.Create();
            alert.SetTitle(title);
            alert.SetButton("Ok", delegate {
                alert.Dismiss();
            });
            alert.SetMessage(message);
            alert.Show();
        });
    }
}