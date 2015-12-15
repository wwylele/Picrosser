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
        }

        BackgroundWorker solvingWorker;

        private void buttonSolve_Click(object sender, RoutedEventArgs e) {
            buttonSolve.IsEnabled = false;
            buttonSubmit.IsEnabled = false;
            InitQuestionPresent();
            solvingWorker.RunWorkerAsync();
        }


        Solver solver;
        private void solvingWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if(solver.Result == Solver.ResultEnum.CONTRADICTORY) {
                Rectangle contRect = new Rectangle();
                Color SemiRed = new Color();
                SemiRed.R = 255;
                SemiRed.G = SemiRed.B = 0;
                SemiRed.A = 128;
                contRect.Fill = new SolidColorBrush(SemiRed);
                if(solver.ContradictoryInCols) {
                    contRect.Margin = new Thickness(
                        picrossPixelSize * (picrossLeftSpaces + solver.ContradictoryIndex),
                        0, 0, 0
                        );
                    contRect.Width = picrossPixelSize;
                    contRect.Height = picrossPixelSize * (picrossTopSpaces + question.Height);
                } else {
                    contRect.Margin = new Thickness(
                        0,
                        picrossPixelSize * (picrossTopSpaces + solver.ContradictoryIndex),
                        0, 0
                        );
                    contRect.Height = picrossPixelSize;
                    contRect.Width = picrossPixelSize * (picrossLeftSpaces + question.Width);
                }
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
            Solver.Touch touch = (Solver.Touch)e.UserState;
            pixels[touch.colIndex, touch.rowIndex].Fill =
                touch.on ? brushOn : brushOff;
        }
        private void solvingWorker_DoWork(object sender, DoWorkEventArgs e) {
            solver = new Solver();
            BackgroundWorker bw = (BackgroundWorker)sender;
            foreach(Solver.Touch touch in solver.SolveByStep(question)) {
                Thread.Sleep(10);
                bw.ReportProgress(0, touch);
            }
        }

        /// <summary>
        /// <c>MainWindow</c> constructor.
        /// </summary>
        public MainWindow() {
            InitializeComponent();

            InitQuestionPresent();

            solvingWorker = new BackgroundWorker();
            solvingWorker.WorkerReportsProgress = true;
            solvingWorker.DoWork += solvingWorker_DoWork;
            solvingWorker.ProgressChanged += solvingWorker_ProgressChanged;
            solvingWorker.RunWorkerCompleted += solvingWorker_RunWorkerCompleted;

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
