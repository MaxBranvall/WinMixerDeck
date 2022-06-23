using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StreamDeckCS.EventsReceived;

namespace WinMixerDeck.Models
{
    public class PluginKey
    {

        [JsonProperty("appName")]
        public string appName { get; set; }

        [JsonProperty("keyFunction")]
        public string keyFunction { get; set; }

        public Coordinates coordinates = new Coordinates();
    }
}
