using System;
using System.Net;
using Mirage.Logging;
using Mirage.SocketLayer;
using UnityEngine;

namespace Mirage
{
    /// <summary>
    /// A <see cref="IConnection"/> that is directly sends data to a <see cref="IDataHandler"/>
    /// </summary>
    public class PipePeerConnection : IConnection
    {
        static readonly ILogger logger = LogFactory.GetLogger<PipePeerConnection>();

        /// <summary>
        /// handler of other connection
        /// </summary>
        IDataHandler otherHandler;
        /// <summary>
        /// other connection that is passed to handler
        /// </summary>
        IConnection otherConnection;

        /// <summary>
        /// Name used for debugging
        /// </summary>
        string name;

        Action OnDisconnect;

        private PipePeerConnection() { }

        public static (IConnection clientConn, IConnection serverConn) Create(IDataHandler clientHandler, IDataHandler serverHandler, Action ClientOnDisconnect, Action ServerOnDisconnect)
        {
            var client = new PipePeerConnection();
            client.OnDisconnect = ClientOnDisconnect;
            var server = new PipePeerConnection();
            server.OnDisconnect = ServerOnDisconnect;

            client.otherHandler = serverHandler ?? throw new ArgumentNullException(nameof(serverHandler));
            server.otherHandler = clientHandler ?? throw new ArgumentNullException(nameof(clientHandler));

            client.otherConnection = server;
            server.otherConnection = client;

            client.State = ConnectionState.Connected;
            server.State = ConnectionState.Connected;

            client.name = "[Client Pipe Connection]";
            server.name = "[Server Pipe Connection]";

            return (client, server);
        }

        public override string ToString()
        {
            return name;
        }

        EndPoint IConnection.EndPoint => new PipeEndPoint();


        public ConnectionState State { get; private set; } = ConnectionState.Connected;

        void IConnection.Disconnect()
        {
            if (State == ConnectionState.Disconnected)
                return;

            State = ConnectionState.Disconnected;
            OnDisconnect?.Invoke();

            // tell other connection to also disconnect
            otherConnection.Disconnect();
        }

        INotifyToken IConnection.SendNotify(byte[] packet)
        {
            if (State == ConnectionState.Disconnected)
                return default;

            receive(packet);

            return new PipeNotifyToken();
        }

        void IConnection.SendReliable(byte[] message)
        {
            if (State == ConnectionState.Disconnected)
                return;

            receive(message);
        }

        void IConnection.SendUnreliable(byte[] packet)
        {
            if (State == ConnectionState.Disconnected)
                return;

            receive(packet);
        }

        private void receive(byte[] packet)
        {
            logger.Assert(State == ConnectionState.Connected);
            otherHandler.ReceiveMessage(otherConnection, new ArraySegment<byte>(packet));
        }

        public class PipeEndPoint : EndPoint
        {
        }

        /// <summary>
        /// Token that invokes <see cref="INotifyToken.Delivered"/> immediately
        /// </summary>
        public struct PipeNotifyToken : INotifyToken
        {
            public event Action Delivered
            {
                add
                {
                    value.Invoke();
                }
                remove
                {
                    // nothing
                }
            }
            public event Action Lost
            {
                add
                {
                    // nothing
                }
                remove
                {
                    // nothing
                }
            }
        }

    }
}
