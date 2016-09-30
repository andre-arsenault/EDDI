﻿using EDDI;
using SimpleFeedReader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Utilities;

namespace EliteDangerousGalnetMonitor
{
    public class GalnetMonitor : EDDIMonitor
    {
        private readonly string SOURCE = "https://community.elitedangerous.com/galnet-rss";

        private static readonly object monitorLock = new object();
        private Thread monitorThread;

        public string MonitorName()
        {
            return "Galnet Monitor";
        }

        public string MonitorVersion()
        {
            return "1.0.0";
        }

        public string MonitorDescription()
        {
            return "Monitor Galnet for new news items and generate suitable events when new items are posted";
        }

        public void Start()
        {
            lock (monitorLock)
            {
                if (monitorThread == null)
                {
                    monitorThread = new Thread(() => { monitor(); });
                    monitorThread.IsBackground = true;
                    monitorThread.Start();
                }
            }
        }

        public void Stop()
        {
            lock(monitorLock)
            {
                if (monitorThread != null)
                {
                    monitorThread.Abort();
                    monitorThread = null;
                }
            }
        }

        public System.Windows.Controls.UserControl ConfigurationTabItem()
        {
            return null;
        }

        private void monitor()
        {
            DateTime latestDate;
            try
            {
                latestDate = new DateTime(Convert.ToInt64(File.ReadAllText(Constants.DATA_DIR + @"\galnet")));
            }
            catch
            {
                latestDate = DateTime.Now;
                File.WriteAllText(Constants.DATA_DIR + @"\galnet", latestDate.Ticks.ToString());
            }
            while (true)
            {
                List<News> newsItems = new List<News>();
                DateTime latestNewDate = latestDate;
                foreach (FeedItem item in new FeedReader(new GalnetFeedItemNormalizer(), true).RetrieveFeed(SOURCE))
                {
                    News newsItem = new News(item.PublishDate.DateTime, item.Title, item.GetContent());
                    newsItems.Add(newsItem);
                    if (item.PublishDate > latestNewDate)
                    {
                        latestNewDate = item.PublishDate.DateTime;
                    }
                }

                if (newsItems.Count > 0)
                {
                    Eddi.Instance.eventHandler(new GalnetNewsPublishedEvent(DateTime.Now, newsItems));
                }
                if (latestNewDate > latestDate)
                {
                    latestDate = latestNewDate;
                    Logging.Debug("Updated latest date to " + latestDate.ToString());
                    //File.WriteAllText(Constants.DATA_DIR + @"\galnet", latestDate.Ticks.ToString());
                }
                Thread.Sleep(120000);
            }
        }
    }
}
