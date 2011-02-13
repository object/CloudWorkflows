using System;
using System.Activities.DurableInstancing;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.DurableInstancing;
using System.Text;
using System.Activities;
using System.Threading;
using System.Xml.Linq;
using ActivityLibrary;

namespace ConsoleHost
{
    class Program
    {
        private static AutoResetEvent _event;
        private static bool _more = true;
        private static int _result = 0;

        static void Main(string[] args)
        {
            var instanceStore = CreateInstanceStore();
            _event = new AutoResetEvent(false);
            var activity = new AccumulatorActivity();
            var application = CreateWorkflowApplication(activity, instanceStore);
            var applicationId = application.Id;
            application.Run();

            while (_more)
            {
                Console.Write("Enter number: ");
                int number = int.Parse(Console.ReadLine());
                application = CreateWorkflowApplication(activity, instanceStore);
                application.Load(applicationId);
                application.ResumeBookmark("GetNumber", number);
                _event.WaitOne();
            }

            Console.WriteLine("Result is " + _result.ToString());
        }

        static void NotifySum(int sum)
        {
            Console.WriteLine("Accumulated sum is " + sum.ToString());
            _event.Set();
        }

        static WorkflowApplication CreateWorkflowApplication(AccumulatorActivity activity, InstanceStore instanceStore)
        {
            var application = new WorkflowApplication(activity);
            application.InstanceStore = instanceStore;
            application.Completed = new Action<WorkflowApplicationCompletedEventArgs>((e) =>
            {
                _result = (int)e.Outputs["Sum"];
                _more = false;
                _event.Set();
            });
            application.PersistableIdle = new Func<WorkflowApplicationIdleEventArgs, PersistableIdleAction>((e) => PersistableIdleAction.Unload);
            application.Extensions.Add(new Notification(NotifySum));
            return application;
        }

        static InstanceStore CreateInstanceStore()
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
