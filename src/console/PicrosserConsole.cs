using System;
using System.Collections.Generic;
using System.Linq;
using Picrosser;
namespace PicrosserConsole {
    class PicrosserConsole {
        static void Main(string[] args) {
            string text = "", line;
            int sec = 0;
            while(sec < 2) {
                line = Console.ReadLine();
                text += line + "\n";
                if(line.Equals("")) ++sec;
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
                found = true;
                Console.WriteLine("=======================");
                for(int y = 0; y < question.Height; ++y) {
                    for(int x = 0; x < question.Width; ++x) {
                        switch(ps[x, y]) {
                        case PixelStateEnum.UNKNOWN:
                            Console.Write(" ?");
                            break;
                        case PixelStateEnum.ON:
                            Console.Write(" *");
                            break;
                        case PixelStateEnum.OFF:
                            Console.Write("  ");
                            break;
                        }
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
