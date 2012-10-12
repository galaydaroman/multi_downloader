using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Net;
using System.IO;
using System.Threading;

namespace download_files
{
    class download_file
    {
        private static object locker_df = new object();
        private Uri uri;
        private string url;
        private DownloadFileStatus status;
        private Exception error;
        private long contentBytes = 0;
        private int console_line = 0;
        private int indexThread;
        
        public Uri URI { get { return uri; } }
        public string FileName { get; set; }
        public string Url { get { return url; } }
        public DownloadFileStatus Status { get { return status; } }
        public Exception Error { get { return error; } }
        public long ContentBytes { get { return contentBytes; } }
        public int ConsoleLine { get { return console_line; } }
        public int IndexThread { get { return indexThread; } }

        // =========================

        private WebClient web_client;
        private Stream stream;

        private string dest_dir;
        private FileStream file_stream;
        private Thread thread;
        private AutoResetEvent threadReset;
        private IAsyncResult currentAsyncResult;

        public DownloadCompletedHandler DownloadCompleted { get; set; }
        public DownloadProgressHandler DownloadProgress { get; set; }


        public download_file(string dest_dir, int index)
        {
            this.web_client = new WebClient();
            this.dest_dir = dest_dir;
            this.status = DownloadFileStatus.SUCCESS;

            this.indexThread = index;
            this.threadReset = new AutoResetEvent(false);
            this.thread = new Thread(downloadAsync);
            thread.Start();
        }

        public void download(string url, int c_line)
        {
            if (status == DownloadFileStatus.INIT || status == DownloadFileStatus.DOWNLOAD) return;
            reset();
            console_line = c_line;
            this.url = url;
            uri = new Uri(url);

            // start thread
            threadReset.Set();
        }

        private void downloadAsync()
        {
            while (true)
            {
                threadReset.WaitOne();
                Thread.Sleep(250);
                defineTotalBytes();
                try
                {
                    stream = web_client.OpenRead(uri);
                }
                catch (Exception ex)
                {
                    error = ex;
                }
                start_download();
            }
        }

        private void reset()
        {
            status = DownloadFileStatus.INIT;
            error = null;
            contentBytes = 0;
            console_line = 0;
            FileName = null;
            file_stream = null;
            stream = null;
        }

        private void defineTotalBytes()
        {
			lock (locker_df)
			{
				for (int i = 0; i < 5; i++)
				{
					try
					{
						HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
						HttpWebResponse response = (HttpWebResponse)request.GetResponse();
						contentBytes = response.ContentLength;

                        FileName = (response.ResponseUri ?? URI).LocalPath;
                        if (string.IsNullOrEmpty(FileName)) FileName = URI.LocalPath;
                        FileName = Path.GetFileName(FileName);

						response.Close();
					}
					catch
					{
                        contentBytes = 0;
                        continue;
					}
					break;
				}
            }
        }

        private void init_file_stream()
        {
            string file_name = Path.Combine(dest_dir, FileName);
            
            if (!File.Exists(file_name))
            {
                file_stream = File.Create(file_name, 512 * 1024, FileOptions.Asynchronous);
            }
            else
            {
                file_stream = File.OpenWrite(file_name);
                file_stream.Seek(0, SeekOrigin.End);
            }
        }

        private void start_download()
        {
            currentAsyncResult = DownloadProgress.BeginInvoke(this, new DownloadProgressEventArgs(this, 0, 0), null, null);
            try
            {
                if (stream == null || Error != null) return;
                if (!stream.CanRead)
                {
                    error = new Exception("Url has no access to read");
                    return;
                }

                init_file_stream();
                download_process();

            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                if (file_stream != null) file_stream.Close();
                if (stream != null) stream.Close();
                status = error == null ? DownloadFileStatus.SUCCESS : DownloadFileStatus.ERROR;
                if (!currentAsyncResult.IsCompleted)
                    DownloadProgress.EndInvoke(currentAsyncResult);
                DownloadCompleted.Invoke(this);
            }
        }

        private void download_process()
        {
            status = DownloadFileStatus.DOWNLOAD;
            if (!stream.CanRead || (file_stream.Length == contentBytes)) return;
            if (file_stream.Length > 0 && !stream.CanSeek)
            {
                file_stream.SetLength(0);
            }

            int _count;
            long bytesPerTime = 0;
            
            long pos = file_stream.Position;
            byte[] buffer = new byte[512 * 1024];            
            long startTime = DateTime.Now.Ticks;
            if (pos != 0) stream.Seek(pos, SeekOrigin.Begin);

            while ((_count = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                file_stream.Write(buffer, 0, _count);
                pos += _count;
                bytesPerTime += _count;
                var seconds = TimeSpan.FromTicks(DateTime.Now.Ticks - startTime).TotalSeconds;
                if (seconds > 0.35)
                {
                    double speed = (double)bytesPerTime / seconds;
                    bytesPerTime = 0;
                    startTime = DateTime.Now.Ticks;
                    if (currentAsyncResult.IsCompleted)
                    {
                        DownloadProgress.EndInvoke(currentAsyncResult);
                        currentAsyncResult = DownloadProgress.BeginInvoke(this, new DownloadProgressEventArgs(this, pos, speed), null, null);
                    }
                }
            }
        }

        public void free()
        {
            thread.Abort();
        }

    }

 

    public delegate void DownloadCompletedHandler(object sender);
    public delegate void DownloadProgressHandler(object sender, DownloadProgressEventArgs e);

    public class DownloadProgressEventArgs : EventArgs
    {
        public long bytes { get; set; }
        public string name { get; set; }
        public double speed { get; set; }

        public DownloadProgressEventArgs(object sender, long bytes, double speed)
        {
            this.bytes = bytes;
            this.name = (sender as download_file).FileName;
            this.speed = speed;
        }
    }

    enum DownloadFileStatus
    {
        INIT,
        DOWNLOAD,
        SUCCESS,
        ERROR
    }

}
