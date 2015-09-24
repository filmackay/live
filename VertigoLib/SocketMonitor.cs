using System;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;

namespace Vertigo
{

    public enum Error : uint
    {
        NO_ERROR = 0,
        ERROR_INSUFFICIENT_BUFFER = 122,
        ERROR_NOT_FOUND = 1168,
    }

    public class SocketMonitor
    {
        private bool _tcpFound;
        private MIB_TCPROW _tcpRow;
        private ushort remotePort;
        private uint[] remoteAddr;

        public SocketMonitor(string host, ushort port)
        {
            // get IP's
            IPAddress[] ipAddresses;
            IPAddress ipAddress;
            if (IPAddress.TryParse(host, out ipAddress))
            {
                // single IP specified
                ipAddresses = new[] {ipAddress};
            }
            else
            {
                // hostname specified
                ipAddresses = Dns.GetHostAddresses(host);
            }

            // convert to network order
            remotePort = ToNetworkOrder(port);
            remoteAddr = ipAddresses
                .Select(ip =>
                    {
                        var b = ip.GetAddressBytes();
                        return ToNetworkOrder((uint)((b[0] << 24) | (b[1] << 16) | (b[2] << 8) | b[3]));
                    })
                .ToArray();
        }

        private void FindSocket()
        {
            var buff = IntPtr.Zero;
            try
            {
                // get all sockets
                var buffSize = 0;
                while (GetTcpTable(buff, ref buffSize, false) == Error.ERROR_INSUFFICIENT_BUFFER)
                {
                    if (buff != IntPtr.Zero)
                        Marshal.FreeHGlobal(buff);
                    buff = Marshal.AllocHGlobal(buffSize);
                }
                var err = GetTcpTable(buff, ref buffSize, false);
                var numEntries = Marshal.ReadInt32(buff);
                var entrySize = Marshal.SizeOf(typeof(MIB_TCPROW));
                const int entriesOffset = sizeof(uint);
                for (var i = 0; i < numEntries; i++)
                {
                    var nativePtr = buff + entriesOffset + (entrySize * i);
                    var row = (MIB_TCPROW)Marshal.PtrToStructure(nativePtr, typeof(MIB_TCPROW));

                    if (row.dwRemotePort == remotePort && remoteAddr.Contains(row.dwRemoteAddr) && row.dwState == (uint)MIB_TCPROW_STATE.MIB_TCP_STATE_ESTAB)
                    {
                        // enable monitoring
                        var rw = new TCP_ESTATS_FINE_RTT_RW_v0 { EnableCollection = 1 };
                        var rwPtr = Marshal.AllocHGlobal(Marshal.SizeOf(rw));
                        Marshal.StructureToPtr(rw, rwPtr, false);
                        var ret = SetPerTcpConnectionEStats(ref row, TCP_ESTATS_TYPE.TcpConnectionEstatsFineRtt, rwPtr, 0, Marshal.SizeOf(rw), 0);
                        Marshal.FreeHGlobal(rwPtr);

                        // remember
                        _tcpFound = true;
                        _tcpRow = row;
                        return;
                    }
                }

                // did not find a socket
                _tcpFound = false;
            }
            finally
            {
                if (buff != IntPtr.Zero)
                    Marshal.FreeHGlobal(buff);
            }
        }

