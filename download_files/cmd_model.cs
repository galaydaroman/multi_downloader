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
        private int thread_count = -1;
        private AutoResetEvent threadResetEvent;
        private long startTime;
        private bool validArguments;
        private bool started = false;


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
            md = new multi_downloader(file_list, dest_dir, threadResetEvent, thread_count);
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

            file_with_list = args[0];
            if (!File.Exists(file_with_list))
            {
                print_text(string.Format("Error: File \"{0}\" doesn't exists.", file_with_list));
                return false;
            }
            file_list = File.ReadAllLines(file_with_list).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

            if (args.Length >= 2)
            {
                dest_dir = args[1];
                try
                {
                    if (!Directory.Exists(dest_dir)) Directory.CreateDirectory(dest_dir);
                }
                catch
                {
                    dest_dir = ".";
                }
                dest_dir = Path.GetFullPath(dest_dir);
            }

            if (args.Length >= 3)
            {
                int c = 0;
                thread_count = int.TryParse(args[2], out c) ? c : -1;
            }

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
