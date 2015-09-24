using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Subjects;
using System.Threading;

namespace Vertigo
{
    public class Pinger
    {
        private readonly byte[] _signature; // unique to this Pinger
        private readonly Socket _socket;
        private readonly EndPoint _endPoint;
        private readonly Subject<TimeSpan> _echoReplies = new Subject<TimeSpan>();
        private readonly SocketAsyncEventArgs _recvArgs;
        private int _recv;
        public IObservable<TimeSpan> EchoReplies { get { return _echoReplies; } }
        private readonly Random _random = new Random();

        public Pinger(string hostNameOrAddress)
        {
            var addresses = Dns.GetHostAddresses(hostNameOrAddress);
            if (addresses.Length == 0)
                throw new InvalidOperationException("Host not found");

            // configure raw socket communication
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);
            _endPoint = new IPEndPoint(addresses[0], 0);
            _random.NextBytes(_signature = new byte[16]);

            // configure receiving of replies
            _recvArgs = new SocketAsyncEventArgs { RemoteEndPoint = _endPoint };
            _recvArgs.SetBuffer(new byte[1024], 0, 1024);
            _recvArgs.Completed += (sender, args) => HandleRecv(args);
        }

        public void Dispose()
        {
            _socket.Disconnect(false);
            _socket.Dispose();
        }

        public void SendEcho()
        {
            var sendTime = BitConverter.GetBytes(HiResTimer.Now());
            var body = new byte[_signature.Length + sendTime.Length];
            Buffer.BlockCopy(_signature, 0, body, 0, _signature.Length);
            Buffer.BlockCopy(sendTime, 0, body, _signature.Length, sendTime.Length);
            var packet = new IcmpPacket { Data = body };
            var bytes = packet.Format();
            var sendArgs = new SocketAsyncEventArgs { RemoteEndPoint = _endPoint };
            sendArgs.SetBuffer(bytes, 0, bytes.Length);

            // send echo
            _socket.SendToAsync(sendArgs);

            // if first, start listening for echo replies
            if (Interlocked.CompareExchange(ref _recv, 1, 0) == 0)
                StartRecv();
        }

        private void StartRecv()
        {
            if (!_socket.ReceiveFromAsync(_recvArgs))
                HandleRecv(_recvArgs);
        }

        private void HandleRecv(SocketAsyncEventArgs args)
        {
            // validate signature
            var signature = new byte[_signature.Length];
            Buffer.BlockCopy(args.Buffer, 24, signature, 0, signature.Length);
            if (_signature.SequenceEqual(signature))
            {
                // handle reply
                var sendTime = BitConverter.ToInt64(args.Buffer, 24 + signature.Length);
                var roundTrip = HiResTimer.ToTimeSpan(HiResTimer.Now() - sendTime);
                _echoReplies.OnNext(roundTrip);
            }

            // start another recv
            StartRecv();
        }

        public class IcmpPacket
        {
            public byte Type = 0x08; // echo request
            public byte Code;
            public UInt16 CheckSum;
            public byte[] Data = new byte[0];

            public void Parse(byte[] packet)
            {
                Type = packet[20];
                Code = packet[21];
                CheckSum = BitConverter.ToUInt16(packet, 22);
                var dataLen = packet.Length - 24;
                Data = new byte[dataLen];
                Buffer.BlockCopy(packet, 24, Data, 0, dataLen);
            }

            public byte[] Format()
            {
                // prepare data
                //var packet = new byte[Data.Length + 8];
                //packet[0] = Type;
                //packet[1] = Code;
                //Buffer.BlockCopy(Data, 0, packet, 4, Data.Length);

                // prepare data
                var packet = new byte[Data.Length + 4];
                packet[0] = Type;
                packet[1] = Code;
                Buffer.BlockCopy(Data, 0, packet, 4, Data.Length);

                // calculate checksum
                UInt32 checkSum = 0;
                var index = 0;
                while (index < packet.Length)
                {
                    checkSum += Convert.ToUInt32(BitConverter.ToUInt16(packet, index));
                    index += 2;
                }
                checkSum = (checkSum >> 16) + (checkSum & 0xffff);
                checkSum += (checkSum >> 16);
                CheckSum = (UInt16)~checkSum;

                // set checksum
                Buffer.BlockCopy(BitConverter.GetBytes(CheckSum), 0, packet, 2, 2);
                return packet;
            }
        }
    }
}
