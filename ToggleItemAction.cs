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

    [ActionUuid(Uuid = "tocsoft.streamdeck.openhab.singleColorToggle")]
    public class ToggleItemAction : BaseStreamDeckActionWithSettingsModel<Models.SingleLightOptions>
    {
        public LightState currentState;
        private string context;
        private OpenhabManager habManager;
        private IDisposable itemMonitor;
        private bool isOn;

        public ToggleItemAction()
        {
        }

        public override async Task OnKeyUp(StreamDeckEventPayload args)
        {
            if(habManager != null)
            {
                await habManager.SetState(SettingsModel.ItemName, ((int)(this.isOn ? LightState.None : SettingsModel.LightState)).ToString());
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
            if(SettingsModel.LightState == LightState.None)
            {
                await Manager.SetImageAsync(context, $"images/allOffSingle@2x.png");
            }
            else
            {
                var imageState = isOn? "On" : "Off";
                var color = SettingsModel.LightState.ToString().ToLower();

                await Manager.SetImageAsync(context, $"images/{color}{imageState}@2x.png");
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

                    this.isOn = active == SettingsModel.LightState;
                    
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
