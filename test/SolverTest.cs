using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Picrosser;
using System.Collections.Generic;
using System.Linq;
namespace PicrosserTest {
    [TestClass]
    public class SolverTest {
        [TestMethod]
        public void TestSolveBySearching() {
            Question question = new Question("1 1\n1 1\n1 1\n1 1\n\n1 1\n1 1\n1 1\n1 1");
            int count = 0;
            foreach(var s in Solver.SolveBySearching(question)) {
                ++count;
                Assert.IsTrue(question.VerifySolution(Solver.ConverToPureSolution(s)));
            }
            Assert.AreEqual(count, 2);

        }
    }
}
