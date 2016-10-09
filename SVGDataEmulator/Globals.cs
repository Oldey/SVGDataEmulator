using System;
using System.Threading;

namespace SVGDataEmulator
{
    public static class Globals
    {
        public class src
        {
            public string filename { get; set; }
            public string path { get; set; }
            public bool enabled { get; set; }
        }

        public class setPoint
        {
            public string element { get; set; }
            public string units { get; set; }
            public string limitLo { get; set; }
            public string limitHi { get; set; }
            public string limitLoLo { get; set; }
            public string limitHiHi { get; set; }
            public string name { get; set; }
            public string precision { get; set; }
            public string valueFrom { get; set; }
            public string valueTo { get; set; }
            public string type { get; set; }
            public string quality { get; set; }
        }

        public static string sourcesXml = "sources.xml";
        public static string savePath = String.Empty;

        //Если пользователь залез в региональные настройки языка и изменил настройку разделителя целой и дробной части для чисел, то можно принудительно вернуть правильные настройки для текущего приложения
        //Смотри: "Панель управления"->"Язык и региональные стандарты"->"Форматы"->"Дополнительные параметры"->"Числа"->"Разделитель целой и дробной части"
        //Например в настройках Windows, для русского языка пользователь может выставить разделителем точку, вместо запятой!
        public static void SetDecimal(string ci)
        {
            switch (ci)
            {
                case "en-US":
                    Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator = ".";
                    Thread.CurrentThread.CurrentUICulture.NumberFormat.NumberDecimalSeparator = ".";
                    break;
                case "es-GT":
                    Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator = ".";
                    Thread.CurrentThread.CurrentUICulture.NumberFormat.NumberDecimalSeparator = ".";
                    break;
                case "ru-RU":
                    Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator = ",";
                    Thread.CurrentThread.CurrentUICulture.NumberFormat.NumberDecimalSeparator = ",";
                    break;
                default:
                    break;
            }
        }
    }
}
