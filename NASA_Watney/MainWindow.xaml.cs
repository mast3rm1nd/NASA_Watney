using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Threading;
using System.Timers;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace NASA_Watney
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        static MyVisualHost VisualHost;
        //static Point ImageCenter = new Point();

        public MainWindow()
        {
    //        var test = "11111111000000001111111100000000111111110000000";
    //        var test2 = Regex.Replace(test, ".{8}", "$0,");

    //        //var enumerable = "111111110000000011111111000000001111111100000000".Batch(8).Reverse();
    //        const int separateOnLength = 2;
    //        string separated = new string(
    //test.Select((x, i) => i > 0 && i % separateOnLength == 0 ? new[] { ' ', x } : new[] { x })
    //    .SelectMany(x => x)
    //    .ToArray()
    //);

            InitializeComponent();

            VisualHost = new MyVisualHost(new Point(Image_ContentControl.Width / 2, Image_ContentControl.Height / 2));

            Image_ContentControl.Content = VisualHost;

            //VisualHost.Transmit("9");

            //VisualHost.Transmit("98");

            // написать про то, что задержка сигнала не эмулируется ))))


            //VisualHost.Center.X = Image_ContentControl.Width / 2;
            //VisualHost.Center.Y = Image_ContentControl.Height / 2;

            //ImageCenter.X = Image_ContentControl.Width / 2;
            //ImageCenter.Y = Image_ContentControl.Height / 2;
        }



        public class MyVisualHost : FrameworkElement
        {
            private static VisualCollection Childrens;

            private static Point Center;

            //private Line PointingLine;
            private static double PointingLineLength = 150.0;

            private static Point PointingLineEnd = new Point();

            private static char CurrentSymbol = '-';

            private static string SymbolsToDraw = "-ABCDEF0123456789";

            private static string TextToTransmit;

            private static double AngleBetweenSymbols = 360.0 / SymbolsToDraw.Count();
            private static double RadiansBetweenSymmols = AngleBetweenSymbols / (180 / Math.PI);

            public MyVisualHost(Point center)
            {
                //test
                //var t = TextToAsciiBytes("ASCII Encoding Example");
                //test

                Childrens = new VisualCollection(this);
                Center = center;

                InitializeSymbols();
                InitializePointingLine();
                //RotatePointingArrowOnAngle(100);
                //NavigateToChar('B');
                //NavigateToChar('2');
            }


            void InitializeSymbols()
            {              
                var fontWidth = 15;

                var currentAngle = 0.0;

                foreach(var symbolToDraw in SymbolsToDraw)
                {
                    var visual = new DrawingVisual();
                    Childrens.Add(visual);

                    using (var dc = visual.RenderOpen())
                    {
                        var textCoord = new Point(
                            Center.X - (fontWidth / 3) * symbolToDraw.ToString().Length,
                            //Center.X - 5,
                            Center.Y - PointingLineLength - fontWidth
                            );


                        textCoord = RotatePointOnAngle(textCoord, currentAngle);

                        //textCoord.X -= fontWidth / 3;
                        //textCoord.Y -= fontWidth / 2;

                        //textCoord.X -= fontWidth / 3;
                        textCoord.Y -= fontWidth / 1.75;

                        dc.DrawText(new FormattedText
                            (
                              symbolToDraw.ToString(),
                              new System.Globalization.CultureInfo("en-us"),
                              FlowDirection.LeftToRight,
                              new Typeface("Verdana"),
                              fontWidth,
                              Brushes.Black
                              ),
                              textCoord
                            );
                    }

                    currentAngle += AngleBetweenSymbols;
                }
                
            }


            void InitializePointingLine()
            {
                PointingLineEnd.X = Center.X;
                PointingLineEnd.Y = Center.Y - PointingLineLength;

                var visual = new DrawingVisual();
                Childrens.Add(visual);
                
                using (var dc = visual.RenderOpen())
                {                    
                    dc.DrawLine(new Pen(Brushes.Black, 3), Center, PointingLineEnd);
                }
            }


            Thread TransmitThread;
            static TextBox TextBoxWithReceivedBytes;
            static Button TransmitButton;
            public void Transmit(string text, TextBox tbToWriteBytes, Button button)
            {
                TextBoxWithReceivedBytes = tbToWriteBytes;
                TransmitButton = button;

                TextToTransmit = TextToAsciiBytes(text);

                TransmitThread = new Thread(new ThreadStart(TransmitBytes));
                TransmitThread.IsBackground = true;
                TransmitThread.Start();
            }


            public void TransmitBytes()
            {
                foreach (var ch in TextToTransmit)
                {
                    NavigateToChar(ch);
                    CharReceived(ch);
                    PauseBetweenPointing();
                }

                NavigateToChar('-');

                

                Dispatcher.BeginInvoke(new Action(delegate
                {
                    TransmitButton.IsEnabled = true;

                    Childrens.RemoveAt(Childrens.Count - 1);

                    InitializePointingLine();
                }));
            }

            private void CharReceived(char ch)
            {
                if(ch != '-')
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    var text = TextBoxWithReceivedBytes.Text + ch.ToString();

                    text = text.Replace(" ", "");

                    const int separateOnLength = 2;

                    TextBoxWithReceivedBytes.Text = new string(
                    text.Select((x, i) => i > 0 && i % separateOnLength == 0 ? new[] { ' ', x } : new[] { x })
                        .SelectMany(x => x)
                        .ToArray()
                    );

                    
                    //var textBox = ReceivedBytes_TextBox;
                    //ReceivedBytes_TextBox.Text += ch.ToString();
                }));
            }


            //static void SetTextboxTextSafe(string text)
            //{
            //    ReceivedBytes_TextBox.Text = text;
            //}


            Thread Navigate;
            private void NavigateToChar(char charToPointOn)
            {
                Debug.WriteLine(string.Format("Навигация к символу '{0}'", charToPointOn));

                var ticksDifference = SymbolsToDraw.IndexOf(charToPointOn) - SymbolsToDraw.IndexOf(CurrentSymbol);

                var angleToTurn = ticksDifference * AngleBetweenSymbols;

                CurrentAngleToTurn = angleToTurn;

                if (angleToTurn < 180 && angleToTurn > 0) // градус может быть нулевым!!!!!
                    NavigationSpeedDegrees = 0.5;
                else if (angleToTurn > 180)
                {
                    CurrentAngleToTurn = 360 - angleToTurn;
                    CurrentAngleToTurn *= -1;
                    NavigationSpeedDegrees = -0.5;
                }
                else if (angleToTurn < 0)
                {
                    if(angleToTurn > -180)
                        NavigationSpeedDegrees = -0.5;
                    else
                    {
                        CurrentAngleToTurn = angleToTurn + 360;
                        NavigationSpeedDegrees = 0.5;
                    }
                }
                else if (angleToTurn == 0)
                {
                    CurrentAngleToTurn = AngleBetweenSymbols / 2;
                    NavigationSpeedDegrees = 0.25;

                    RotatePointingArrow();

                    CurrentAngleToTurn = -AngleBetweenSymbols / 2;
                    NavigationSpeedDegrees = -0.25;
                }
                    


                //RotatePointingArrow();
                //Navigate = new Thread(new ThreadStart(RotatePointingArrow));
                //Navigate.IsBackground = true;
                //Navigate.Start();

                RotatePointingArrow();

                CurrentSymbol = charToPointOn;
            }

            static double CurrentAngleToTurn;
            //static double LeftDegrees;
            static double NavigationSpeedDegrees;
            //static System.Timers.Timer TimerTicker;
            //static bool IsRotationEnded = true;
            private void RotatePointingArrow()
            {               
                //IsRotationEnded = false;

                while(true)
                {
                    Thread.Sleep(10);

                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        Childrens.RemoveAt(Childrens.Count - 1);

                        var visual = new DrawingVisual();
                        Childrens.Add(visual);

                        using (var dc = visual.RenderOpen())
                        {
                            dc.DrawLine(new Pen(Brushes.Black, 3), Center, PointingLineEnd);
                        }
                    }));


                    CurrentAngleToTurn -= NavigationSpeedDegrees;


                    if (CurrentAngleToTurn > 0)
                    {
                        //CurrentAngleToTurn - NavigationSpeedDegrees;

                        if (CurrentAngleToTurn - NavigationSpeedDegrees < 0)
                        {
                            NavigationSpeedDegrees = CurrentAngleToTurn; // если следующий тик перескочит точку назначения, то повернём только на оставшиеся градусы
                        }
                    }
                    else if (CurrentAngleToTurn < 0)
                    {
                        if (CurrentAngleToTurn - NavigationSpeedDegrees > 0)
                        {
                            NavigationSpeedDegrees = CurrentAngleToTurn; // если следующий тик перескочит точку назначения, то повернём только на оставшиеся градусы
                        }
                    }
                    else // осталось 0 градусов
                    {
                        //IsRotationEnded = true;
                        //TimerTicker.Stop();
                        return;
                    }


                    PointingLineEnd = RotatePointOnAngle(PointingLineEnd, NavigationSpeedDegrees);
                } // while (true)                
            }

            //private void TickRotation(object sender, ElapsedEventArgs e)
            //{              
            //    Dispatcher.BeginInvoke(new Action(delegate
            //    {
            //        Childrens.RemoveAt(Childrens.Count - 1);

            //        var visual = new DrawingVisual();
            //        Childrens.Add(visual);

            //        using (var dc = visual.RenderOpen())
            //        {
            //            dc.DrawLine(new Pen(Brushes.Black, 3), Center, PointingLineEnd);
            //        }                    
            //    }));


            //    CurrentAngleToTurn -= NavigationSpeedDegrees;


            //    if (CurrentAngleToTurn > 0)
            //    {
            //        //CurrentAngleToTurn - NavigationSpeedDegrees;

            //        if(CurrentAngleToTurn - NavigationSpeedDegrees < 0)
            //        {
            //            NavigationSpeedDegrees = CurrentAngleToTurn; // если следующий тик перескочит точку назначения, то повернём только на оставшиеся градусы
            //        }
            //    }
            //    else if(CurrentAngleToTurn < 0)
            //    {
            //        if (CurrentAngleToTurn - NavigationSpeedDegrees > 0)
            //        {
            //            NavigationSpeedDegrees = CurrentAngleToTurn; // если следующий тик перескочит точку назначения, то повернём только на оставшиеся градусы
            //        }
            //    }
            //    else // осталось 0 градусов
            //    {
            //        IsRotationEnded = true;
            //        //TimerTicker.Stop();
            //        return;
            //    }
                                

            //    PointingLineEnd = RotatePointOnAngle(PointingLineEnd, NavigationSpeedDegrees);
            //}



            //static System.Timers.Timer TimerPause;
            private void PauseBetweenPointing()
            {
                //TimerPause = new System.Timers.Timer(1);

                //TimerPause.Elapsed += TimerPause_Elapsed;

                //TimerPause.Start();
                Thread.Sleep(1500);
            }

            //private void TimerPause_Elapsed(object sender, ElapsedEventArgs e)
            //{
            //    Thread.Sleep(1500);
            //    TimerPause.Stop();
            //}

            protected override int VisualChildrenCount
            {
                get { return Childrens.Count; }
            }

            protected override Visual GetVisualChild(int index)
            {
                if (index < 0 || index >= Childrens.Count)
                {
                    throw new ArgumentOutOfRangeException();
                }

                return Childrens[index];
            }


            Point RotatePointOnAngle(Point pointToRotate, double angleInDegrees)
            {
                var centerPoint = Center;

                double angleInRadians = angleInDegrees / (180 / Math.PI);
                double cosTheta = Math.Cos(angleInRadians);
                double sinTheta = Math.Sin(angleInRadians);

                var newX = cosTheta * (pointToRotate.X - centerPoint.X) - sinTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.X;
                var newY = sinTheta * (pointToRotate.X - centerPoint.X) + cosTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.Y;

                return new Point(newX, newY);
            }

            private string TextToAsciiBytes(string text)
            {
                ASCIIEncoding ascii = new ASCIIEncoding();
                Byte[] bytes = ascii.GetBytes(text);

                //return bytes;
                //return String.Join("", bytes.Select(p => p.ToString()).ToArray());
                //{0:X2}
                return String.Join("", bytes.Select(p => p.ToString("X2")).ToArray());
            }
        }

        private void Transmit_Button_Click(object sender, RoutedEventArgs e)
        {
            //VisualHost.Test();
            //VisualHost.MovePointingLine();
            Transmit_Button.IsEnabled = false;
            ReceivedBytes_TextBox.Text = "";
            VisualHost.Transmit(NASA_Message_TextBox.Text, ReceivedBytes_TextBox, Transmit_Button);

            //Transmit_Button.IsEnabled = true;
        }        
    }
}
