using System;
using System.Collections.Generic;
using System.Text;

namespace PriceServer
{
    public class ComputerPart
    {
        public string Name { get; set; } 
        public decimal Price { get; set; }
        public string Description { get; set; }
        public byte[] ImageBytes { get; set; }
    }
}
