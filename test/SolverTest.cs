using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Picrosser;
using System.Collections.Generic;
using System.Linq;
using System.IO;
namespace PicrosserTest {
    [TestClass]
    public class SolverTest {
        [TestMethod]
        public void TestSolveBySearching() {
            Question question = new Question("1 1\n1 1\n1 1\n1 1\n\n1 1\n1 1\n1 1\n1 1");
            int count = 0;
            foreach(var s in Solver.SolveBySearching(question)) {
                ++count;
                Assert.IsTrue(question.VerifySolution(s));
            }
            Assert.AreEqual(2, count);

        }

        [TestMethod]
        public void TestSolveSimple() {
            string text = "", line;
            int sec = 0;
            StreamReader file = new StreamReader("..\\..\\..\\Examples\\example1.txt");
            while(sec < 2) {
                line = file.ReadLine();
                if(line == null) line = "";
                text += line + "\n";
                if(line.Equals("")) ++sec;
            }
            file.Close();

            Question question = new Question(text);
            Solver solver = new Solver();
            Assert.AreEqual(Solver.ResultEnum.FINISHED, solver.Solve(question));
            Assert.IsTrue(question.VerifySolution(Solver.ConvertToPureSolution(solver.pixelStates)));

        }
    }
}
