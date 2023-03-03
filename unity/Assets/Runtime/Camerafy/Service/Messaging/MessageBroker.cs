using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;

namespace Camerafy.Service.Messaging
{
    /// <summary>
    /// Main class that allows communication with the RabbitMQ message broker server.
    /// </summary>
    public class MessageBroker
    {
        public class Channel : IDisposable
        {
            public delegate void DataReceivedDelegate(byte[] InData);

            private string OutQueue = null;

            private IModel Model = null;

            private EventingBasicConsumer Consumer = null;

            public event DataReceivedDelegate Received = null;

            public Channel(string InPeerId, IConnection InConnection)
            {
                // create new channel
                this.Model = InConnection.CreateModel();

                // declare queue for client-to-server communication
                string InQueue = $"{InPeerId}.C2S";
                this.Model.QueueDeclare(queue: InQueue, durable: true, exclusive: false, autoDelete: false);

                // declare queue for server-to-client communication
                this.OutQueue = $"{InPeerId}.S2C";
                this.Model.QueueDeclare(queue: this.OutQueue, durable: true, exclusive: false, autoDelete: false);

                // add channel consumer for incoming events
                this.Consumer = new EventingBasicConsumer(this.Model);
                {
                    this.Consumer.Received += (model, args) => 
                    {
                        if((DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - args.BasicProperties.Timestamp.UnixTime) > (Application.Application.Current.Config.MessageExirationTime * 1000 /* convert seconds to milliseconds */))
                            return;

                        // forward received data
                        this.Received?.Invoke(args.Body.ToArray()); 
                        // acknowledge received data
                        this.Model.BasicAck(args.DeliveryTag, false); 
                    };
                }
                this.Model.BasicConsume(queue: InQueue, autoAck: false, consumer: this.Consumer);
            }

            /// <summary>
            /// Send data from server to client.
            /// </summary>
            /// <param name="InData"></param>
            public void Send(byte[] InData) { this.Model.BasicPublish("", this.OutQueue, null, InData); }

            public void Dispose() { this.Model.Dispose(); }
        }

        #region SINGLETON

        /// <summary>
        /// Signletone instance.
        /// </summary>
        private static MessageBroker Signleton = null;

        /// <summary>
        /// Get current MessageBroker singleton instance.
        /// </summary>
        public static MessageBroker Current
        {
            get 
            {
                // lazy initialize singleton instance
                if (MessageBroker.Signleton == null)
                {
                    MessageBroker.Signleton = new MessageBroker();
                }

                return MessageBroker.Signleton;
            }
        }

        #endregion

        private ConnectionFactory ConnectionFactory = null;

        /// <summary>
        /// Connection to the server, which can be automatically recovered on failures.
        /// </summary>
        private IConnection Connection = null;

        /// <summary>
        /// True if message broker is connected to the server.
        /// </summary>
        public bool IsConnected { get { return this.Connection != null; } }

        public MessageBroker()
        {
            ConnectionFactory = new ConnectionFactory()
            {
                HostName = "localhost",
                AutomaticRecoveryEnabled = true
            };

            this.Connect();
        }

        public Channel CreateNewChannel(string InPeerId)
        {
            return this.IsConnected ? new Channel(InPeerId, this.Connection) : null;
        }

        /// <summary>
        /// Connect to the message broker server.
        /// </summary>
        private void Connect()
        {
            try
            {
                this.Connection = ConnectionFactory.CreateConnection();
            }
            catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException ex)
            {
                Logger.Error($"Unable to connect to message broker. {ex.Message}");
            }
        }
    }
}
