using System;
using System.Collections.Generic;
using System.Text;

namespace BierWeerPoging2.Models
{
    class Temperature
    {
        public double Temp { get; set; }
        public int Pressure { get; set; }
        public int Humidity { get; set; }
        public double Temp_min { get; set; }
        public double Temp_max { get; set; }
    }
}
