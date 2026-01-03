using System.Buffers.Binary;
using System.Net.NetworkInformation;
using System.Security.Cryptography;

namespace RockSolid.Uuid;

public static class Uuid
{

    public static readonly Guid Nil = Guid.Parse("00000000-0000-0000-0000-000000000000");
    public static readonly Guid Max = Guid.Parse("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF");
    public static readonly Guid DNS = Guid.Parse("6ba7b810-9dad-11d1-80b4-00c04fd430c8");
    public static readonly Guid URL = Guid.Parse("6ba7b811-9dad-11d1-80b4-00c04fd430c8");
    public static readonly Guid OID = Guid.Parse("6ba7b812-9dad-11d1-80b4-00c04fd430c8");
    public static readonly Guid X500 = Guid.Parse("6ba7b814-9dad-11d1-80b4-00c04fd430c8");

    private static readonly DateTimeOffset s_gregorianEpoch = new(1582, 10, 15, 0, 0, 0, TimeSpan.Zero);

    public delegate byte[]? AddressFactory();

    private static readonly Lazy<Generator> s_generator = new(static () => new Generator(DefaultAddressFactory), isThreadSafe: true);

    private static Guid Create(Span<byte> buffer, int version, int variant = 0b10)
    {
        buffer[6] = (byte)((version << 4) | (buffer[6] & 0x0F));
        buffer[8] = (byte)((variant << 6) | (buffer[8] & 0x3F));
        return new Guid(buffer, bigEndian: true);
    }

    public static Guid CreateV1()
        => s_generator.Value.NextV1();

    public static Guid CreateV1(DateTimeOffset time, short clockSeq, ReadOnlySpan<byte> node)
    {
        if (time < s_gregorianEpoch)
            throw new ArgumentException("Cannot be before 1582-10-15", nameof(time));

        if (node.Length != 6)
            throw new ArgumentException("Must be of length 6", nameof(node));

        long ticks = (time - s_gregorianEpoch).Ticks & 0x0FFFFFFFFFFFFFFFL;
        int timeLow = (int)(ticks & 0xFFFFFFFF);
        short timeMid = (short)((ticks >> 32) & 0xFFFF);
        short timeHigh = (short)(((ticks >> 48) & 0x0FFF) | (1 << 12));
        clockSeq = (short)((clockSeq & 0x3FFF) | 0x8000);

        return new Guid(
            timeLow,
            timeMid,
            timeHigh,
            (byte)(clockSeq >> 8),
            (byte)(clockSeq & 0xFF),
            node[0],
            node[1],
            node[2],
            node[3],
            node[4],
            node[5]
        );
    }

    public static Guid CreateV3(Guid ns, ReadOnlySpan<byte> name)
        => Create(MD5.HashData([.. ns.ToByteArray(bigEndian: true), .. name]), 3);

    public static Guid CreateV4()
        => Create(RandomNumberGenerator.GetBytes(16), 4);

    public static Guid CreateV4(Span<byte> buffer)
        => Create(buffer[..16], 4);

    public static Guid CreateV5(Guid ns, ReadOnlySpan<byte> name)
        => Create(SHA1.HashData([.. ns.ToByteArray(bigEndian: true), .. name]).AsSpan(0, 16), 5);

    public static Guid CreateV6()
        => s_generator.Value.NextV6();

    public static Guid CreateV6(DateTimeOffset time, short clockSeq, ReadOnlySpan<byte> node)
    {

        if (time < s_gregorianEpoch)
            throw new ArgumentException("Cannot be before 1582-10-15", nameof(time));

        if (node.Length != 6)
            throw new ArgumentException("Must be of length 6", nameof(node));

        long ticks = (time - s_gregorianEpoch).Ticks & 0x0FFFFFFFFFFFFFFFL;
        short timeLow = (short)((ticks & 0x0FFF) | (6 << 12));
        short timeMid = (short)((ticks >> 12) & 0xFFFF);
        int timeHigh = (int)((ticks >> 28) & 0xFFFFFFFF);
        clockSeq = (short)((clockSeq & 0x3FFF) | 0x8000);

        return new Guid(
            timeHigh,
            timeMid,
            timeLow,
            (byte)(clockSeq >> 8),
            (byte)(clockSeq & 0xFF),
            node[0],
            node[1],
            node[2],
            node[3],
            node[4],
            node[5]
        );
    }

