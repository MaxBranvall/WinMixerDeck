using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StreamDeckCS;
using StreamDeckCS.Helpers;
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
        string originalTitle = "";
        int i = 0;
        float interval = 0.25f;
        Dictionary<string, KeyWrapper> keys = new Dictionary<string, KeyWrapper>();
        List<WillAppear> willAppears = new List<WillAppear>();
        List<float> intervals = new List<float> { 0.0f, 1.0f, 2.0f, 5.0f, 10.0f, 25.0f };

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
            core.DidReceiveGlobalSettingsEvent += Core_DidReceiveGlobalSettingsEvent;

            myTimer.Elapsed += MyTimer_Elapsed;

        }

        private void Core_DidReceiveGlobalSettingsEvent(object sender, DidReceiveGlobalSettings e)
        {
            core.LogMessage("Got global settings");

            interval = JsonConvert.DeserializeObject<float>(e.payload.settings["interval"].ToString()) / 100;

            foreach (var k in willAppears)
            {
                core.setTitle(k.context, "Vol Int:\n" + (int)(interval * 100));
            }

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
            PluginKey keySettings;

            context = e.context;

            try
            {
                //NewMethod();

                switch (e.action)
                {
                    case "com.mbranvall.winmixerdeck.applicationpicker":

                        // will throw exception when new key is added
                        try
                        {
                            keySettings = JsonConvert.DeserializeObject<PluginKey>(e.payload.settings[context].ToString());

                        } catch (Exception ex)
                        {
                            keySettings = new PluginKey();
                            keySettings.appName = "No app";
                            keySettings.coordinates = e.payload.coordinates;
                        }

                        keySettings.coordinates = e.payload.coordinates;

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
                        core.setTitle(context, keys[context].appName);
                        break;

                    case "com.mbranvall.winmixerdeck.volumecontroller":

                        try
                        {
                            keySettings = JsonConvert.DeserializeObject<PluginKey>(e.payload.settings[context].ToString());
                            keySettings.updated = false;
                        } catch (Exception ex)
                        {
                            keySettings = new PluginKey();
                            keySettings.keyFunction = "volUp";
                            keySettings.coordinates = e.payload.coordinates;
                        }                        

                        keySettings.coordinates = e.payload.coordinates;

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

                            string newAppName;

                            if (!keySettings.updated)
                            {
                                if (keySettings.keyFunction == "volUp")
                                {
                                    newAppName = getAppNameFromAppPicker(keySettings.coordinates.row + 1, keySettings.coordinates.column);
                                } else
                                {
                                    newAppName = getAppNameFromAppPicker(keySettings.coordinates.row - 1, keySettings.coordinates.column);
                                }

                                this._updateVolumeKey(context, newAppName);

                            }

                        }
                        catch (Exception ex)
                        {
                            core.LogMessage("App picker does not exist yet..");
                        }

                        var img = keySettings.keyFunction == "volUp" ? "./assets/VOL_+.jpg" : "./assets/VOL_-.jpg";
                        core.setImage(context, img);

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

        private string getAppNameFromAppPicker(int row, int column)
        {
            KeyValuePair<string, KeyWrapper> tmp;

            tmp = keys.FirstOrDefault(key => (key.Value.coordinates.row == row && key.Value.coordinates.column == column));

            return keys[tmp.Key].appName;
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

            core.LogMessage("Updated key with context: " + context + " With app: " + newAppName);

            var x = new { AppName = newAppName, keyFunction = keys[context].keyFunction, updated = true };
            JObject tmp = new JObject();
            tmp.Add(context, JObject.FromObject(x));

            // now set the settings
            core.setSettings(context, tmp);

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
                                core.sendToPI(context, "com.mbranvall.winmixerdeck.volumecontroller", JObject.FromObject(x));
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
                                core.sendToPI(context, "com.mbranvall.winmixerdeck.volumecontroller", JObject.FromObject(x));
                            }
                        }
                    }

                    break;
            }

        }

        private void Core_WillAppearEvent(object sender, WillAppear e)
        {
            core.LogMessage("Context appeared: " + e.context + e.action);

            switch(e.action)
            {
                case "com.mbranvall.winmixerdeck.applicationpicker":
                    try
                    {
                        core.getSettings(e.context);
                    }
                    catch
                    {
                        core.LogMessage("No application registered to key yet...");
                    }
                    break;

                case "com.mbranvall.winmixerdeck.volumecontroller":
                    core.LogMessage("volume controller appeared");
                    core.getSettings(e.context);
                    break;

                case PluginConsts.VOL_INTERVAL:
                    core.getGlobalSettings(core.pluginUUID);
                    willAppears.Add(e);
                    break;
                default:
                    break;

            }



        }

        private void Core_PropertyInspectorAppearedEvent(object sender, PropertyInspectorDidAppear e)
        {

            core.LogMessage("PI Appeared. Action: " + e.action);
            string action = null;

            switch (e.action)
            {
                case PluginConsts.APP_CHOOSER:

                    action = PluginConsts.APP_CHOOSER;
                    // send audio session names to applicationpicker action
                    var names = getAudioSessions();
                    core.sendToPI(e.context, PluginConsts.APP_CHOOSER, JObject.FromObject(new Payload(names)));

                    break;

                case PluginConsts.VOL_CONTROLLER:

                    action = PluginConsts.VOL_CONTROLLER;
                    break;

                case PluginConsts.VOL_INTERVAL:
                    action = PluginConsts.VOL_INTERVAL;
                    core.sendToPI(e.context, action, JObject.FromObject( new { audioIntervals = this.intervals, messageType = "populate" } ));
                    break;

                default:
                    core.LogMessage("Action not recognized");
                    break;
            }

            core.sendToPI(e.context, action, JObject.FromObject(new { messageType = "handshake" }));



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

            core.setTitle(this.context, this.originalTitle);

            //string context = keyHeldDown.context;
            //core.LogMessage("Held button with context: " + context);

            //if (keyHeldDown.action == "com.mbranvall.winmixerdeck.applicationpicker")
            //{

            //    var audioSessions = getAudioSessions();

            //    var tmp = new { appName = audioSessions[new Random().Next(audioSessions.Count)] };

            //    core.LogMessage("Setting new app name: " + tmp.appName);
            //    JObject pload = new JObject();
            //    pload.Add(context, JObject.FromObject(tmp));

            //    core.setSettings(context, pload);
            //    core.setTitle(tmp.appName, context);
            //    core.getSettings(context);


            //} else if (keyHeldDown.action == PluginConsts.VOL_INTERVAL) {

            //    core.sendToPI(keyHeldDown.context, PluginConsts.VOL_INTERVAL, JObject.FromObject(new { messageType = "incrementInterval" }));

            //} else
            //{
            //    return;
            //}

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

                case PluginConsts.VOL_INTERVAL:
                    int curIndex = intervals.IndexOf(interval);
                    interval = (curIndex == intervals.Count - 1) ? intervals[0] : intervals[curIndex + 1];
                    core.setGlobalSettings(core.pluginUUID, JObject.FromObject(new { curInterval = this.interval }));
                    core.sendToPI(e.context, PluginConsts.VOL_INTERVAL, JObject.FromObject(new { messageType = "handshake" }));
                    core.setTitle(e.context, "Vol Int:\n" + (int)interval);
                    break;

                case PluginConsts.APP_CHOOSER:
                    var audioSessions = getAudioSessions();

                    var tmp = new { appName = audioSessions[new Random().Next(audioSessions.Count)] };

                    core.LogMessage("Setting new app name: " + tmp.appName);
                    JObject pload = new JObject();
                    pload.Add(e.context, JObject.FromObject(tmp));

                    core.setSettings(e.context, pload);
                    core.setTitle(e.context, tmp.appName);
                    core.getSettings(e.context);
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

                                    var currVolume = sessions.getVolumeLevel(session);

                                    if (keys[e.context].keyFunction == "volUp")
                                    {

                                        var diff = 1 - currVolume;

                                        if ((interval / 100) > diff)
                                        {
                                            sessions.adjustVolume(session, currVolume + diff);
                                            
                                        } else
                                        {
                                            sessions.adjustVolume(session, currVolume + interval / 100);
                                        }
                                        
                                    }
                                    else
                                    {

                                        if ((interval / 100) > currVolume)
                                        {
                                            sessions.adjustVolume(session, 0);
                                        } else
                                        {
                                            sessions.adjustVolume(session, sessions.getVolumeLevel(session) - interval / 100);
                                        }
                                        
                                    }

                                    // get context of associated app key
                                    foreach (var y in keys)
                                    {
                                        if (y.Value.appName == keys[e.context].appName && y.Value.keyFunction == null)
                                        {

                                            var z = y;
                                            double vol = Math.Round(sessions.getVolumeLevel(session), 2);

                                            core.setTitle(y.Key, (vol * 100).ToString());
                                            this.originalTitle = y.Value.appName;
                                            this.context = y.Key;
                                            myTimer.Interval = 3000;
                                            myTimer.AutoReset = false;
                                            myTimer.Start();
                                        }
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
