using System.Buffers.Binary;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace RockSolid.Uuid;

/// <summary>
/// 
/// </summary>
public static class Uuid
{

    /// <summary>
    /// The nil UUID (all bits clear), as defined by RFC 9562 section 5.9.
    /// </summary>
    public static readonly Guid Nil = Guid.Parse("00000000-0000-0000-0000-000000000000");
    /// <summary>
    /// The Max UUID (all bits set), as defined by RFC 9562 section 5.10.
    /// </summary>
    public static readonly Guid Max = Guid.Parse("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF");
    /// <summary>
    /// Namespace UUID, as defined by RFC 9562 section 6.6.
    /// </summary>
    public static readonly Guid DNS = Guid.Parse("6ba7b810-9dad-11d1-80b4-00c04fd430c8");
    /// <summary>
    /// Namespace UUID, as defined by RFC 9562 section 6.6.
    /// </summary>
    public static readonly Guid URL = Guid.Parse("6ba7b811-9dad-11d1-80b4-00c04fd430c8");
    /// <summary>
    /// Namespace UUID, as defined by RFC 9562 section 6.6.
    /// </summary>
    public static readonly Guid OID = Guid.Parse("6ba7b812-9dad-11d1-80b4-00c04fd430c8");
    /// <summary>
    /// Namespace UUID, as defined by RFC 9562 section 6.6.
    /// </summary>
    public static readonly Guid X500 = Guid.Parse("6ba7b814-9dad-11d1-80b4-00c04fd430c8");

    private static readonly DateTimeOffset s_gregorianEpoch = new(1582, 10, 15, 0, 0, 0, TimeSpan.Zero);

    internal delegate byte[]? AddressFactory();

    private static readonly Lazy<Generator> s_generator = new(static () => new Generator(DefaultAddressFactory), isThreadSafe: true);

    private static Guid Create(Span<byte> buffer, int version, int variant = 0b10)
    {
        buffer[6] = (byte)((version << 4) | (buffer[6] & 0x0F));
        buffer[8] = (byte)((variant << 6) | (buffer[8] & 0x3F));
        return new Guid(buffer, bigEndian: true);
    }

    /// <summary>
    /// Creates a new UUID version 1 (time-based).
    /// </summary>
    /// <returns>A <see cref="System.Guid"/> value.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>derives timestamps from <see cref="DateTime.UtcNow"/>,</description></item>
    /// <item><description>uses a clock sequence that increments when the clock does not advance,</description></item>
    /// <item><description>and uses either the first available MAC address or a randomly generated multicast node identifier.</description></item>
    /// </list>
    ///  0                   1                   2                   3
    ///  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// |                           time_low                            |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// |           time_mid            |  ver  |       time_high       |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// |var|         clock_seq         |             node              |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// |                              node                             |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// RFC 9562, Figure 9: UUIDv4 Field and Bit Layout
    /// </remarks>
    public static Guid CreateV1()
        => s_generator.Value.NextV1();

    /// <summary>
    /// Creates a UUID version 1 value from timestamp, clock sequence, and node values.
    /// </summary>
    /// <param name="time">
    /// The UTC timestamp used to construct the 60-bit UUID time field.
    /// Must not be earlier than the Gregorian epoch (1582-10-15).
    /// </param>
    /// <param name="clockSeq">
    /// The 14-bit clock sequence. Higher bits are masked off; the RFC variant bits are applied automatically.
    /// </param>
    /// <param name="node">
    /// A 6-byte node identifier, typically a MAC address or multicast address.
    /// </param>
    /// <returns>A <see cref="System.Guid"/> value.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="time"/> is earlier than the Gregorian epoch
    /// or when <paramref name="node"/> is not 6 bytes long.
    /// </exception>
    /// <remarks>
    /// The timestamp is converted to 100-nanosecond ticks since 1582-10-15 (Gregorian epoch),
    /// masked to 60 bits, and packed into the <c>time_low</c>, <c>time_mid</c>, and <c>time_hi_and_version</c>
    /// fields as defined in RFC 9562.
    /// </remarks>
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

