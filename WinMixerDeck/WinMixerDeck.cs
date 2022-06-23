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
        Dictionary<string, PluginKey> keys = new Dictionary<string, PluginKey>();

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
            core.LogMessage("Got settings");

            context = e.context;
            PluginKey keySettings;

            try
            {

                keySettings = JsonConvert.DeserializeObject<PluginKey>(e.payload2.general[context].ToString());
                keySettings.coordinates = e.payload2.coordinates;

                if (keys.ContainsKey(context))
                {
                    keys[context] = keySettings;
                }
                else
                {
                    keys.Add(context, keySettings);
                }

                var f = (keySettings.keyFunction == "volUp") ? "up" : "down";
                core.setTitle(keys[context].coordinates.row + $"\n{f}", e.context);

            } catch
            {

            }

        }

        private void Core_SendToPluginEvent(object sender, SendToPlugin e)
        {
            core.LogMessage("PI send to plugin");
        }

        private void Core_WillAppearEvent(object sender, WillAppear e)
        {
            core.LogMessage("Context appeared: " + e.context);

            core.getSettings(e.context);

            try
            {
                core.setTitle(keys[e.context].appName, e.context);
            } catch
            {
                core.LogMessage("No application registered to key yet...");
            }

        }

        private void Core_PropertyInspectorAppearedEvent(object sender, PropertyInspectorDidAppear e)
        {

            core.LogMessage("PI Appeared");

            var sessions = new AudioSessionManager();
            List<AudioSession> x = sessions.getAudioSessions();

            List<string> names = new List<string>();

            foreach (var session in x) {
                names.Add(session.Name);
            }

            core.sendToPI(new Payload(names), e.context);

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
