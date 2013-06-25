using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SamsungChordTest
{
    public class MultiplayerGame
    {
        public string Id { get; set; }

        public string Name { get; set; }
    }

    public class MessageEventArgs : EventArgs
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
    {
        public event EventHandler<MessageEventArgs> Received = delegate { };

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
        public virtual Task Connect(MultiplayerGame game)
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