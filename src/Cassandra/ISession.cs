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
using System.Net;
using System.Threading.Tasks;
using Cassandra.DataStax.Graph;
using Cassandra.Metrics;

namespace Cassandra
{
    /// <summary>
    /// A session holds connections to a Cassandra cluster, allowing it to be queried.
    /// <para>
    /// Each session maintains multiple connections to the cluster nodes,
    /// provides policies to choose which node to use for each query (round-robin on
    /// all nodes of the cluster by default), and handles retries for failed query (when
    /// it makes sense), etc...
    /// </para>
    /// <para>
    /// Session instances are thread-safe and usually a single instance is enough
    /// per application. However, a given session can only be set to one keyspace
    /// at a time, so one instance per keyspace is necessary.
    /// </para>
    /// </summary>
    public interface ISession : IDisposable
    {
        /// <summary>
        /// Gets the Cassandra native binary protocol version
        /// </summary>
        int BinaryProtocolVersion { get; }
        
        /// <summary>
        /// Determines if the object has been disposed.
        /// </summary>
        bool IsDisposed { get; }

        /// <summary>
        /// Gets name of currently used keyspace. 
        /// </summary>
        string Keyspace { get; }

        /// <summary>
        /// Gets the user defined type mappings
        /// </summary>
        UdtMappingDefinitions UserDefinedTypes { get; }

        /// <summary>
        /// Session name. This will be autogenerated if it is not set with <see cref="Builder.WithSessionName"/>.
        /// This is used as part of the metric bucket name, for example, which can be used to separate metric paths per session.
        /// </summary>
        string SessionName { get; }

        /// <summary>
        /// Begins asynchronous execute operation.
        /// </summary>
        IAsyncResult BeginExecute(IStatement statement, AsyncCallback callback, object state);

        /// <summary>
        /// Begins asynchronous execute operation
        /// </summary>
        IAsyncResult BeginExecute(string cqlQuery, ConsistencyLevel consistency, AsyncCallback callback, object state);

        /// <summary>
        /// Begins asynchronous prepare operation
        /// </summary>
        IAsyncResult BeginPrepare(string cqlQuery, AsyncCallback callback, object state);

        /// <summary>
        /// Switches to the specified keyspace.
        /// </summary>
        /// <param name="keyspaceName">Case-sensitive name of keyspace to be used.</param>
        /// <exception cref="InvalidQueryException">When keyspace does not exist</exception>
        void ChangeKeyspace(string keyspaceName);

        /// <summary>
        ///  Creates new keyspace in current cluster.        
        /// </summary>
        /// <param name="keyspaceName">Case-sensitive name of keyspace to be created.</param>
        /// <param name="replication">
        /// Replication property for this keyspace.
        /// To set it, refer to the <see cref="ReplicationStrategies"/> class methods. 
        /// It is a dictionary of replication property sub-options where key is a sub-option name and value is a value for that sub-option. 
        /// <para>Default value is <c>SimpleStrategy</c> with <c>replication_factor = 1</c></para>
        /// </param>
        /// <param name="durableWrites">Whether to use the commit log for updates on this keyspace. Default is set to <c>true</c>.</param>
        void CreateKeyspace(string keyspaceName, Dictionary<string, string> replication = null, bool durableWrites = true);

        /// <summary>
        ///  Creates new keyspace in current cluster.
        ///  If keyspace with specified name already exists, then this method does nothing.
        /// </summary>
        /// <param name="keyspaceName">Case-sensitive name of keyspace to be created.</param>
        /// <param name="replication">
        /// Replication property for this keyspace.
        /// To set it, refer to the <see cref="ReplicationStrategies"/> class methods. 
        /// It is a dictionary of replication property sub-options where key is a sub-option name and value is a value for that sub-option.
        /// <para>Default value is <c>'SimpleStrategy'</c> with <c>'replication_factor' = 2</c></para>
        /// </param>
        /// <param name="durableWrites">Whether to use the commit log for updates on this keyspace. Default is set to <c>true</c>.</param>
        void CreateKeyspaceIfNotExists(string keyspaceName, Dictionary<string, string> replication = null, bool durableWrites = true);

        /// <summary>
        ///  Deletes specified keyspace from current cluster.
        ///  If keyspace with specified name does not exist, then exception will be thrown.
        /// </summary>
        /// <param name="keyspaceName">Name of keyspace to be deleted.</param>
        void DeleteKeyspace(string keyspaceName);

        /// <summary>
        ///  Deletes specified keyspace from current cluster.
        ///  If keyspace with specified name does not exist, then this method does nothing.
        /// </summary>
        /// <param name="keyspaceName">Name of keyspace to be deleted.</param>
        void DeleteKeyspaceIfExists(string keyspaceName);

        /// <summary>
        /// Ends asynchronous execute operation
        /// </summary>
        /// <param name="ar"></param>
        /// <returns></returns>
        RowSet EndExecute(IAsyncResult ar);

        /// <summary>
        /// Ends asynchronous prepare operation
        /// </summary>
        PreparedStatement EndPrepare(IAsyncResult ar);
        
