using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;

namespace ActivityLibrary
{

    public sealed class GetNumberActivity : NativeActivity<int>
    {
        protected override void Execute(NativeActivityContext context)
        {
            context.CreateBookmark("GetNumber", Resumed);
        }

        private void Resumed(NativeActivityContext context, Bookmark bookmark, object value)
        {
            int number = 0;
            if (value != null && int.TryParse(value.ToString(), out number))
            {
                Result.Set(context, number);
            }
        }

        protected override bool CanInduceIdle
        {
            get
            {
                return true;
            }
        }
    }
}
