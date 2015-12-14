using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace Picrosser {

    /// <summary>
    /// Picross puzzle solver class.
    /// </summary>
    public class Solver {

        /// <summary>
        /// A class that decribes one step in a solution.
        /// </summary>
        public class Touch {

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

        PixelStateEnum[,] pixelStates;

        /// <summary>
        /// Get the state of a specified pixel.
        /// </summary>
        /// <param name="colIndex">Column index of the pixel.</param>
        /// <param name="rowIndex">Row index of the pixel.</param>
        /// <returns>The state of the pixel.</returns>
        public PixelStateEnum GetPixelState(int colIndex, int rowIndex) {
            return pixelStates[colIndex, rowIndex];
        }

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

            /// <summary>The method <c>Solve</c> has not been called 
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
        /// The solving state of the <c>Solver</c>
        /// </summary>
        public ResultEnum Result {
            get; private set;
        }

        /// <summary>
        /// Valid only if <c>Result==ResultEnum.CONTRADICTORY</c>. 
        /// Indicating whether the contradiction was found in a column or not.
        /// </summary>
        public bool ContradictoryInCols {
            get; private set;
        }

        /// <summary>
        /// Valid only if <c>Result==ResultEnum.CONTRADICTORY</c>. 
        /// Indicating the index of the column/row that contains contradiction.
        /// </summary>
        public int ContradictoryIndex {
            get; private set;
        }

        static IEnumerable<int[]/*Spaces before each number*/> EnumNumberSpaces(int[] numbers, int length) {
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

        static bool IsZero(BitArray b) {
            foreach(bool k in b) if(k) return false;
            return true;
        }

        static IEnumerable<BitArray> FilterCandidates(
            IEnumerable<BitArray> cans, BitArray onSlice, BitArray offSlice) {
            LinkedList<BitArray> newCandidates = new LinkedList<BitArray>();
            foreach(BitArray bits in cans) {
                if(IsZero(((BitArray)bits.Clone()).And(offSlice)) &&
                    IsZero(((BitArray)bits.Clone()).Not().And(onSlice))) {
                    newCandidates.AddLast(bits);
                }
            }
            return newCandidates;
        }
        /// <summary>
        /// Solve a picross puzzle.
        /// </summary>
        /// <param name="question">The picross puzzle to be solved.</param>
        /// <returns>An IEnumerable of solution steps.</returns>
        /// <example>
        /// This example shows a common way to use <c>Solve</c>:
        /// <code>
        /// Question question=new Question();
        /// Solver solver=new Solver();
        /// foreach(var step in solver.Solve(question)){
        ///     if(step.on)
        ///         //turn on the pixel at (step.colIndex,step.rowIndex)
        ///     else
        ///         //turn pff the pixel at (step.colIndex,step.rowIndex)
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
        public IEnumerable<Touch> Solve(Question question) {
            Result = ResultEnum.SOLVING;
            pixelStates = new PixelStateEnum[question.Width, question.Height];
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
                        BitArray onSlice = GetSlice[flip](index, true);
                        BitArray offSlice = GetSlice[flip](index, false);

                        candidates[flip][index] = FilterCandidates(candidates[flip][index], onSlice, offSlice);
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
                            offSlice.And(((BitArray)bits.Clone()).Not());
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
                                yield return new Touch {
                                    colIndex = x,
                                    rowIndex = y,
                                    on = true
                                };
                            } else if(offSlice.Get(i)) {
                                worked = true;
                                pixelStates[x, y] = PixelStateEnum.OFF;
                                yield return new Touch {
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
                        Result = ResultEnum.INDEFINITE;
                        goto tag_indefinite;
                    }
                }
            Result = ResultEnum.FINISHED;
            tag_indefinite:


            yield break;
        }
    }
}