        /// <summary>
        /// Executes the provided statement with the provided execution profile.
        /// The execution profile must have been added previously to the Cluster using <see cref="Builder.WithExecutionProfiles"/>.
        /// </summary>
        /// <param name="statement">Statement to execute.</param>
        /// <param name="executionProfileName">ExecutionProfile name to be used while executing the statement.</param>
        RowSet Execute(IStatement statement, string executionProfileName);

        /// <summary>
        /// Executes the provided query.
        /// </summary>
        RowSet Execute(IStatement statement);

        /// <summary>
        /// Executes the provided query.
        /// </summary>
        RowSet Execute(string cqlQuery);
        
        /// <summary>
        /// Executes the provided query with the provided execution profile.
        /// The execution profile must have been added previously to the Cluster using <see cref="Builder.WithExecutionProfiles"/>.
        /// </summary>
        /// <param name="cqlQuery">Query to execute.</param>
        /// <param name="executionProfileName">ExecutionProfile name to be used while executing the statement.</param>
        RowSet Execute(string cqlQuery, string executionProfileName);
        
        /// <summary>
        /// Executes the provided query.
        /// </summary>
        RowSet Execute(string cqlQuery, ConsistencyLevel consistency);

        /// <summary>
        /// Executes the provided query.
        /// </summary>
        RowSet Execute(string cqlQuery, int pageSize);

        /// <summary>
        /// Executes a query asynchronously
        /// </summary>
        /// <param name="statement">The statement to execute (simple, bound or batch statement)</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task<RowSet> ExecuteAsync(IStatement statement);

        /// <summary>
        /// Executes a query asynchronously with the provided execution profile.
        /// The execution profile must have been added previously to the Cluster using <see cref="Builder.WithExecutionProfiles"/>.
        /// </summary>
        /// <param name="statement">The statement to execute (simple, bound or batch statement)</param>
        /// <param name="executionProfileName">ExecutionProfile name to be used while executing the statement.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task<RowSet> ExecuteAsync(IStatement statement, string executionProfileName);

        /// <summary>
        /// Prepares the provided query string.
        /// </summary>
        /// <param name="cqlQuery">cql query to prepare</param>
        PreparedStatement Prepare(string cqlQuery);
        
        /// <summary>
        /// Prepares the query string, sending the custom payload request.
        /// </summary>
        /// <param name="cqlQuery">cql query to prepare</param>
        /// <param name="customPayload">Custom outgoing payload to send with the prepare request</param>
        PreparedStatement Prepare(string cqlQuery, IDictionary<string, byte[]> customPayload);
        
        /// <summary>
        /// Prepares the query on the provided keyspace.
        /// </summary>
        /// <param name="cqlQuery">Cql query to prepare</param>
        /// <param name="keyspace">The keyspace to prepare this query with</param>
        /// <remarks>Setting the keyspace parameter is only available with protocol v5 (not supported by the driver yet) or DSE 6.0+.</remarks>
        PreparedStatement Prepare(string cqlQuery, string keyspace);

        /// <summary>
        /// Prepares the provided query string asynchronously on the provided keyspace, sending the custom payload
        /// as part of the request.
        /// </summary>
        /// <param name="cqlQuery">Cql query to prepare</param>
        /// <param name="keyspace">The keyspace to prepare this query with</param>
        /// <param name="customPayload">Custom outgoing payload to send with the prepare request</param>
        /// <remarks>Setting the keyspace parameter is only available with protocol v5 (not supported by the driver yet) or DSE 6.0+.</remarks>
        PreparedStatement Prepare(string cqlQuery, string keyspace, IDictionary<string, byte[]> customPayload);

        /// <summary>
        /// Prepares the provided query string asynchronously.
        /// </summary>
        /// <param name="cqlQuery">cql query to prepare</param>
        Task<PreparedStatement> PrepareAsync(string cqlQuery);
        
        /// <summary>
        /// Prepares the provided query string asynchronously, and sending the custom payload request.
        /// </summary>
        /// <param name="cqlQuery">cql query to prepare</param>
        /// <param name="customPayload">Custom outgoing payload to send with the prepare request</param>
        Task<PreparedStatement> PrepareAsync(string cqlQuery, IDictionary<string, byte[]> customPayload);

        /// <summary>
        /// Prepares the query asynchronously on the provided keyspace.
        /// </summary>
        /// <param name="cqlQuery">Cql query to prepare</param>
        /// <param name="keyspace">The keyspace to prepare this query with</param>
        /// <remarks>Setting the keyspace parameter is only available with protocol v5 (not supported by the driver yet) or DSE 6.0+.</remarks>
        Task<PreparedStatement> PrepareAsync(string cqlQuery, string keyspace);

        /// <summary>
        /// Prepares the provided query asynchronously on the provided keyspace, sending the custom payload
        /// as part of the request.
        /// </summary>
        /// <param name="cqlQuery">Cql query to prepare</param>
        /// <param name="keyspace">The keyspace to prepare this query with</param>
        /// <param name="customPayload">Custom outgoing payload to send with the prepare request</param>
        /// <remarks>Setting the keyspace parameter is only available with protocol v5 (not supported by the driver yet) or DSE 6.0+.</remarks>
        Task<PreparedStatement> PrepareAsync(string cqlQuery, string keyspace, IDictionary<string, byte[]> customPayload);

