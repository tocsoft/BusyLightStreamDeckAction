using BusyLightStreamDeckAction;
using StreamDeckLib;
using StreamDeckLib.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenhabStreamDeckAction
{
    [ActionUuid(Uuid = "tocsoft.streamdeck.openhab.switch")]
    public class RotateItemAction : BaseStreamDeckActionWithSettingsModel<Models.PiLightOptions>
    {
        public LightState currentState;
        private string context;
        private OpenhabManager habManager;
        private IDisposable itemMonitor;

        public RotateItemAction()
        {
        }

        public override async Task OnKeyUp(StreamDeckEventPayload args)
        {
            if (habManager != null)
            {
                await habManager.SetState(SettingsModel.ButtonItemName, "pressed");
                await habManager.GetState(SettingsModel.ItemName);// trigger a relaod of the state
            }
        }

        public override async Task OnDidReceiveSettings(StreamDeckEventPayload args)
        {
            this.context = args.context;
            await base.OnDidReceiveSettings(args);
            Restart();
        }

        public override async Task OnWillAppear(StreamDeckEventPayload args)
        {
            this.context = args.context;
            await base.OnWillAppear(args);
            Restart();
        }

        public override Task OnApplicationDidLaunch(StreamDeckEventPayload args)
        {
            this.context = args.context;
            return base.OnApplicationDidLaunch(args);
        }
        public override Task OnApplicationDidTerminate(StreamDeckEventPayload args)
        {
            this.context = args.context;
            return base.OnApplicationDidTerminate(args);
        }

        private async Task UpdateImage()
        {
            switch (currentState)
            {
                case LightState.None:
                    await Manager.SetImageAsync(context, $"images/allOff@2x.png");
                    break;
                case LightState.Red:
                    await Manager.SetImageAsync(context, $"images/redOnly@2x.png");
                    break;
                case LightState.Amber:
                    await Manager.SetImageAsync(context, $"images/amberOnly@2x.png");
                    break;
                case LightState.Green:
                    await Manager.SetImageAsync(context, $"images/greenOnly@2x.png");
                    break;
                default:
                    break;
            }
        }

        private async Task Restart()
        {
            StopMonitoring();

            if (!string.IsNullOrWhiteSpace(SettingsModel?.IPAddress) && !string.IsNullOrWhiteSpace(SettingsModel?.ItemName))
            {
                habManager = OpenhabManager.Server(SettingsModel.IPAddress);

                itemMonitor = habManager.MonitorState(SettingsModel.ItemName, s =>
                {
                    // state has changed in here see if it matchs the current item
                    var active = (LightState)int.Parse(s);
                    this.currentState = active;
                    this.UpdateImage();
                });
            }

            this.UpdateImage();
        }

        private void StopMonitoring()
        {
            habManager = null;
            itemMonitor?.Dispose();
            itemMonitor = null;
        }

        public override async Task OnWillDisappear(StreamDeckEventPayload args)
        {
            this.context = args.context;
            await base.OnWillDisappear(args);
            StopMonitoring();
        }
    }
}
