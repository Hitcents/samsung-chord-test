using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using System.Runtime.Serialization;

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

        private readonly Context _context;
        private ChordManager _manager;
        private IChordChannel _publicChannel;
        private IChordChannel _channel;
        private List<MultiplayerGame> _games = new List<MultiplayerGame>();
        private TaskCompletionSource<bool> _startSource;
        private MultiplayerGame _game;
        private object _lock = new object();

        private enum MessageType
        {
            /// <summary>
            /// Message asking if anyone has a game on the public channel
            /// </summary>
            ListGames,
            /// <summary>
            /// Message responding with a game on the public channel
            /// </summary>
            Game,
        }

        [DataContract]
        private class PublicMessage
        {
            [DataMember]
            public MessageType Type { get; set; }

            [DataMember]
            public MultiplayerGame Game { get; set; }
        }

        public SamsungMultiplayerService(Context context)
        {
            _context = context;
        }

        public bool Started { get; set; }

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
                    _manager = ChordManager.GetInstance(_context);

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
                _manager = ChordManager.GetInstance(_context);

            _manager.SetTempDirectory(Path.Combine(Path.GetTempPath(), "Chord"));
            _manager.SetHandleEventLooper(Looper.MainLooper);

            _startSource = new TaskCompletionSource<bool>();

            var types = _manager.AvailableInterfaceTypes;
            int result = _manager.Start(types.First().IntValue(), this);
            if (result != ChordManager.ErrorNone)
            {
                _startSource.SetException(result.ToException());
            }

            return _startSource.Task;
        }

        public override Task Host(MultiplayerGame game)
        {
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
                lock (_lock)
                    _games = new List<MultiplayerGame>();

                if (t.IsFaulted)
                {
                    return t;
                }

                _publicChannel.SendDataToAll(ChordManager.PublicChannel, new PublicMessage
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


            return base.Join(game);
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
            if (_startSource != null)
            {
                Started = true;
                _publicChannel = _manager.JoinChannel(ChordManager.PublicChannel, this);
                _startSource.SetResult(true);
                _startSource = null;
            }
        }

        #endregion

        #region ChordChannel Listener

        public void OnDataReceived(string fromNode, string fromChannel, string payloadType, byte[][] payload)
        {
            if (fromChannel == ChordManager.PublicChannel)
            {
                var message = payload.ToMessage<PublicMessage>();
                switch (message.Type)
                {
                    case MessageType.ListGames:
                        if (_game != null && !_game.Started)
                        {
                            _publicChannel.SendData(fromChannel, fromNode, new PublicMessage
                            {
                                Type = MessageType.Game,
                                Game = _game,

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
            else
            {

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
            
        }

        public void OnNodeLeft(string fromNode, string fromChannel)
        {
            //TODO: need to handle this
        }

        #endregion
    }
}