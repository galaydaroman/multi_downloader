using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace download_files
{
    public class multi_downloader
    {
        private object locker = new object();
        private int parallel_count = 3;
        private download_file[] downloader;
        private int started_count = 0;
        private int process_line_start = 0;
        
        private long total_bytes = 0;
        public long TotalBytes { get { return total_bytes; } }
        private string dest_dir;
        public string DestinationDir { get { return dest_dir; } }
        private string[] files;
        public string[] Files { get { return files; } }

        public string ErrorList = null;




        // =======================================

        private System.Threading.AutoResetEvent resetEvent;

        public multi_downloader(string[] files, string dest_dir, System.Threading.AutoResetEvent resetEvent, int threads = -1)
        {
            this.files = files;
            this.dest_dir = dest_dir;
            this.resetEvent = resetEvent;
            started_count = 0;
            parallel_count = threads > 0 ? threads : parallel_count;

            downloader = new download_file[parallel_count];
            for (int i = 0; i < parallel_count; i++)
            {
                downloader[i] = new download_file(DestinationDir, i + 1);
                downloader[i].DownloadProgress += new DownloadProgressHandler(download_progress);
                downloader[i].DownloadCompleted += new DownloadCompletedHandler(download_completed);
            }

            Console.WriteLine(string.Format(@"
  Downloader started.
  {0} files to be downloaded.
  Download directory: ""{1}""  
  Parallel in: {2} threads
  
  ============================

"
                , files.Count(), dest_dir, parallel_count));
        }

        public void process()
        {
            Console.CursorVisible = false;
            process_line_start = Console.CursorTop;
            for (int i = 0; i < parallel_count && i < files.Count(); i++)
            {
                downloader[i].download(files[i], process_line_start + started_count);
                started_count++;
            }
        }

        private void download_completed(object sender)
        {
            lock (locker)
            {
                download_file current = (download_file)sender;
                Console.SetCursorPosition(0, current.ConsoleLine);
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.ForegroundColor = current.Error == null ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed;
                Console.WriteLine(
                    string.Format("{0,5}  -> {4,14}{3,8}  {1,-30}  {2,-30}",
                        getIcon(current.Status),
                        current.FileName,
                        current.Error == null ? "" : string.Format("Error: {0}", current.Error.Message),
                        ContentSizeToStr(current.ContentBytes),
                        string.Empty
                        )
                    .PadRight(Console.WindowWidth - 1));
                Console.ResetColor();
                total_bytes += current.ContentBytes;

                if (current.Error != null)
                {
                    ErrorList += string.Format("{0,5}File: {1}\n{0,5}Error message: {2}\n\n",
                        " ",
                        current.FileName,
                        current.Error.Message + (current.Error.InnerException != null ? ("\nInnerMessage: " + current.Error.InnerException.Message) : ""));
                }
            }
            download_file _current = (download_file)sender;

			//check to finish
			if (started_count == files.Count() && 
				downloader.Where(x => x.Status == DownloadFileStatus.SUCCESS || 
                x.Status == DownloadFileStatus.ERROR).Count() == parallel_count)
			{
                Console.SetCursorPosition(0, downloader.Max(x => x.ConsoleLine) + 2);
                Console.ResetColor();
                Console.WriteLine(" -------------------------");

                if (ErrorList != null)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine(ErrorList);
                    Console.ResetColor();
                    Console.WriteLine(" -------------------------");
                }
                
                resetEvent.Set();
			}
			else if (started_count != files.Count())
			{
				_current.download(files[started_count], process_line_start + started_count);
				started_count++;
			}
        }

        private void download_progress(object sender, DownloadProgressEventArgs e)
        {
            lock (locker)
            {
                download_file current = (download_file)sender;
                int p = (int)(Math.Round((double)e.bytes * 100 / (current.ContentBytes == 0 ? e.bytes : current.ContentBytes)));
                Console.SetCursorPosition(0, current.ConsoleLine);
                if (p > 0) Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(
                    string.Format("{0,5}{3,3}{1,4}% {4,8} {5,8}   {2,-30}",
                        getIcon(current.Status),
                        p,
                        e.name,
                        current.IndexThread,
                        ContentSizeToStr(e.speed),
                        ContentSizeToStr(current.ContentBytes))
                    .PadRight(Console.WindowWidth - 1));
                Console.ResetColor();
            }
        }

        public static string ContentSizeToStr(double s)
        {
            string[] type = new string[] {"B", "KB", "MB", "GB"};
            double speed = s;
            string res = string.Empty;
            for (int i = 0; i < type.Length; i++)
            {
                if (i > 0) speed /= 1024;
                if (speed > 1024) continue;
                else
                {
                    res = string.Format("{0:###0.#}", speed) + type[i];
                    break;
                }
            }
            return res;
        }

        public void free()
        {
            foreach (var t in downloader.Where(x => x != null))
            {
                t.free();
            }
        }

        private string getIcon(DownloadFileStatus st)
        {
            string res = null;
            switch (st)
            {
                case DownloadFileStatus.INIT: res = "+"; break;
                case DownloadFileStatus.DOWNLOAD: res = ">"; break;
                case DownloadFileStatus.SUCCESS: res = "*"; break;
                case DownloadFileStatus.ERROR: res = "E"; break;
            }
            return string.Format("{0}", res);
        }

    }
}
