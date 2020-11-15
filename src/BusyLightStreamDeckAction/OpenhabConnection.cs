using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Tocsoft.BusyLightStreamDeckAction
{
    public class OpenhabConnection
    {
        public OpenhabConnection(string url)
        {
            Url = url;
        }

        public string Url { get; }

        private List<MonitorDisposable> monitors = new List<MonitorDisposable>();
        private Dictionary<string, string> itemState = new Dictionary<string, string>();

        private void RecordItemState(string item, string state)
        {
            if (!itemState.TryGetValue(item, out var oldstate) || oldstate != state)
            {
                lock (monitors)
                {
                    foreach (var m in monitors.Where(x => x.ItemName == item))
                    {
                        m.Callback?.Invoke(state);
                    }
                }
            }

            itemState[item] = state;
        }

        private void StopMonitoring(MonitorDisposable disposable)
        {
            lock (monitors)
            {
                monitors.Remove(disposable);
            }

            EnsureBackgroundTaskState();
        }

        CancellationTokenSource cts = null;
        Task t = null;
        private void EnsureBackgroundTaskState()
        {
            lock (monitors)
            {
                if (monitors.Any())
                {
                    cts = new CancellationTokenSource();
                    var token = cts.Token;
                    t = Task.Run(async () =>
                    {
                        while (!token.IsCancellationRequested)
                        {
                            var groups = monitors.GroupBy(x => x.ItemName);

                            try
                            {
                                var tasks = groups.Select(async x => await this.GetState(x.Key)).ToArray();

                                await Task.WhenAll(tasks);
                            }
                            catch (Exception ex)
                            {
                                client = null;
                            }

                            await Task.Delay(1000, token);
                        }
                    }, token);
                }
                else
                {
                    // stop the current process running
                    cts.Cancel();
                    cts = null;
                    t = null;
                }
            }
        }

        public IDisposable MonitorState(string item, Action<string> updatedStateCallback)
        {
            var hasCache = itemState.TryGetValue(item, out var oldstate);

            var monitor = new MonitorDisposable(this, item, updatedStateCallback);
            lock (monitors)
            {
                monitors.Add(monitor);
            }
            EnsureBackgroundTaskState();

            GetState(item).ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully)
                {
                    if (hasCache && oldstate == t.Result)
                    {
                        monitor.Callback(t.Result);
                    }
                }
            });

            return monitor;
        }


        HttpClient client;
        public async Task<string> GetState(string item)
        {
            client ??= new HttpClient();

            var response = await client.GetAsync($"http://{Url}/rest/items/{item}/state/");

            var state = await response.Content.ReadAsStringAsync();

            RecordItemState(item, state);

            return state;
        }

        public async Task SetState(string item, string value)
        {
            client ??= new HttpClient();
            var response = await client.PutAsync($"http://{Url}/rest/items/{item}/state/", new StringContent(value ?? ""));

            // TODO push out updates to all monitors with the new state!!!
        }

        private class MonitorDisposable : IDisposable
        {
            public MonitorDisposable(OpenhabConnection manager, string itemName, Action<string> callback)
            {
                Manager = manager;
                ItemName = itemName;
                Callback = callback;
            }

            private OpenhabConnection Manager { get; set; }

            public string ItemName { get; set; }

            public Action<string> Callback { get; set; }

            public void Dispose()
            {
                Manager.StopMonitoring(this);
            }

        }
    }
}
