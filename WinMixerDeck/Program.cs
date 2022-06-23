using System;
using System.Threading.Tasks;

namespace WinMixerDeck
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            WinMixerDeck plugin = new WinMixerDeck(args);
            await plugin.Start();

        }
    }
}
