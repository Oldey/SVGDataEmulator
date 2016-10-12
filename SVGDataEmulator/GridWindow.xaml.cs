using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Xml.Linq;
using static SVGDataEmulator.Globals;

namespace SVGDataEmulator
{
    /// <summary>
    /// Interaction logic for GridWindow.xaml
    /// </summary>
    public partial class GridWindow : Window
    {
        private string alias = String.Empty;

        public GridWindow()
        {
            InitializeComponent();
            var ci = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentUICulture = ci;
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(ci.Name);
            SetDecimal(ci.ToString());
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            XDocument xdoc = XDocument.Load(sourcesXml);
            XElement xSource = null;
            alias = Tag as string;
            foreach (XElement xe in xdoc.Element("sources").Elements("source"))
            {
                if (xe.Attribute("alias").Value == alias)
                {
                    xSource = xe;
                    break;
                }
            }

            if (xSource == null)
            {
                return;
            }

            List<setPoint> setPointsList = new List<setPoint>();
            setPointsList = listView.ItemsSource as List<setPoint>;
            int i = 0;
            foreach (XElement xe in xSource.Elements())
            {
                xe.Attribute("n").Value = setPointsList[i].element;
                xe.Attribute("engineeringUnits").Value = setPointsList[i].units;
                xe.Attribute("limitLo").Value = setPointsList[i].limitLo;
                xe.Attribute("limitHi").Value = setPointsList[i].limitHi;
                xe.Attribute("limitLoLo").Value = setPointsList[i].limitLoLo;
                xe.Attribute("limitHiHi").Value = setPointsList[i].limitHiHi;
                xe.Attribute("bodName").Value = setPointsList[i].name;
                xe.Attribute("bodPrecision").Value = setPointsList[i].precision;
                xe.Attribute("v1").Value = setPointsList[i].valueFrom;
                xe.Attribute("v2").Value = setPointsList[i].valueTo;
                xe.Attribute("t").Value = setPointsList[i].type;
                xe.Attribute("q").Value = setPointsList[i].quality;
                i++;
            }
            xdoc.Save(sourcesXml);

            string propXmlFile = System.IO.Path.Combine(savePath, alias, "prop.xml");
            XDocument xdocProp = XDocument.Load(propXmlFile);
            XElement xrootProp = xdocProp.Element("prop");
            //string UTCDate = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            //xroot.Attribute("d").Value = UTCDate;
            int j = 0;
            foreach (XElement xe in xrootProp.Elements("bod").ToList())
            {
                xe.Attribute("engineeringUnits").Value = setPointsList[j].units;
                xe.Attribute("limitLo").Value = setPointsList[j].limitLo;
                xe.Attribute("limitHi").Value = setPointsList[j].limitHi;
                xe.Attribute("limitLoLo").Value = setPointsList[j].limitLoLo;
                xe.Attribute("limitHiHi").Value = setPointsList[j].limitHiHi;
                xe.Attribute("bodName").Value = setPointsList[j].name;
                xe.Attribute("bodPrecision").Value = setPointsList[j].precision;
                j++;
            }
            xdocProp.Save(propXmlFile);

            string dataXmlFile = System.IO.Path.Combine(savePath, alias, "data.xml");
            XDocument xdocData = XDocument.Load(dataXmlFile);
            XElement xrootData = xdocData.Element("data");
            //string UTCDate = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            //xroot.Attribute("d").Value = UTCDate;
            int k = 0;
            foreach (XElement xe in xrootData.Elements("bod").ToList())
            {
                xe.Attribute("t").Value = setPointsList[k].type;
                xe.Attribute("q").Value = setPointsList[k].quality;
                k++;
            }
            xdocData.Save(dataXmlFile);

            MessageBox.Show(
                "Setpoints for \"" + alias + "\" were saved successfully.",
                "Action complete",
                MessageBoxButton.OK);
        }
    }
}
