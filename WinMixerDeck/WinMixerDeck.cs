using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StreamDeckCS;
using StreamDeckCS.EventsReceived;
using System.Timers;
using WinMixerCoreConsoleV2;
using WinMixerCoreConsoleV2.models;
using WinMixerDeck.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WinMixerDeck
{
    public class WinMixerDeck
    {

        StreamdeckCore core;
        bool pressed = false;
        Timer myTimer;
        string context;
        int i = 0;
        float interval = 0.05f;
        Dictionary<string, KeyWrapper> keys = new Dictionary<string, KeyWrapper>();

        public WinMixerDeck(string[] args)
        {
            core = new StreamdeckCore(args);
            myTimer = new Timer();

            core.KeyUpEvent += Core_KeyUpEvent;
            core.KeyDownEvent += Core_KeyDownEvent;
            core.PropertyInspectorAppearedEvent += Core_PropertyInspectorAppearedEvent;
            core.WillAppearEvent += Core_WillAppearEvent;
            core.WillDisappearEvent += Core_WillDisappearEvent;
            core.SendToPluginEvent += Core_SendToPluginEvent;
            core.DidReceiveSettingsEvent += Core_DidReceiveSettingsEvent;

            myTimer.Elapsed += MyTimer_Elapsed;

        }

        private void Core_WillDisappearEvent(object sender, WillDisappear e)
        {
            core.LogMessage("Context disappeared: " + e.context);

            // remove not visible keys from local cache. This squashes the bug related to
            // multiple apps being assigned to the same grid slot when they are in separate folders.
            keys.Remove(e.context);

        }

        // Gets settings, stores them in dictionary, sets title of key to name of app it is controlling
        private void Core_DidReceiveSettingsEvent(object sender, DidReceiveSettings e)
        {
            core.LogMessage("Got settings. Action: " + e.action);

            context = e.context;

            try
            {
                //NewMethod();

                switch (e.action)
                {
                    case "com.mbranvall.winmixerdeck.applicationpicker":

                        PluginKey keySettings = JsonConvert.DeserializeObject<PluginKey>(e.payload2.general[context].ToString());
                        core.LogMessage(JsonConvert.SerializeObject(e));

                        core.LogMessage("App name in settings: " + keySettings.appName);
                        keySettings.coordinates = e.payload2.coordinates;

                        if (keys.ContainsKey(context))
                        {
                            keys[context] = keySettings;
                        }
                        else
                        {
                            keys.Add(context, keySettings);
                        }

                        core.setTitle(keys[context].appName, context);
                        break;

                    case "com.mbranvall.winmixerdeck.volumecontroller":

                        PluginKey keySettings2 = JsonConvert.DeserializeObject<PluginKey>(e.payload2.general[context].ToString());
                        core.LogMessage(JsonConvert.SerializeObject(e));
                        keySettings2.coordinates = e.payload2.coordinates;

                        if (keys.ContainsKey(context))
                        {
                            keys[context] = keySettings2;
                        }
                        else
                        {
                            keys.Add(context, keySettings2);
                        }

                        var img = keySettings2.keyFunction == "volUp" ? "./assets/VOL_+.jpg" : "./assets/VOL_-.jpg";
                        core.setImage(img, context);

                        break;

                    default:
                        break;

                }

            }
            catch (Exception ex)
            {
                core.LogMessage("Caught error:  " + ex);
            }

        }

        private void Core_SendToPluginEvent(object sender, SendToPlugin e)
        {
            core.LogMessage("PI sent to plugin: " + e.context);

            switch(e.action)
            {
                case "com.mbranvall.winmixerdeck.volumecontroller":

                    PluginKey pluginKey = JsonConvert.DeserializeObject<PluginKey>(e.payload["payload"].ToString());

                    core.LogMessage(JsonConvert.SerializeObject(pluginKey).ToString());

                    if (pluginKey.keyFunction == "volUp")
                    {
                        // get app name of key directly below it
                        foreach(var key in keys)
                        {                            

                            if ((key.Value.coordinates.row == pluginKey.coordinates.row + 1) && (key.Value.coordinates.column == pluginKey.coordinates.column))
                            {
                                core.LogMessage(key.Value.appName);
                                var x = new { appName = key.Value.appName, messageType = "associatedApplication" };
                                core.LogMessage(JsonConvert.SerializeObject(x));
                                core.sendToPI(JObject.FromObject(x), context);
                            }
                        }

                    } else
                    {
                        // get app name of key directly above it
                        foreach (var key in keys)
                        {
                            if ((key.Value.coordinates.row == pluginKey.coordinates.row - 1) && (key.Value.coordinates.column == pluginKey.coordinates.column))
                            {
                                core.LogMessage(key.Value.appName);
                                var x = new { appName = key.Value.appName, messageType = "associatedApplication" };
                                core.sendToPI(JObject.FromObject(x), context);
                            }
                        }
                    }

                    break;
            }

        }

        private void Core_WillAppearEvent(object sender, WillAppear e)
        {
            core.LogMessage("Context appeared: " + e.context);

            core.getSettings(e.context);

            switch(e.action)
            {
                case "com.mbranvall.winmixerdeck.applicationpicker":
                    try
                    {
                        //core.setTitle(keys[e.context].appName, e.context);
                    }
                    catch
                    {
                        core.LogMessage("No application registered to key yet...");
                    }
                    break;

                default:
                    break;

            }



        }

        private void Core_PropertyInspectorAppearedEvent(object sender, PropertyInspectorDidAppear e)
        {

            core.LogMessage("PI Appeared. Action: " + e.action);
            //List<string> names = new List<string>();
            //var sessions = new AudioSessionManager();
            //List<AudioSession> x = sessions.getAudioSessions();

            //x.ForEach(session => names.Add(session.Name));
            //core.sendToPI(new Payload(names), e.context);

            switch (e.action)
            {
                case "com.mbranvall.winmixerdeck.applicationpicker":

                    // send audio session names to applicationpicker action

                    List<string> names = new List<string>();
                    var sessions = new AudioSessionManager();
                    List<AudioSession> x = sessions.getAudioSessions();

                    x.ForEach(session => names.Add(session.Name));
                    core.sendToPI(JObject.FromObject(new Payload(names)), e.context);

                    break;

                case "com.mbranvall.winmixerdeck.volumecontroller":

                    core.LogMessage(e.context);
                    //var y = new { appName = keys[e.context].appName };
                    //core.sendToPI(JObject.FromObject(y), e.context);

                    break;

                case "com.mbranvall.winmixerdeck.volumeinterval":
                    break;

                default:
                    core.LogMessage("Action not recognized");
                    break;
            }



        }

        private void MyTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            core.setTitle("Hold", context);
            core.LogMessage("Held button for 2 secs");
            myTimer.Stop();
        }

        private void Core_KeyDownEvent(object sender, KeyDown e)
        {
            myTimer.Interval = 2000;
            myTimer.Enabled = true;
            myTimer.AutoReset = false;
        }

        private void Core_KeyUpEvent(object sender, KeyUp e)
        {

            myTimer.Stop();

            if (keys.ContainsKey(e.context))
            {
                core.LogMessage("found key");
                var sessions = new AudioSessionManager();
                List<AudioSession> x = sessions.getAudioSessions();

                foreach (var session in x)
                {

                    if (session.Name == keys[e.context].appName)
                    {
                        try
                        {
                            core.LogMessage("adjusting volume");

                            if (keys[e.context].keyFunction == "volUp")
                            {
                                sessions.adjustVolume(session, sessions.getVolumeLevel(session) + interval);
                            }
                            else
                            {
                                sessions.adjustVolume(session, sessions.getVolumeLevel(session) - interval);
                            }
                        }
                        catch
                        {

                        }

                        break;
                    }
                }
            }

            //var sessions = new WinMixerCoreConsoleV2.AudioSessionManager();
            //List<AudioSession> x = sessions.getAudioSessions();

            //List<string> names = new List<string>();

            //foreach (var session in x)
            //{
            //    names.Add(session.Name);
            //}

            //core.sendToPI(new Payload(names), e.context);

            //var sessions = new WinMixerCoreConsoleV2.AudioSessionManager();

            //List<AudioSession> x = sessions.getAudioSessions();

            //var msg = x[i++].Name;

            //core.setTitle(msg, e.context);

            //core.LogMessage($"set title to: {msg}! Process name");
            //pressed = !pressed;
            //myTimer.Stop();

        }

        public async Task Start()
        {
            await core.Start();
        }
    }
}
