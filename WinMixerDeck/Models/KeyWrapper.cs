using Newtonsoft.Json;
using StreamDeckCS.Helpers;

namespace WinMixerDeck.Models
{
    public class KeyWrapper
    {

        [JsonProperty("appName")]
        public virtual string appName { get; set; }

        [JsonProperty("keyFunction")]
        public string keyFunction { get; set; }

        [JsonProperty("updated")]
        public bool updated { get; set; }

        public Coordinates coordinates = new Coordinates();
    }
}
