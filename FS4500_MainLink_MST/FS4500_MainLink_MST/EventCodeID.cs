using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FS4500_MainLink_MST
{
    internal class EventCodeID
    {
        public string Name { get; set; } = "";
        public int NumericValue { get; set; } = -1;

        public EventCodeID(string name, int numericValue)
        {
            Name = name;
            NumericValue = numericValue;
        }
    }
}
