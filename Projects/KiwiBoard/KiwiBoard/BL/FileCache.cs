using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace KiwiBoard.BL
{
    public class FileCache
    {
        public static FileCache Default = new FileCache();

        public static string CacheFolder = System.Web.Hosting.HostingEnvironment.MapPath(@"~/App_Data");

        static FileCache()
        {
            foreach (var file in Directory.EnumerateFiles(CacheFolder, "*.cache"))
            {
                if ((DateTime.Now - File.GetLastWriteTime(file)).TotalDays > 7)
                {
                    File.Delete(file);
                }
            }
        }

        public string this[string fileName]
        {
            get
            {
                var file = Directory.EnumerateFiles(CacheFolder, fileName, SearchOption.TopDirectoryOnly).SingleOrDefault();
                return file != null ? File.ReadAllText(file) : null;
            }
        }

        public bool Contains(string fileName)
        {
            return Directory.EnumerateFiles(CacheFolder, fileName, SearchOption.TopDirectoryOnly).SingleOrDefault() != null;
        }

        public void Set(string fileContent, string fileName)
        {
            File.WriteAllText(Path.Combine(CacheFolder, fileName), fileContent);
        }

        public void SetProfile(string content, string jobId, string machine)
        {
            this.Set(content, string.Format("profile_{0}_{1}.cache", jobId, machine));
        }

        public string TryGetProfile(string jobId, out string machine)
        {
            var searchName = string.Format("profile_{0}_*", jobId);
            var file = Directory.EnumerateFiles(CacheFolder, searchName, SearchOption.TopDirectoryOnly).SingleOrDefault();
            if (file != null)
            {
                machine = Regex.Match(file, @"(?<=profile_[^_]+_)[^_]+(?=.cache)").Value;
                return File.ReadAllText(file);
            }
            else
            {
                machine = null;
                return null;
            }
        }
    }
}