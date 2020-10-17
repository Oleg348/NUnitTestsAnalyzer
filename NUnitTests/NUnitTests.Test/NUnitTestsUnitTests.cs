
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using VerifyCS = NUnitTests.Test.CSharpCodeFixVerifier<
    NUnitTests.NUnitTestsAnalyzer,
    NUnitTests.NUnitTestsCodeFixProvider>;

namespace NUnitTests.Test
{
    [TestClass]
    public class NUnitTestsUnitTest
    {
        //No diagnostics expected to show up
        [TestMethod]
        public async Task TestMethod1()
        {
            var test = @"";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public async Task TestMethod2()
        {
            var test = @"
using NUnit.Framework;

namespace NUnitTests.Test
{
    [TestFixture]
    internal sealed class Test
    {
        [Test]
        public void Test_test()
        {

        }
    }
}";

            var fixtest = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class TYPENAME
        {   
        }
    }";

            var expected = VerifyCS.Diagnostic("NUnitTests").WithLocation(0).WithArguments("TypeName");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }
    }
}
