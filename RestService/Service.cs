using System;
using System.Activities;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;
using System.Text;
using ActivityLibrary;

namespace RestService
{
    [ServiceContract]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class Service
    {
        [WebGet(UriTemplate = "/add?x={x}&y={y}")]
        public int Add(int x, int y)
        {
            var activity = new AddActivity();
            activity.X = x;
            activity.Y = y;
            var output = WorkflowInvoker.Invoke(activity);
            return (int)output["Result"];
        }

        [WebGet(UriTemplate = "/accumulate/{sessionId}/{number}")]
        public int Accumulate(string sessionId, string number)
        {
            int num = int.Parse(number);
            AccumulatorSession session = null;
            if (!AccumulatorSession.Sessions.ContainsKey(sessionId))
            {
                session = new AccumulatorSession(sessionId);
                session.Start();
            }

            session = AccumulatorSession.Sessions[sessionId];
            session.Application.ResumeBookmark("GetNumber", num);
            session.SyncEvent.WaitOne();

            return session.Sum;
        }

        [WebGet(UriTemplate = "/paccumulate")]
        [OperationContract(Name = "PersistentAccumulateStartSession")]
        public string PersistentAccumulate()
        {
            try
            {
                var session = new PersistentAccumulatorSession();
                session.Start();
                return session.Application.Id.ToString();
            }
            catch (Exception exception)
            {
                OutgoingWebResponseContext response = WebOperationContext.Current.OutgoingResponse;
                response.StatusCode = HttpStatusCode.Forbidden;
                response.StatusDescription = exception.Message;
                return exception.Message;
            }
        }

        [WebGet(UriTemplate = "/paccumulate/{sessionId}/{number}")]
        public int PersistentAccumulate(string sessionId, string number)
        {
            var session = new PersistentAccumulatorSession(sessionId);
            session.Resume(int.Parse(number));
            return session.Completed ? 0 : session.Sum;
        }
    }
}
