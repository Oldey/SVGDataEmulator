﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Xml.Linq;
using static SVGDataEmulator.Globals;

namespace SVGDataEmulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<DispatcherTimer> timers = new List<DispatcherTimer>();
        private int updateTime;
        private string analogType;
        private string boolType = "BOOL";

        //private string getRandomString(int len)
        //{
        //    var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        //    var stringChars = new char[len];
        //    var random = new Random();
        //    for (int i = 0; i < len; i++)
        //    {
        //        stringChars[i] = chars[random.Next(chars.Length)];
        //    }
        //    return new String(stringChars);
        //}

        private List<src> getListBoxItemsSourceFromXmlFile(XDocument xdoc)
        {
            List<src> sourcesList = new List<src>();
            foreach (XElement xe in xdoc.Element("sources").Elements("source"))
            {
                sourcesList.Add(new src()
                {
                    alias = xe.Attribute("alias").Value,
                    path = xe.Attribute("path").Value,
                    enabled = Boolean.Parse(xe.Attribute("enabled").Value)
                });
            }
            return sourcesList;
        }

        private List<setPoint> getGridItemsSourceFromXmlFile(string alias)
        {
            XDocument xdoc = XDocument.Load(sourcesXml);
            XElement xSource = null;
            foreach (XElement xe in xdoc.Element("sources").Elements("source"))
            {
                if (xe.Attribute("alias").Value == alias)
                {
                    xSource = xe;
                    break;
                }
            }

            List<setPoint> setPointsList = new List<setPoint>();
            if (xSource == null)
            {
                return setPointsList;
            }

            foreach (XElement xe in xSource.Elements())
            {
                setPointsList.Add(new setPoint()
                {
                    element = xe.Attribute("n").Value,
                    units = xe.Attribute("engineeringUnits").Value,
                    limitLo = xe.Attribute("limitLo").Value,
                    limitHi = xe.Attribute("limitHi").Value,
                    limitLoLo = xe.Attribute("limitLoLo").Value,
                    limitHiHi = xe.Attribute("limitHiHi").Value,
                    name = xe.Attribute("bodName").Value,
                    precision = xe.Attribute("bodPrecision").Value,
                    valueFrom = xe.Attribute("v1").Value,
                    valueTo = xe.Attribute("v2").Value,
                    type = xe.Attribute("t").Value,
                    quality = xe.Attribute("q").Value
                });
            }
            return setPointsList;
        }

        public MainWindow()
        {
            InitializeComponent();

            var ci = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentUICulture = ci;
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(ci.Name);
            SetDecimal(ci.ToString());

            if (File.Exists(sourcesXml))
            {
                XDocument xdoc = XDocument.Load(sourcesXml);
                listBoxSources.ItemsSource = getListBoxItemsSourceFromXmlFile(xdoc);
                if (xdoc.Element("sources").HasAttributes)
                {
                    textBoxSavePath.Text = xdoc.Element("sources").Attribute("defaultSavePath").Value;
                    savePath = textBoxSavePath.Text;
                }
            }
            else
            {
                XDocument xdoc = new XDocument(new XElement("sources"));
                xdoc.Save(sourcesXml);
            }
        }

        private void buttonAddSource_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Scalable Vector Graphics(*.svg)|*.svg"; // |All files(*.*)|*.*
            openFileDialog.CheckFileExists = true;
            if (openFileDialog.ShowDialog() == true)
            {
                string path = openFileDialog.FileName;
                string alias = String.Empty;

                var aliasDialog = new AliasWindow(path);
                if (aliasDialog.ShowDialog() == true)
                {
                    alias = aliasDialog.ResponseText;
                }
                else
                {
                    return;
                }

                XDocument xdoc = XDocument.Load(sourcesXml);

                List<string> aliases = new List<string>();
                foreach (XElement xe in xdoc.Element("sources").Elements("source"))
                {
                    aliases.Add(xe.Attribute("alias").Value);
                }
                if (aliases.Contains(alias))
                {
                    MessageBox.Show(
                        "A source with the name \"" + alias + "\" is already on the list. The selected source will not be added.",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error,
                        MessageBoxResult.OK,
                        MessageBoxOptions.DefaultDesktopOnly);
                    return;
                }

                XNode xLastSource = xdoc.Element("sources").LastNode;
                XElement newxe = new XElement("source");
                newxe.SetAttributeValue("alias", alias);
                newxe.SetAttributeValue("path", path);
                newxe.SetAttributeValue("enabled", true);
                if (xLastSource == null) xdoc.Element("sources").Add(newxe);
                else xLastSource.AddAfterSelf(newxe);

                savePath = new DirectoryInfo(System.IO.Path.GetDirectoryName(path)).Parent.FullName;
                textBoxSavePath.Text = savePath;
                xdoc.Element("sources").SetAttributeValue("defaultSavePath", savePath);

                xdoc.Save(sourcesXml);
                listBoxSources.ItemsSource = getListBoxItemsSourceFromXmlFile(xdoc);
            }
        }

        private void buttonRemoveSource_Click(object sender, RoutedEventArgs e)
        {
            List<src> sourcesList = new List<src>();
            List<string> aliases = new List<string>();
            foreach (var tObj in (listBoxSources.ItemsSource as List<src>).Where(myObj => myObj.enabled))
            {
                sourcesList.Add(tObj);
                aliases.Add(tObj.alias);
            }   
            listBoxSources.ItemsSource = sourcesList;

            XDocument xdoc = XDocument.Load(sourcesXml);
            List<XElement> sources = xdoc.Element("sources").Elements("source").ToList<XElement>();
            foreach (XElement xe in sources)
            {
                if (!aliases.Contains(xe.Attribute("alias").Value))
                    xe.Remove();
            }
            xdoc.Save(sourcesXml);  
        }

        private void buttonEmulateOnce_Click(object sender, RoutedEventArgs e)
        {
            double range1 = Double.Parse(textBoxValueRangeFrom.Text);
            double range2 = Double.Parse(textBoxValueRangeTo.Text);
            string prefix = textBoxPrefix.Text;
            string attr = textBoxAttr.Text;
            string precision = textBoxPrecision.Text;
            string limitLoLo = textBoxLimitLoLo.Text;
            string limitLo = textBoxLimitLo.Text;
            string limitHi = textBoxLimitHi.Text;
            string limitHiHi = textBoxLimitHiHi.Text;

            List<string> propAttributes = new List<string>();
            List<string> dataAttributes = new List<string>();
            propAttributes.Add("engineeringUnits");
            propAttributes.Add("limitLo");
            propAttributes.Add("limitHi");
            propAttributes.Add("limitLoLo");
            propAttributes.Add("limitHiHi");
            propAttributes.Add("bodCode");
            propAttributes.Add("bodName");
            propAttributes.Add("bodShortName");
            propAttributes.Add("bodHistory");
            propAttributes.Add("bodEvent");
            propAttributes.Add("bodPrecision");
            propAttributes.Add("n");
            dataAttributes.Add("n");
            dataAttributes.Add("v");
            dataAttributes.Add("t");
            dataAttributes.Add("q");

            List<string> sourcesToGo = new List<string>();
            foreach (var tObj in (listBoxSources.ItemsSource as List<src>).Where(myObj => myObj.enabled))
                sourcesToGo.Add(tObj.path);

            foreach (string path in sourcesToGo)
            {
                List<Dictionary<string, string>> fullPropList = new List<Dictionary<string, string>>();
                List<Dictionary<string, string>> fullDataList = new List<Dictionary<string, string>>();
                string line;
                List<String> temp = new List<String>();
                StreamReader fileR = new StreamReader(path);
                while ((line = fileR.ReadLine()) != null)
                {
                    temp.Add(line);
                }
                fileR.Close();

                int tempLength = temp.Count;
                int lineOfFirstG, lineOfLastG;
                int startGCount;
                analogType = textBoxType.Text;
                int bodCode = 0;
                int rngSeed = (int)DateTime.Now.Ticks & 0x0000FFFF;

                for (int i = 0; i < tempLength; i++)
                {
                    string bodShortName = String.Empty;
                    string t = boolType;

                    if (temp[i].Contains("<g id=\"" + prefix))
                    {
                        int pos_end = temp[i].IndexOf("<g id=\"") + 7;
                        bodShortName = String.Empty;
                        while (temp[i][pos_end] != '\"')
                        {
                            bodShortName += temp[i][pos_end];
                            pos_end++;
                        }
                        lineOfFirstG = i++;
                        bodCode++;
                        startGCount = 1;
                        do
                        {
                            if (temp[i].Contains("<g"))
                                startGCount++;
                            if (temp[i].Contains("</g"))
                                startGCount--;
                            i++;
                        } while (startGCount != 0);
                        lineOfLastG = i--;

                        for (int k = lineOfFirstG; k < lineOfLastG; k++)
                        {
                            if (temp[k].Contains("<text") && temp[k].Contains(attr))
                            {
                                t = analogType;
                                break;
                            }
                        }

                        Dictionary<string, string> dictProp = new Dictionary<string, string>();
                        Dictionary<string, string> dictData = new Dictionary<string, string>();
                        string bodn = "bod" + bodCode.ToString();
                        dictProp.Add(propAttributes[0], "-");
                        dictProp.Add(propAttributes[1], limitLo);
                        dictProp.Add(propAttributes[2], limitHi);
                        dictProp.Add(propAttributes[3], limitLoLo);
                        dictProp.Add(propAttributes[4], limitHiHi);
                        dictProp.Add(propAttributes[5], bodCode.ToString());
                        dictProp.Add(propAttributes[6], bodShortName); // full name
                        dictProp.Add(propAttributes[7], bodShortName);
                        dictProp.Add(propAttributes[8], "");
                        dictProp.Add(propAttributes[9], "");
                        dictProp.Add(propAttributes[10], precision);
                        dictProp.Add(propAttributes[11], bodn);
                        dictData.Add(dataAttributes[0], bodn);
                        Random rng = new Random(++rngSeed);
                        if (t == analogType)
                        {
                            dictData.Add(dataAttributes[1], (rng.NextDouble() * (range2 - range1) + range1).ToString());
                        }
                        else /*if (t == boolType)*/
                        {
                            dictData.Add(dataAttributes[1], Convert.ToBoolean(rng.Next(2)).ToString());
                        }
                        dictData.Add(dataAttributes[2], t);
                        dictData.Add(dataAttributes[3], "192");
                        fullPropList.Add(dictProp);
                        fullDataList.Add(dictData);
                    }
                    else continue;
                }

                XDocument xdoc = XDocument.Load(sourcesXml);
                List<XElement> sources = xdoc.Element("sources").Elements("source").ToList<XElement>();
                XElement xSource = null;
                string xAlias = null;
                foreach (XElement xe in sources)
                {
                    if (xe.Attribute("path").Value == path)
                    {
                        xSource = xe;
                        xAlias = xe.Attribute("alias").Value;
                        break;
                    }
                }
                //if (xSource == null)
                //    break;
                xSource.RemoveNodes();

                for (int j = 0; j < fullPropList.Count; j++)
                {
                    XElement xBod = new XElement(fullPropList[j][propAttributes[11]]);
                    xBod.Add(new XAttribute("n", fullPropList[j][propAttributes[7]]));
                    xBod.Add(new XAttribute("engineeringUnits", fullPropList[j][propAttributes[0]]));
                    xBod.Add(new XAttribute("limitLo", fullPropList[j][propAttributes[1]]));
                    xBod.Add(new XAttribute("limitHi", fullPropList[j][propAttributes[2]]));
                    xBod.Add(new XAttribute("limitLoLo", fullPropList[j][propAttributes[3]]));
                    xBod.Add(new XAttribute("limitHiHi", fullPropList[j][propAttributes[4]]));
                    xBod.Add(new XAttribute("bodName", fullPropList[j][propAttributes[6]]));
                    xBod.Add(new XAttribute("bodPrecision", fullPropList[j][propAttributes[10]]));
                    xBod.Add(new XAttribute("v1", textBoxValueRangeFrom.Text));
                    xBod.Add(new XAttribute("v2", textBoxValueRangeTo.Text));
                    xBod.Add(new XAttribute("t", fullDataList[j][dataAttributes[2]]));
                    xBod.Add(new XAttribute("q", fullDataList[j][dataAttributes[3]]));
                    xSource.Add(xBod);
                }
                xdoc.Save(sourcesXml);

                string UTCDate = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
                XDocument xdocProp = new XDocument();
                xdocProp.Add(new XElement("prop", new XAttribute("d", UTCDate)));
                XElement xrootProp = xdocProp.Element("prop");

                foreach (Dictionary<string, string> item in fullPropList)
                {
                    xrootProp.Add(new XElement("bod"));
                    XElement xbod = xrootProp.Elements().Last<XElement>();
                    foreach (KeyValuePair<string, string> DE in item) 
                        xbod.Add(new XAttribute(DE.Key, DE.Value));
                }
                if (!Directory.Exists(System.IO.Path.Combine(savePath, xAlias)))
                    Directory.CreateDirectory(System.IO.Path.Combine(savePath, xAlias));
                xdocProp.Save(System.IO.Path.Combine(savePath, xAlias, "prop.xml"));

                UTCDate = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
                XDocument xdocData = new XDocument();
                xdocData.Add(new XElement("data", new XAttribute("d", UTCDate)));
                XElement xrootData = xdocData.Element("data");

                foreach (Dictionary<string, string> item in fullDataList)
                {
                    xrootData.Add(new XElement("bod"));
                    XElement xbod = xrootData.Elements().Last<XElement>();
                    foreach (KeyValuePair<string, string> DE in item)
                        xbod.Add(new XAttribute(DE.Key, DE.Value));
                }
                if (!Directory.Exists(System.IO.Path.Combine(savePath, xAlias)))
                    Directory.CreateDirectory(System.IO.Path.Combine(savePath, xAlias));
                xdocData.Save(System.IO.Path.Combine(savePath, xAlias, "data.xml"));
            }

            MessageBox.Show(
                "Initial data files and setpoints values for selected schemas were created successfully.",
                "Action complete",
                MessageBoxButton.OK);
        }

        private void buttonStartEmulate_Click(object sender, RoutedEventArgs e)
        {
            updateTime = Convert.ToInt32(Math.Round((Single.Parse(textBoxUpdateTime.Text) * 1000)));

            foreach (var tObj in (listBoxSources.ItemsSource as List<src>).Where(myObj => myObj.enabled))
            {
                DispatcherTimer timer = new DispatcherTimer();  // если надо, то в скобках указываем приоритет, например DispatcherPriority.Render
                timer.Tick += new EventHandler(timerTick);
                timer.Interval = new TimeSpan(0, 0, 0, 0, updateTime);
                timer.Tag = tObj.alias;
                timers.Add(timer);
                timer.Start();
            }

        }

        private void timerTick(object sender, EventArgs e)
        {
            string alias = (sender as DispatcherTimer).Tag.ToString();

            XDocument xdocSources = XDocument.Load(sourcesXml);
            XElement xSource = null;
            foreach (XElement xe in xdocSources.Element("sources").Elements("source"))
            {
                if (xe.Attribute("alias").Value == alias)
                {
                    xSource = xe;
                    break;
                }
            }
            if (xSource == null)
                return;
            List<XElement> xBods = xSource.Elements().ToList<XElement>();

            XDocument xdoc = XDocument.Load(System.IO.Path.Combine(savePath, alias, "data.xml"));
            XElement xroot = xdoc.Element("data");
            string UTCDate = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            xroot.Attribute("d").Value = UTCDate;
            int rngSeed = (int)DateTime.Now.Ticks & 0x0000FFFF;
            analogType = textBoxType.Text;
            int i = 0;
            foreach (XElement xe in xroot.Elements("bod").ToList())
            {
                double range1 = Double.Parse(xBods[i].Attribute("v1").Value);
                double range2 = Double.Parse(xBods[i].Attribute("v2").Value);
                Random rng = new Random(++rngSeed);
                if (xe.Attribute("t").Value == analogType)
                {
                    xe.Attribute("v").Value = (rng.NextDouble() * (range2 - range1) + range1).ToString();
                }
                else if (xe.Attribute("t").Value == boolType)
                {
                    xe.Attribute("v").Value = Convert.ToBoolean(rng.Next(2)).ToString();
                }
                i++;
            }
            xdoc.Save(System.IO.Path.Combine(savePath, alias, "data.xml"));
            label_lastUpdateTime.Content = DateTime.Now.ToString();
        }

        private void buttonStopEmulate_Click(object sender, RoutedEventArgs e)
        {
            foreach (DispatcherTimer timer in timers)
            {
                timer.Stop();
            }
            MessageBox.Show(
                "Current emulation process is stopped.",
                "Action complete",
                MessageBoxButton.OK);
        }

        private void buttonOpenGridWindow_Click(object sender, RoutedEventArgs e)
        {
            GridWindow gridWindow = new GridWindow();
            if (listBoxSources.SelectedIndex > -1)
            {
                string alias = (listBoxSources.ItemsSource as List<src>)[listBoxSources.SelectedIndex].alias;
                gridWindow.Title = "Setpoints for " + alias;
                gridWindow.Tag = alias;
                gridWindow.listView.ItemsSource = getGridItemsSourceFromXmlFile(alias);
                gridWindow.Show();
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            XDocument xdoc = XDocument.Load(sourcesXml);
            List<src> sourcesList = (List<src>)listBoxSources.ItemsSource;
            foreach (XElement xe in xdoc.Element("sources").Elements("source"))
            {
                for (int i = 0; i < sourcesList.Count; i++)
                {
                    if (xe.Attribute("alias").Value == sourcesList[i].alias)
                    {
                        xe.SetAttributeValue("enabled", "true");
                        break;
                    }
                }
            }
            xdoc.Save(sourcesXml);
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            XDocument xdoc = XDocument.Load(sourcesXml);
            List<src> sourcesList = (List<src>)listBoxSources.ItemsSource;
            foreach (XElement xe in xdoc.Element("sources").Elements("source"))
            {
                for (int i = 0; i < sourcesList.Count; i++)
                {
                    if (xe.Attribute("alias").Value == sourcesList[i].alias)
                    {
                        xe.SetAttributeValue("enabled", "false");
                        break;
                    }
                }
            }
            xdoc.Save(sourcesXml);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            foreach (Window w in App.Current.Windows)
                w.Close();
        }
    }
}
