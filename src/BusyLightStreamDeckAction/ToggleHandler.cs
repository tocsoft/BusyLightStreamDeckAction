using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Tocsoft.StreamDeck;
using Tocsoft.StreamDeck.Events;

namespace Tocsoft.BusyLightStreamDeckAction
{
    public class ToggleHandler
    {
        private readonly IActionManager<MyActionSettings> manager;
        private readonly OpenhabManager openhabManager;
        private IDisposable monitor;
        private OpenhabConnection connection;
        private string currentItem;
        private string currentState;

        public ToggleHandler(IActionManager<MyActionSettings> manager, OpenhabManager openhabManager)
        {
            this.manager = manager;
            this.openhabManager = openhabManager;
        }

        public void OnWillAppear(WillAppearEvent willAppearEvent)
        {
            try
            {
                OnSettingsChanged(willAppearEvent.Payload.Settings.ToObject<MyActionSettings>());
            }
            catch
            {

            }
        }

        private string StateParse(string state)
        {
            switch (state)
            {
                case "0":
                case "0,0":
                case "0,0,0":
                case "0.0":
                case "OFF":
                    return "OFF";
                default:
                    return "ON";
            }

        }
        private void UpdateState(string state)
        {
            this.currentState = StateParse(state);
            UpdateIcon();
        }

        public Task OnSettingsChanged(MyActionSettings settings)
        {
            if (!string.IsNullOrEmpty(settings.OpenhabUri))
            {
                this.connection = openhabManager.Connect(settings.OpenhabUri);
                this.currentItem = settings.ItemName;
                if (!string.IsNullOrEmpty(settings.ItemName))
                {
                    this.monitor?.Dispose();

                    this.monitor = connection.MonitorState(settings.ItemName, UpdateState);
                }
            }
            return Task.CompletedTask;
        }

        private void UpdateIcon()
        {
            switch (currentState)
            {
                case "ON":
                    manager.SetStateAsync(ActionState.Default);
                    _ = manager.SetImageAsync("Icons\\On_Other.png");
                    break;
                default:
                    _ = manager.SetImageAsync("Icons\\Off.png");
                    break;
            }
        }

        public async Task OnKeyPress(int counter)
        {
            if (!string.IsNullOrEmpty(currentItem))
            {
                await this.connection?.Trigger(currentItem, "TOGGLE");

                var state = await this.connection?.GetState(currentItem);
                UpdateState(state);
            }

            UpdateIcon();
        }

        public async Task OnKeyHold(int counter)
        {
            if (!string.IsNullOrEmpty(currentItem))
            {
                await this.connection?.Trigger(currentItem, "OFF");

                UpdateState("OFF");
            }
        }

        TimeSpan longPressDuration = TimeSpan.FromSeconds(1);

        Task running = null;
        Stopwatch sw = null;
        int upCounter = 0;

        public void OnKeyUp()
        {
            upCounter++;
            sw?.Restart();
        }

        public void OnKeyDown()
        {
            if (running == null)
            {
                sw = Stopwatch.StartNew();
                upCounter = 0;
                running = Task.Run(async () =>
                {
                    //start processing loop
                    while (true)
                    {
                        if (sw.ElapsedMilliseconds > 250)
                        {
                            var c = upCounter;
                            if (upCounter > 0)
                            {
                                _ = Task.Run(() => OnKeyPress(c));
                                break;
                            }
                            else if (sw.Elapsed >= longPressDuration)
                            {
                                _ = Task.Run(() => OnKeyHold(c + 1));
                                break;
                            }
                        }
                        await Task.Delay(50);
                    }

                    upCounter = 0;
                    running = null;
                    sw = null;
                });
            }
            else
            {
                sw?.Stop();
            }
            // start processing the run on the first down
        }

        public class MyActionSettings
        {
            public string OpenhabUri { get; set; }
            public string ItemName { get; set; }
        }
    }
}
