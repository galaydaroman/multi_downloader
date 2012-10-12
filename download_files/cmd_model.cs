using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace download_files
{
    public class cmd_model
    {
        private string dest_dir = ".";
        public string DestDir
        {
            get { return dest_dir; }
        }
        private string[] file_list = null;
        public string[] FileList
        {
            get { return file_list; }
        }

        private string file_with_list = null;
        public string FileWithList
        {
            get { return file_with_list; }
        }

        private multi_downloader md;
        private AutoResetEvent threadResetEvent;
        private long startTime;
        private bool validArguments;
        private bool started = false;
        private Dictionary<string, object> flags;


        // ============================

        public cmd_model(string[] args)
        {
            validArguments = loadAndCheckArguments(args);
            threadResetEvent = new AutoResetEvent(false);
        }

        public void start()
        {
            if (!validArguments) return;
            startTime = DateTime.Now.Ticks;
            md = new multi_downloader(file_list, dest_dir, threadResetEvent, (int)(flags["threads"]));
            md.process();
            started = true;
        }

        public void endWait()
        {
            if (!started) return;
            threadResetEvent.WaitOne();
            md.free();

            TimeSpan time = TimeSpan.FromTicks(DateTime.Now.Ticks - startTime);
            long content_size = md.TotalBytes;
            double speed = content_size / time.TotalSeconds;
            string time_str = string.Format("{0}:{1}:{2}", 
                time.Hours.ToString().PadLeft(2, '0'), 
                time.Minutes.ToString().PadLeft(2, '0'), 
                time.Seconds.ToString().PadLeft(2, '0'));
            Console.ResetColor();
            Console.WriteLine("\n\n Done\n Time: {0}\n Content size: {1}\n Avarage speed: {2}", 
                time_str, multi_downloader.ContentSizeToStr(content_size), multi_downloader.ContentSizeToStr(speed));
            Console.CursorVisible = true;
        }

        private bool loadAndCheckArguments(string[] args)
        {
            print_about();
            if (args == null || args.Length == 0)
            {
                print_help();
                return false;
            }

            // Parsing Arguments
            file_with_list = null;
            dest_dir = null;
            flags = new Dictionary<string, object>();
            string[] empty_flags = new string[] { "i", "interface" };

            foreach (var arg in args)
            {
                var last = flags.LastOrDefault();
                if (flags.Count > 0 && last.Value == null && !empty_flags.Any(x => x == last.Key))
                {
                    flags[last.Key] = arg;
                    continue;
                }

                // Flag
                if (arg.StartsWith("-"))
                {
                    flags[arg.Substring(1).ToLower()] = null;
                }
                else
                {
                    if (file_with_list == null) file_with_list = arg;
                    else if (dest_dir == null) dest_dir = arg;
                }
            }

            //Process flags
            for (var i = 0; i < flags.Count; i++)
            {
                var flag = flags.ElementAt(i);
                switch (flag.Key)
                {
                    case "threads":
                    case "t":
                        int c;
                        flags["threads"] = int.TryParse(flag.Value.ToString(), out c) ? c : -1;
                        break;
                    case "interface":
                    case "i":
                        //without value
                        flags["interface"] = null;
                        break;
                }
            }

            //Interface call
            if (flags.ContainsKey("interface"))
            {
                var result = ParamsForm.OpenDialog();

                if (result != null)
                {
                    file_with_list = null;
                    file_list = result.Links.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
                    dest_dir = result.DestinationFolder;
                    flags["threads"] = result.ThreadCount;
                }
                else if (file_with_list == null)
                {
                    print_help();
                    return false;
                }
            }

            //Check file list and load links
            if (file_list == null && !File.Exists(file_with_list))
            {
                print_text(string.Format("Error: File \"{0}\" doesn't exists.", file_with_list));
                return false;
            }
            else if (file_list == null)
            {
                file_list = File.ReadAllLines(file_with_list).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            }
            

            //Output parameter
            try
            {
                if (!Directory.Exists(dest_dir)) Directory.CreateDirectory(dest_dir);
            }
            catch
            {
                dest_dir = ".";
            }
            dest_dir = Path.GetFullPath(dest_dir);

            return true;
        }

        public void print_help()
        {
            print_text(Resource.help_text);
        }

        public void print_about()
        {
            Console.WriteLine();
            print_text(Resource.about_text);
        }

        public void print_text(string t)
        {
            Console.WriteLine(t);
            Console.WriteLine();
        }

    }
}
