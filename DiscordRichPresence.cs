using DiscordRPC;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dream_Charter {

    // in this case
    //  only Details is being changed to update location
    //  large image could be changed with location but is not

    internal static class DiscordRichPresence {

        // --- VARIABLES ---

        internal static DiscordRpcClient Client { get; private set; }

        internal static bool ClientEnabled { get; private set; }

        private static string? Secret { get; set; }

        internal static readonly string SECRET_FOLDER = Path.Combine(".", "secrets");
        private const string DISCORD_SECRET_PATH = "discord.txt";
        private const string DISCORD_SECRET_DEFAULT = "APP_ID";

        internal static readonly string ASSETS_FOLDER = Path.Combine(".", "assets");
        private const string SMALL_IMAGE_ID = "default_small";

        private const string LARGE_IMAGE_FILE = "default_large_image.txt";
        private const string DEFAULT_LARGE_IMAGE_ID = "default_large";
        internal static string LARGE_IMAGE_ID { get; set; }

        private const string GAME_NAME_FILE = "game_name.txt";
        private const string DEFAULT_GAME_NAME = "uknown";
        internal static string GAME_NAME { get; set; }

        // --- METHODS ---

        // -- Start --

        private static void _setClose() {
            // setup close commands
            AppDomain.CurrentDomain.ProcessExit += _on_ForceStop;
            AppDomain.CurrentDomain.UnhandledException += _on_ForceStop;
            Console.CancelKeyPress += _on_ForceStop;
        }

        private static void _cacheAppID() {
            // check for secret folder
            if (!Path.Exists(SECRET_FOLDER)) {
                Directory.CreateDirectory(SECRET_FOLDER);
            }

            // check for discord secret
            Secret = File.GetOrCreateDefault(Path.Combine(SECRET_FOLDER, DISCORD_SECRET_PATH), DISCORD_SECRET_DEFAULT);
            if (Secret == DISCORD_SECRET_DEFAULT) {
                Secret = null;
            }
        }

        private static void _setDefault() {
            // check for assets folder
            if (!Path.Exists(SECRET_FOLDER)) {
                Directory.CreateDirectory(SECRET_FOLDER);
            }

            // default image
            LARGE_IMAGE_ID = File.GetOrCreateDefault(
                Path.Combine(DreamLocation.DATA_FOLDER, LARGE_IMAGE_FILE),
                DEFAULT_LARGE_IMAGE_ID
            );

            // game name
            GAME_NAME = File.GetOrCreateDefault(
                Path.Combine(DreamLocation.DATA_FOLDER, GAME_NAME_FILE),
                DEFAULT_GAME_NAME
            );

            // set defaults
            Set(
                new() {
                    Details = $"Beginning the exploration...",
                    State = $"Playing ⟨{GAME_NAME}⟩",
                    Assets = new() {
                        LargeImageText = DreamLocation.StartingLocationId,
                        SmallImageText = "Dream Charter"
                    }
                },
                largeAsset: LARGE_IMAGE_ID,
                smallAsset: (LARGE_IMAGE_ID == DEFAULT_LARGE_IMAGE_ID) ? null : SMALL_IMAGE_ID
            );
        }

        public static void Start() {
            // get secret
            _cacheAppID();
            if (Secret is null) { goto failed; }

            // create client
            Client = new DiscordRpcClient(Secret); // client requires a client ID. you can find these on an application in the discord developer page.
            if (Client is null) { goto failed; }

            // set stop events and init
            _setClose();
            Client.Initialize();
            _setDefault();

            // success
            ClientEnabled = true;
            Console.WriteLine("...connected to discord rich presence");
            Console.WriteLine();
            return;
        failed: // failed
            ClientEnabled = false;
            Console.WriteLine("...failed to connect to discord rich presence");
            Console.WriteLine();
            return;
        }

        // -- Set --

        private enum _assetSetSize {
            large,
            small
        }

        private static void _setImageAsset(ref RichPresence richPresence, _assetSetSize size, string path) {
            // you need to upload assets to discord dev portal under rich presence ⪼ art assets
            
            // check if presence has asset
            if (richPresence.HasAssets()) {
                switch (size) {
                    case _assetSetSize.large:
                        richPresence.Assets.LargeImageKey = path;
                        break;
                    case _assetSetSize.small:
                        richPresence.Assets.SmallImageKey = path;
                        break;
                }
            }
        }

        public static void Set(
            RichPresence richPresence,
            string? largeAsset = null,
            string? smallAsset = null
        ) {
            // check large asset ref
            if (largeAsset is not null) {
                _setImageAsset(ref richPresence, _assetSetSize.large, largeAsset);
            }

            // check small asset ref
            if (smallAsset is not null) {
                _setImageAsset(ref richPresence, _assetSetSize.small, smallAsset);
            }

            // set presence
            Client.SetPresence(richPresence);
        }

        // -- Stop --

        private static void _on_ForceStop(object? sender, EventArgs args) {
            Stop();
            if (args is ConsoleCancelEventArgs consoleArgs) {
                consoleArgs.Cancel = false;
            }
        }

        public static void Stop() {
            // dont try to dispose if client not enabled 
            if (!ClientEnabled) { return; }

            // dispose of client
            Client.Dispose();
            Console.WriteLine();
            Console.WriteLine("disconnected from discord rich presence");
        }
    }
}
