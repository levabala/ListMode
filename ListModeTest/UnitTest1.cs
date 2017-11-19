using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using ListModeInstance;
using ListModeManager;

namespace ListModeTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            string path = Directory.GetCurrentDirectory() + "\\" + DateTime.Now.ToString("yyyy");
            if (Directory.Exists(path))
                Directory.Delete(path, true);

            Experiment exp1 = new Experiment("Exp One", "./one.raw", rndDateTime(), rndDateTime());
            Experiment exp2 = new Experiment("Exp Two", "./two.raw", rndDateTime(), rndDateTime());
            Experiment exp3 = new Experiment("Exp Three", "./three.raw", rndDateTime(), rndDateTime());            
            File.WriteAllText(exp1.rawPath, "asd");
            File.WriteAllText(exp2.rawPath, "asd");
            File.WriteAllText(exp3.rawPath, "asd");

            LMManager manager = new LMManager(Directory.GetCurrentDirectory());
            manager.appendRaw(exp1);
            manager.appendRaw(exp2);
            manager.appendRaw(exp3);
        }

        private static Random rnd = new Random();
        private DateTime rndDateTime()
        {
            int count = rnd.Next(0, 10);
            return DateTime.Now.AddDays(count);
        }
    }
}
