//
//      Copyright (C) DataStax Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Cassandra.ExecutionProfiles;
using Cassandra.Tests.Connections.TestHelpers;
using Moq;
using NUnit.Framework;

namespace Cassandra.Tests
{
    [TestFixture]
    public class SessionBuilderTests
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Diagnostics.CassandraTraceSwitch.Level = TraceLevel.Verbose;
        }
        
        [Test]
        public async Task DuplicateContactPointsShouldIgnore()
        {
            var listener = new TestTraceListener();
            Trace.Listeners.Add(listener);
            var originalLevel = Diagnostics.CassandraTraceSwitch.Level;
            Diagnostics.CassandraTraceSwitch.Level = TraceLevel.Warning;
            try
            {
                const string ip1 = "127.100.100.100";
                const string singleUniqueIp = "127.100.100.101";
                var ip2 = new IPEndPoint(IPAddress.Parse("127.100.100.100"), 9040);
                var ip3 = IPAddress.Parse("127.100.100.100");
                var session = await Session.Builder()
                                     .AddContactPoints(ip1, ip1, ip1)
                                     .AddContactPoints(ip2, ip2, ip2)
                                     // IPAddresses are converted to strings so these 3 will be equal to the previous 3
                                     .AddContactPoints(ip3, ip3, ip3)
                                     .AddContactPoint(singleUniqueIp)
                                     .BuildInternalAsync().ConfigureAwait(false);

                Assert.AreEqual(3, session.GetResolvedEndpoints().Count);
                Trace.Flush();
                Assert.AreEqual(5, listener.Queue.Count(msg => msg.Contains("Found duplicate contact point: 127.100.100.100. Ignoring it.")));
                Assert.AreEqual(2, listener.Queue.Count(msg => msg.Contains("Found duplicate contact point: 127.100.100.100:9040. Ignoring it.")));
            }
            finally
            {
                Trace.Listeners.Remove(listener);
                Diagnostics.CassandraTraceSwitch.Level = originalLevel;
            }
        }
        
        [Test]
        public void BuildThrowsNoHostAvailable()
        {
            var builder = Session.Builder()
                                 .AddContactPoint("127.100.100.100");
            Assert.ThrowsAsync<NoHostAvailableException>(() => builder.BuildAsync());
            Assert.Throws<NoHostAvailableException>(() => builder.Build());
        }

        [Test]
        public void SessionIsDisposedAfterInitError()
        {
            const string ip = "127.100.100.100";
            var listener = new TestTraceListener();
            Trace.Listeners.Add(listener);
            var originalLevel = Diagnostics.CassandraTraceSwitch.Level;
            Diagnostics.CassandraTraceSwitch.Level = TraceLevel.Warning;
            var sessionName = Guid.NewGuid().ToString();
            try
            {
                var builder = Session.Builder()
                                     .AddContactPoint(ip)
                                     .WithSessionName(sessionName);
                Assert.Throws<NoHostAvailableException>(() => builder.Build());
                Trace.Flush();
                Assert.AreEqual(1, listener.Queue.Count(msg => msg.Contains($"Session [{sessionName}] has been shut down.")));
            }
            finally
            {
                Trace.Listeners.Remove(listener);
                Diagnostics.CassandraTraceSwitch.Level = originalLevel;
            }
        }

        [Test]
        public void Should_Not_Leak_Connections_When_Node_Unreachable_Test()
        {
            var socketOptions = new SocketOptions().SetReadTimeoutMillis(1).SetConnectTimeoutMillis(1);
            var builder = Session.Builder()
                                 .AddContactPoint(TestHelper.UnreachableHostAddress)
                                 .WithSocketOptions(socketOptions);
            const int length = 1000;
            decimal initialLength = GC.GetTotalMemory(true);
            for (var i = 0; i < length; i++)
            {
                var ex = Assert.ThrowsAsync<NoHostAvailableException>(() => builder.BuildAsync());
                Assert.AreEqual(1, ex.Errors.Count);
            }
            GC.Collect();
            Assert.Less(GC.GetTotalMemory(true) / initialLength, 1.3M,
                "Should not exceed a 20% (1.3) more than was previously allocated");
        }

        static object[] _hostDistanceTestData = new object[]
        {
            // Test Case 1
            new object[]
            {
                // LBP data
                new []
                {
                    new Dictionary<string, HostDistance>
                    {
                        { "127.0.0.1", HostDistance.Ignored },
                        { "127.0.0.2", HostDistance.Local },
                        { "127.0.0.3", HostDistance.Ignored }
                    },

                    new Dictionary<string, HostDistance>
                    {
                        { "127.0.0.1", HostDistance.Local },
                        { "127.0.0.2", HostDistance.Local },
                        { "127.0.0.3", HostDistance.Remote }
                    },

                    new Dictionary<string, HostDistance>
                    {
                        { "127.0.0.1", HostDistance.Remote },
                        { "127.0.0.2", HostDistance.Ignored },
                        { "127.0.0.3", HostDistance.Local }
                    }
                },

                // Expected result
                new Dictionary<string, HostDistance>
                {
                    { "127.0.0.1", HostDistance.Local },
                    { "127.0.0.2", HostDistance.Local },
                    { "127.0.0.3", HostDistance.Local }
                }
            },

            // Test Case 2
            new object[]
            {
                // LBP data
                new []
                {
                    new Dictionary<string, HostDistance>
                    {
                        { "127.0.0.1", HostDistance.Ignored },
                        { "127.0.0.2", HostDistance.Remote },
                        { "127.0.0.3", HostDistance.Remote }
                    },

                    new Dictionary<string, HostDistance>
                    {
                        { "127.0.0.1", HostDistance.Ignored },
                        { "127.0.0.2", HostDistance.Ignored },
                        { "127.0.0.3", HostDistance.Remote }
                    },

                    new Dictionary<string, HostDistance>
                    {
                        { "127.0.0.1", HostDistance.Ignored },
                        { "127.0.0.2", HostDistance.Ignored },
                        { "127.0.0.3", HostDistance.Local }
                    }
                },
                // Expected result
                new Dictionary<string, HostDistance>
                {
                    { "127.0.0.1", HostDistance.Ignored },
                    { "127.0.0.2", HostDistance.Remote },
                    { "127.0.0.3", HostDistance.Local }
                }
            }
        };

        [Test, TestCaseSource(nameof(SessionBuilderTests._hostDistanceTestData))]
        public async Task Should_OnlyDisposePoliciesOnce_When_NoProfileIsProvided(
            Dictionary<string, HostDistance>[] lbpData, Dictionary<string, HostDistance> expected)
        {
            var lbps = lbpData.Select(lbp => new FakeHostDistanceLbp(lbp)).ToList();
            var testConfig = new TestConfigurationBuilder()
            {
                ControlConnectionFactory = new FakeControlConnectionFactory(),
                ConnectionFactory = new FakeConnectionFactory(),
                Policies = new Cassandra.Policies(
                    lbps[0], 
                    new ConstantReconnectionPolicy(50), 
                    new DefaultRetryPolicy(), 
                    NoSpeculativeExecutionPolicy.Instance, 
                    new AtomicMonotonicTimestampGenerator()),
                ExecutionProfiles = lbps.Skip(1).Select(
                    (lbp, idx) => new 
                    { 
                        idx, 
                        a = new ExecutionProfile(null, null, null, lbp, null, null, null) 
                            as IExecutionProfile
                    }).ToDictionary(obj => obj.idx.ToString(), obj => obj.a)
            }.Build();
            var initializerMock = Mock.Of<IInitializer>();
            Mock.Get(initializerMock)
                .Setup(i => i.ContactPoints)
                .Returns(lbpData.SelectMany(dict => dict.Keys).Distinct().Select(addr => new IPEndPoint(IPAddress.Parse(addr), 9042)).ToList);
            Mock.Get(initializerMock)
                .Setup(i => i.GetConfiguration())
                .Returns(testConfig);
            
            var session = 
                await Session.BuildFromAsync(initializerMock, new List<string>(), testConfig).ConfigureAwait(false);
            await session.ShutdownAsync().ConfigureAwait(false);

            foreach (var h in session.AllHosts())
            {
                Assert.AreEqual(expected[h.Address.Address.ToString()], h.GetDistanceUnsafe());
            }
        }

        internal class FakeHostDistanceLbp : ILoadBalancingPolicy
        {
            private readonly IDictionary<string, HostDistance> _distances;
            private ISession _session;

            public FakeHostDistanceLbp(IDictionary<string, HostDistance> distances)
            {
                _distances = distances;
            }

            public void Initialize(ISession session)
            {
                _session = session;
            }

            public HostDistance Distance(Host host)
            {
                return _distances[host.Address.Address.ToString()];
            }

            public IEnumerable<Host> NewQueryPlan(string keyspace, IStatement query)
            {
                return _session.AllHosts().OrderBy(h => Guid.NewGuid().GetHashCode()).Take(_distances.Count);
            }
        }
    }
}