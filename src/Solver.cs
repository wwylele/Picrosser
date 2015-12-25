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

        /// <summary>
        /// Convert a <c>PixelStateEnum[,]</c> to <c>bool[,]</c>
        /// </summary>
        /// <param name="pixels">The <c>PixelStateEnum[,]</c> to convert</param>
        /// <returns>Convertion result</returns>
        public static bool[,] ConvertToPureSolution(PixelStateEnum[,] pixels) {
            bool[,] ret = new bool[pixels.GetLength(0), pixels.GetLength(1)];
            for(int x = 0; x < pixels.GetLength(0); ++x) {
                for(int y = 0; y < pixels.GetLength(1); ++y) {
                    switch(pixels[x, y]) {
                    case PixelStateEnum.ON:
                        ret[x, y] = true;
                        break;
                    case PixelStateEnum.OFF:
                        ret[x, y] = false;
                        break;
                    default:
                        throw new ArgumentException();
                    }
                }
            }
            return ret;
        }



        void GetColSlice(int colIndex, out MyBitArray on, out MyBitArray off) {
            int size = pixelStates.GetLength(1);
            on = new MyBitArray(size);
            off = new MyBitArray(size);
            for(int i = 0; i < size; ++i) {
                switch(pixelStates[colIndex, i]) {
                case PixelStateEnum.ON:
                    on.Set(i);
                    break;
                case PixelStateEnum.OFF:
                    off.Set(i);
                    break;
                }
            }
        }
        void GetRowSlice(int rowIndex, out MyBitArray on, out MyBitArray off) {
            int size = pixelStates.GetLength(0);
            on = new MyBitArray(size);
            off = new MyBitArray(size);
            for(int i = 0; i < size; ++i) {
                switch(pixelStates[i, rowIndex]) {
                case PixelStateEnum.ON:
                    on.Set(i);
                    break;
                case PixelStateEnum.OFF:
                    off.Set(i);
                    break;
                }
            }
        }
        delegate void GetSliceDel(int rowIndex, out MyBitArray on, out MyBitArray off);



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

        static LinkedList<MyBitArray> GetAllCandidates(int[] numbers, int length) {
            LinkedList<MyBitArray> result = new LinkedList<MyBitArray>();
            if(numbers.Length == 0) {
                result.AddLast(new MyBitArray(length));
            } else {

                foreach(int[] spaces in EnumNumberSpaces(numbers, length)) {
                    MyBitArray can = new MyBitArray(length);
                    int pos = 0;
                    for(int i = 0; i < spaces.Length; ++i) {
                        pos += spaces[i];
                        for(int j = 0; j < numbers[i]; ++j) {
                            can.Set(pos++);
                        }
                    }
                    result.AddLast(can);
                }
            }
            return result;
        }



        //These are used for SolveBySearching to hack in SoveByStep
        //to reuse candidates sets.
        private bool usePresetCandidates = false;
        private LinkedList<MyBitArray>
            [/* 0:cols,1:rows */]
            [/* col/row index */] candidates;
        //Create a inital candidates set from the question
        private static LinkedList<MyBitArray>[][] GetInitCandidatesSet(Question question) {
            var candidates = new LinkedList<MyBitArray>[2][]{
                    new LinkedList<MyBitArray>[question.Width],
                    new LinkedList<MyBitArray>[question.Height]
                };
            for(int coli = 0; coli < question.Width; ++coli) {
                candidates[0][coli] = GetAllCandidates(question.GetColNumbers(coli), question.Height);
            }
            for(int rowi = 0; rowi < question.Height; ++rowi) {
                candidates[1][rowi] = GetAllCandidates(question.GetRowNumbers(rowi), question.Width);
            }
            return candidates;
        }
        // Create a "deep" clone of a candidates set:
        // - clone the set itself,
        // - clone column array and rows array,
        // - clone the IEnumerable for each column/row,
        // - but not clone each BitArray, since we use it as immutable
        private static LinkedList<MyBitArray>[][] CloneCandidatesSet(LinkedList<MyBitArray>[][] c) {
            var candidates = new LinkedList<MyBitArray>[2][]{
                    new LinkedList<MyBitArray>[c[0].Length],
                    new LinkedList<MyBitArray>[c[1].Length]
                };
            for(int i = 0; i < c[0].Length; ++i) {
                candidates[0][i] = new LinkedList<MyBitArray>();
                foreach(var b in c[0][i]) candidates[0][i].AddLast(b);
            }
            for(int i = 0; i < c[1].Length; ++i) {
                candidates[1][i] = new LinkedList<MyBitArray>();
                foreach(var b in c[1][i]) candidates[1][i].AddLast(b);
            }
            return candidates;
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

            //If no pixelStates are set, initialize an empty one
            if(pixelStates == null)
                pixelStates = new PixelStateEnum[question.Width, question.Height];
            if(pixelStates.GetLength(0) != question.Width ||
                pixelStates.GetLength(1) != question.Height) {
                throw new InvalidOperationException("The initial state of pixelStates is invalid");
            }
            if(!usePresetCandidates) {
                //If we didn't get hacked by SolveBySearching,
                //just produce CandidatesSet normally.
                candidates = GetInitCandidatesSet(question);
            }

            int width = question.Width;
            int height = question.Height;
            int hw;//a temp var for swap width and height
            var GetSlice = new GetSliceDel[] {
                GetColSlice,GetRowSlice
            };
            while(true) {
                bool worked = false;//will be set to true if we make some progress

                //for each iterations, we have two rounds.
                //In the first round, work on each columns,
                //then flip the question over along the diagonal,
                //and do the second round work on each columns again 
                //(i.e. each rows of the original question)
                //then flip back.

                for(int flip = 0; flip < 2; ++flip, hw = width, width = height, height = hw) {
                    for(int index = 0; index < width; ++index) {

                        //report current column/row
                        yield return new StepMove {
                            moveToCol = flip == 0,
                            index = index
                        };

                        //Get the bit slice of current column
                        //onSlice indicates if it has been turned on for each pixel
                        //offSlice indicates if it has been turned off for each pixel
                        MyBitArray onSlice, offSlice;
                        GetSlice[flip](index, out onSlice, out offSlice);

                        //Filter out all the candidates that do not match the slice
                        var node = candidates[flip][index].First;
                        while(node != null) {
                            var next = node.Next;
                            if(!node.Value.AndIsZero(offSlice) || !node.Value.NotAndIsZero(onSlice)) {
                                candidates[flip][index].Remove(node);
                            }
                            node = next;
                        }

                        if(!candidates[flip][index].Any()) {
                            //If all candidates has been filter out
                            //there is contradiction
                            Result = ResultEnum.CONTRADICTORY;
                            ContradictoryInCols = flip == 0;
                            ContradictoryIndex = index;
                            yield break;
                        }

                        //Find which pixels can be turned on or off in this round
                        onSlice.Not();
                        offSlice.Not();
                        foreach(MyBitArray bits in candidates[flip][index]) {
                            onSlice.And(bits);
                            offSlice.AndNot(bits);
                        }
                        //Now onSlice contain all pixels to be turned on
                        //and offSlice contain all pixels to be turned off
                        //Do the turning work and report
                        for(int i = 0; i < height; ++i) {
                            int x, y;
                            if(flip == 0) {
                                x = index;
                                y = i;
                            } else {
                                x = i;
                                y = index;
                            }
                            if(onSlice.Test(i)) {
                                worked = true;
                                pixelStates[x, y] = PixelStateEnum.ON;
                                yield return new StepTouch {
                                    colIndex = x,
                                    rowIndex = y,
                                    on = true
                                };
                            } else if(offSlice.Test(i)) {
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

                //If no progress has been made in this interation,
                //we cannot do anything more, and break the interation
                if(worked == false) break;

            }

            //Did we solve the whole puzzle?
            for(int ci = 0; ci < question.Width; ++ci)
                for(int ri = 0; ri < question.Height; ++ri) {
                    if(pixelStates[ci, ri] == PixelStateEnum.UNKNOWN) {
                        //No, there are still some unknown pixels.
                        FirstUnknownColIndex = ci;
                        FirstUnknownRowIndex = ri;
                        Result = ResultEnum.INDEFINITE;
                        goto tag_indefinite;
                    }
                }
            //Yes, we solved it!
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
        /// <param name="question">The puzzle to be solved.</param>
        /// <returns>An IEnumerable of each solutions.</returns>
        public static IEnumerable<bool[,]> SolveBySearching(Question question) {
            var works = new LinkedList<PixelStateEnum[,]>();
            var candidatesList = new LinkedList<LinkedList<MyBitArray>[][]>();
            works.AddFirst(new PixelStateEnum[question.Width, question.Height]);
            candidatesList.AddFirst(GetInitCandidatesSet(question));
            Solver solver = new Solver();
            solver.usePresetCandidates = true;
            while(works.Any()) {
                solver.pixelStates = works.First();
                solver.candidates = candidatesList.First();
                works.RemoveFirst();
                candidatesList.RemoveFirst();
                switch(solver.Solve(question)) {
                case ResultEnum.FINISHED:
                    yield return ConvertToPureSolution(solver.pixelStates);
                    break;
                case ResultEnum.INDEFINITE:
                    PixelStateEnum[,] a, b;
                    a = solver.pixelStates;
                    b = (PixelStateEnum[,])a.Clone();
                    LinkedList<MyBitArray>[][] cloneCan;
                    cloneCan = CloneCandidatesSet(solver.candidates);
                    a[solver.FirstUnknownColIndex, solver.FirstUnknownRowIndex]
                        = PixelStateEnum.OFF;
                    b[solver.FirstUnknownColIndex, solver.FirstUnknownRowIndex]
                        = PixelStateEnum.ON;
                    works.AddFirst(a);
                    works.AddFirst(b);
                    candidatesList.AddFirst(solver.candidates);
                    candidatesList.AddFirst(cloneCan);
                    break;
                }
            }
            yield break;
        }
    }
}