        /// <summary>
        /// Retrieves the driver metrics for this session.
        /// </summary>
        IDriverMetrics GetMetrics();
        
        /// <summary>
        /// Executes a graph statement.
        /// </summary>
        /// <param name="statement">The graph statement containing the query</param>
        /// <example>
        /// <code>
        /// GraphResultSet rs = session.ExecuteGraph(new SimpleGraphStatement("g.V()"));
        /// </code>
        /// </example>
        GraphResultSet ExecuteGraph(IGraphStatement statement);

        /// <summary>
        /// Executes a graph statement.
        /// </summary>
        /// <param name="statement">The graph statement containing the query</param>
        /// <example>
        /// <code>
        /// Task&lt;GraphResultSet$gt; task = session.ExecuteGraphAsync(new SimpleGraphStatement("g.V()"));
        /// </code>
        /// </example>
        Task<GraphResultSet> ExecuteGraphAsync(IGraphStatement statement);

        /// <summary>
        /// Executes a graph statement with the provided execution profile.
        /// The execution profile must have been added previously to the Cluster
        /// using <see cref="Builder.WithExecutionProfiles"/>.
        /// </summary>
        /// <param name="statement">The graph statement containing the query</param>
        /// <param name="executionProfileName">The graph execution profile name to use while executing this statement.</param>
        /// <example>
        /// <code>
        /// GraphResultSet rs = session.ExecuteGraph(new SimpleGraphStatement("g.V()"), "graphProfile");
        /// </code>
        /// </example>
        GraphResultSet ExecuteGraph(IGraphStatement statement, string executionProfileName);
        
        /// <summary>
        /// Executes a graph statement asynchronously with the provided graph execution profile.
        /// The graph execution profile must have been added previously to the Cluster
        /// using <see cref="Builder.WithExecutionProfiles"/>.
        /// </summary>
        /// <param name="statement">The graph statement containing the query</param>
        /// <param name="executionProfileName">The graph execution profile name to use while executing this statement.</param>
        /// <example>
        /// <code>
        /// Task&lt;GraphResultSet$gt; task = session.ExecuteGraphAsync(new SimpleGraphStatement("g.V()"), "graphProfile");
        /// </code>
        /// </example>
        Task<GraphResultSet> ExecuteGraphAsync(IGraphStatement statement, string executionProfileName);

        /// <summary>
        /// Disposes the session asynchronously.
        /// </summary>
        Task ShutdownAsync();

        /// <summary>
        ///  Gets read-only metadata on the connected cluster. 
        /// <para>This includes the
        ///  know nodes (with their status as seen by the driver) as well as the schema
        ///  definitions.
        /// </para>
        /// <para>This method may trigger the creation of a connection if none has been established yet.
        /// </para>
        /// </summary>
        Metadata Metadata { get; }

        /// <summary>
        /// Cluster client configuration
        /// </summary>
        Configuration Configuration { get; }

        /// <summary>
        /// Event that gets triggered when a new host is added to the cluster
        /// </summary>
        event Action<Host> HostAdded;

        /// <summary>
        /// Event that gets triggered when a host has been removed from the cluster
        /// </summary>
        event Action<Host> HostRemoved;

        /// <summary>
        ///  Returns all known hosts of this cluster.
        /// </summary>
        ICollection<Host> AllHosts();

        /// <summary>
        /// Get the host instance for a given Ip address.
        /// </summary>
        /// <param name="address">Ip address of the host</param>
        /// <returns>The host or null if not found</returns>
        Host GetHost(IPEndPoint address);
        
        /// <summary>
        /// Gets a collection of replicas for a given partitionKey. Backward-compatibility only, use GetReplicas(keyspace, partitionKey) instead.
        /// </summary>
        /// <param name="partitionKey">Byte array representing the partition key</param>
        /// <returns></returns>
        ICollection<Host> GetReplicas(byte[] partitionKey);

        /// <summary>
        /// Gets a collection of replicas for a given partitionKey on a given keyspace
        /// </summary>
        /// <param name="keyspace">Byte array representing the partition key</param>
        /// <param name="partitionKey">Byte array representing the partition key</param>
        /// <returns></returns>
        ICollection<Host> GetReplicas(string keyspace, byte[] partitionKey);

        /// <summary>
        /// Updates keyspace metadata (including token metadata for token aware routing) for a given keyspace or a specific keyspace table.
        /// If no keyspace is provided then this method will update the metadata and token map for all the keyspaces of the cluster.
        /// </summary>
        Task<bool> RefreshSchemaAsync(string keyspace = null, string table = null);

        /// <summary>
        /// Updates keyspace metadata (including token metadata for token aware routing) for a given keyspace or a specific keyspace table.
        /// If no keyspace is provided then this method will update the metadata and token map for all the keyspaces of the cluster.
        /// </summary>
        bool RefreshSchema(string keyspace = null, string table = null);
    }
}
