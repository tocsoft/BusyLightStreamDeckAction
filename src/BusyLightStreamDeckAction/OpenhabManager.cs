using System;
using System.Collections.Generic;
using System.Linq;

namespace Tocsoft.BusyLightStreamDeckAction
{
    public class OpenhabManager
    {
        private static List<OpenhabConnection> managers = new List<OpenhabConnection>();

        public OpenhabConnection Connect(string url)
        {
            var manager = managers.FirstOrDefault(x => x.Url.Equals(url, StringComparison.OrdinalIgnoreCase));
            if (manager == null)
            {
                lock (managers)
                {
                    manager = managers.FirstOrDefault(x => x.Url.Equals(url, StringComparison.OrdinalIgnoreCase));
                    if (manager == null)
                    {
                        manager = new OpenhabConnection(url);
                        managers.Add(manager);
                    }
                }
            }
            return manager;
        }
    }
}
