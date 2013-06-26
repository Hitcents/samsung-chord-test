using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using SamsungChord;

namespace SamsungChordTest
{
    public static class SamsungExtensions
    {
        public static Exception ToException(this int error)
        {
            Exception exc;
            switch (error)
            {
                case ChordManager.ErrorFailed:
                    exc = new Exception(SamsungMultiplayerService.ErrorStarting + "ErrorFailed, " + error);
                    break;
                case ChordManager.ErrorInvalidInterface:
                    exc = new Exception(SamsungMultiplayerService.ErrorStarting + "ErrorInvalidInterface, " + error);
                    break;
                case ChordManager.ErrorInvalidParam:
                    exc = new Exception(SamsungMultiplayerService.ErrorStarting + "ErrorInvalidParam, " + error);
                    break;
                case ChordManager.ErrorInvalidState:
                    exc = new Exception(SamsungMultiplayerService.ErrorStarting + "ErrorInvalidState, " + error);
                    break;
                default:
                    exc = new Exception(SamsungMultiplayerService.ErrorStarting + "Unknown, " + error);
                    break;
            }
            return exc;
        }

        public static T ToMessage<T>(this byte[][] payload)
        {
            string data = SamsungMultiplayerService.Encoding.GetString(payload[0]);
            return JsonConvert.DeserializeObject<T>(data);
        }

        public static object ToMessage(this byte[][] payload, Type type)
        {
            string data = SamsungMultiplayerService.Encoding.GetString(payload[0]);
            return JsonConvert.DeserializeObject(data, type);
        }

        public static byte[][] ToPayload(this object message)
        {
            string data = JsonConvert.SerializeObject(message);
            return new byte[][]
            {
                SamsungMultiplayerService.Encoding.GetBytes(data),
            };
        }
    }

    public class SamsungMultiplayerService : MultiplayerService, IChordManagerListener, IChordChannelListener
    {
        public const string ErrorStarting = "Error starting Chord: ";
        public const string ErrorCreating = "Error creating ChordManager: ";
        public const int FindGamesTimeout = 1000;
        public static readonly Encoding Encoding = Encoding.UTF8;

        private readonly Activity _activity;
        private readonly List<MultiplayerGame> _games = new List<MultiplayerGame>();
        private ChordManager _manager;
        private IChordChannel _publicChannel;
        private IChordChannel _channel;
        private TaskCompletionSource<bool> _startSource;
        private MultiplayerGame _game;
        private object _lock = new object();

        private static class MessageType
        {
            /// <summary>
            /// Message asking if anyone has a game on the public channel
            /// </summary>
            public const string ListGames = "L";
            /// <summary>
            /// Message responding with a game on the public channel
            /// </summary>
            public const string Game = "G";
        }

        [DataContract]
        private class PublicMessage
        {
            [DataMember]
            public string Type { get; set; }

            [DataMember]
            public MultiplayerGame Game { get; set; }
        }

        public SamsungMultiplayerService(Activity activity)
        {
            _activity = activity;
        }

        public bool Started { get; set; }

        public Dictionary<string, Type> MessageTypes { get; set; }

        public override bool Supported
        {
            get
            {
                if (Build.VERSION.SdkInt < BuildVersionCodes.IceCreamSandwich)
                {
                    return false;
                }

                if (_manager != null)
                    return true;

                try
                {
                    _manager = ChordManager.GetInstance(_activity);

                    return true;
                }
                catch (Exception exc)
                {
                    Console.WriteLine(ErrorCreating + exc);

                    return false;
                }
            }
        }

        private Task Start()
        {
            if (Started)
            {
                return Task.Factory.StartNew(() => { });
            }

            if (_manager == null)
                _manager = ChordManager.GetInstance(_activity);

            _manager.SetTempDirectory(Path.Combine(Path.GetTempPath(), "Chord"));
            _manager.SetHandleEventLooper(Looper.MainLooper);

            _startSource = new TaskCompletionSource<bool>();

            int result = _manager.Start(ChordManager.InterfaceTypeWifi, this);
            if (result != ChordManager.ErrorNone)
            {
                _startSource.SetException(result.ToException());
            }

            return _startSource.Task;
        }

        public override Task Host(MultiplayerGame game)
        {
            game.Id = Guid.NewGuid().ToString("N");

            return Start().ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    throw t.Exception.InnerExceptions.First();
                }

