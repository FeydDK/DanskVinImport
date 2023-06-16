using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Threading;
using System.Timers;
using System.Xml;

class RealTimeClock
{
    static void Main(string[] args)
    {
        bool isInformationPrinted = false;
        DanskVinImport3.DVIService.monitorSoapClient ds = new DanskVinImport3.DVIService.monitorSoapClient();
        // Sæt den ønskede størrelse på konsolvinduet
        int consoleWidth = 130;
        int consoleHeight = 45;
        Console.SetWindowSize(consoleWidth, consoleHeight);

        Console.CursorVisible = false;

        // Vis logoet
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.SetCursorPosition(50, 0);
        string logo = @"
            
________                        __     ____   ____.__         .___                              __   
\______ \ _____    ____   _____|  | __ \   \ /   /|__| ____   |   | _____ ______   ____________/  |_ 
 |    |  \\__  \  /    \ /  ___/  |/ /  \   Y   / |  |/    \  |   |/     \\____ \ /  _ \_  __ \   __\
 |    `   \/ __ \|   |  \\___ \|    <    \     /  |  |   |  \ |   |  Y Y  \  |_> >  <_> )  | \/|  |  
/_______  (____  /___|  /____  >__|_ \    \___/   |__|___|  / |___|__|_|  /   __/ \____/|__|   |__|  
        \/     \/     \/     \/     \/                    \/            \/|__|                       
                                                           
";
        Console.WriteLine(logo);
        Console.ResetColor();

        // Start temperaturmåling
        TemperatureMonitor.StartMonitoring();

        // Start nyhedsovervågning
        NewsMonitor.StartMonitoring();

        // Opret TimeZoneInfo-objekter for de ønskede tidszoner
        TimeZoneInfo londonTimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
        TimeZoneInfo singaporeTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
        TimeZoneInfo newYorkTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

        // Initialiser variable til at følge tidligere værdier
        double previousOutdoorTemp = 0;
        double previousStockTemp = 0;
        List<string> stockItemsUnderMin = ds.StockItemsUnderMin();
        List<string> stockItemsOverMax = ds.StockItemsOverMax();
        List<string> stockItemsMostSold = ds.StockItemsMostSold();
        DateTime previousTime = DateTime.MinValue;

        // Start uret
        while (true)
        {
           
            // Tjek om temperaturen eller tiden er ændret
            bool temperatureChanged = TemperatureMonitor.GetOutdoorTemperature() != previousOutdoorTemp ||
                                      TemperatureMonitor.GetStockTemperature() != previousStockTemp;
            bool timeChanged = DateTime.Now != previousTime;

            // Opdater kun visningen, hvis der er ændringer
            if (temperatureChanged || timeChanged)
            {
                // Opdater temperaturvisningen
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.SetCursorPosition(0, 8);
                Console.WriteLine("\nTemperatur og fugtighed \n\nLager:");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"Temp: {TemperatureMonitor.GetStockTemperature()}°C\nFugt: {TemperatureMonitor.GetStockHumidity()}%\n");

                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("Udenfor:");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"Temp: {TemperatureMonitor.GetOutdoorTemperature()}°C\nFugt: {TemperatureMonitor.GetOutdoorHumidity()}%\n");

                Console.SetCursorPosition(0, 23);
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("\nDATO / TID\n");
                Console.ForegroundColor = ConsoleColor.White;
                Console.ResetColor();

                // Opdater tidsvisningen
                Console.SetCursorPosition(0, 26);
                DisplayTime("London:", londonTimeZone);
                DisplayTime("Singapore:", singaporeTimeZone);
                DisplayTime("New York:", newYorkTimeZone);

                // Vis nyhederne
                Console.SetCursorPosition(0, Console.WindowHeight - 5);
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("--------------------------------------------------------------");
                Console.WriteLine("Nyheder fra Nordjyske.dk:");

                // Opdater tidligere værdier
                previousOutdoorTemp = TemperatureMonitor.GetOutdoorTemperature();
                previousStockTemp = TemperatureMonitor.GetStockTemperature();
                previousTime = DateTime.Now;

                if (previousStockTemp < 10)
                {
                    Console.SetCursorPosition(50, 11);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write(new string(' ', Console.WindowWidth - 50)); // Ryd delen fra kolonne 50 til slutningen af linjen
                    Console.SetCursorPosition(50, 11);
                    Console.WriteLine("Temperatur er for lav");
                }
                else if (previousStockTemp > 14)
                {
                    Console.SetCursorPosition(50, 11);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(new string(' ', Console.WindowWidth - 50)); // Ryd delen fra kolonne 50 til slutningen af linjen
                    Console.SetCursorPosition(50, 11);
                    Console.WriteLine("Temperatur er for høj");
                }
                else
                {
                    Console.SetCursorPosition(50, 11);
                    Console.Write(new string(' ', Console.WindowWidth - 50)); // Ryd delen fra kolonne 50 til slutningen af linjen
                    Console.SetCursorPosition(50, 11);
                    Console.WriteLine("Temperatur er normal");
                    Console.ForegroundColor = ConsoleColor.Green;
                }
            }

            if (!isInformationPrinted)
            {
                Console.SetCursorPosition(50, 9);
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("Lagerstatus:");

                // Udskriv varer under minimum
                Console.SetCursorPosition(50, 23);
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("Varer under minimum:");
                Console.ForegroundColor = ConsoleColor.Red;
                for (int i = 0; i < stockItemsUnderMin.Count; i++)
                {
                    Console.SetCursorPosition(50, 25 + i);
                    Console.WriteLine(stockItemsUnderMin[i]);
                }

                // Udskriv varer over maksimum
                Console.SetCursorPosition(50, 28);
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("Varer over maksimum:");
                Console.ForegroundColor = ConsoleColor.Green;
                for (int i = 0; i < stockItemsOverMax.Count; i++)
                {
                    Console.SetCursorPosition(50, 30 + i);
                    Console.WriteLine(" " + stockItemsOverMax[i]);
                }

                // Udskriv mest solgte i dag
                Console.SetCursorPosition(50, 33);
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("Mest solgte i dag:");
                Console.ForegroundColor = ConsoleColor.White;
                for (int i = 0; i < stockItemsMostSold.Count; i++)
                {
                    Console.SetCursorPosition(50, 35 + i);
                    Console.WriteLine(" " + stockItemsMostSold[i]);
                }

                // Sæt flagvariablen til sandt
                isInformationPrinted = true;
            }

            // Vent i et sekund, før uret opdateres
            Thread.Sleep(1000);
        }
    }




    // Formater datostrengen
    static string FormatDate(DateTime dateTime)
    {
        CultureInfo cultureInfo = new CultureInfo("da-DK");
        string formattedDate = dateTime.ToString("d. MMMM yyyy", cultureInfo);
        string capitalizedFormattedDate = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(formattedDate);
        return capitalizedFormattedDate;
    }

    // Formater tidsstrengen
    static string FormatTime(DateTime dateTime)
    {
        return dateTime.ToString("HH:mm:ss");
    }

    // Vis tiden for en bestemt tidszone
    static void DisplayTime(string label, TimeZoneInfo timeZone)
    {
        Console.WriteLine(label);
        Console.WriteLine($"Dato: {FormatDate(TimeZoneInfo.ConvertTime(DateTime.Now, timeZone))}");
        Console.WriteLine($"Tid: {FormatTime(TimeZoneInfo.ConvertTime(DateTime.Now, timeZone))}");
        Console.WriteLine();
    }
}

class TemperatureMonitor
{
    private static double outdoorTemperature;
    private static double outdoorHumidity;
    private static double stockTemperature;
    private static double stockHumidity;
    private static bool isMonitoring;
    private static DanskVinImport3.DVIService.monitorSoapClient ds;

    public static void StartMonitoring()
    {
        // Opretter en DVIService-klient til temperaturdata
        ds = new DanskVinImport3.DVIService.monitorSoapClient();
        isMonitoring = true;

        // Starter en separat tråd til overvågning af temperatur
        Thread monitorThread = new Thread(MonitorTemperature);
        monitorThread.Start();
    }

    private static void MonitorTemperature()
    {
        while (isMonitoring)
        {
            // Henter temperaturdata fra DVIService
            outdoorTemperature = ds.OutdoorTemp();
            outdoorHumidity = ds.OutdoorHumidity();
            stockTemperature = ds.StockTemp();
            stockHumidity = ds.StockHumidity();

            // Venter i et minut før næste opdatering af temperatur
            Thread.Sleep(TimeSpan.FromMinutes(1));
        }
    }

    public static double GetOutdoorTemperature()
    {
        return outdoorTemperature;
    }

    public static double GetOutdoorHumidity()
    {
        return outdoorHumidity;
    }

    public static double GetStockTemperature()
    {
        return stockTemperature;
    }

    public static double GetStockHumidity()
    {
        return stockHumidity;
    }
}



class NewsMonitor
{
    private static List<SyndicationItem> newsItems; // En liste til at gemme nyhedsartiklerne
    private static System.Timers.Timer newsTimer; // En timer til at opdatere nyheder periodisk
    private static List<string> previousNews; // En liste til at gemme tidligere viste nyheder

    public static void StartMonitoring()
    {
        newsItems = new List<SyndicationItem>(); // Opretter en tom liste til nyhedsartikler
        previousNews = new List<string>(); // Opretter en tom liste til tidligere viste nyheder

        // Henter og viser de initiale nyheder
        FetchAndDisplayNews();

        // Starter timeren til at hente og opdatere nyhedsartikler
        TimeSpan updateInterval = TimeSpan.FromMinutes(1);
        newsTimer = new System.Timers.Timer(updateInterval.TotalMilliseconds);
        newsTimer.Elapsed += UpdateNews;
        newsTimer.Start();
    }

    private static void FetchAndDisplayNews()
    {
        SyndicationItem[] newNewsItems = FetchNewsFromRSS(); // Henter nyhedsartikler fra RSS-feed

        if (newNewsItems != null && newNewsItems.Length > 0)
        {
            // Tilføjer de nye nyhedsartikler til listen
            newsItems.Clear();
            newsItems.AddRange(newNewsItems);

            // Viser nyhederne
            DisplayNews(null);
        }
    }

    private static void UpdateNews(object sender, ElapsedEventArgs e)
    {
        // Henter og viser opdaterede nyheder
        FetchAndDisplayNews();
    }

    private static SyndicationItem[] FetchNewsFromRSS()
    {
        try
        {
            string url = "https://www.nordjyske.dk/rss/nyheder";
            XmlReader reader = XmlReader.Create(url);
            SyndicationFeed feed = SyndicationFeed.Load(reader); // Indlæser RSS-feedet
            reader.Close();
            return feed?.Items?.ToArray(); // Returnerer nyhedsartikler som et array
        }
        catch (Exception)
        {
            // Håndterer undtagelsen eller logger fejlen
            Console.WriteLine("Failed to fetch news from RSS feed:");
            return null;
        }
    }

    private static SyndicationItem[] GetRandomNews(int count)
    {
        if (newsItems != null && newsItems.Count > 0)
        {
            // Vælger tilfældige nyhedsartikler
            List<SyndicationItem> randomNews = new List<SyndicationItem>();
            Random random = new Random();

            while (randomNews.Count < count)
            {
                int randomIndex = random.Next(newsItems.Count);
                SyndicationItem randomItem = newsItems[randomIndex];

                if (!randomNews.Contains(randomItem))
                {
                    randomNews.Add(randomItem);
                }
            }

            return randomNews.ToArray(); // Returnerer tilfældige nyhedsartikler som et array
        }

        return null;
    }

    public static void DisplayNews(object state)
    {
        int count = 3;
        int consoleHeight = Console.WindowHeight;
        int startLine = consoleHeight - count;

        // Rydder tidligere viste nyheder
        ClearPreviousNews();

        Console.SetCursorPosition(0, startLine); // Indstiller cursorens position til visning af nyheder

        SyndicationItem[] randomNews = GetRandomNews(count);

        if (randomNews != null && randomNews.Length > 0)
        {
            for (int i = 0; i < randomNews.Length; i++)
            {
                string newsTitle = randomNews[i].Title.Text;
                previousNews.Add(newsTitle);
                Console.SetCursorPosition(0, startLine + i);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(newsTitle);
            }
        }
        else
        {
            Console.SetCursorPosition(0, startLine);
            Console.WriteLine("No news available.");
        }
    }

    private static void ClearPreviousNews()
    {
        int consoleHeight = Console.WindowHeight;
        int startLine = consoleHeight - 3;

        foreach (string newsTitle in previousNews)
        {
            Console.SetCursorPosition(0, startLine);
            Console.Write(new string(' ', Console.WindowWidth));
            startLine++;
        }

        previousNews.Clear(); // Rydder tidligere viste nyheder
    }

    public static void StopMonitoring()
    {
        newsTimer.Stop();
        newsTimer.Dispose();
    }
}