using System;

namespace OpenhabStreamDeckAction.Models
{
    public class SingleLightOptions
    {
        public string IPAddress { get; set; }
        public string ItemName { get; set; }
        public string Light { get; set; }
        public LightState LightState => (Enum.TryParse<LightState>(Light, out var state)) ? state : LightState.None;
    }
}
