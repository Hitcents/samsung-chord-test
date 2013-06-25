using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace SamsungChordTest
{
    [DataContract]
    public class MultiplayerGame
    {
        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public bool Started { get; set; }
    }

    public class MessageEventArgs
    {
        public object Message { get; set; }

        /// <summary>
        /// A quick helper method to cast
        /// </summary>
        public T OfMessageType<T>()
        {
            return (T)Message;
        }
    }

    public class MultiplayerService

#if ANDROID
        : Java.Lang.Object
#endif

    {
        public event EventHandler<MessageEventArgs> Received = delegate { };

        public virtual bool Supported
        {
            get { return true; }
        }

        /// <summary>
        /// Hosts a new game
        /// </summary>
        public virtual Task Host(MultiplayerGame game)
        {
            return Task.Delay(1000);
        }

        /// <summary>
        /// Connects to an existing game
        /// </summary>
        public virtual Task Join(MultiplayerGame game)
        {
            return Task.Delay(1000);
        }

        /// <summary>
        /// Finds a list of available games
        /// </summary>
        public virtual Task<List<MultiplayerGame>> FindGames()
        {
            return Task.Factory.StartNew(() =>
            {
                return new List<MultiplayerGame>
                {
                    new MultiplayerGame
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        Name = "Test Game",
                    },
                };
            });
        }

        /// <summary>
        /// Sends a message that is serialized in a particular format
        /// </summary>
        public virtual Task Send(object message)
        {
            return Task.Delay(1000);
        }

        protected virtual void OnReceived(object message)
        {
            Received(this, new MessageEventArgs { Message = message });
        }
    }
}