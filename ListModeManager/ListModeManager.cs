using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ListModeInstance;
using System.IO;

namespace ListModeManager
{
    public class LMManager {         
        public string rootDirectory;    
        //year -> month -> ListMode
        public Dictionary<string, Dictionary<string, List<ListMode>>> listModeTree = new Dictionary<string, Dictionary<string, List<ListMode>>>();
        public List<ListMode> allListModes = new List<ListMode>();

        public LMManager(string rootDirectory)
        {
            setDirectory(rootDirectory);
        }

        public void appendRaw(Experiment exp)
        {
            appendRaw(exp.rawPath, exp.startTime, exp.endTime);
        }

        public void appendRaw(string rawPath, DateTime startTime, DateTime endTime)
        {
            string name = Path.GetFileName(rawPath);
            if (allListModes.Any((l) => { return name == l.rawName; }))
                return;

            string year = startTime.ToString("yyyy");
            string month = startTime.ToString("MMMM");

            string path = rootDirectory + "\\" + year + "\\" + month;            
            if (!listModeTree.ContainsKey(year))
            {
                listModeTree.Add(year, new Dictionary<string, List<ListMode>>());
                Directory.CreateDirectory(rootDirectory + "\\" + year);
            }
            if (!listModeTree[year].ContainsKey(month))
            {
                listModeTree[year].Add(month, new List<ListMode>());
                Directory.CreateDirectory(rootDirectory + "\\" + year + "\\" + month);
            }
            ListMode listMode = new ListMode(path, rawPath, startTime, endTime);
            listModeTree[year][month].Add(listMode);
        }

        private void setDirectory(string path)
        {
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException();

            allListModes.Clear();
            listModeTree.Clear();

            rootDirectory = path;
            string[] years = Directory.GetDirectories(rootDirectory);
            foreach(string dirPath in years)
            {
                string year = Path.GetDirectoryName(dirPath);
                listModeTree[year] = new Dictionary<string, List<ListMode>>();

                string[] months = Directory.GetDirectories(dirPath);
                foreach (string monthPath in months)
                {
                    string month = Path.GetDirectoryName(monthPath);
                    listModeTree[year][month] = new List<ListMode>();

                    string[] lms = Directory.GetDirectories(monthPath);
                    foreach (string lmFolder in lms)
                    {
                        ListMode listMode;
                        try
                        {
                            listMode = new ListMode(lmFolder);
                            allListModes.Add(listMode);
                            listModeTree[year][month].Add(listMode);
                        }
                        catch (Exception e)
                        {

                        }
                    }                    
                }
            }
        }        
    }

}
