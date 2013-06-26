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
        /// <summary>
        /// This is the private "channel name" in chord, the game's id
        /// </summary>
        [DataMember]
        public string Id { get; set; }

        /// <summary>
        /// This is the friendly name for the game for the user
        /// </summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// If the game is started or not
        /// </summary>
        [DataMember]
        public bool Started { get; set; }

        /// <summary>
        /// The opponent's id for identifying them over multiplayer
        /// </summary>
        [DataMember]
        public string OpponentId { get; set; }
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
        /// <summary>
        /// Event when a message is received, this should be on the UI thread
        /// </summary>
        public event EventHandler<MessageEventArgs> Received = delegate { };

        /// <summary>
        /// If multiplayer is supported on this device
        /// </summary>
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
                        Name = "Test Game 1",
                    },
                    new MultiplayerGame
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        Name = "Test Game 2",
                    },
                };
            });
        }

        /// <summary>
        /// Sends a message that is serialized in a particular format
        /// </summary>
        public virtual Task Send(string messageId, object message)
        {
            return Task.Delay(1000);
        }

        protected virtual void OnReceived(object message)
        {
            Received(this, new MessageEventArgs { Message = message });
        }
    }
}