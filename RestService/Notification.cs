using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ActivityLibrary;

namespace RestService
{
    public class Notification : IHostNotification
    {
        private Action<int> _notify;

        public Notification(Action<int> notify)
        {
            _notify = notify;
        }

        public void NotifySum(int sum)
        {
            _notify(sum);
        }
    }
}
