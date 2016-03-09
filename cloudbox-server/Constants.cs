using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace cloudbox_server
{
    public static class Constants
    {
       public static ConcurrentQueue<string> inTransfer = new ConcurrentQueue<string>();
       public static int IsStart;
       public static int locker;
       public static int locker2;
       public static int IsChanged; 

    }
}
