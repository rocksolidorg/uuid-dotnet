# RockSolid.Uuid

A high-quality UUID/GUID generation library for .NET 8+, implementing **RFC 9562**.

## Description

This library provides a high-quality implementation of GUID/UUID generation for .NET applications based on
[RFC 9562](https://www.rfc-editor.org/rfc/rfc9562.html). It is designed for developers who require deterministic
behavior, predictable sorting characteristics, and platform-neutral identifiers across distributed systems, storage
engines, and event-driven architectures.

## Supported UUID Versions

- **v1** (time-based)
- **v3** (name-based, MD5)
- **v4** (random)
- **v5** (name-based, SHA-1)
- **v6** (time-based)
- **v7** (time-based)
- **v8** (custom)

## Quick start

```csharp
using System.Text;
using RockSolid.Uuid;

// Time-ordered UUIDs (useful for DB indexes / event streams)
Guid idv1 = Uuid.CreateV1(); // Legacy 
Guid idV6 = Uuid.CreateV6(); // Reordered time-based (Gregorian epoch ticks)
Guid idV7 = Uuid.CreateV7(); // Unix time (ms) + randomness

// Random UUID (v4)
Guid idV4 = Uuid.CreateV4();

// Name-based, deterministic UUIDs (same inputs => same UUID)
Guid idv3 = Uuid.CreateV3(Uuid.DNS, Encoding.UTF8.GetBytes("example.com"));
Guid idv5 = Uuid.CreateV5(Uuid.URL, Encoding.UTF8.GetBytes("https://example.com/resource"));

// v1/v6 with explicit inputs (reproducible / testable)
var time = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
short clockSeq = 42;
byte[] node = { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55 };

Guid v1 = Uuid.CreateV1(time, clockSeq, node);
Guid v6 = Uuid.CreateV6(time, clockSeq, node);

// Parse time-based UUIDs back into components
(DateTimeOffset t1, short seq1, byte[] node1) = Uuid.ParseV1(v1);
(DateTimeOffset t6, short seq6, byte[] node6) = Uuid.ParseV6(v6);

// Nil / Max constants (RFC-defined sentinels)
Guid nil = Uuid.Nil;
Guid max = Uuid.Max;
```