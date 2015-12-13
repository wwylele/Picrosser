using System;
using System.Collections.Generic;
using System.Linq;

namespace Picrosser {
    /// <summary>
    /// This class describes a Picrosser puzzle.
    /// </summary>
    public class Question {

        /// <summary>
        /// Columns count.
        /// </summary>
        public int Width {
            get; private set;
        }

        /// <summary>
        /// Rows count.
        /// </summary>
        public int Height {
            get; private set;
        }

        private int[/*Width*/][] colNumbers;
        private int[/*Height*/][] rowNumbers;

        /// <summary>
        /// Get the number sequence of a column.
        /// </summary>
        /// <param name="index">Column index</param>
        /// <returns></returns>
        public int[] GetColNumbers(int index) {
            return colNumbers[index];
        }

        /// <summary>
        /// Get the number sequence of a row.
        /// </summary>
        /// <param name="index">Row index</param>
        /// <returns></returns>
        public int[] GetRowNumbers(int index) {
            return rowNumbers[index];
        }
        void ThrowIfContainLessThanOne(int[] numbers) {
            foreach(int number in numbers)
                if(number < 1) {
                    throw new ArgumentException("invalid number");
                }
        }

        /// <summary>
        /// Set the number sequence of a column.
        /// </summary>
        /// <param name="index">Column index.</param>
        /// <param name="numbers">The number sequence to be set to.</param>
        /// <exception cref="ArgumentException">
        /// Throw if numbers less than one appear in the sequence.
        /// </exception>
        public void SetColNumbers(int index, int[] numbers) {
            ThrowIfContainLessThanOne(numbers);
            colNumbers[index] = numbers;
        }

        /// <summary>
        /// Set the number sequence of a row
        /// </summary>
        /// <param name="index">Row index</param>
        /// <param name="numbers">The number sequence to be set to.</param>
        /// <exception cref="ArgumentException">
        /// Throw if numbers less than one appear in the sequence.
        /// </exception>
        public void SetRowNumbers(int index, int[] numbers) {
            ThrowIfContainLessThanOne(numbers);
            rowNumbers[index] = numbers;
        }

        /// <summary>
        /// Constructor with specified width and height.
        /// </summary>
        /// <param name="width">width of the puzzle.</param>
        /// <param name="height">height of the puzzle.</param>
        public Question(int width, int height) {
            colNumbers = new int[Width = width][];
            rowNumbers = new int[Height = height][];
        }

        /// <summary>
        /// Constructor with a default puzzle.
        /// </summary>
        public Question() :
            this("0\n\n0\n") { }

        int[] StringToNumbers(string line) {
            string[] split = line.Trim().Split(new char[] { ' ', ',' });
            if(split.Length == 0) throw new ArgumentException();
            int[] numbers;
            try {
                numbers = split.Where(x=>!x.Equals("")).Select(Int32.Parse).ToArray();
            } catch(FormatException) {
                throw new ArgumentException();
            }
            if(numbers.Length == 1 && numbers[0] == 0) return new int[0];
            if(numbers.Any((x) => x < 1)) throw new ArgumentException();
            return numbers;
        }

        /// <summary>
        /// Constructor with a formatted string describing the puzzle.
        /// </summary>
        /// <remarks>
        /// The string should be <c>(w+1+h)</c> line, 
        /// where w is puzzle's width, and h is puzzle's height. 
        /// The first <c>w</c> lines may contain the number sequence of each column, 
        /// followed by an empty line, and <c>h</c> lines containing the sequence of each row. 
        /// The number sequence may be write in integers separate by spaces or commas.
        /// </remarks>
        /// <param name="text">The formatted string describing the puzzle.</param>
        /// <exception cref="ArgumentException">
        /// Throw if the string is in wrong format, 
        /// or numbers less than one appear in the sequence.
        /// </exception>
        public Question(string text) {
            LinkedList<int[]> col = new LinkedList<int[]>();
            LinkedList<int[]> row = new LinkedList<int[]>();
            System.IO.StringReader reader = new System.IO.StringReader(text);
            string line;
            while(true) {
                line = reader.ReadLine();
                if(line == null || line.Equals("")) break;
                col.AddLast(StringToNumbers(line));
            }
            while(true) {
                line = reader.ReadLine();
                if(line == null || line.Equals("")) break;
                row.AddLast(StringToNumbers(line));
            }
            if(!col.Any() || !row.Any()) throw new ArgumentException();
            colNumbers = col.ToArray();
            rowNumbers = row.ToArray();
            Width = colNumbers.Length;
            Height = rowNumbers.Length;
        }


    }
}