    /// <summary>
    /// Creates a UUID version 3 value from namespace and name values.
    /// </summary>
    /// <param name="ns">
    /// The UTC timestamp used to construct the 60-bit UUID time field.
    /// Must not be earlier than the Gregorian epoch (1582-10-15).
    /// </param>
    /// <param name="name">
    /// The 14-bit clock sequence. Higher bits are masked off; the RFC variant bits are applied automatically.
    /// </param>
    /// <returns>A <see cref="System.Guid"/> value.</returns>
    /// <remarks>
    ///   0                   1                   2                   3
    ///   0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                            md5_high                           |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |          md5_high             |  ver  |       md5_mid         |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |var|                        md5_low                            |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                            md5_low                            |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  RFC 9562, Figure 7: UUIDv3 Field and Bit Layout
    /// </remarks>
    public static Guid CreateV3(Guid ns, ReadOnlySpan<byte> name)
        => Create(MD5.HashData([.. ns.ToByteArray(bigEndian: true), .. name]), 3);

    /// <summary>
    /// Creates a UUID version 4 from a large random number.
    /// </summary>
    /// <returns>A <see cref="System.Guid"/> value.</returns>
    /// <remarks>
    /// The random number is generated by <see cref="System.Security.Cryptography.RandomNumberGenerator"/>. 
    /// 
    ///   0                   1                   2                   3
    ///   0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                           random_a                            |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |          random_a             |  ver  |       random_b        |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |var|                       random_c                            |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                           random_c                            |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// RFC 9562, Figure 9: UUIDv4 Field and Bit Layout
    /// </remarks>
    public static Guid CreateV4()
        => Create(RandomNumberGenerator.GetBytes(16), 4);

    /// <summary>
    /// Creates a UUID version 4 from the passed in buffer.
    /// </summary>
    /// <returns>A <see cref="System.Guid"/> value.</returns>
    /// <remarks>
    ///   0                   1                   2                   3
    ///   0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                           random_a                            |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |          random_a             |  ver  |       random_b        |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |var|                       random_c                            |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                           random_c                            |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// RFC 9562, Figure 9: UUIDv4 Field and Bit Layout
    /// </remarks>
    public static Guid CreateV4(Span<byte> buffer)
        => Create(buffer[..16], 4);

    /// <summary>
    /// Create a UUID version 5 from namespace and name.
    /// </summary>
    /// <param name="ns"></param>
    /// <param name="name"></param>
    /// <returns>A <see cref="System.Guid"/> value.</returns>
    /// <remarks>
    ///  0                   1                   2                   3
    ///  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                           sha1_high                           |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |         sha1_high             |  ver  |      sha1_mid         |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |var|                       sha1_low                            |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                           sha1_low                            |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// RFC 9562, Figure 9: UUIDv5 Field and Bit Layout
    /// </remarks>
    public static Guid CreateV5(Guid ns, ReadOnlySpan<byte> name)
        => Create(SHA1.HashData([.. ns.ToByteArray(bigEndian: true), .. name]).AsSpan(0, 16), 5);

    /// <summary>
    /// Creates a UUID version 6 values using an internal generator.
    /// </summary>
    /// <returns>A <see cref="System.Guid"/> value.</returns>
    public static Guid CreateV6()
        => s_generator.Value.NextV6();

    /// <summary>
    /// Creates a UUID version 6 value from timestamp, clock sequence, and node values.
    /// </summary>
    /// <param name="time">
    /// The UTC timestamp used to construct the 60-bit UUID time field.
    /// Must not be earlier than the Gregorian epoch (1582-10-15).
    /// </param>
    /// <param name="clockSeq">
    /// The 14-bit clock sequence. Higher bits are masked off; the RFC variant bits are applied automatically.
    /// </param>
    /// <param name="node">
    /// A 6-byte node identifier, typically a MAC address or multicast address.
    /// </param>
    /// <returns>A <see cref="System.Guid"/> value.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="time"/> is earlier than the Gregorian epoch
    /// or when <paramref name="node"/> is not 6 bytes long.
    /// </exception>
    /// <remarks>
    /// The timestamp is converted to 100-nanosecond ticks since 1582-10-15 (Gregorian epoch),
    /// masked to 60 bits, and packed into the <c>time_low</c>, <c>time_mid</c>, and <c>time_hi_and_version</c>
    /// fields as defined in RFC 9562.
    /// 
    ///   0                   1                   2                   3
    ///   0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
    ///   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///   |                           time_high                           |
    ///   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///   |           time_mid            |  ver  |       time_low        |
    ///   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///   |var|         clock_seq         |             node              |
    ///   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///   |                              node                             |
    ///   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+ 
    ///                     RFC 9562, Figure 9: UUIDv6 Field and Bit Layout
    /// </remarks>
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

