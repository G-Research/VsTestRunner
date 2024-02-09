using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using FluentAssertions;
using System.IO;
using System.Linq;

namespace VsTestRunner.Tests
{
    [TestFixture]
    internal class OptionsTests
    {
        [Test]
        public void TestCanLoadOptionsFromFile()
        {
            Options options = new Options();

            options.DockerImage.Should().BeNullOrEmpty();
            options.DockerImage = new string[] { "my-overriden-image" };
            options.DockerImage.Should().Contain("my-overriden-image");

            options.SettingsFile = Path.Join(TestContext.CurrentContext.TestDirectory, "test-options.json");

            options.DockerImage.Should().Contain("my-working-image");
            options.TestAssemblyFile.Should().Be("test-list.json");
            options.TestAssemblies.Count().Should().Be(2);
            options.TestAssemblies.Contains("assembly1").Should().BeTrue();
            options.TestAssemblies.Contains("assembly2").Should().BeTrue();
        }
    }
}
