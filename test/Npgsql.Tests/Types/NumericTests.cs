﻿using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using NpgsqlTypes;
using NUnit.Framework;

namespace Npgsql.Tests.Types;

public class NumericTests : MultiplexingTestBase
{
    static readonly object[] ReadWriteCases = new[]
    {
        new object[] { "0.0000000000000000000000000001::numeric", 0.0000000000000000000000000001M },
        new object[] { "0.000000000000000000000001::numeric", 0.000000000000000000000001M },
        new object[] { "0.00000000000000000001::numeric", 0.00000000000000000001M },
        new object[] { "0.0000000000000001::numeric", 0.0000000000000001M },
        new object[] { "0.000000000001::numeric", 0.000000000001M },
        new object[] { "0.00000001::numeric", 0.00000001M },
        new object[] { "0.0001::numeric", 0.0001M },
        new object[] { "0.123456000000000100000000::numeric", 0.123456000000000100000000M },
        new object[] { "1::numeric", 1M },
        new object[] { "10000::numeric", 10000M },
        new object[] { "100000000::numeric", 100000000M },
        new object[] { "1000000000000::numeric", 1000000000000M },
        new object[] { "10000000000000000::numeric", 10000000000000000M },
        new object[] { "100000000000000000000::numeric", 100000000000000000000M },
        new object[] { "1000000000000000000000000::numeric", 1000000000000000000000000M },
        new object[] { "10000000000000000000000000000::numeric", 10000000000000000000000000000M },

        new object[] { "1E-28::numeric", 0.0000000000000000000000000001M },
        new object[] { "1E-24::numeric", 0.000000000000000000000001M },
        new object[] { "1E-20::numeric", 0.00000000000000000001M },
        new object[] { "1E-16::numeric", 0.0000000000000001M },
        new object[] { "1E-12::numeric", 0.000000000001M },
        new object[] { "1E-8::numeric", 0.00000001M },
        new object[] { "1E-4::numeric", 0.0001M },
        new object[] { "1E+0::numeric", 1M },
        new object[] { "1E+4::numeric", 10000M },
        new object[] { "1E+8::numeric", 100000000M },
        new object[] { "1E+12::numeric", 1000000000000M },
        new object[] { "1E+16::numeric", 10000000000000000M },
        new object[] { "1E+20::numeric", 100000000000000000000M },
        new object[] { "1E+24::numeric", 1000000000000000000000000M },
        new object[] { "1E+28::numeric", 10000000000000000000000000000M },

        new object[] { "1.2222333344445555666677778888::numeric", 1.2222333344445555666677778888M },
        new object[] { "11.222233334444555566667777888::numeric", 11.222233334444555566667777888M },
        new object[] { "111.22223333444455556666777788::numeric", 111.22223333444455556666777788M },
        new object[] { "1111.2222333344445555666677778::numeric", 1111.2222333344445555666677778M },

        new object[] { "+79228162514264337593543950335::numeric", +79228162514264337593543950335M },
        new object[] { "-79228162514264337593543950335::numeric", -79228162514264337593543950335M },

        // It is important to test rounding on both even and odd
        // numbers to make sure midpoint rounding is away from zero.
        new object[] { "1::numeric(10,2)", 1.00M },
        new object[] { "2::numeric(10,2)", 2.00M },

        new object[] { "1.2::numeric(10,1)", 1.2M },
        new object[] { "1.2::numeric(10,2)", 1.20M },
        new object[] { "1.2::numeric(10,3)", 1.200M },
        new object[] { "1.2::numeric(10,4)", 1.2000M },
        new object[] { "1.2::numeric(10,5)", 1.20000M },

        new object[] { "1.4::numeric(10,0)", 1M },
        new object[] { "1.5::numeric(10,0)", 2M },
        new object[] { "2.4::numeric(10,0)", 2M },
        new object[] { "2.5::numeric(10,0)", 3M },

        new object[] { "-1.4::numeric(10,0)", -1M },
        new object[] { "-1.5::numeric(10,0)", -2M },
        new object[] { "-2.4::numeric(10,0)", -2M },
        new object[] { "-2.5::numeric(10,0)", -3M },

        // Bug 2033
        new object[] { "0.0036882500000000000000000000", 0.0036882500000000000000000000M },
        // Bug 5848
        new object[] { "10836968.715000000000000000000000", 10836968.715000000000000000000000M },

        new object[] { "936490726837837729197", 936490726837837729197M },
        new object[] { "9364907268378377291970000", 9364907268378377291970000M },
        new object[] { "3649072683783772919700000000", 3649072683783772919700000000M },
        new object[] { "1234567844445555.000000000", 1234567844445555.000000000M },
        new object[] { "11112222000000000000", 11112222000000000000M },
        new object[] { "0::numeric", 0M },
    };

