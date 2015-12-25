using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Picrosser;
namespace PicrosserConsole {
    class PicrosserConsole {
        static void Main(string[] args) {
            string text = "", line;
            int sec = 0;
            if(args.Length != 0) {
                try {
                    StreamReader file = new StreamReader(args[0]);
                    while(sec < 2) {
                        line = file.ReadLine();
                        if(line == null) line = "";
                        text += line + "\n";
                        if(line.Equals("")) ++sec;
                    }
                    file.Close();
                } catch(Exception) {
                    Console.WriteLine("Failed to open the file.");
                    return;
                }

            } else {
                Console.WriteLine("Please input the picross puzzle:");
                while(sec < 2) {
                    line = Console.ReadLine();
                    if(line == null) line = "";
                    text += line + "\n";
                    if(line.Equals("")) ++sec;
                }
            }

            Question question;
            try {
                question = new Question(text);
            } catch(ArgumentException) {
                Console.WriteLine("Invalid Picross!");
                return;
            }

            bool found = false;
            foreach(var ps in Solver.SolveBySearching(question)) {
                if(!question.VerifySolution(ps)) {
                    Console.WriteLine("Sorry I made a mistake.");
                    break;
                }
                found = true;
                Console.WriteLine("=======================");
                for(int y = 0; y < question.Height; ++y) {
                    for(int x = 0; x < question.Width; ++x) {
                        Console.Write(ps[x, y] ? " *" : "  ");
                    }
                    Console.WriteLine("");
                }
            }
            if(!found) {
                Console.WriteLine("No solution found");
            } else {
                Console.WriteLine("Finished");
            }
        }
    }
}
