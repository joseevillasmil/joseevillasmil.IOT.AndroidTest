using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using joseevillasmil.IOT.AndroidTest.IOT;
using Azure.Data.Tables;
using System.Collections.Generic;

namespace joseevillasmil.IOT.AndroidTest
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        const string queueStr = "Queue Shared Key";
        const string queueName = "Queue Name";
        const string _endpoint = "http://192.168.1.10:8100/?command={device}|{state}";
        const string storageKey = "Storage Shared Key";
        private IotQueueFunctions iotQueueFunctions;
        string method = "cloud";
        string token = "";
        List<Device> devices = new List<Device>() { 
            new Device()
            {
                PartitionKey = "HOME",
                RowKey = "LuzCuarto",
                Status = "Off"
            }
        };
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Button button1 = FindViewById<Button>(Resource.Id.button1);
            button1.Click += EncenderApagar;

            ThreadPool.QueueUserWorkItem(d => {
                    Handler handler = new Handler(Looper.MainLooper);
                    Action action = async  () =>
                    {
                        try
                        {
                            using (HttpClient httpClient = new HttpClient())
                            {
                                httpClient.Timeout = TimeSpan.FromSeconds(2);
                                var response = httpClient.GetAsync(_endpoint).GetAwaiter().GetResult();
                                string responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                                if (!String.IsNullOrEmpty(responseBody))
                                {
                                    method = "local";
                                    token += Auth.GenerateToken("admin");

                                    button1.Enabled = true;
                                }
                            }
                        }
                        catch (Exception e)
                        {

                        }
                    };
                    handler.Post(action);
            });
            ThreadPool.QueueUserWorkItem(d => {
                Handler handler = new Handler(Looper.MainLooper);
                Action action = async () =>
                {
                    try
                    {
                        if (iotQueueFunctions == null) iotQueueFunctions = new IotQueueFunctions(queueStr, queueName);
                        button1.Enabled = true;
                        iotQueueFunctions.SendMessage("TestControl|Control");
                    }
                    catch (Exception f)
                    {
                    }
                };
                handler.Post(action);
            });
            ThreadPool.QueueUserWorkItem(d => {
                Handler handler = new Handler(Looper.MainLooper);
                Action action = async () =>
                {
                    try
                    {
                        TableClient stclient = new TableClient(storageKey, "Devices");
                        for(int i = 0; i < devices.Count; i++)
                        {
                            devices[i] = stclient.GetEntity<Device>(devices[i].PartitionKey, devices[i].RowKey);
                            if (String.IsNullOrEmpty(devices[i].Status)) devices[i].Status = "Off";
                            if (String.Equals(devices[i].Status, "On"))
                            {
                                button1.SetBackgroundColor(Android.Graphics.Color.LightGreen);
                                button1.Text = "Apagar";
                            }
                            if (String.Equals(devices[i].Status, "Off"))
                            {
                                button1.SetBackgroundColor(Android.Graphics.Color.LightGray);
                                button1.Text = "Encencer";
                            }
                        }
                    }
                    catch (Exception f)
                    {
                    }
                };
                handler.Post(action);
            });


        }


        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }


        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private void EncenderApagar(object sender, EventArgs eventArgs)
        {
            string command = "";
            Button button1 = FindViewById<Button>(Resource.Id.button1);
            switch(devices[0].Status)
            {
                case "Off":
                    command = "On";
                    button1.SetBackgroundColor(Android.Graphics.Color.LightGreen);
                    button1.Text = "Apagar";
                    devices[0].Status = "On";
                    break;

                case "On":
                    command = "Off";
                    button1.SetBackgroundColor(Android.Graphics.Color.LightGray);
                    button1.Text = "Encencer";
                    devices[0].Status = "Off";
                    break;
            }

            if (String.Equals(method, "cloud"))
            {
                if (iotQueueFunctions == null) iotQueueFunctions = new IotQueueFunctions(queueStr, queueName);
                iotQueueFunctions.SendMessage($"{devices[0].RowKey}|{command}");
            }

            if (String.Equals(method, "local"))
            {
                try
                {
                    string url = _endpoint.Replace("{device}", devices[0].RowKey).Replace("{state}", command);
                    using (HttpClient client = new HttpClient())
                    {
                        using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, url))
                        {
                            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                            HttpResponseMessage response = client.SendAsync(requestMessage).GetAwaiter().GetResult();
                            string responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        }

                    }

                }
                catch (Exception e)
                {

                }
            }
        }
    }
}
