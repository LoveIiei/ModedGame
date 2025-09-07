using System;
using System.Data.Common;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace TryRenam
{
    internal class Program
    {
        static void Main(string[] args)
        {
            int num = 121;
            string n_name = $"pl{num}_0000";
            string mPath = @"C:\Users\huawe\Downloads\冰原-惠惠人物（替换男女【皮制】套）\冰原-惠惠人物（替换男女【皮制】套）\nativePC\pl";
            string[] files = Directory.GetDirectories(mPath);
            foreach (string dir in files)
            {
                if (!dir.Contains("f_equip") && !dir.Contains("m_equip")) continue;
                string newDir = Path.Combine(dir, n_name);
                string[] subfolders = Directory.GetDirectories(dir);
                foreach (string subfolder in subfolders)
                {
                    if (!subfolder.Contains(num.ToString()))
                    {
                        Directory.Move(subfolder, newDir);
                    }
                    string[] elements = Directory.GetDirectories(newDir);
                    foreach (string element in elements)
                    {
                        string[] dataFiles = Directory.GetFiles(Path.Combine(element, "mod"));
                        string pattern = @"\d+";
                        foreach (string file in dataFiles)
                        {
                                string dataName = Path.GetFileName(file)!;
                                if (!dataName.Contains(num.ToString()))
                                {
                                bool replaced = false;
                                string newDataName = Regex.Replace(dataName, @"\d+", match =>
                                {
                                    if (!replaced)
                                    {
                                        replaced = true;        // replace only the first match
                                        return num.ToString();
                                    }
                                    return match.Value;         // keep the rest unchanged
                                });
                                Directory.Move(file, file.Replace(dataName, newDataName));
                                }
                        }
                    }
                }
            }
        }
    }
}
