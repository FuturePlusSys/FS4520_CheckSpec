using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FS4500_MainLink_MST
{
    internal class StateDataFieldInfo
    {
        public string Name { get; set; } = "";
        public int Offset { get; set; } = 0;
        public int Width { get; set; } = 0;
        public StateDataFieldInfo(string name, int offset, int width)
        {
            Name = name;
            Offset = offset;
            Width = width;
        }
    }
}
