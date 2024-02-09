using FakeItEasy;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VsTestRunner.Core.Interfaces;

namespace VsTestRunner.Core.Tests
{
    [TestFixture]
    public class VsTestCommandLineHelperTests
    {
        [Test]
        public void EmptyFilterOptionsShouldResultInEmptyFilterString()
        {
            var commandLineOptions = A.Dummy<ICommandLineTestOptions>();
            A.CallTo(() => commandLineOptions.Filter).Returns(null);
            A.CallTo(() => commandLineOptions.IncludeCategories).Returns(new string[] { });
            A.CallTo(() => commandLineOptions.ExcludeCategories).Returns(new string[] { });

            TestOptions testOptions = new TestOptions(commandLineOptions);

            var filterString = VsTestCommandLineHelper.GetFilterClause(testOptions);
            filterString.Should().BeNull();
        }

        [Test]
        public void JustFilterStingShouldResultInFilterString()
        {
            var commandLineOptions = A.Dummy<ICommandLineTestOptions>();
            A.CallTo(() => commandLineOptions.Filter).Returns("(TestCategory=foo)");
            A.CallTo(() => commandLineOptions.IncludeCategories).Returns(new string[] { });
            A.CallTo(() => commandLineOptions.ExcludeCategories).Returns(new string[] { });

            TestOptions testOptions = new TestOptions(commandLineOptions);

            var filterString = VsTestCommandLineHelper.GetFilterClause(testOptions);
            filterString.Should().Be("(TestCategory=foo)");
        }

        [Test]
        public void HelperShouldCorrectlyBuildFilterStringFromTestOptions()
        {
            var commandLineOptions = A.Dummy<ICommandLineTestOptions>();
            A.CallTo(() => commandLineOptions.Filter).Returns("(TestCategory=foo)");
            A.CallTo(() => commandLineOptions.IncludeCategories).Returns(new string[] { "alice" });
            A.CallTo(() => commandLineOptions.ExcludeCategories).Returns(new string[] { "bob", "bill" });

            TestOptions testOptions = new TestOptions(commandLineOptions);

            var filterString = VsTestCommandLineHelper.GetFilterClause(testOptions);
            filterString.Should().Be("((TestCategory!=bob&TestCategory!=bill)&(TestCategory=alice))&(TestCategory=foo)");
        }

        [Test]
        public void HelperShouldCorrectlyBuildTestNameFilterOptions()
        {
            var commandLineOptions = A.Dummy<ICommandLineTestOptions>();
            A.CallTo(() => commandLineOptions.Tests).Returns(new string[] { "MyExplicitTest1", "MyExplicitTest2" });

            TestOptions testOptions = new TestOptions(commandLineOptions);
            var commandline = VsTestCommandLineHelper.GetVsTestCommandLineArguments("MyTest", testOptions, "E:\\code", new System.IO.FileInfo("E:\\code\\MyTest.dll"));
            commandline.Should().Contain("--Tests:MyExplicitTest1,MyExplicitTest2");
        }


    }
}