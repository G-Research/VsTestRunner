using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsTestRunner.Tests
{
    [TestFixture]
    public class DummyTest
    {
        [Test]
        public void PassingTest()
        {
            Assert.Pass();
        }
    }
}
