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
            core.SendToPluginEvent += Core_SendToPluginEvent;
            core.DidReceiveSettingsEvent += Core_DidReceiveSettingsEvent;

            myTimer.Elapsed += MyTimer_Elapsed;

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

                        VolControlKey keySettings2 = JsonConvert.DeserializeObject<VolControlKey>(e.payload2.general[context].ToString());
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
                        core.LogMessage("Setting image: ");
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

        //private void NewMethod()
        //{
        //    if (keys.ContainsKey(context))
        //    {
        //        keys[context] = keySettings;
        //    }
        //    else
        //    {
        //        keys.Add(context, keySettings);
        //    }
        //}

        private void Core_SendToPluginEvent(object sender, SendToPlugin e)
        {
            core.LogMessage("PI send to plugin");
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
                    core.sendToPI(new Payload(names), e.context);

                    break;

                case "com.mbranvall.winmixerdeck.volumecontroller":

                    core.LogMessage(e.context);
                    core.sendToPI(new Payload(null), e.context);

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

            context = e.context;
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