                _game = game;
                _channel = _manager.JoinChannel(game.Id, this);
            });
        }

        public override Task<List<MultiplayerGame>> FindGames()
        {
            return Start().ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    return t;
                }

                lock (_lock)
                    _games.Clear();

                _publicChannel.SendDataToAll(MessageType.ListGames, new PublicMessage
                {
                    Type = MessageType.ListGames,

                }.ToPayload());

                return Task.Delay(FindGamesTimeout);
            })
            .Unwrap()
            .ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    throw t.Exception.InnerExceptions.First();
                }

                return _games;
            });
        }

        public override Task Join(MultiplayerGame game)
        {
            return Task.Factory.StartNew(() =>
            {
                if (!Started)
                {
                    throw new InvalidOperationException("Chord is not started!");
                }

                _channel = _manager.JoinChannel(game.Id, this);
                _game = game;
            });
        }

        public override Task Send(string messageId, object message)
        {
            return Task.Factory.StartNew(() =>
            {
                if (_channel == null)
                    throw new InvalidOperationException("Game has not joined a private channel!");

                if (_game == null || !_game.Started)
                    throw new InvalidOperationException("The game has not been started!");

                _channel.SendData(_game.OpponentId, messageId, message.ToPayload());
            });
        }

        public override void Stop()
        {
            if (_manager != null)
            {
                _manager.Stop();
                if (_publicChannel != null)
                {
                    _manager.LeaveChannel(_publicChannel.Name);
                    _publicChannel.Dispose();
                    _publicChannel = null;
                }
                if (_channel != null)
                {
                    _manager.LeaveChannel(_channel.Name);
                    _channel.Dispose();
                    _channel = null;
                }
                _game = null;
                Started = false;
            }
        }

        #region ChordManager Listener

        public void OnError(int error)
        {
            if (_startSource != null)
            {
                Started = false;
                _startSource.SetException(error.ToException());
                _startSource = null;
            }
        }

        public void OnNetworkDisconnected()
        {
            //TODO: do anything here?
        }

        public void OnStarted(string name, int reason)
        {
            _publicChannel = _manager.JoinChannel(ChordManager.PublicChannel, this);

            //I have no idea why we need a delay here, but the first request fails otherwise
            Task.Delay(3000).ContinueWith(t =>
            {
                if (_startSource != null)
                {
                    Started = true;
                    _startSource.SetResult(true);
                    _startSource = null;
                }
            });
        }

        #endregion

        #region ChordChannel Listener

        public void OnDataReceived(string fromNode, string fromChannel, string payloadType, IntPtr payload)
        {
            try
            {
                var array = JNIEnv.GetArray<byte[]>(payload);

                //Public channel
                if (fromChannel == ChordManager.PublicChannel)
                {
                    var message = array.ToMessage<PublicMessage>();

                    switch (payloadType)
                    {
                        case MessageType.ListGames:
                            if (_game != null && !_game.Started)
                            {
                                _publicChannel.SendData(fromNode, MessageType.Game, new PublicMessage
                                {
                                    Type = MessageType.Game,
                                    Game = new MultiplayerGame
                                    {
                                        Id = _game.Id,
                                        Name = _game.Name,
                                        OpponentId = _manager.Name,
                                    },

                                }.ToPayload());
                            }
                            break;
                        case MessageType.Game:
                            lock (_lock)
                                _games.Add(message.Game);
                            break;
                        default:
                            break;
                    }
                }
                //Our private channel
                else if (_channel != null && _channel.Name == fromChannel)
                {
                    if (MessageTypes == null)
                    {
                        Console.WriteLine("MessageTypes is null!");
                    }
                    else
                    {
                        Type messageType;
                        if (MessageTypes.TryGetValue(payloadType, out messageType))
                        {
                            var message = array.ToMessage(messageType);

                            _activity.RunOnUiThread(() => OnReceived(message));
                        }
                        else
                        {
                            Console.WriteLine("No message type found for: " + messageType);
                        }
                    }
                }
            }
            finally
            {
                JNIEnv.DeleteLocalRef(payload);
            }
        }

        public void OnFileChunkReceived(string fromNode, string fromChannel, string fileName, string hash, string fileType, string exchangeId, long fileSize, long offset)
        {

        }

        public void OnFileChunkSent(string toNode, string toChannel, string fileName, string hash, string fileType, string exchangeId, long fileSize, long offset, long chunkSize)
        {

        }

        public void OnFileFailed(string node, string channel, string filename, string hash, string exchangeId, int reason)
        {

        }

        public void OnFileReceived(string fromNode, string fromChannel, string fileName, string hash, string fileType, string exchangeId, long fileSize, string tmpFilePath)
        {

        }

        public void OnFileSent(string toNode, string toChannel, string fileName, string hash, string fileType, string exchangeId)
        {

        }

        public void OnFileWillReceive(string fromNode, string fromChannel, string fileName, string hash, string fileType, string exchangeId, long fileSize)
        {

        }

        public void OnNodeJoined(string fromNode, string fromChannel)
        {
            if (fromChannel != ChordManager.PublicChannel && _game != null && _game.Id == fromChannel)
            {
                _game.OpponentId = fromNode;
                _game.Started = true;
            }
        }

        public void OnNodeLeft(string fromNode, string fromChannel)
        {
            //TODO: need to handle this
        }

        #endregion
    }
}