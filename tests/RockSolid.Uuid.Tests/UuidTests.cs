namespace RockSolid.Uuid.Tests;

using RockSolid.Uuid;
using System.Linq;

public class UuidTests
{


    [Fact]
    public void TestV1()
    {
        var time = new DateTimeOffset(2022, 2, 22, 14, 22, 22, TimeSpan.FromHours(-5)).ToUniversalTime().UtcDateTime;
        var expected = Guid.Parse("C232AB00-9414-11EC-B3C8-9F6BDECED846");
        var actual = Uuid.CreateV1(time, 0x33C8, [0x9F, 0x6B, 0xDE, 0xCE, 0xD8, 0x46]);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TestV1Generation()
    {
        var guids = Enumerable.Range(1, 1000).Select(i => Uuid.CreateV1()).ToArray();
        for (int i = 0; i < guids.Length; ++i)
        {
            Assert.Equal(1, guids[i].Version);
            Assert.Equal(0b10, guids[i].Variant >> 2);
            for (int j = i + 1; j < guids.Length; ++j)
                Assert.NotEqual(guids[i], guids[j]);
        }
    }

    [Fact]
    public void TestV1NodeIs48BitsLong()
    {
        Assert.Throws<ArgumentException>(() => Uuid.CreateV1(DateTime.UtcNow, 0, []));
    }

    [Fact]
    public void TestV1RejectsDatesBeforeGregorianCalendarStart()
    {
        Assert.Throws<ArgumentException>(() => Uuid.CreateV1(DateTimeOffset.MinValue, 0, []));
    }

    [Fact]
    public void TestV1Parse()
    {
        var time = new DateTimeOffset(2022, 2, 22, 14, 22, 22, TimeSpan.FromHours(-5)).ToUniversalTime().UtcDateTime;
        short clockSeq = 0x33C8;
        ReadOnlySpan<byte> node = [0x9F, 0x6B, 0xDE, 0xCE, 0xD8, 0x46];
        var guid = Uuid.CreateV1(time, clockSeq, node);
        var (actualTime, actualClockSeq, actualNode) = Uuid.ParseV1(guid);
        Assert.Equal(time, actualTime);
        Assert.Equal(clockSeq, actualClockSeq);
        Assert.Equal(node, actualNode);
    }

    [Fact]
    public void TestV3()
    {
        var expected = Guid.Parse("5df41881-3aed-3515-88a7-2f4a814cf09e");
        var actual = Uuid.CreateV3(Uuid.DNS, "www.example.com"u8);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TestV4()
    {
        var expected = Guid.Parse("919108f7-52d1-4320-9bac-f847db4148a8");
        var actual = Uuid.CreateV4([0x91, 0x91, 0x08, 0xF7, 0x52, 0xD1, 0x33, 0x20, 0x5B, 0xAC, 0xF8, 0x47, 0xDB, 0x41, 0x48, 0xA8]);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TestV4Generation()
    {
        var guids = Enumerable.Range(1, 1000).Select(i => Uuid.CreateV4()).ToArray();
        for (int i = 0; i < guids.Length; ++i)
        {
            Assert.Equal(4, guids[i].Version);
            Assert.Equal(0b10, guids[i].Variant >> 2);
            for (int j = i + 1; j < guids.Length; ++j)
                Assert.NotEqual(guids[i], guids[j]);
        }
    }

    [Fact]
    public void TestV5()
    {
        var expected = Guid.Parse("2ed6657d-e927-568b-95e1-2665a8aea6a2");
        var actual = Uuid.CreateV5(Uuid.DNS, "www.example.com"u8);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TestV6()
    {
        var time = new DateTimeOffset(2022, 2, 22, 14, 22, 22, TimeSpan.FromHours(-5)).ToUniversalTime().UtcDateTime;
        var expected = Guid.Parse("1EC9414C-232A-6B00-B3C8-9F6BDECED846");
        var actual = Uuid.CreateV6(time, 0x33C8, [0x9F, 0x6B, 0xDE, 0xCE, 0xD8, 0x46]);
        Assert.Equal(expected, actual);
    }


    [Fact]
    public void TestV6RejectsDatesBeforeGregorianCalendarStart()
    {
        Assert.Throws<ArgumentException>(() => Uuid.CreateV6(DateTimeOffset.MinValue, 0, []));
    }

    [Fact]
    public void TestV6Generation()
    {
        var guids = Enumerable.Range(1, 1000).Select(i => Uuid.CreateV6()).ToArray();
        for (int i = 0; i < guids.Length; ++i)
        {
            Assert.Equal(6, guids[i].Version);
            Assert.Equal(0b10, guids[i].Variant >> 2);
            for (int j = i + 1; j < guids.Length; ++j)
                Assert.NotEqual(guids[i], guids[j]);
        }
    }

    [Fact]
    public void TestV6NodeIs48BitsLong()
    {
        Assert.Throws<ArgumentException>(() => Uuid.CreateV6(DateTime.UtcNow, 0, []));
    }


    [Fact]
    public void TestV6Parse()
    {
        var time = new DateTimeOffset(2022, 2, 22, 14, 22, 22, TimeSpan.FromHours(-5)).ToUniversalTime().UtcDateTime;
        short clockSeq = 0x33C8;
        ReadOnlySpan<byte> node = [0x9F, 0x6B, 0xDE, 0xCE, 0xD8, 0x46];
        var guid = Uuid.CreateV6(time, clockSeq, node);
        var (actualTime, actualClockSeq, actualNode) = Uuid.ParseV6(guid);
        Assert.Equal(time, actualTime);
        Assert.Equal(clockSeq, actualClockSeq);
        Assert.Equal(node, actualNode);
    }

    [Fact]
    public void TestV7()
    {
        var time = new DateTimeOffset(2022, 2, 22, 14, 22, 22, TimeSpan.FromHours(-5)).ToUniversalTime().UtcDateTime;
        var expected = Guid.Parse("017F22E2-79B0-7CC3-98C4-DC0C0C07398F");
        var actual = Uuid.CreateV7(time, 0xCC3, 0x18C4DC0C0C07398F);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TestV7Generation()
    {
        var guids = Enumerable.Range(1, 1000).Select(i => Uuid.CreateV7()).ToArray();
        for (int i = 0; i < guids.Length; ++i)
            for (int j = i + 1; j < guids.Length; ++j)
                Assert.NotEqual(guids[i], guids[j]);
    }

    [Fact]
    public void TestV7Min()
    {
        var expected = Guid.Parse("00000000-0000-7000-8000-000000000000");
        var actual = Uuid.CreateV7(0, 0, 0);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TestV7Max()
    {
        var expected = Guid.Parse("FFFFFFFF-FFFF-7FFF-BFFF-FFFFFFFFFFFF");
        var actual = Uuid.CreateV7((1L << 48) - 1, 0x3FFF, 0xFFFFFFFFFFFFFFFF);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TestV8()
    {
        var expected = Guid.Parse("2489E9AD-2EE2-8E00-8EC9-32D5F69181C0");
        var actual = Uuid.CreateV8(0x2489E9AD2EE2, 0xE00, 0xEC932D5F69181C0);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TestV8Buffer()
    {
        var expected = Guid.Parse("919108f7-52d1-8320-9bac-f847db4148a8");
        var actual = Uuid.CreateV8([0x91, 0x91, 0x08, 0xF7, 0x52, 0xD1, 0x33, 0x20, 0x5B, 0xAC, 0xF8, 0x47, 0xDB, 0x41, 0x48, 0xA8]);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TestV8NameBased()
    {
        var expected = Guid.Parse("5c146b14-3c52-8afd-938a-375d0df1fbf6");
        var actual = Uuid.CreateV8(Uuid.DNS, "www.example.com"u8);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TestMissingMacAddress()
    {
        var generator = new Uuid.Generator(() => null);
        var guid = generator.NextV1();
        Assert.Equal(1, guid.Version);
    }


}
