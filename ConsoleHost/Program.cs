using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using System.Threading;
using ActivityLibrary;

namespace ConsoleHost
{
    class Program
    {
        private static AutoResetEvent _event;

        static void Main(string[] args)
        {
            _event = new AutoResetEvent(false);
            bool more = true;
            int result = 0;
            var activity = new AccumulatorActivity();
            var accumulator = new WorkflowApplication(activity);
            accumulator.Completed = new Action<WorkflowApplicationCompletedEventArgs>((e) =>
            {
                result = (int)e.Outputs["Sum"];
                more = false;
                _event.Set();
            });
            accumulator.Extensions.Add(new Notification(NotifySum));
            accumulator.Run();

            while (more)
            {
                Console.Write("Enter number: ");
                int number = int.Parse(Console.ReadLine());
                accumulator.ResumeBookmark("GetNumber", number);
                _event.WaitOne();
            }

            Console.WriteLine("Result is " + result);
        }

        static void NotifySum(int sum)
        {
            Console.WriteLine("Accumulated sum is " + sum.ToString());
            _event.Set();
        }
    }
}
