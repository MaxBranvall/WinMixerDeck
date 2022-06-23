using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StreamDeckCS;

namespace WinMixerDeck
{
    class Payload : IPayload
    {
        [JsonProperty("messageType")]
        string messageType = "getSessions";

        [JsonProperty("audioSessions")]
        List<string> sessions { get; set; }

        public Payload(List<string> audioSessions)
        {
            this.sessions = audioSessions;
        }
    }
}
