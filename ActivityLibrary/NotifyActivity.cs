using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;

namespace ActivityLibrary
{

    public sealed class NotifyActivity : CodeActivity
    {
        // Define an activity input argument of type string
        public InArgument<int> Input { get; set; }

        // If your activity returns a value, derive from CodeActivity<TResult>
        // and return the value from the Execute method.
        protected override void Execute(CodeActivityContext context)
        {
            IHostNotification extension = context.GetExtension<IHostNotification>();
            if (extension != null)
            {
                extension.NotifySum(context.GetValue(this.Input));
            }
        }
    }
}
