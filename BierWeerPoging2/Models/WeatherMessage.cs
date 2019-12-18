using System;
using System.Collections.Generic;
using System.Text;

namespace BierWeerPoging2.Models
{
    class WeatherMessage : LocationMessage
    {
        public WeatherRoot Weather { get; set; }

        public WeatherMessage(WeatherRoot weather)
        {
            this.Weather = weather;
        }
    }
}
