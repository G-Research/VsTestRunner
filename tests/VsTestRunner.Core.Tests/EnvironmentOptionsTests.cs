using FluentAssertions;
using NUnit.Framework;
using System.Collections.Generic;
using VsTestRunner.Core.Interfaces;

namespace VsTestRunner.Core.Tests
{
    public class EnvironmentOptionsTests
    {
        [Test]
        public void TestParsingEnvironmentVariablesAndAdditionalMounts()
        {
            var environmentOptions = new EnvironmentOptions(new TestOptions { DockerImage = new string[] { "my.docker.image" }, AdditionalMounts = new string[] { "E:\\myCode\\|/code", "E:\\another-mount|/foo" }, EnvironmentVariables = new string[] { "bob=foo" } });
            environmentOptions.TryGetDockerImage("net5.0", out string dockerImage).Should().BeTrue();
            dockerImage.Should().Be("my.docker.image");
            environmentOptions.AdditionalMounts.Count.Should().Be(2);
            environmentOptions.AdditionalMounts[0].LocalDirectory.Should().Be("E:\\myCode\\");
            environmentOptions.AdditionalMounts[0].MountDirectory.Should().Be("/code");
            environmentOptions.AdditionalMounts[1].LocalDirectory.Should().Be("E:\\another-mount");
            environmentOptions.AdditionalMounts[1].MountDirectory.Should().Be("/foo");

            environmentOptions.EnvironmentVariables.Count.Should().Be(1);
            environmentOptions.EnvironmentVariables[0].Key.Should().Be("bob");
            environmentOptions.EnvironmentVariables[0].Value.Should().Be("foo");
        }

        [Test]
        public void TestParsingDockerImageConfiguration()
        {
            var environmentOptions = new EnvironmentOptions(new TestOptions { DockerImage = new string[] { "netcoreapp3.1=my.docker.image:3.1", "net5.0=my.docker.image:5.0", "my.docker.image:6.0" } });
            environmentOptions.TryGetDockerImage("netcoreapp3.1", out string dockerImage).Should().BeTrue();
            dockerImage.Should().Be("my.docker.image:3.1");
            environmentOptions.TryGetDockerImage("net5.0", out dockerImage).Should().BeTrue();
            dockerImage.Should().Be("my.docker.image:5.0");
            environmentOptions.TryGetDockerImage("net6.0", out dockerImage).Should().BeTrue();
            dockerImage.Should().Be("my.docker.image:6.0");
        }

        class TestOptions : IEnvironmentOptions
        {
            public uint MaxDegreeOfParallelism { get; set; }

            public bool RemoveAllEnvironmentVariables { get; set; }

            public IEnumerable<string> EnvironmentVariables { get; set; } = new List<string>();

            public IEnumerable<string> AdditionalMounts { get; set; } = new List<string>();

            public IEnumerable<string> DockerImage { get; set; } = new List<string>();

            public string ResultsDirectory { get; set; }

            public bool WriteToBaseResultsDirectory { get; set; }
        }
    }
}
