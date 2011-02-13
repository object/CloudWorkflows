using System;
using System.Activities;
using System.Activities.DurableInstancing;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.DurableInstancing;
using System.Threading;
using System.Web;
using System.Xml.Linq;
using ActivityLibrary;

namespace RestService
{
    public class PersistentAccumulatorSession : IDisposable
    {
        public string SessionId { get; private set; }
        public WorkflowApplication Application { get; private set; }
        public AutoResetEvent SyncEvent { get; private set; }
        public int Sum { get; set; }
        public bool Completed { get; set; }
        private bool Suspended { get; set; }

        public PersistentAccumulatorSession(string sessionId = null)
        {
            this.Application = CreateWorkflowApplication(new AccumulatorActivity(), CreateInstanceStore());
            this.SessionId = sessionId ?? this.Application.Id.ToString();
            this.SyncEvent = new AutoResetEvent(false);
            this.Sum = 0;
            this.Completed = false;
            this.Suspended = false;
        }

        public void Dispose()
        {
            Stop();
            this.SyncEvent.Dispose();
        }

        public void Start()
        {
            this.Application.Run();
        }

        public void Stop()
        {
            if (!this.Completed)
            {
                this.Application.Cancel();
                this.Completed = true;
            }
        }

        public void Suspend()
        {
            this.Application.Persist();
            this.SyncEvent.WaitOne();
        }

        public void Resume(int number)
        {
            if (this.Suspended)
            {
                this.Application = CreateWorkflowApplication(new AccumulatorActivity(), CreateInstanceStore());
            }

            this.Application.Load(new Guid(this.SessionId));
            this.Application.ResumeBookmark("GetNumber", number);
            this.SyncEvent.WaitOne();
        }

        public void NotifySum(int sum)
        {
            this.Sum = sum;
            this.SyncEvent.Set();
        }

        internal WorkflowApplication CreateWorkflowApplication(AccumulatorActivity activity, InstanceStore instanceStore)
        {
            var application = new WorkflowApplication(activity);
            application.InstanceStore = instanceStore;
            application.Unloaded = new Action<WorkflowApplicationEventArgs>((e) =>
                {
                    this.Suspended = true;
                    this.SyncEvent.Set();
                });
            application.Completed = new Action<WorkflowApplicationCompletedEventArgs>((e) =>
                {
                    this.Sum = (int)e.Outputs["Sum"];
                    this.Completed = true;
                    this.SyncEvent.Set();
                });
            application.PersistableIdle = new Func<WorkflowApplicationIdleEventArgs, PersistableIdleAction>((e) => PersistableIdleAction.Unload);
            application.Extensions.Add(new Notification(NotifySum));
            this.Suspended = false;
            return application;
        }

        internal static InstanceStore CreateInstanceStore()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["InstanceStore"].ConnectionString;
            var instanceStore = new SqlWorkflowInstanceStore(connectionString);
            var instanceHandle = instanceStore.CreateInstanceHandle();
            var ownerCommand = new CreateWorkflowOwnerCommand();
            XNamespace hostNamespace = XNamespace.Get("urn:schemas-microsoft-com:System.Activities/4.0/properties");
            XName hostKey = hostNamespace.GetName("WorkflowHostType");
            var hostValue = new InstanceValue(XNamespace.Get("http://tempuri.org").GetName("SampleInstance"));
            ownerCommand.InstanceOwnerMetadata.Add(hostKey, hostValue);
            instanceStore.DefaultInstanceOwner = instanceStore.Execute(
                               instanceHandle,
                               ownerCommand,
                               TimeSpan.FromSeconds(30)).InstanceOwner;
            instanceHandle.Free();

            return instanceStore;
        }
    }
}