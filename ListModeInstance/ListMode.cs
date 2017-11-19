using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Xml;
using System.Runtime.Serialization.Formatters.Binary;

namespace ListModeInstance
{    
    [Serializable]
    public class ListMode
    {
        public static string LOW_DIR = "low";
        public static string HIGH_DIR = "high";
        public static string DETAILS_NAME = "details";
        public static string CONFIG_FILE_NAME = "ListMode.config";        

        static string START_TIME = "startTime";
        static string END_TIME = "endTime";
        
        DateTime startTime, endTime;
        public string directory, rawPath, rawName, highResPath, lowResPath;

        public ListMode()
        {

        }

        public ListMode(string directory, Experiment exp)
            : this(directory, exp.rawPath, exp.startTime, exp.endTime)
        {

        }

        public ListMode(string directory, string rawPath, DateTime startTime, DateTime endTime)
        {
            rawName = Path.GetFileName(rawPath);
            this.startTime = startTime;
            this.endTime = endTime;

            //check if .raw isn't already here
            string path = directory + "\\" + GenerateFolderName(startTime, Path.GetFileNameWithoutExtension(rawPath));
            Directory.CreateDirectory(path);
            if (path != new FileInfo(rawPath).Directory.FullName)
                File.Copy(rawPath, path + "\\" + rawName);
            Setup(path);

            saveDetails();
        }

        public ListMode(string directory)
        {
            rawName = Path.GetFileName(rawPath);
            Setup(directory);
            restoreDetails();
        }

        private void Setup(string directory)
        {
            string[] raws = Directory.GetFiles(directory);
            if (raws.Length != 1)
                throw new Exception("Invalid *.raw amount in the directory");

            this.directory = directory;
            highResPath = directory + "//" + HIGH_DIR;
            lowResPath = directory + "//" + LOW_DIR;

            if (!Directory.Exists(highResPath))
                Directory.CreateDirectory(highResPath);
            if (!Directory.Exists(lowResPath))
                Directory.CreateDirectory(lowResPath);

            rawPath = raws[0];            
        }

        private void restoreDetails()
        {
            string path = directory + "//" + DETAILS_NAME + ".xml";
            if (File.Exists(path))
            {
                XmlReader reader = XmlReader.Create(path);
                startTime = DateTime.Parse(reader[START_TIME]);
                endTime = DateTime.Parse(reader[END_TIME]);                  
            }                
        }

        private void saveDetails()
        {
            string path = directory + "\\" + DETAILS_NAME + ".xml";
            XmlDocument document = new XmlDocument();

            XmlNode root = document.CreateElement("Configuration");
            XmlNode st = document.CreateElement(START_TIME);
            st.InnerText = startTime.ToString();
            XmlNode et = document.CreateElement(END_TIME);
            et.InnerText = endTime.ToString();

            root.AppendChild(st);
            root.AppendChild(et);

            document.AppendChild(root);
            document.Save(path);
        }        

        private void setRaw(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException();

            if (new FileInfo(path).Directory.FullName != directory)
                File.Copy(path, directory + "//" + Path.GetFileName(path));
        }
        
        public static string GenerateFolderName(DateTime dateTime, string name)
        {
            return String.Format("Day{0}_{1}", dateTime.Day, name);
        }

