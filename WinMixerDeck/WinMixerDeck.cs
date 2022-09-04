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
using WinMixerDeck.Helpers;
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
        KeyDown keyHeldDown;
        int i = 0;
        float interval = 0.05f;
        Dictionary<string, KeyWrapper> keys = new Dictionary<string, KeyWrapper>();

        enum KEY_FUNCTION { VOL_UP, VOL_DOWN };

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

            string contextRef;

            context = e.context;

            try
            {
                //NewMethod();

                switch (e.action)
                {
                    case "com.mbranvall.winmixerdeck.applicationpicker":

                        PluginKey keySettings = null;


                        // will throw exception when new key is added
                        try
                        {
                            keySettings = JsonConvert.DeserializeObject<PluginKey>(e.payload2.general[context].ToString());

                        } catch (Exception ex)
                        {
                            keySettings = new PluginKey();
                            keySettings.appName = "No app";
                            keySettings.coordinates = e.payload2.coordinates;
                        }

                        keySettings.coordinates = e.payload2.coordinates;

                        if (keys.ContainsKey(context))
                        {
                            keys[context] = keySettings;
                        }
                        else
                        {
                            keys.Add(context, keySettings);
                        }

                        try 
                        {
                            core.LogMessage("Searching for volume up key..");
                            if (this._hasVolumeKey(KEY_FUNCTION.VOL_UP, keySettings.coordinates, out contextRef))
                            {
                                this._updateVolumeKey(contextRef, keySettings.appName);
                            }

                            core.LogMessage("Searching for volume down key..");
                            if (this._hasVolumeKey(KEY_FUNCTION.VOL_DOWN, keySettings.coordinates, out contextRef))
                            {
                                this._updateVolumeKey(contextRef, keySettings.appName);
                            }

                        } catch(Exception ex)
                        {
                            core.LogMessage("Volume key not found: " + ex);
                        }

                        core.LogMessage("Setting title: " + keys[context].appName);
                        core.setTitle(keys[context].appName, context);
                        break;

                    case "com.mbranvall.winmixerdeck.volumecontroller":

                        PluginKey keySettings2 = null;
                        
                        try
                        {
                            keySettings2 = JsonConvert.DeserializeObject<PluginKey>(e.payload2.general[context].ToString());
                        } catch (Exception ex)
                        {
                            keySettings2 = new PluginKey();
                            keySettings2.keyFunction = "volUp";
                            keySettings2.coordinates = e.payload2.coordinates;
                        }                        

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

        private bool _hasVolumeKey(KEY_FUNCTION keyFunction, Coordinates coordinates, out string context)
        {

            KeyValuePair<string, KeyWrapper> tmp;

            // if a volume key is above application key
            if (keyFunction == KEY_FUNCTION.VOL_UP)
            {

                tmp = keys.FirstOrDefault(key => (key.Value.coordinates.column == coordinates.column && key.Value.coordinates.row == coordinates.row - 1));

            } else {

                tmp = keys.FirstOrDefault(key => (key.Value.coordinates.column == coordinates.column && key.Value.coordinates.row == coordinates.row + 1));

            }

            try
            {
                context = tmp.Key;
                return true;

            } catch(Exception ex)
            {
                core.LogMessage("No valid volume key");
            }

            context = null;

            return false;

        }

        private bool _updateVolumeKey(string context, string newAppName)
        {
            keys[context].appName = newAppName;

            core.LogMessage("Updated key with context: " + context + " With: " + newAppName);

            var x = new { context = new { AppName = newAppName, keyFunction = keys[context].keyFunction } };

            // now set the settings
            core.setSettings(context, JObject.FromObject(x));

            return true;
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
            core.LogMessage("Context appeared: " + e.context + e.action);

            core.getSettings(e.context);

            switch(e.action)
            {
                case "com.mbranvall.winmixerdeck.applicationpicker":
                    try
                    {
                    }
                    catch
                    {
                        core.LogMessage("No application registered to key yet...");
                    }
                    break;

                case "com.mbranvall.winmixerdeck.volumecontroller":
                    core.LogMessage("volume controller appeared");
                    break;
                default:
                    break;

            }



        }

        private void Core_PropertyInspectorAppearedEvent(object sender, PropertyInspectorDidAppear e)
        {

            core.LogMessage("PI Appeared. Action: " + e.action);

            switch (e.action)
            {
                case "com.mbranvall.winmixerdeck.applicationpicker":

                    // send audio session names to applicationpicker action

                    var names = getAudioSessions();

                    core.sendToPI(JObject.FromObject(new Payload(names)), e.context);
                    core.sendToPI(JObject.FromObject(new { messageType = "handshake" }), e.context);

                    break;

                case "com.mbranvall.winmixerdeck.volumecontroller":

                    core.LogMessage(e.context);
                    core.sendToPI(JObject.FromObject(new { messageType = "handshake" }), e.context);

                    break;

                case "com.mbranvall.winmixerdeck.volumeinterval":
                    break;

                default:
                    core.LogMessage("Action not recognized");
                    break;
            }



        }   
        
        private List<string> getAudioSessions()
        {
            List<string> names = new List<string>();
            var sessions = new AudioSessionManager();
            List<AudioSession> x = sessions.getAudioSessions();

            x.ForEach(session => names.Add(session.Name));
            return names;
        }

        private void MyTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            myTimer.Stop();

            string context = keyHeldDown.context;
            core.LogMessage("Held button with context: " + context);

            if (keyHeldDown.action == "com.mbranvall.winmixerdeck.applicationpicker")
            {

                var audioSessions = getAudioSessions();

                var tmp = new { appName = audioSessions[new Random().Next(audioSessions.Count)]};

                core.LogMessage("Setting new app name: " + tmp.appName);
                JObject pload = new JObject();
                pload.Add(context, JObject.FromObject(tmp));

                core.setSettings(context, pload);
                core.setTitle(tmp.appName, context);
                core.getSettings(context);


            } else
            {                
                return;
            }

        }

        private void Core_KeyDownEvent(object sender, KeyDown e)
        {
            keyHeldDown = e;
            myTimer.Interval = 2000;        
            myTimer.Enabled = true;
            myTimer.AutoReset = false;
        }

        private void Core_KeyUpEvent(object sender, KeyUp e)
        {

            myTimer.Stop();

            switch(e.action)
            {
                case PluginConsts.APP_CHOOSER:
                    break;

                case PluginConsts.VOL_CONTROLLER:

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

                    break;

                default:
                    break;
            }

        }

        public async Task Start()
        {
            await core.Start();
        }
    }
}
