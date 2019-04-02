using System;
using System.IO;
using SimpleXML;
using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;
using System.Threading;
using System.Runtime.InteropServices;

namespace SimpleXML_term
{
    class Program
    {
        // Diagnostics & Test Data
        static Stopwatch stopwatch;
        static string UTF16uri = @"C:\Users\Simon\Documents\GitHub\Hamann\XML_Aktuell\2019-03-07\HAMANN.xml";
        static string UTF8uri = @"C:\Users\Simon\Documents\GitHub\Hamann\XML_Aktuell\2019-03-07\HAMANN-UTF8BOM.xml";
        static void Main(string[] args)
        {
            bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                UTF16uri = @"/home/simon/repos/SimpleXML/HAMANN.xml";
                UTF8uri = UTF16uri;
            }
            
            sleep();
            TestSXML(UTF16uri);
            sleep();
            TestXmlTextReader(UTF8uri);
            sleep();
            TestXmlReader(UTF8uri);
            sleep();
            TestLinq(UTF8uri);
            sleep();
            // Console.ReadKey();
        }

        public static void sleep() => Thread.Sleep(2000);

        public static void TestSXML(string uri)
        {
            // Prep
            stopwatch = new Stopwatch();
            MemoryStream ms = new MemoryStream(File.ReadAllBytes(uri), 0, Convert.ToInt32(new FileInfo(uri).Length));
            stopwatch.Start();
            
            // Initialization of a reader
            SimpleDoc reader = new SimpleDoc();

            // Subscribe to events managed by your code here
            // reader.MgmtEvents.StartUpComplete += AcceptStartup;

            // Load the data into the parser
            reader.Load(ms);

            // Take ihe reader for a spin
            reader._testRead();

            stopwatch.Stop();
            reader.Close();
            ms.Close();
            Console.WriteLine("SimpleXML: " + stopwatch.ElapsedMilliseconds.ToString());
        }

        public static void TestXmlTextReader(string uri)
        {
            stopwatch = new Stopwatch();
            var sa = new MemoryStream(File.ReadAllBytes(uri), 0, Convert.ToInt32(new FileInfo(uri).Length));
            stopwatch.Start();
            using (XmlTextReader xr = new XmlTextReader(sa))
            {
                while (xr.Read())
                {

                }
            }
            stopwatch.Stop();
            sa.Close();
            Console.WriteLine("XmlTextReader: " + stopwatch.ElapsedMilliseconds.ToString());

        }

        public static void TestXmlReader(string uri)
        {
            stopwatch = new Stopwatch();
            var sb = new MemoryStream(File.ReadAllBytes(uri), 0, Convert.ToInt32(new FileInfo(uri).Length));
            stopwatch.Start();
            using (XmlReader xr = XmlReader.Create(sb))
            {
                while (xr.Read())
                {
                    
                }
            }
            stopwatch.Stop();
            sb.Close();
            Console.WriteLine("XmlReader: " + stopwatch.ElapsedMilliseconds.ToString());
        }

        public static void TestLinq(string uri)
        {
            stopwatch = new Stopwatch();
            stopwatch.Start();
            XDocument doc = XDocument.Load(uri);
            stopwatch.Stop();
            Console.WriteLine("Linq: " + stopwatch.ElapsedMilliseconds.ToString());

        }

        // Unused Helper:
        public static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public static void AcceptStartup(object sender, EventArgs args)
        {
            Console.WriteLine("Startup complete!");
        }

        public static void AcceptOTag(object sender, EventArgs arg)
        {
            var a = arg as Element;
            Console.WriteLine(a.Name + " opened.");
        }

        public static void AcceptCTag(object sender, EventArgs arg)
        {
            var a = arg as Element;
            Console.WriteLine(a.Name + " closed.");
        }
    }
}