        public TimeSpan? LatestLatency
        {
            get
            {
                var count = 0;

                while (true)
                {
                    ++count;

                    var rodPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(TCP_ESTATS_FINE_RTT_ROD_v0)));
                    var ret = _tcpFound
                                  ? GetPerTcpConnectionEStats(ref _tcpRow,
                                                              TCP_ESTATS_TYPE.TcpConnectionEstatsFineRtt,
                                                              IntPtr.Zero, 0, 0,
                                                              IntPtr.Zero, 0, 0,
                                                              rodPtr, 0,
                                                              Marshal.SizeOf(typeof (TCP_ESTATS_FINE_RTT_ROD_v0)))
                                  : Error.ERROR_INSUFFICIENT_BUFFER;
                    if (ret == Error.NO_ERROR)
                    {
                        // found socket
                        var rod = (TCP_ESTATS_FINE_RTT_ROD_v0)Marshal.PtrToStructure(rodPtr, typeof(TCP_ESTATS_FINE_RTT_ROD_v0));
                        return TimeSpan.FromTicks(rod.SumRtt * 10);
                    }
                    if (count > 1)
                        return null;

                    // try to find socket again
                    FindSocket();
                }
            }
        }

        public static ushort ToNetworkOrder(ushort host)
        {
            return (ushort)(((host & 0xff) << 8) | ((host >> 8) & 0xff));
        }

        public static uint ToNetworkOrder(uint host)
        {
            return (((ToNetworkOrder((ushort)host) & (uint)0xffff) << 0x10) | (ToNetworkOrder((ushort)(host >> 0x10)) & (uint)0xffff));
        }

        private enum MIB_TCPROW_STATE : uint
        {
            MIB_TCP_STATE_CLOSED = 1,
            MIB_TCP_STATE_LISTEN = 2,
            MIB_TCP_STATE_SYN_SENT = 3,
            MIB_TCP_STATE_SYN_RCVD = 4,
            MIB_TCP_STATE_ESTAB = 5,
            MIB_TCP_STATE_FIN_WAIT1 = 6,
            MIB_TCP_STATE_FIN_WAIT2 = 7,
            MIB_TCP_STATE_CLOSE_WAIT = 8,
            MIB_TCP_STATE_CLOSING = 9,
            MIB_TCP_STATE_LAST_ACK = 10,
            MIB_TCP_STATE_TIME_WAIT = 11,
            MIB_TCP_STATE_DELETE_TCB = 12,
        }

        private enum TCP_ESTATS_TYPE : uint
        {
            TcpConnectionEstatsSynOpts,
            TcpConnectionEstatsData,
            TcpConnectionEstatsSndCong,
            TcpConnectionEstatsPath,
            TcpConnectionEstatsSendBuff,
            TcpConnectionEstatsRec,
            TcpConnectionEstatsObsRec,
            TcpConnectionEstatsBandwidth,
            TcpConnectionEstatsFineRtt,
            TcpConnectionEstatsMaximum,
        }


        [StructLayout(LayoutKind.Sequential)]
        private struct TCP_ESTATS_FINE_RTT_RW_v0
        {
            public byte EnableCollection;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TCP_ESTATS_FINE_RTT_ROD_v0
        {
            public uint RttVar;
            public uint MaxRtt;
            public uint MinRtt;
            public uint SumRtt;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_TCPROW
        {
            public uint dwState;
            public uint dwLocalAddr;
            public ushort dwLocalPort;
            public ushort dwLocalPort2;
            public uint dwRemoteAddr;
            public ushort dwRemotePort;
            public ushort dwRemotePort2;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_TCPTABLE
        {
            public uint dwNumEntries;
            public MIB_TCPROW[] table;
        }

        [DllImport("Iphlpapi.dll", EntryPoint = "GetTcpTable")]
        private static extern Error GetTcpTable(IntPtr buff, ref int buffSize, bool sort);

        [DllImport("Iphlpapi.dll", EntryPoint = "GetPerTcpConnectionEStats")]
        private static extern Error GetPerTcpConnectionEStats(ref MIB_TCPROW rowPtr, TCP_ESTATS_TYPE EstatsType, IntPtr rw, uint RwVersion, int RwSize, IntPtr ros, uint RosVersion, int RosSize, IntPtr rod, uint RodVersion, int RodSize);

        [DllImport("Iphlpapi.dll", EntryPoint = "SetPerTcpConnectionEStats")]
        private static extern Error SetPerTcpConnectionEStats(ref MIB_TCPROW rowPtr, TCP_ESTATS_TYPE EstatsType, IntPtr Rw, uint RwVersion, int RwSize, uint Offset);
    }
}
