using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows;
using System.IO;

namespace DNDBot
{
    class ConsoleMethods : MainWindow
    {

        public override void WriteToConsole(string message, Brush brush = null)
        {
            Encoding encoding = Encoding.UTF8;
            if (message.Contains("debug"))
                lastLogColor = Brushes.Lime;
            if (message.Contains("info"))
                lastLogColor = Brushes.Blue;
            if (message.Contains("warn"))
                lastLogColor = Brushes.Yellow;
            if (message.Contains("fail"))
                lastLogColor = Brushes.Red;
            if (message.Contains("[DATA]"))
                lastLogColor = Brushes.DarkRed;

            if (brush != null)
                lastLogColor = brush;
            Dispatcher.InvokeAsync(async () =>
            {
                await Task.Delay(50);  
                ConsoleScroll.ScrollToEnd();
                ConsoleScroll.UpdateLayout();
                ConsoleScroll.ScrollToVerticalOffset(ConsoleScroll.ScrollableHeight);
            }, DispatcherPriority.Background);


            TextBlock logEntry = new TextBlock
            {
                Text = $"> {message}",
                Foreground = lastLogColor,
                Background = Brushes.Transparent,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = ConsoleScroll.ActualWidth - 20
            };

            ConsolePanel.Children.Add(logEntry);

             
            Dispatcher.InvokeAsync(() =>
            {
                ConsoleScroll.UpdateLayout();  
                ConsoleScroll.ScrollToEnd();  
                ConsoleScroll.ScrollToVerticalOffset(ConsoleScroll.ExtentHeight);  
            }, DispatcherPriority.Background);



            ConsoleScroll.ScrollToEnd();

            lastLogColor = Brushes.White;

            string logFileEntry = $"{DateTime.Now}: {message}\n";
            File.AppendAllText("logs.txt", logFileEntry, encoding);



        }
    }
}
