using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using NUnit.Framework;
using RestService;

namespace Tests
{
    [TestFixture]
    public class PersistentAccumulatorSessionTests
    {
        [Test]
        public void CreateNewSession()
        {
            using (var session = new PersistentAccumulatorSession())
            {
                Assert.IsNotNull(session, "Session is null");
            }
        }

        [Test]
        public void RunSession()
        {
            using (var session = new PersistentAccumulatorSession())
            {
                session.Start();
                Assert.AreEqual(0, session.Sum, "Incorrect sum");
            }
        }

        [Test]
        public void ResumeSession()
        {
            using (var session = new PersistentAccumulatorSession())
            {
                session.Start();
                session.Suspend();
                session.Resume(1);
                Assert.AreEqual(1, session.Sum, "Incorrect sum");
            }
        }

        [Test]
        public void ValidateDatabaseConnection()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["InstanceStore"].ConnectionString;
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                Assert.AreEqual(ConnectionState.Open, connection.State);
            }
        }

        [Test]
        public void CreateInstanceStore()
        {
            var instanceStore = PersistentAccumulatorSession.CreateInstanceStore();
            Assert.IsNotNull(instanceStore, "Instance store is null");
        }
    }
}
