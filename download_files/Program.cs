using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace download_files
{
    class Program
    {
        static void Main(string[] args)
        {
            cmd_model model = new cmd_model(args);
            model.start();
            model.endWait();
        }
    }
}
