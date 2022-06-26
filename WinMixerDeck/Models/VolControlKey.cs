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
    public class VolControlKey : KeyWrapper
    {
        public override string appName { get => null; set => base.appName = null; }
    }
}