        public delegate void ProcessorCallback(int[][] flashNeutrons, double progress);
        public static void Parse(
            List<string> filesNames, int strob, int channelsCount, int channelWidth,
            int framesCount, int tau, double[] kt, int[] detectors, int channel0,
            ProcessorCallback[] ProcessorCallbacks)
        {
            int frame = 0;
            int save_count = 0;
            int saves_done = 0;
            int threadsAlive = 0;
            bool parsingFinished = false;
            HashSet<int> detectorsHashSet = new HashSet<int>(detectors);

            for (int k = 0; k < filesNames.Count; k++)
            {
                string nam = filesNames[k];

                FileStream fs = File.OpenRead(nam);
                long len = fs.Length;

                BinaryReader br = new BinaryReader(fs);
                long pos = 0;

                Stopwatch sw = Stopwatch.StartNew();

                float speed_mbs = 0;
                float speed_x = 0;
                int lastFrameNeutrons = 0;

                int[] kk = new int[0x100];

                long fbeg = 0;
                long fend = 0;
                long f0 = 0;
                long f1 = 0;
                long f2 = 0;
                long f3 = 0;
                long f4 = 0;
                long f5 = 0;
                long f6 = 0;
                long f7 = 0;
                long f8 = 0;

                int maxDetector = detectors.Max();

                List<int>[] neutrons = new List<int>[maxDetector + 1];
                for (int d = 0; d <= maxDetector; d++)
                {
                    neutrons[d] = new List<int>();
                }

                int neutronsCount = 0;

                while (pos < len)
                {
                    Stopwatch sw2 = Stopwatch.StartNew();

                    byte[] buf = br.ReadBytes(1000000);
                    pos += buf.Length;

                    for (int i = 0; i < buf.Length; i++)
                    {
                        int lo = (buf[i++]) | (buf[i++] << 8) | (buf[i++] << 16);

                        byte hi = buf[i];
                        kk[hi]++;

                        switch (hi)
                        {
                            case 0xf0: f0 = lo; break;
                            case 0xf1: f1 = lo; break;
                            case 0xf2: f2 = (long)lo | ((long)f4) << 24; break;
                            case 0xf3: f3 = (long)lo | ((long)f4) << 24; break;
                            case 0xf4: f4++; break;
                            case 0xf5: f5 = (long)lo | ((long)f4) << 24; break;
                            case 0xf6: f6 = (long)lo | ((long)f4) << 24; break;
                            case 0xf7: f7 = (long)lo | ((long)f4) << 24; break;
                            case 0xf8: f8 = (long)lo | ((long)f4) << 24; break;
                            case 0xfb: fbeg = (lo + ((long)(f4) << 24)); break;
                            case 0xfa:
                                fend = (long)lo | ((long)(f4) << 24); frame += 1;
                                if (frame % framesCount == 0)
                                {
                                    float spec_time = (float)(fend * 16e-9);

                                    int neutronsDelta = neutronsCount - lastFrameNeutrons;
                                    lastFrameNeutrons = neutronsCount;

                                    int[][] array = new int[neutrons.Length][];
                                    for (int ii = 0; ii < array.Length; ii++)
                                        array[ii] = neutrons[ii].ToArray();

                                    Thread summatorThread = new Thread((object arg) =>
                                    {
                                        save_count++;

                                        speed_x = (float)(spec_time / sw.Elapsed.TotalSeconds);
                                        speed_mbs = (float)(buf.Length / sw2.Elapsed.TotalSeconds / 1000000.0);

                                        double parsing = ((float)pos / len);

                                        foreach (ProcessorCallback callback in ProcessorCallbacks)
                                            callback(arg as int[][], parsing);
                                        //SummatorCall(arg, save_count, parsing, ref saves_done);

                                        Console.WriteLine(
                                        "saves: {0,5}  speed: {3,6:f2}x  threads: {4,2}  time: {1,8:f2}  frame: {2,6}  parsing: {5,4:f1}%",//  neutronsCount: {4}  neutronsDelta: {5}", 
                                        saves_done, spec_time, frame, speed_x, threadsAlive, parsing);// neutronsCount, neutronsDelta);                                                                                                                                                   

                                        threadsAlive--;
                                    });
                                    summatorThread.IsBackground = true;
                                    threadsAlive++;
                                    summatorThread.Start(array);

                                    for (int d = 0; d < neutrons.Length; d++)
                                        neutrons[d] = new List<int>();
                                }
                                break;
                        }


                        if (hi < detectors.Max() && detectorsHashSet.Contains(hi))//detectors.Contains(hi))
                        {
                            long t = (long)lo | ((long)f4) << 24;
                            if (t > fbeg && fbeg > fend)
                            {
                                float tmks = (float)((t - fbeg) * 16e-3);
                                int tch = (int)(tmks / tau * kt[hi]) - channel0;
                                if (tch >= 0 && tch < channelsCount)
                                {
                                    neutrons[hi].Add(tch);
                                    neutronsCount++;
                                }
                            }
                        }
                    }
                }
            }

            parsingFinished = true;
            if (threadsAlive == 0)
                foreach (ProcessorCallback callback in ProcessorCallbacks)
                    callback(new int[0][], 1);
        }

        public static ProcessorCallback lowSummer = new ProcessorCallback((flash, progress) =>
        {

        });

        public static ProcessorCallback highSummer = new ProcessorCallback((flash, progress) =>
        {

        });

        public static ProcessorCallback detectorsSplitter = new ProcessorCallback((flash, progress) =>
        {

        });

        public static ProcessorCallback listIndexer = new ProcessorCallback((flash, progress) =>
        {

        });

        public byte[] Serialize()
        {
            using (var memoryStream = new MemoryStream())
            {
                (new BinaryFormatter()).Serialize(memoryStream, this);
                return memoryStream.ToArray();
            }
        }

        public void Save(string directory)
        {
            string path = directory + "//" + CONFIG_FILE_NAME;
            File.WriteAllBytes(path, Serialize());
        }
    }
}
