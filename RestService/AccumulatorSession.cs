using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using ActivityLibrary;

namespace RestService
{
    public class AccumulatorSession
    {
        public static Dictionary<string, AccumulatorSession> Sessions = new Dictionary<string, AccumulatorSession>();

        public string SessionId { get; private set; }
        public WorkflowApplication Application { get; private set; }
        public AutoResetEvent SyncEvent { get; private set; }
        public int Sum { get; set; }

        public AccumulatorSession(string sessionId)
        {
            Sessions.Add(sessionId, this);

            this.SessionId = sessionId;
            this.Application = new WorkflowApplication(new AccumulatorActivity());
            this.SyncEvent = new AutoResetEvent(false);
            this.Sum = 0;
        }

        public void Start()
        {
            this.Application.Completed = new Action<WorkflowApplicationCompletedEventArgs>((e) =>
            {
                Sessions.Remove(this.SessionId);
                this.Sum = 0;
                this.SyncEvent.Set();
            });
            this.Application.Extensions.Add(new Notification(NotifySum));
            this.Application.Run();
        }

        public void NotifySum(int sum)
        {
            this.Sum = sum;
            this.SyncEvent.Set();
        }
    }
}