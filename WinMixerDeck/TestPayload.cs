using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StreamDeckCS;
using StreamDeckCS.EventsReceived;

namespace WinMixerDeck
{
    public class TestPayload : IPayload
    {
        public string msg { get; set; }

        [JsonProperty("coordinates")]
        public Coordinates coordinates = new Coordinates();

        public TestPayload(string msg, int row, int col)
        {
            this.msg = msg;
            this.coordinates.row = row;
            this.coordinates.column = col;
        }
    }
}
