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
    public class PersistentAccumulatorSession
    {
        public string SessionId { get; private set; }
        public WorkflowApplication Application { get; private set; }
        public AutoResetEvent SyncEvent { get; private set; }
        public int Sum { get; set; }
        public bool Completed { get; set; }

        public PersistentAccumulatorSession(string sessionId = null)
        {
            this.Application = CreateWorkflowApplication(new AccumulatorActivity(), CreateInstanceStore());
            this.SessionId = sessionId ?? this.Application.Id.ToString();
            this.SyncEvent = new AutoResetEvent(false);
            this.Sum = 0;
            this.Completed = false;
        }

        public void Run()
        {
            this.Application.Run();
        }

        public void Run(int number)
        {
            this.Application.Load(new Guid(this.SessionId));
            this.Application.ResumeBookmark("GetNumber", number);
            this.SyncEvent.WaitOne();
        }

        public void NotifySum(int sum)
        {
            this.Sum = sum;
            this.SyncEvent.Set();
        }

        private WorkflowApplication CreateWorkflowApplication(AccumulatorActivity activity, InstanceStore instanceStore)
        {
            var application = new WorkflowApplication(activity);
            application.InstanceStore = instanceStore;
            application.Completed = new Action<WorkflowApplicationCompletedEventArgs>((e) =>
            {
                this.Sum = (int)e.Outputs["Sum"];
                this.Completed = true;
                this.SyncEvent.Set();
            });
            application.PersistableIdle = new Func<WorkflowApplicationIdleEventArgs, PersistableIdleAction>((e) => PersistableIdleAction.Unload);
            application.Extensions.Add(new Notification(NotifySum));
            return application;
        }

        private InstanceStore CreateInstanceStore()
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