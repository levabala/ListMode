using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ListModeInstance
{
    public class Experiment
    {
        public string name, rawPath;
        public DateTime startTime, endTime;

        public Experiment(string name, string rawPath, DateTime startTime, DateTime endTime)
        {
            this.name = name;
            this.rawPath = rawPath;
            this.startTime = startTime;
            this.endTime = endTime;
        }
    }
}
