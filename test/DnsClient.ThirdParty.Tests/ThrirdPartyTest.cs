// Copyright 2024 Michael Conrad.
// Licensed under the Apache License, Version 2.0.
// See LICENSE file for details.

using System;
using MongoDB.Driver;
using Xunit;

namespace DnsClient.ThirdParty.Tests
{
    public class ThrirdPartyTest
    {
        private const string TestConnection = "mongodb+srv://doesnotexist.internal.example/?serverSelectionTimeout=2&connectTimeoutMS=2000";

#if NET472

        [Fact]
        public void MongoDriver_Compat_2_8()
        {
            var ex = Record.Exception(() => new MongoClient(TestConnection));
            Assert.IsType<MongoConfigurationException>(ex);
            Assert.Contains("SRV record for doesnotexist.internal.example", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

#endif

#if NET6_0_OR_GREATER
        [Fact]
        public void MongoDriver_Compat_210()
        {
            // This will invoke the lookup once, and errors out but gets ignored for some reason now.
            var client = new MongoClient(TestConnection);

            // This should initialize the cluster and also may invoke the DnsMonitor stuff?!
            // Connection should then just timeout though
            var ex = Record.Exception(() => client.StartSession());
            Assert.IsType<TimeoutException>(ex);
        }
#endif
    }
}
