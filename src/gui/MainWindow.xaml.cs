using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Threading;
using Picrosser;
namespace PicrosserUI {
    /// <summary>
    /// MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        Question question = new Question();

        Rectangle[,] pixels;
        Rectangle cursor;

        Brush brushUnknown = new SolidColorBrush(Colors.Gray);
        Brush brushOn = new SolidColorBrush(Colors.Orange);
        Brush brushOff = new SolidColorBrush(Colors.White);

        int picrossLeftSpaces = 1, picrossTopSpaces = 1;
        int picrossPixelSize = 16;

        void InitQuestionPresent() {
            picrossCanvas.Children.Clear();
            pixels = new Rectangle[question.Width, question.Height];

            picrossLeftSpaces = 1;
            picrossTopSpaces = 1;
            for(int i = 0; i < question.Height; ++i) {
                if(picrossLeftSpaces < question.GetRowNumbers(i).Length)
                    picrossLeftSpaces = question.GetRowNumbers(i).Length;
            }
            for(int i = 0; i < question.Width; ++i) {
                if(picrossTopSpaces < question.GetColNumbers(i).Length)
                    picrossTopSpaces = question.GetColNumbers(i).Length;
            }

            int[] numbers;
            for(int x = 0; x < question.Width; ++x) {
                numbers = question.GetColNumbers(x);
                if(numbers.Length == 0)
                    numbers = new int[] { 0 };
                for(int i = 0; i < numbers.Length; ++i) {
                    TextBlock label = new TextBlock();
                    label.Text = numbers[i].ToString();
                    label.TextAlignment = TextAlignment.Center;
                    label.Width = picrossPixelSize;
                    label.Height = picrossPixelSize;
                    label.Margin = new Thickness(
                        picrossPixelSize * (x + picrossLeftSpaces),
                        picrossPixelSize * (i + picrossTopSpaces - numbers.Length), 0, 0);
                    picrossCanvas.Children.Add(label);
                }
            }
            for(int y = 0; y < question.Height; ++y) {
                numbers = question.GetRowNumbers(y);
                if(numbers.Length == 0)
                    numbers = new int[] { 0 };
                for(int i = 0; i < numbers.Length; ++i) {
                    TextBlock label = new TextBlock();
                    label.Text = numbers[i].ToString();
                    label.TextAlignment = TextAlignment.Center;
                    label.Width = picrossPixelSize;
                    label.Height = picrossPixelSize;
                    label.Margin = new Thickness(
                        picrossPixelSize * (i + picrossLeftSpaces - numbers.Length),
                        picrossPixelSize * (y + picrossTopSpaces), 0, 0);
                    picrossCanvas.Children.Add(label);
                }
            }

            for(int x = 0; x < question.Width; ++x)
                for(int y = 0; y < question.Height; ++y) {
                    pixels[x, y] = new Rectangle();
                    pixels[x, y].Width = picrossPixelSize - 1;
                    pixels[x, y].Height = picrossPixelSize - 1;
                    pixels[x, y].Margin = new Thickness(
                        picrossPixelSize * (x + picrossLeftSpaces),
                        picrossPixelSize * (y + picrossTopSpaces), 0, 0);

                    pixels[x, y].Fill = brushUnknown;
                    picrossCanvas.Children.Add(pixels[x, y]);
                }
            cursor = new Rectangle();
            cursor.Fill = new SolidColorBrush(new Color() {
                R = 0,
                G = 0,
                B = 255,
                A = 100
            });
            picrossCanvas.Children.Add(cursor);
        }

        BackgroundWorker solvingWorker;

        private void buttonSolve_Click(object sender, RoutedEventArgs e) {
            buttonSolve.IsEnabled = false;
            buttonSubmit.IsEnabled = false;
            InitQuestionPresent();
            solvingWorker.RunWorkerAsync();
        }

        private void MoveRectangleToColOrRow(Rectangle rect,bool moveToCol,int index) {
            if(moveToCol) {
                rect.Margin = new Thickness(
                    picrossPixelSize * (picrossLeftSpaces + index),
                    0, 0, 0
                    );
                rect.Width = picrossPixelSize;
                rect.Height = picrossPixelSize * (picrossTopSpaces + question.Height);
            } else {
                rect.Margin = new Thickness(
                    0,
                    picrossPixelSize * (picrossTopSpaces + index),
                    0, 0
                    );
                rect.Height = picrossPixelSize;
                rect.Width = picrossPixelSize * (picrossLeftSpaces + question.Width);
            }
        }

        Solver solver;
        private void solvingWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            picrossCanvas.Children.Remove(cursor);
            if(solver.Result == Solver.ResultEnum.CONTRADICTORY) {
                Rectangle contRect = new Rectangle();
                contRect.Fill = new SolidColorBrush(new Color() {
                    R=255,G=0,B=0,A=128
                });
                MoveRectangleToColOrRow(contRect,
                    solver.ContradictoryInCols,
                    solver.ContradictoryIndex);
                picrossCanvas.Children.Add(contRect);
                MessageBox.Show("Find a contradiction when solving!\n"
                    + "No possible solution.");
            } else if(solver.Result == Solver.ResultEnum.INDEFINITE) {
                MessageBox.Show("Cannot determine the rest pixels.\n"
                    + "Maybe there are more than one solutions.");
            }
            buttonSolve.IsEnabled = true;
            buttonSubmit.IsEnabled = true;
        }

        private void solvingWorker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            BackgroundWorker bw = (BackgroundWorker)sender;
            if(e.UserState is Solver.StepTouch) {
                Solver.StepTouch touch = (Solver.StepTouch)e.UserState;
                pixels[touch.colIndex, touch.rowIndex].Fill =
                    touch.on ? brushOn : brushOff;
            } else if(e.UserState is Solver.StepMove) {
                Solver.StepMove move = (Solver.StepMove)e.UserState;
                MoveRectangleToColOrRow(cursor, move.moveToCol, move.index);

            }
            
        }
        private void solvingWorker_DoWork(object sender, DoWorkEventArgs e) {
            solver = new Solver();
            BackgroundWorker bw = (BackgroundWorker)sender;
            foreach(object step in solver.SolveByStep(question)) {
                int sleepTimeT = sleepTime;
                if(step is Solver.StepTouch && sleepTimeT != 0)
                    Thread.Sleep(sleepTimeT);
                bw.ReportProgress(0, step);
            }
        }

        int[] speedGears = new int[] { 0, 10, 100, 1000 };
        volatile int sleepTime;

        /// <summary>
        /// <c>MainWindow</c> constructor.
        /// </summary>
        public MainWindow() {
            InitializeComponent();

            sliderSpeed.Maximum = speedGears.Length - 1;
            sliderSpeed.Minimum = 0;
            sliderSpeed.Value = 1;

            InitQuestionPresent();

            solvingWorker = new BackgroundWorker();
            solvingWorker.WorkerReportsProgress = true;
            solvingWorker.DoWork += solvingWorker_DoWork;
            solvingWorker.ProgressChanged += solvingWorker_ProgressChanged;
            solvingWorker.RunWorkerCompleted += solvingWorker_RunWorkerCompleted;

        }

        private void sliderSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            sleepTime = speedGears[(int)((sender as Slider).Value)];
        }

        private void buttonSubmit_Click(object sender, RoutedEventArgs e) {
            Question newQuestion = null;
            try {
                newQuestion = new Question(textPicross.Text);
            } catch(ArgumentException) {
                MessageBox.Show("Invalid Picross!");
                return;
            }
            question = newQuestion;
            InitQuestionPresent();
        }
    }
}
