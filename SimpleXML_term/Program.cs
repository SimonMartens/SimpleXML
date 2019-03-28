﻿using System;
using System.IO;
using SimpleXML;
using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;
using System.Threading;

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
            sleep();
            TestSXML(UTF16uri);
            sleep();
            TestXmlTextReader(UTF8uri);
            sleep();
            TestXmlReader(UTF8uri);
            sleep();
            TestLinq(UTF8uri);
            sleep();
            TestStreamReading(UTF16uri);
            // Console.ReadKey();
        }

        public static void sleep() => Thread.Sleep(4000);

        public static void TestSXML(string uri)
        {
            stopwatch = new Stopwatch();
            MemoryStream ms = new MemoryStream(File.ReadAllBytes(uri), 0, Convert.ToInt32(new FileInfo(uri).Length));
            stopwatch.Start();
            using (SimpleDoc reader = new SimpleDoc(ms))
            {
                reader._testRead();
                stopwatch.Stop();
            }
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

        public static void TestStreamReading(string uri)
        {

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
    }
}