﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Api.DiskSpace;
using NzbDrone.Integration.Test.Client;

namespace NzbDrone.Integration.Test.ApiTests
{
    [TestFixture]
    public class DiskSpaceFixture : IntegrationTest
    {
        public ClientBase<DiskSpaceResource> DiskSpace;

        protected override void InitRestClients()
        {
            base.InitRestClients();

            DiskSpace = new ClientBase<DiskSpaceResource>(RestClient, ApiKey, "diskSpace");
        }

        [Test]
        public void get_all_diskspace()
        {
            var items = DiskSpace.All();

            items.Should().NotBeEmpty();
            items.First().FreeSpace.Should().NotBe(0);
            items.First().TotalSpace.Should().NotBe(0);
            items.First().Path.Should().NotBeNullOrEmpty();
        }
    }
}
