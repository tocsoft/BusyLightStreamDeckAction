using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Tocsoft.StreamDeck;
using Tocsoft.StreamDeck.Events;

namespace Tocsoft.BusyLightStreamDeckAction
{
    public class MyActionHandler
    {
        private readonly IActionManager<MyActionSettings> manager;
        private readonly OpenhabManager openhabManager;
        private IDisposable monitor;
        private OpenhabConnection connection;
        private string currentItem;
        private int currentState;

        public MyActionHandler(IActionManager<MyActionSettings> manager, OpenhabManager openhabManager)
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

        public Task OnSettingsChanged(MyActionSettings settings)
        {
            if (!string.IsNullOrEmpty(settings.OpenhabUri))
            {
                this.connection = openhabManager.Connect(settings.OpenhabUri);
                this.currentItem = settings.ItemName;
                if (!string.IsNullOrEmpty(settings.ItemName))
                {
                    this.monitor?.Dispose();

                    this.monitor = connection.MonitorState(settings.ItemName, s =>
                    {
                        this.currentState = int.Parse(s);
                        UpdateIcon();
                    });
                }
            }
            return Task.CompletedTask;
        }

        private void UpdateIcon()
        {
            switch (currentState)
            {
                case 1:
                    _ = manager.SetImageAsync("Icons\\On_Red.png");
                    break;
                case 2:
                    _ = manager.SetImageAsync("Icons\\On_Other.png");
                    break;
                case 3:
                    _ = manager.SetImageAsync("Icons\\On_Green.png");
                    break;
                default:
                    _ = manager.SetImageAsync("Icons\\Off.png");
                    break;
            }
        }

        public void OnKeyUp()
        {
            upCounter++;
            sw?.Restart();
        }


        public void OnKeyPress(int counter)
        {

            if (!string.IsNullOrEmpty(currentItem))
            {
                if (counter >= 2)
                {
                    currentState = 3;// double click to go directly to green
                }
                else
                {
                    currentState++;
                    if (currentState > 3)
                    {
                        currentState = 1;
                    }
                    if (currentState < 1)
                    {
                        currentState = 3;
                    }
                }
            }

            this.connection?.SetState(currentItem, currentState.ToString());// turn it off
            UpdateIcon();
        }

        public void OnKeyHold(int counter)
        {
            if (!string.IsNullOrEmpty(currentItem))
            {
                currentState = 0;
                this.connection?.SetState(currentItem, "0");// turn it off
                UpdateIcon();
            }
        }

        TimeSpan longPressDuration = TimeSpan.FromSeconds(1);

        Task running = null;
        Stopwatch sw = null;
        int upCounter = 0;

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
