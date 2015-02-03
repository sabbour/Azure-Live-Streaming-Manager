using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ScheduleManagerApp.Helpers.Converters
{
    public class StreamingUrlValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var param = parameter as string;
            switch (param)
            {
                case "Smooth":
                    return string.Format("http://amsplayer.azurewebsites.net/player.html?player=flash&format=smooth&url={0}", value);
                case "HLS":
                    return string.Format("http://www.flashls.org/latest/examples/chromeless/?src={0}", value);
                case "DASH":
                    return string.Format("http://dashplayer.azurewebsites.net/?url={0}", value);
                default:
                    return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