    public static Guid CreateV7()
        => CreateV7(DateTimeOffset.UtcNow);

    public static Guid CreateV7(DateTimeOffset time)
    {
        Span<byte> buffer = stackalloc byte[18];
        BinaryPrimitives.WriteUInt64BigEndian(buffer, (ulong)time.ToUnixTimeMilliseconds());
        RandomNumberGenerator.Fill(buffer[8..]);
        return Create(buffer[2..], 7);
    }

    public static Guid CreateV7(DateTimeOffset time, ushort randA, ulong randB)
        => CreateV7((ulong)time.ToUnixTimeMilliseconds(), randA, randB);

    public static Guid CreateV7(ulong time, ushort randA, ulong randB)
    {
        Span<byte> buffer = stackalloc byte[18];
        BinaryPrimitives.WriteUInt64BigEndian(buffer, time);
        BinaryPrimitives.WriteUInt16BigEndian(buffer[8..], randA);
        BinaryPrimitives.WriteUInt64BigEndian(buffer[10..], randB);
        return Create(buffer[2..], 7);
    }

    public static (DateTimeOffset, short, byte[]) ParseV1(Guid guid)
    {
        var buffer = guid.ToByteArray(bigEndian: true).AsSpan();
        var timeLow = (long)BinaryPrimitives.ReadUInt32BigEndian(buffer);
        var timeMid = (long)BinaryPrimitives.ReadUInt16BigEndian(buffer[4..]);
        var timeHigh = (long)(BinaryPrimitives.ReadUInt16BigEndian(buffer[6..]) & 0x0FFF);
        var ticks = (timeHigh << 48) | (timeMid << 32) | timeLow;
        var time = s_gregorianEpoch + TimeSpan.FromTicks(ticks);
        var clockSeq = (short)(BinaryPrimitives.ReadUInt16BigEndian(buffer[8..]) & 0x3FFF);
        var node = buffer[10..].ToArray();
        return (time, clockSeq, node);
    }

    public static (DateTimeOffset, short, byte[]) ParseV6(Guid guid)
    {
        var buffer = guid.ToByteArray(bigEndian: true).AsSpan();
        var timeHigh = (long)BinaryPrimitives.ReadUInt32BigEndian(buffer[..]);
        var timeMid = (long)BinaryPrimitives.ReadUInt16BigEndian(buffer[4..]);
        var timeLow = (long)(BinaryPrimitives.ReadUInt16BigEndian(buffer[6..]) & 0x0FFF);
        var ticks = (timeHigh << 28) | (timeMid << 12) | timeLow;
        var time = s_gregorianEpoch + TimeSpan.FromTicks(ticks);
        var clockSeq = (short)(BinaryPrimitives.ReadUInt16BigEndian(buffer[8..]) & 0x3FFF);
        var node = buffer[10..].ToArray();
        return (time, clockSeq, node);
    }

    private static byte[]? DefaultAddressFactory()
    {
        return NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(nic =>
                nic.OperationalStatus == OperationalStatus.Up &&
                nic.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                nic.GetPhysicalAddress().GetAddressBytes().Length == 6)
            .Select(nic => nic.GetPhysicalAddress().GetAddressBytes())
            .FirstOrDefault();
    }

    internal sealed class Generator(AddressFactory addressFactory)
    {
        private readonly byte[] _node = addressFactory() ?? GetRandomAddress();
        private readonly object _lock = new();
        private DateTime _lastTime = DateTime.MinValue;
        private short _clockSeq = GetRandomClockSeq();

        internal static short GetRandomClockSeq()
        {
            var clockSeq = RandomNumberGenerator.GetBytes(2);
            return (short)(((clockSeq[0] << 8) | clockSeq[1]) & 0x3FFF);
        }

        private DateTime Generate()
        {
            var time = DateTime.UtcNow;
            if (time <= _lastTime)
                _clockSeq = (short)((_clockSeq + 1) & 0x3FFF);
            else
                _lastTime = time;
            return time;
        }

        public Guid NextV1()
        {
            lock (_lock)
            {
                return CreateV1(Generate(), _clockSeq, _node);
            }
        }

        public Guid NextV6()
        {
            lock (_lock)
            {
                return CreateV6(Generate(), _clockSeq, _node);
            }
        }

        private static byte[] GetRandomAddress()
        {
            var address = RandomNumberGenerator.GetBytes(6);
            address[0] |= 0x01; // multicast bit            
            return address;
        }

    }
}