    /// <summary>
    /// Creates a UUID v7 value from <see cref="DateTimeOffset.UtcNow"/> using millisecond precision.
    /// </summary>
    /// <returns>A <see cref="System.Guid"/> value.</returns>
    public static Guid CreateV7()
        => CreateV7(DateTimeOffset.UtcNow);

    /// <summary>
    /// Creates a UUID v7 value from timestamp using millisecond precision.
    /// </summary>
    /// <param name="time"></param>
    /// <returns>A <see cref="System.Guid"/> value.</returns>
    public static Guid CreateV7(DateTimeOffset time)
    {
        Span<byte> buffer = stackalloc byte[18];
        BinaryPrimitives.WriteUInt64BigEndian(buffer, (ulong)time.ToUnixTimeMilliseconds());
        RandomNumberGenerator.Fill(buffer[8..]);
        return Create(buffer[2..], 7);
    }

    /// <summary>
    /// Creates a UUID v7 value from timestamp using millisecond precision.
    /// </summary>
    /// <param name="time"></param>
    /// <param name="randA"></param>
    /// <param name="randB"></param>
    /// <returns>A <see cref="System.Guid"/> value.</returns>
    public static Guid CreateV7(DateTimeOffset time, ushort randA, ulong randB)
        => CreateV7((ulong)time.ToUnixTimeMilliseconds(), randA, randB);

    /// <summary>
    /// Creates a UUID v7 value from unix timestamp in milliseconds.
    /// </summary>
    /// <param name="time"></param>
    /// <param name="randA"></param>
    /// <param name="randB"></param>
    /// <returns>A <see cref="System.Guid"/> value.</returns>
    /// <remarks>
    ///   0                   1                   2                   3
    ///   0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                           unix_ts_ms                          |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |          unix_ts_ms           |  ver  |       rand_a          |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |var|                        rand_b                             |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                            rand_b                             |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// RFC 9562, Figure 11: UUIDv7 Field and Bit Layout
    /// </remarks>
    public static Guid CreateV7(ulong time, ushort randA, ulong randB)
    {
        Span<byte> buffer = stackalloc byte[18];
        BinaryPrimitives.WriteUInt64BigEndian(buffer, time);
        BinaryPrimitives.WriteUInt16BigEndian(buffer[8..], randA);
        BinaryPrimitives.WriteUInt64BigEndian(buffer[10..], randB);
        return Create(buffer[2..], 7);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="buffer"></param>
    /// <returns>A <see cref="System.Guid"/> value.</returns>
    public static Guid CreateV8(Span<byte> buffer)
        => Create(buffer[..16], 8);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="customA"></param>
    /// <param name="customB"></param>
    /// <param name="customC"></param>
    /// <returns>A <see cref="System.Guid"/> value.</returns>
    public static Guid CreateV8(long customA, short customB, long customC)
    {
        Span<byte> buffer = stackalloc byte[18];
        BinaryPrimitives.WriteInt64BigEndian(buffer, customA);
        BinaryPrimitives.WriteInt16BigEndian(buffer[8..], customB);
        BinaryPrimitives.WriteInt64BigEndian(buffer[10..], customC);
        return Create(buffer[2..], 8);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ns"></param>
    /// <param name="name"></param>
    /// <returns>A <see cref="System.Guid"/> value.</returns>
    public static Guid CreateV8(Guid ns, ReadOnlySpan<byte> name)
        => Create(SHA256.HashData([.. ns.ToByteArray(bigEndian: true), .. name]).AsSpan(0, 16), 8);


    /// <summary>
    /// 
    /// </summary>
    /// <param name="guid"></param>
    /// <returns></returns>
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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="guid"></param>
    /// <returns></returns>
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

        public static byte[] GetRandomAddress()
        {
            var address = RandomNumberGenerator.GetBytes(6);
            address[0] |= 0x01; // multicast bit            
            return address;
        }

    }
}
