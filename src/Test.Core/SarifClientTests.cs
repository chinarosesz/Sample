using Microsoft.CodeAnalysis.Sarif;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sarif.Util.Console;
using System;
using System.Collections.Generic;
using System.Text;

namespace Test.Core
{
    [TestClass]
    public class SarifClientTests
    {
        [TestMethod]
        public void GoldenPath_ReadVersion_From_Sarif()
        {
            SarifClient client = new SarifClient();
            SarifLog sarifLog = client.InsertSnippets(@"testdata/raw.sarif", @"https://litra.visualstudio.com/Playground/_git/Handyman/commit/a0bc23210e0c1d7a521e95b5a2e82c1e35418670/");
            client.WriteSarifLogToFile(sarifLog, @"d:/temp/ready.sarif");
            Console.WriteLine();
        }
    }
}
