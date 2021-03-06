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

using Cassandra.Metrics.Abstractions;

namespace Cassandra.Metrics.Registries
{
    internal class RetryPolicyOnRetryMetrics : IRetryPolicyMetrics
    {
        public RetryPolicyOnRetryMetrics(IInternalMetricsRegistry<NodeMetric> nodeMetricsRegistry, string context)
        {
            ReadTimeout = nodeMetricsRegistry.Counter(context, NodeMetric.Counters.RetriesOnReadTimeout);
            WriteTimeout = nodeMetricsRegistry.Counter(context, NodeMetric.Counters.RetriesOnWriteTimeout);
            Unavailable = nodeMetricsRegistry.Counter(context, NodeMetric.Counters.RetriesOnUnavailable);
            Other = nodeMetricsRegistry.Counter(context, NodeMetric.Counters.RetriesOnOtherError);
            Total = nodeMetricsRegistry.Counter(context, NodeMetric.Counters.Retries);
        }

        public IDriverCounter ReadTimeout { get; }

        public IDriverCounter WriteTimeout { get; }

        public IDriverCounter Unavailable { get; }

        public IDriverCounter Other { get; }

        public IDriverCounter Total { get; }
    }
}