    static readonly string[] numericTests = new string[]
    {
        "264383.511600000000000000000000",
        "980568.13428000000338620221111111111111678123678129",
        "8980568.13428000000338620221111111111111678123678129",
        "58980568.13428000000338620200011111111111678123678129",
        "458980568.134280000003386202002222222222222678123678129",
        "1458980568.13428000000338620201222222222222678123678129",
        "1458980568.13428000000338620200000000000000000000000000",
        "911112998.25401999999668454040000000000000000000000000",
        "-911112998.25401999999668454040000000000000000000000000",
        "9739.695000007986111111111111060000000000000000000000",
        "966.691375001041666666666666600000000000000000000000",
        "76.2600000000000000000000000000",
        "279.0000000000000000000000000000",
        "380000.0000000000000000000000000000",
        "38123000000",
        "38100000000",
    };

    [Test]
    [TestCaseSource(nameof(ReadWriteCases))]
    public async Task Read(string query, decimal expected)
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = new NpgsqlCommand("SELECT " + query, conn);
        var value = (decimal)(await cmd.ExecuteScalarAsync())!;
        Assert.That(value, Is.EqualTo(expected));
    }

    [Test]
    [TestCaseSource(nameof(ReadWriteCases))]
    public async Task Write(string query, decimal expected)
    {
        using var conn = await OpenConnectionAsync();
        using var cmd = new NpgsqlCommand("SELECT @p, @p = " + query, conn);
        cmd.Parameters.AddWithValue("p", expected);
        using var rdr = await cmd.ExecuteReaderAsync();
        rdr.Read();
        Assert.That(rdr.GetFieldValue<decimal>(0), Is.EqualTo(expected));
        Assert.That(rdr.GetFieldValue<bool>(1));
    }

    [Test]
    public async Task Numeric()
    {
        await AssertType(5.5m, "5.5", "numeric", NpgsqlDbType.Numeric, DbType.Decimal);
        await AssertTypeWrite(5.5m, "5.5", "numeric", NpgsqlDbType.Numeric, DbType.VarNumeric, inferredDbType: DbType.Decimal);

        await AssertType((short)8, "8", "numeric", NpgsqlDbType.Numeric, DbType.Decimal, isDefault: false);
        await AssertType(8,        "8", "numeric", NpgsqlDbType.Numeric, DbType.Decimal, isDefault: false);
        await AssertType((byte)8,  "8", "numeric", NpgsqlDbType.Numeric, DbType.Decimal, isDefault: false);
        await AssertType(8F,       "8", "numeric", NpgsqlDbType.Numeric, DbType.Decimal, isDefault: false);
        await AssertType(8D,       "8", "numeric", NpgsqlDbType.Numeric, DbType.Decimal, isDefault: false);
        await AssertType(8M,       "8", "numeric", NpgsqlDbType.Numeric, DbType.Decimal, isDefault: false);
    }

    [Test, Description("Tests that when Numeric value does not fit in a System.Decimal and reader is in ReaderState.InResult, the value was read wholly and it is safe to continue reading")]
    public async Task Read_overflow_is_safe()
    {
        using var conn = await OpenConnectionAsync();
        //This 29-digit number causes OverflowException. Here it is important to have unread column after failing one to leave it ReaderState.InResult
        using var cmd = new NpgsqlCommand(@"SELECT (0.20285714285714285714285714285)::numeric, generate_series FROM generate_series(1, 2)", conn);
        using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
        var i = 1;

        var expected = decimal.Parse("0.20285714285714285714285714285", CultureInfo.InvariantCulture);

        while (reader.Read())
        {
            Assert.That(reader.GetDecimal(0), Is.EqualTo(expected));

            var intValue = reader.GetInt32(1);

            Assert.That(intValue, Is.EqualTo(i++));
            Assert.That(conn.FullState, Is.EqualTo(ConnectionState.Open | ConnectionState.Fetching));
            Assert.That(conn.State, Is.EqualTo(ConnectionState.Open));
            Assert.That(reader.State, Is.EqualTo(ReaderState.InResult));
        }
    }

    [Test]
    [TestCaseSource(nameof(numericTests))]
    public async Task Scale_overflow_is_safe(string number)
    {
        using var conn = await OpenConnectionAsync();
        //This 29-digit number causes OverflowException. Here it is important to have unread column after failing one to leave it ReaderState.InResult
        using var cmd = new NpgsqlCommand($@"SELECT ({number})::numeric, generate_series FROM generate_series(1, 2)", conn);
        using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
        var i = 1;

        var expected = decimal.Parse(number, CultureInfo.InvariantCulture);

        while (reader.Read())
        {
            //_ = reader.GetDecimal(0);
            Assert.That(reader.GetDecimal(0), Is.EqualTo(expected));

            var intValue = reader.GetInt32(1);

            Assert.That(intValue, Is.EqualTo(i++));
            Assert.That(conn.FullState, Is.EqualTo(ConnectionState.Open | ConnectionState.Fetching));
            Assert.That(conn.State, Is.EqualTo(ConnectionState.Open));
            Assert.That(reader.State, Is.EqualTo(ReaderState.InResult));
        }
    }

    [Test]
    [TestCaseSource(nameof(ReadWriteCases))]
    public async Task Read_BigInteger(string query, decimal expected)
    {
        var bigInt = new BigInteger(expected);
        using var conn = await OpenConnectionAsync();
        using var cmd = new NpgsqlCommand("SELECT " + query, conn);
        using var rdr = await cmd.ExecuteReaderAsync();
        await rdr.ReadAsync();

        if (decimal.Floor(expected) == expected)
            Assert.That(rdr.GetFieldValue<BigInteger>(0), Is.EqualTo(bigInt));
        else
            Assert.That(() => rdr.GetFieldValue<BigInteger>(0),
                Throws.Exception
                    .With.TypeOf<InvalidCastException>()
                    .With.Message.EqualTo("Numeric value with non-zero fractional digits not supported by BigInteger"));
    }

    [Test]
    [TestCaseSource(nameof(ReadWriteCases))]
    public async Task Write_BigInteger(string query, decimal expected)
    {
        if (decimal.Floor(expected) == expected)
        {
            var bigInt = new BigInteger(expected);
            using var conn = await OpenConnectionAsync();
            using var cmd = new NpgsqlCommand("SELECT @p, @p = " + query, conn);
            cmd.Parameters.AddWithValue("p", bigInt);
            using var rdr = await cmd.ExecuteReaderAsync();
            await rdr.ReadAsync();
            Assert.That(rdr.GetFieldValue<BigInteger>(0), Is.EqualTo(bigInt));
            Assert.That(rdr.GetFieldValue<bool>(1));
        }
    }

    [Test]
    public async Task BigInteger_large()
    {
        var num = BigInteger.Parse(string.Join("", Enumerable.Range(0, 17000).Select(i => ((i + 1) % 10).ToString())));
        using var conn = await OpenConnectionAsync();
        using var cmd = new NpgsqlCommand("SELECT '0.1'::numeric, @p", conn);
        cmd.Parameters.AddWithValue("p", num);
        using var rdr = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
        await rdr.ReadAsync();
        Assert.Throws<InvalidCastException>(() => rdr.GetFieldValue<BigInteger>(0));
        Assert.That(rdr.GetFieldValue<BigInteger>(1), Is.EqualTo(num));
    }

    [Test]
    public async Task NumericZero_WithScale()
    {
        // Scale should not be lost when dealing with 0
        using var conn = await OpenConnectionAsync();
        using var cmd = new NpgsqlCommand("SELECT @p", conn);
        var param = new NpgsqlParameter("p", DbType.Decimal, 10, null, ParameterDirection.Input, false, 10, 2, DataRowVersion.Default, 0.00M);
        cmd.Parameters.Add(param);
        using var rdr = await cmd.ExecuteReaderAsync();
        await rdr.ReadAsync();
        var value = rdr.GetFieldValue<decimal>(0);

#if NET7_0_OR_GREATER
        Assert.That(value.Scale, Is.EqualTo(2));
#else
        Assert.That(value.ToString(CultureInfo.InvariantCulture), Is.EqualTo(0.00M.ToString(CultureInfo.InvariantCulture)));
#endif
    }

    public NumericTests(MultiplexingMode multiplexingMode) : base(multiplexingMode) {}
}
