// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DnsClient.Tests
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class ThreadSafeBitArrayTest
    {
        [Fact]
        public void QueryId_NewId()
        {
            using var id = QueryId.NewId();
            Assert.InRange(id.Id, 1, ushort.MaxValue);
        }

        [Fact]
        public void QueryId_NewId_Many()
        {
            for (var i = 0; i < ushort.MaxValue; i++)
            {
                using var id = QueryId.NewId();
                Assert.InRange(id.Id, 1, ushort.MaxValue);
            }
        }

        [Fact]
        public void ThreadSafeBitArrayTest_TrySet_IndexOutOfRange()
        {
            var arr = new ThreadSafeFixedBitmap();
            Assert.Throws<ArgumentOutOfRangeException>(() => arr.TrySet(ushort.MaxValue + 1, true));
        }

        [Fact]
        public void ThreadSafeBitArrayTest_Get_IndexOutOfRange()
        {
            var arr = new ThreadSafeFixedBitmap();
            Assert.Throws<ArgumentOutOfRangeException>(() => arr.Get(ushort.MaxValue + 1));
        }

        [Fact]
        public void ThreadSafeBitArrayTest_Get()
        {
            var arr = new ThreadSafeFixedBitmap();

            for (var i = 0; i < arr.Length; i++)
            {
                Assert.False(arr.Get(i));
            }
        }

        [Fact]
        public void ThreadSafeBitArrayTest_TrySet_A()
        {
            var arr = new ThreadSafeFixedBitmap();

            for (var i = 0; i < arr.Length; i++)
            {
                var v = arr.TrySet(i, true);

                Assert.True(v);
            }

            for (var i = 0; i < arr.Length; i++)
            {
                Assert.True(arr.Get(i));
            }
        }

        [Fact]
        public void ThreadSafeBitArrayTest_FindFirstFalse()
        {
            var arr = new ThreadSafeFixedBitmap();

            Assert.Equal(0, arr.FindFirstFalse());
            arr.TrySet(0, true);
            arr.TrySet(1, true);
            Assert.Equal(2, arr.FindFirstFalse());
        }

        [Fact]
        public void ThreadSafeBitArrayTest_FindFirstFalseFail()
        {
            var arr = new ThreadSafeFixedBitmap();

            for (var i = 0; i < arr.Length; i++)
            {
                var v = arr.TrySet(i, true);

                Assert.True(v);
            }

            Assert.Equal(-1, arr.FindFirstFalse());
        }

        [Fact]
        public void ThreadSafeBitArrayTest_TrySet_ParallelSet()
        {
            var arr = new ThreadSafeFixedBitmap();

            var changes = 0;

            Parallel.For(0, 256, i =>
            {
                if (arr.TrySet(512, true))
                {
                    Interlocked.Increment(ref changes);
                }
            });

            Assert.Equal(1, changes);
        }

#if NET8_0_OR_GREATER
        [Fact]
        public async Task ThreadSafeBitArrayTest_TrySet_ParallelSet_B()
        {
            var arr = new ThreadSafeFixedBitmap();

            var changes = 0;

            await Parallel.ForAsync(0, 50, async (i, c) =>
            {
                await Task.Delay(20, c);

                if (arr.TrySet(512, true))
                {
                    Interlocked.Increment(ref changes);
                }
            });

            Assert.Equal(1, changes);
        }
#endif

        [Fact]
        public void QueryId_ImplicitConversion_ToInt()
        {
            using var id = QueryId.NewId();
            int intId = id;

            Assert.Equal(id.Id, intId);
            Assert.InRange(intId, 1, ushort.MaxValue);
        }

        [Fact]
        public void QueryId_ImplicitConversion_Null_Throws()
        {
            QueryId id = null;

            Assert.Throws<ArgumentNullException>(() => { int _ = id; });
        }
    }
}
