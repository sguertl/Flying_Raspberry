﻿using Android.App;
using Android.Widget;
using Android.OS;
using Android.Content;
using System.Threading;
using Android.Net.Wifi;
using Android.Runtime;
using System.Collections.Generic;
using System.Linq;
using Android.Graphics;
using Android.Views;

namespace WiFiDronection
{
    [Activity(MainLauncher = true, Icon = "@drawable/icon", Theme = "@android:style/Theme.Holo.Light.NoActionBar.Fullscreen")]
    public class MainActivity : Activity
    {
        private TextView mTvHeader;
        private TextView mTvWifiName;
        private TextView mTvWifiMac;
        private TextView mTvFooter;
        private Button mBtnConnect;
        private Button mBtnHelp;
        private ListView mLvPeer;

        private ArrayAdapter<Peer> mAdapter;
        private List<Peer> mPeerList;
        private string mSelectedSsid;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main);

            var font = Typeface.CreateFromAsset(Assets, "SourceSansPro-Light.ttf");
            //mTvHeader.Typeface = font;

            mTvHeader = FindViewById<TextView>(Resource.Id.tvHeader);
            mTvWifiName = FindViewById<TextView>(Resource.Id.tvWifiName);
            mTvWifiMac = FindViewById<TextView>(Resource.Id.tvWifiMac);
            mTvFooter = FindViewById<TextView>(Resource.Id.tvFooter);
            mBtnConnect = FindViewById<Button>(Resource.Id.btnConnect);
            mBtnHelp = FindViewById<Button>(Resource.Id.btnHelp);

            mTvHeader.Typeface = font;
            mTvWifiName.Typeface = font;
            mTvWifiMac.Typeface = font;
            mTvFooter.Typeface = font;
            mBtnConnect.Typeface = font;
            mBtnHelp.Typeface = font;

            mBtnConnect.Click += OnConnect;

            //mLvPeer = FindViewById<ListView>(Resource.Id.lvPeers);
            //mLvPeer.ItemClick += OnListViewItemClick;

            mPeerList = new List<Peer>();

            WifiManager wm = GetSystemService(WifiService).JavaCast<WifiManager>();
            if(wm.IsWifiEnabled == false)
            {
                wm.SetWifiEnabled(true);
            }
            RefreshWifiList();

        }

        private void RefreshWifiList()
        {
            var wifiManager = GetSystemService(WifiService).JavaCast<WifiManager>();
            wifiManager.StartScan();

            ThreadPool.QueueUserWorkItem(lol =>
            {
                while (true)
                {
                    Thread.Sleep(3000);
                    var wifiList = wifiManager.ScanResults;

                    if (mAdapter == null)
                    {
                        mAdapter = new ArrayAdapter<Peer>(this, Android.Resource.Layout.SimpleListItem1, Android.Resource.Id.Text1);
                        //RunOnUiThread(() => mLvPeer.Adapter = mAdapter);
                    }
                    
                    IEnumerable<ScanResult> results = wifiList.Where(w => w.Ssid.ToUpper().Contains("RPI") || w.Ssid.ToUpper().Contains("RASPBERRY"));

                    foreach (var wifi in results)
                    {
                        var wifi1 = wifi;
                        RunOnUiThread(() =>
                        {
                            if(mPeerList.Any(p => p.SSID == wifi1.Ssid) == false)
                            {
                                Peer p = new Peer { SSID = wifi1.Ssid, BSSID = wifi1.Bssid, Encryption = wifi1.Capabilities };
                                
                                mSelectedSsid = p.SSID;
                                mTvWifiName.Text = "SSID: " + p.SSID;
                                mTvWifiMac.Text = "MAC: " + p.BSSID;

                                mAdapter.Add(p);
                                mPeerList.Add(p);
                            }
                        });
                    }

                    RunOnUiThread(() => mAdapter.NotifyDataSetChanged());
                }
            });
        }

        /*private void OnListViewItemClick(object sender, AdapterView.ItemClickEventArgs itemClickEventArgs)
        {
            var wifiItem = mAdapter.GetItem(itemClickEventArgs.Position);
            mSelectedSsid = wifiItem.SSID;
            OnCreateDialog(0).Show();
        }*/

        private void OnConnect(object sender, System.EventArgs e)
        {
            //var wifiItem = mAdapter.GetItem(itemClickEventArgs.Position);
            //mSelectedSsid = wifiItem.SSID;
            OnCreateDialog(0).Show();
        }

        protected override Dialog OnCreateDialog(int id)
        {
            var customView = LayoutInflater.Inflate(Resource.Layout.WifiDialog, null);
            var builder = new AlertDialog.Builder(this);

            builder.SetIcon(Android.Resource.Drawable.IcMenuPreferences);
            builder.SetView(customView);
            builder.SetTitle("Set Wifi password");
            builder.SetPositiveButton("OK", WpaOkClicked);
            builder.SetNegativeButton("Cancel", CancelClicked);

            return builder.Create();
        }

        private void WpaOkClicked(object sender, DialogClickEventArgs e)
        {
            var dialog = (AlertDialog)sender;
            var password = (EditText)dialog.FindViewById(Resource.Id.etDialogPassword);

            var conf = new WifiConfiguration();
            conf.Ssid = "\"" + mSelectedSsid + "\"";
            conf.PreSharedKey = "\"" + password.Text + "\"";

            var wifiManager = GetSystemService(WifiService).JavaCast<WifiManager>();

            int id = wifiManager.AddNetwork(conf);

            IList<WifiConfiguration> myWifi = wifiManager.ConfiguredNetworks;

            WifiConfiguration wc = myWifi.First(x => x.Ssid.Contains(mSelectedSsid));
            wifiManager.Disconnect();
            wifiManager.EnableNetwork(id, true);
            wifiManager.Reconnect();

            if (wifiManager.IsWifiEnabled)
            {
                StartActivity(typeof(ControllerActivity));
                // Go to next activity
            }
            else
            {
                Toast.MakeText(this, "Could not connect to peer", ToastLength.Short).Show();
            }
        }

        private void CancelClicked(object sender, DialogClickEventArgs e)
        {
            //
        }
    }
}
