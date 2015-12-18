using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace Picrosser {

    /// <summary>
    /// An enum type describing the state of a pixel.
    /// </summary>
    public enum PixelStateEnum {

        /// <summary>The pixel has not been operated.</summary>
        UNKNOWN,

        /// <summary>The pixel has been turned on. </summary>
        ON,

        /// <summary>The pixel has been turned off. </summary>
        OFF
    }

    /// <summary>
    /// Picross puzzle solver class. Can only solve puzzles without assuming
    /// </summary>
    public class Solver {

        /// <summary>
        /// A class that decribes one touching step in a solution.
        /// </summary>
        public class StepTouch {

            /// <summary>
            /// Specifing the pixel position that is being operated.
            /// </summary>
            public int colIndex, rowIndex;

            /// <summary>
            /// <c>true</c> means to turn on the pixel. 
            /// <c>false</c> means to turn off the pixel.
            /// </summary>
            public bool on;
        }

        /// <summary>
        /// A class that decribes one moving step in a solution.
        /// </summary>
        public class StepMove {

            /// <summary>
            /// Indicating whether move to a column or a row.
            /// </summary>
            public bool moveToCol;

            /// <summary>
            /// The index of the column or the row.
            /// </summary>
            public int index;
        }

        /// <summary>
        /// <para>
        /// This field should be set to an initial state of pixels, 
        /// or <c>null</c> (indicating all pixels are unknown), 
        /// before calling <c>Solve</c> or <c>SolveByStep</c>.
        /// The dimension of this array should match the size of quesion 
        /// that passed to <c>Solve</c> or <c>SolveByStep</c>.
        /// </para>
        /// <para>
        /// After calling <c>Solve</c> or <c>SolveByStep</c>, 
        /// all pixel states are set to be the result.
        /// </para>
        /// </summary>
        public PixelStateEnum[,] pixelStates;


        BitArray GetColSlice(int colIndex, bool on) {
            BitArray result = new BitArray(pixelStates.GetLength(1));
            for(int i = 0; i < pixelStates.GetLength(1); ++i) {
                result.Set(i, pixelStates[colIndex, i] == (on ? PixelStateEnum.ON : PixelStateEnum.OFF));
            }
            return result;
        }
        BitArray GetRowSlice(int rowIndex, bool on) {
            BitArray result = new BitArray(pixelStates.GetLength(0));
            for(int i = 0; i < pixelStates.GetLength(0); ++i) {
                result.Set(i, pixelStates[i, rowIndex] == (on ? PixelStateEnum.ON : PixelStateEnum.OFF));
            }
            return result;
        }

        /// <summary>
        /// An enum that descibes the solving state of the <c>Solver</c>
        /// </summary>
        public enum ResultEnum {

            /// <summary>The method <c>Solve</c> or <c>SolveByStep</c> has not been called 
            /// or has not finished.</summary>
            SOLVING,

            /// <summary>Found a contradiction when solving.</summary>
            CONTRADICTORY,

            /// <summary>Cannot determinate some pixels.</summary>
            INDEFINITE,

            /// <summary>Found a unique solution for the puzzle.</summary>
            FINISHED
        }

        /// <summary>
        /// Set after calling <c>Solve</c> or <c>SolveByStep</c>.
        /// The solving state of the <c>Solver</c>.
        /// </summary>
        public ResultEnum Result {
            get; private set;
        }

        /// <summary>
        /// Set after calling <c>Solve</c> or <c>SolveByStep</c>.
        /// Valid only if <c>Result==ResultEnum.CONTRADICTORY</c>. 
        /// Indicating whether the contradiction was found in a column or not.
        /// </summary>
        public bool ContradictoryInCols {
            get; private set;
        }

        /// <summary>
        /// Set after calling <c>Solve</c> or <c>SolveByStep</c>.
        /// Valid only if <c>Result==ResultEnum.CONTRADICTORY</c>. 
        /// Indicating the index of the column/row that contains contradiction.
        /// </summary>
        public int ContradictoryIndex {
            get; private set;
        }

        /// <summary>
        /// Set after calling <c>Solve</c> or <c>SolveByStep</c>.
        /// Valid only if <c>Result==ResultEnum.INDEFINITE</c>. 
        /// The column index of the first unknown pixels after solving. 
        /// </summary>
        public int FirstUnknownColIndex {
            get; private set;
        }
        /// <summary>
        /// Set after calling <c>Solve</c> or <c>SolveByStep</c>.
        /// Valid only if <c>Result==ResultEnum.INDEFINITE</c>. 
        /// The row index of the first unknown pixels after solving. 
        /// </summary>
        public int FirstUnknownRowIndex {
            get; private set;
        }

        static IEnumerable<int[]/*Spaces before each number*/>
            EnumNumberSpaces(int[] numbers, int length) {
            int[] spaces = new int[numbers.Length];
            length -= numbers.Sum();
            if(length < numbers.Length - 1) yield break;
            for(int i = 1; i < numbers.Length; ++i) {
                spaces[i] = 1;
            }
            while(true) {
                yield return (int[])spaces.Clone();
                int nextAdd = numbers.Length - 1;
                while(true) {
                    ++spaces[nextAdd];
                    if(length >= spaces.Sum()) break;
                    spaces[nextAdd] = 1;
                    --nextAdd;
                    if(nextAdd == -1) yield break;
                }
            }
        }

        static LinkedList<BitArray> GetAllCandidates(int[] numbers, int length) {
            LinkedList<BitArray> result = new LinkedList<BitArray>();
            if(numbers.Length == 0) {
                result.AddLast(new BitArray(length));
            } else {

                foreach(int[] spaces in EnumNumberSpaces(numbers, length)) {
                    BitArray can = new BitArray(length);
                    int pos = 0;
                    for(int i = 0; i < spaces.Length; ++i) {
                        pos += spaces[i];
                        for(int j = 0; j < numbers[i]; ++j) {
                            can.Set(pos++, true);
                        }
                    }
                    result.AddLast(can);
                }
            }
            return result;
        }

        //Helper functions for BitArray
        static bool IsZero(BitArray b) {
            foreach(bool k in b) if(k) return false;
            return true;
        }
        static BitArray And(BitArray a, BitArray b) {
            return ((BitArray)a.Clone()).And(b);
        }
        static BitArray Not(BitArray a) {
            return ((BitArray)a.Clone()).Not();
        }

        /// <summary>
        /// Solve a picross puzzle, by enumerate each steps.
        /// If the field <c>pixelStates</c> is not null, this method 
        /// will use it as the initial state of the puzzle.
        /// </summary>
        /// <param name="question">The picross puzzle to be solved.</param>
        /// <returns>An IEnumerable of solution steps.</returns>
        /// <example>
        /// This example shows a common way to use <c>Solve</c>:
        /// <code>
        /// Question question=new Question();
        /// Solver solver=new Solver();
        /// foreach(var step in solver.SolveByStep(question)){
        ///     if(step is TouchStep)
        ///         if(((TouchStep)step).on)
        ///             //turn on the pixel at (step.colIndex,step.rowIndex)
        ///         else
        ///             //turn off the pixel at (step.colIndex,step.rowIndex)
        ///     else if(step is MoveStep)
        ///         //move the cursor according to (MoveStep)step
        /// }
        /// switch(solver.Result){
        /// case Solver.ResultEnum.CONTRADICTORY:
        ///     //Find a contradiction
        ///     break;
        /// case Solver.ResultEnum.INDEFINITE:
        ///     //Multiple solutions
        ///     break;
        /// case Solver.ResultEnum.FINISHED:
        ///     //Unique solution
        ///     break;
        /// }
        /// </code>
        /// </example>
        public IEnumerable<object> SolveByStep(Question question) {
            Result = ResultEnum.SOLVING;
            if(pixelStates == null)
                pixelStates = new PixelStateEnum[question.Width, question.Height];
            if(pixelStates.GetLength(0) != question.Width ||
                pixelStates.GetLength(1) != question.Height) {
                throw new InvalidOperationException("The initial state of pixelStates is invalid");
            }
            IEnumerable<BitArray>[][] candidates = new IEnumerable<BitArray>[2][]{
                new IEnumerable<BitArray>[question.Width],
                new IEnumerable<BitArray>[question.Height]
            };
            for(int coli = 0; coli < question.Width; ++coli) {
                candidates[0][coli] = GetAllCandidates(question.GetColNumbers(coli), question.Height);
            }
            for(int rowi = 0; rowi < question.Height; ++rowi) {
                candidates[1][rowi] = GetAllCandidates(question.GetRowNumbers(rowi), question.Width);
            }

            int width = question.Width;
            int height = question.Height;
            int hw;
            var GetSlice = new Func<int, bool, BitArray>[] {
                GetColSlice,GetRowSlice
            };
            while(true) {
                bool worked = false;

                for(int flip = 0; flip < 2; ++flip, hw = width, width = height, height = hw) {
                    for(int index = 0; index < width; ++index) {
                        yield return new StepMove {
                            moveToCol = flip == 0,
                            index = index
                        };
                        BitArray onSlice = GetSlice[flip](index, true);
                        BitArray offSlice = GetSlice[flip](index, false);

                        candidates[flip][index] = (from k in candidates[flip][index]
                                                   where IsZero(And(k, offSlice)) &&
                                                           IsZero(And(Not(k), onSlice))
                                                   select k).ToList();
                        if(!candidates[flip][index].Any()) {
                            Result = ResultEnum.CONTRADICTORY;
                            ContradictoryInCols = flip == 0;
                            ContradictoryIndex = index;
                            yield break;
                        }
                        onSlice.Not();
                        offSlice.Not();
                        foreach(BitArray bits in candidates[flip][index]) {
                            onSlice.And(bits);
                            offSlice.And(Not(bits));
                        }
                        for(int i = 0; i < height; ++i) {
                            int x, y;
                            if(flip == 0) {
                                x = index;
                                y = i;
                            } else {
                                x = i;
                                y = index;
                            }
                            if(onSlice.Get(i)) {
                                worked = true;
                                pixelStates[x, y] = PixelStateEnum.ON;
                                yield return new StepTouch {
                                    colIndex = x,
                                    rowIndex = y,
                                    on = true
                                };
                            } else if(offSlice.Get(i)) {
                                worked = true;
                                pixelStates[x, y] = PixelStateEnum.OFF;
                                yield return new StepTouch {
                                    colIndex = x,
                                    rowIndex = y,
                                    on = false
                                };
                            }
                        }

                    }
                }


                if(worked == false) break;

            }

            for(int ci = 0; ci < question.Width; ++ci)
                for(int ri = 0; ri < question.Height; ++ri) {
                    if(pixelStates[ci, ri] == PixelStateEnum.UNKNOWN) {
                        FirstUnknownColIndex = ci;
                        FirstUnknownRowIndex = ri;
                        Result = ResultEnum.INDEFINITE;
                        goto tag_indefinite;
                    }
                }
            Result = ResultEnum.FINISHED;
            tag_indefinite:


            yield break;
        }

        /// <summary>
        /// Solve a picross puzzle.
        /// If the field <c>pixelStates</c> is not null, this method 
        /// will use it as the initial state of the puzzle.
        /// </summary>
        /// <param name="question">The result from solving.</param>
        /// <returns></returns>
        public ResultEnum Solve(Question question) {
            foreach(var step in SolveByStep(question)) ;
            return Result;
        }

        /// <summary>
        /// Search all possible solutions for the given puzzle. 
        /// The function is implemented in DFS. 
        /// Some puzzle with unique awnser that cannot be solved 
        /// by <c>Solver.Solve</c> or <c>Solver.SolveByStep</c> can also 
        /// be solved by this function.
        /// </summary>
        /// <param name="question"></param>
        /// <returns></returns>
        public static IEnumerable<PixelStateEnum[,]> SolveBySearching(Question question) {
            var works = new LinkedList<PixelStateEnum[,]>();
            works.AddFirst(new PixelStateEnum[question.Width, question.Height]);
            Solver solver = new Solver();
            while(works.Any()) {
                solver.pixelStates = works.First();
                works.RemoveFirst();
                switch(solver.Solve(question)) {
                case Solver.ResultEnum.FINISHED:
                    yield return solver.pixelStates;
                    break;
                case Solver.ResultEnum.INDEFINITE:
                    PixelStateEnum[,] a, b;
                    a = solver.pixelStates;
                    b = (PixelStateEnum[,])a.Clone();
                    a[solver.FirstUnknownColIndex, solver.FirstUnknownRowIndex]
                        = PixelStateEnum.OFF;
                    b[solver.FirstUnknownColIndex, solver.FirstUnknownRowIndex]
                        = PixelStateEnum.ON;
                    works.AddFirst(a);
                    works.AddFirst(b);
                    break;
                }
            }
            yield break;
        }
    }
}
