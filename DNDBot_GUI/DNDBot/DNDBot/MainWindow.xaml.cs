using DNDBot.Properties;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
namespace DNDBot
{
    ///



    public partial class MainWindow : Window
    {
        public static Process botProcess;
        private readonly object botInputLock = new object();
        private StreamWriter botInputWriter;

        public bool isNightTheme = true;
        public bool botActive = false;
        public DispatcherTimer uptimeTimer;
        public DispatcherTimer networkTimer;
        public Stopwatch stopwatch = new Stopwatch();
        public Brush lastLogColor = Brushes.White;  

        public PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        public PerformanceCounter ramCounter = new PerformanceCounter("Memory", "Available MBytes");
        PerformanceCounter freeRamCounter = new PerformanceCounter("Memory", "Available MBytes");
        private DateTime lastUsageSampleUtc = DateTime.UtcNow;
        private TimeSpan lastGuiCpuTime = Process.GetCurrentProcess().TotalProcessorTime;
        private TimeSpan lastBotCpuTime = TimeSpan.Zero;
        private int lastBotPid = -1;
        private bool hasUsageBaseline;

        public PerformanceCounter cpuCounterBot;
        public PerformanceCounter ramCounterBot;

        public MainWindow()
        {

            InitializeComponent();
            StartNetworkCheck();

            uptimeTimer = new DispatcherTimer();
            uptimeTimer.Interval = TimeSpan.FromSeconds(1);
            uptimeTimer.Tick += UpdateUptime;
            uptimeTimer.Tick += async (s, e) =>
            {
                string usage = await GetTotalUsageAsync();
                CpuRamTextBox.Text = usage;  
            };
        }

        public void MoveWindow(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        public void CloseWindow(object sender, RoutedEventArgs e)
        {
            if (botProcess != null && !botProcess.HasExited)
            {
                botProcess.Kill();  
            }
            stopwatch.Stop();
            Close();
        }

        public void ConsoleInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            e.Handled = true;
            string command = ConsoleInput.Text.Trim();

            if (string.IsNullOrEmpty(command))
            {
                ConsoleInput.Clear();
                return;
            }

            Dispatcher.InvokeAsync(() =>
            {
                ConsoleScroll.UpdateLayout();
                ConsoleScroll.ScrollToVerticalOffset(ConsoleScroll.ExtentHeight);
            }, DispatcherPriority.Background);

            WriteToConsole(command);

            bool sentToBot = SendCommandToBot(command);
            bool handledByGui = ProcessCommand(command);

            if (!sentToBot && !handledByGui)
            {
                WriteToConsole("warn: Bot process is not running.", Brushes.Yellow);
            }

            ConsoleInput.Clear();
        }

        private bool SendCommandToBot(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                return false;
            }

            if (botProcess == null || botProcess.HasExited || botInputWriter == null)
            {
                return false;
            }

            try
            {
                lock (botInputLock)
                {
                    botInputWriter.WriteLine(command);
                }

                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException || ex is InvalidOperationException)
            {
                WriteToConsole($"fail:\n  > Failed to send command: {ex.Message}", Brushes.Red);
                return false;
            }
        }

        public void MinimizeWindow(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        public void OpenConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Public", "DNDBot.dll.config");

                if (File.Exists(filePath))
                {
                    Process.Start(new ProcessStartInfo { FileName = filePath, UseShellExecute = true });
                }
                else
                {
                    MessageBox.Show("Файл не знайдено!", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка при відкритті конфігу: " + ex.Message, "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        public void ToggleTheme(object sender, RoutedEventArgs e)
        {
            Storyboard sb = (Storyboard)FindResource(isNightTheme ? "ThemeTransition" : "ThemeTransitionDark");
            Storyboard sbB = (Storyboard)FindResource(isNightTheme ? "FadeOutButton" : "GetOutButton");

            sb.Begin();
            sbB.Begin();
            isNightTheme = !isNightTheme;

            UpdateButtonTags(MainGrid);  
        }

        public void StopButton(object sender, RoutedEventArgs e)
        {
            botActive = false;
            TimeLabel.Text = "Uptime: 00:00:00";
            BotStatusText.Text = "Status: Offline";
            stopwatch.Stop();
            uptimeTimer.Stop();
            StopBotProcess();
        }

        public void StartButton(object sender, RoutedEventArgs e)
        {
            if (botProcess != null && !botProcess.HasExited)
                return;

            if (botProcess != null && botProcess.HasExited)
            {
                CloseBotInputWriter();
                botProcess.Dispose();
                botProcess = null;
            }

            ConsolePanel.Children.Clear();
            stopwatch = Stopwatch.StartNew();
            BotStatusText.Text = "Status: Active";
            uptimeTimer.Start();
            StartBotProcess();
        }



        public void RestartButton(object sender, RoutedEventArgs e)
        {
            BotStatusText.Text = "Status: Restart";
            try
            {
                if (botProcess != null && !botProcess.HasExited)
                {
                    botProcess.Kill();   
                    botProcess.WaitForExit();  
                    CloseBotInputWriter();
                    botProcess.Dispose();  
                    botProcess = null;
                    ConsolePanel.Children.Clear();
                    botActive = false;
                    stopwatch = Stopwatch.StartNew();
                }

                StartBotProcess();
            }
            catch (Exception ex)
            {
                WriteToConsole($"Ошибка при перезапуске бота: {ex.Message}", Brushes.Lime);
            }

        }



        public void OpenLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (File.Exists("logs.txt"))
                {
                     
                    Process.Start(new ProcessStartInfo("logs.txt") { UseShellExecute = true });
                }
                else
                {
                    MessageBox.Show("Файл логов не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при открытии логов: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            StopBotProcess();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            StopBotProcess();
        }






        public void UpdateButtonTags(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child is Button btn && btn.Tag?.ToString() == "ThemeButton")
                {
                    btn.Tag = isNightTheme ? "GradientChange" : "ThemeButton";
                }

                 
                UpdateButtonTags(child);
            }
        }

        private bool IsInternetAvailable()
        {
            try
            {
                using (var ping = new Ping())
                {
                    PingReply reply = ping.Send("8.8.8.8", 1000);  
                    return reply.Status == IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }

        public void StartNetworkCheck()
        {
            networkTimer = new DispatcherTimer();
            networkTimer.Interval = TimeSpan.FromSeconds(5);  
            networkTimer.Tick += (s, e) =>
            {
                if (IsInternetAvailable())
                {
                    NetworkStatus.Text = "Internet: ✅ Connected";
                }
                else
                {
                    NetworkStatus.Text = "Internet: ❌ Disconnected";
                }
            };
            networkTimer.Start();
        }





        public void UpdateUptime(object sender, EventArgs e)
        {
            TimeSpan uptime = stopwatch.Elapsed;
            TimeLabel.Text = $"Uptime: {uptime:hh\\:mm\\:ss}";
            StartNetworkCheck();
        }






        public void ArchiveLogs()
        {
            try
            {
                string logFilePath = "logs.txt";

                if (File.Exists(logFilePath))
                {
                    string archiveName = $"logs_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
                    File.Move(logFilePath, archiveName);  
                }

                 

                WriteToConsole($"  > Logs saved as {DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt", Brushes.Lime);
            }
            catch (Exception ex)
            {
                WriteToConsole($"fail:\n  > Unknown error: {ex.Message}", Brushes.Red);
            }
        }



        



        public bool ProcessCommand(string command)
        {
            string commandName = command.ToLower();
            ConsoleScroll.ScrollToEnd();

            switch (command.ToLower())
            {
                case "/help":
                    return HelpMethod();
                case "/clear":
                    return ClearMethod();
                case "/clearlogs":
                    return ArchiveLogsMathod();
                case "/stop":
                    return StopMethod();
                case "/info":
                    return InfoMethod();
                case "/status":
                    return StatusMethod();
                case "/botprocess":
                    return BotProcessesMethod();
                case "/uptime":
                    return UptimeMethod();
                case "/restart":
                    return RestartMethod();
                case "/kill":
                    return KillProcessMethod();
                case "/threads":
                    return UsedThreadsMethod();
                case "/handles":
                    return HadlesMathod();
                case "/cpuavg":
                    return CpuavgMathod();
                case "/ramtotal":
                    return TotalRamMethod();
                case "/exit":
                    return ExitMethod();
                case "/killall":
                    return KillMethod();
                default:
                    return false;
            }
        }

        public void StartBotProcess()
        {
            CloseBotInputWriter();
            botProcess = new Process();

            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Public", "DNDBot.exe");

            try
            {

                if (File.Exists(filePath))
                {

                    botProcess.StartInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Public", "DNDBot.exe");
                    botProcess.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                    botProcess.StartInfo.StandardErrorEncoding = Encoding.UTF8;
                    botProcess.StartInfo.RedirectStandardOutput = true;
                    botProcess.StartInfo.RedirectStandardError = true;  
                    botProcess.StartInfo.RedirectStandardInput = true;  
                    botProcess.StartInfo.UseShellExecute = false;
                    botProcess.StartInfo.CreateNoWindow = true;
                    botProcess.OutputDataReceived += BotProcess_OutputDataReceived;
                    botProcess.ErrorDataReceived += BotProcess_OutputDataReceived;

                    botProcess.Start();
                    botInputWriter = botProcess.StandardInput;
                    botInputWriter.AutoFlush = true;
                    botProcess.BeginOutputReadLine();
                    botProcess.BeginErrorReadLine();
                    BotStatusText.Text = "Status: Active";
                    botActive = true;

                    cpuCounterBot = new PerformanceCounter("Process", "% Processor Time", botProcess.ProcessName);
                    ramCounterBot = new PerformanceCounter("Process", "Working Set - Private", botProcess.ProcessName);
                    ResetUsageBaseline();
                }
                else
                {
                    MessageBox.Show("Файл не знайдено!", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка при відкритті: " + ex.Message, "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }




        }

        public void StopBotProcess()
        {
            if (botProcess != null)
            {
                try
                {
                    if (!botProcess.HasExited)  
                    {
                        botProcess.Kill();   
                        botProcess.WaitForExit();  
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при завершении процесса: {ex.Message}");
                }
                finally
                {
                    CloseBotInputWriter();
                    botProcess.Dispose();  
                    botProcess = null;
                    ResetUsageBaseline();
                    lastLogColor = Brushes.Lime;
                     
                    WriteToConsole("\n" +
"------------------------------------------\n" +
"               Bot Stopped!" +
"\n------------------------------------------", Brushes.Lime);
                }
            }
        }

        private void CloseBotInputWriter()
        {
            lock (botInputLock)
            {
                if (botInputWriter == null)
                {
                    return;
                }

                try
                {
                    botInputWriter.Dispose();
                }
                catch
                {
                }
                finally
                {
                    botInputWriter = null;
                }
            }
        }

        private void BotProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Dispatcher.Invoke(() => WriteToConsole(e.Data));  
            }
        }

        private bool KillProcessMethod()
        {
            WriteToConsole("debug: Killing bot process...", Brushes.Lime);
            botProcess.Kill();
            return true;
        }

        private bool UsedThreadsMethod()
        {
            int threadCount = botProcess.Threads.Count;
            WriteToConsole($"debug:\n" +
                $"  > Bot threads: {threadCount}", Brushes.Lime);
            return true;
        }

        private bool HadlesMathod()
        {
            int handleCount = botProcess.HandleCount;
            WriteToConsole($"debug:\n" +
                $"  > Bot handles: {handleCount}", Brushes.Lime);
            return true;
        }

        private bool CpuavgMathod()
        {
            float totalCpuUsage = 0;

            for (int i = 0; i < 5; i++)
            {
                totalCpuUsage += cpuCounterBot.NextValue() / Environment.ProcessorCount;
                Thread.Sleep(1000);
            }

            WriteToConsole($"debug:\n" +
 $"  > Average CPU usage (5 sec): {totalCpuUsage}", Brushes.Lime);

            return true;
        }

        private bool TotalRamMethod()
        {
            float totalRam = new PerformanceCounter("Memory", "Available MBytes").NextValue();
            WriteToConsole($"debug:\n" +
                $"  > Total Available RAM: {totalRam} MB", Brushes.Lime);
            return true;
        }

        private bool ExitMethod()
        {
            WriteToConsole("debug: Exiting...", Brushes.Lime);
            Environment.Exit(0);
            return true;
        }

        private bool RestartMethod()
        {
            WriteToConsole("debug: Restarting bot...", Brushes.Lime);
            botProcess.Kill();
            botProcess.WaitForExit();
            StartBotProcess();
            return true;
        }

        private bool UptimeMethod()
        {
            TimeSpan uptime = DateTime.Now - botProcess.StartTime;

            WriteToConsole($"debug:\n" +
                $"  > Bot uptime: {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}", Brushes.Lime);
            return true;
        }

        private bool BotProcessesMethod()
        {
            WriteToConsole($"debug:\n" +
                $"  > Bot PID: {botProcess.Id}\n" +
                $"  > Bot Process Name: {botProcess.ProcessName}", Brushes.Lime);
            return true;
        }

        private bool StatusMethod()
        {
            WriteToConsole($"debug:\n" +
                $"  > Process {botProcess.ProcessName} is running: {!botProcess.HasExited}", Brushes.Lime);
            return true;
        }

        private bool InfoMethod()
        {
            var usage = CaptureUsageSnapshot();
            WriteToConsole($"debug:\n" +
                $"  > GUI CPU: {usage.guiCpu:F2}%\n" +
                $"  > GUI RAM: {usage.guiRamMb:F2} MB\n" +
                $"  > Bot CPU: {(usage.botRunning ? $"{usage.botCpu:F2}%" : "offline")}\n" +
                $"  > Bot RAM: {(usage.botRunning ? $"{usage.botRamMb:F2} MB" : "offline")}\n" +
                $"  > Free RAM: {usage.freeRamMb:F2} MB", Brushes.Lime);
            return true;
        }

        private bool StopMethod()
        {
            botActive = false;
            TimeLabel.Text = "Uptime: 00:00:00";
            BotStatusText.Text = "Status: Offline";
            stopwatch.Stop();
            uptimeTimer.Stop();
            StopBotProcess();
            WriteToConsole("\n" +
"------------------------------------------\n" +
"               Bot Stopped!" +
"\n------------------------------------------", Brushes.Lime);
            return true;
        }

        private bool ArchiveLogsMathod()
        {
            ArchiveLogs();
            return true;
        }

        private bool ClearMethod()
        {
            ConsolePanel.Children.Clear();
            return true;
        }

        private bool HelpMethod()
        {
            WriteToConsole($"debug: CONSOLE COMMANDS\n" +
                $"  > /clear /info /status /botprocess\n" +
                $"  > /uptime /restart /kill /threads\n" +
                $"  > /handles /cpuavg /ramtotal /exit\n" +
                $"  > /killall", Brushes.Lime);
            return true;
        }

        private bool KillMethod()
        {
            int currentPid = Process.GetCurrentProcess().Id;
            Process[] BotProcesses = Process.GetProcessesByName(botProcess.ProcessName);

            if (BotProcesses.Length == 0)
            {
                WriteToConsole("debug: No bot process found.", Brushes.Lime);
            }
            else
            {
                foreach (Process p in BotProcesses)
                {
                    if (p.Id == currentPid)
                        continue;
                    try
                    {
                        WriteToConsole($"debug: Killing bot process PID {p.Id}...", Brushes.Lime);
                        p.Kill();
                        p.WaitForExit();
                    }
                    catch (Exception ex)
                    {
                        WriteToConsole($"debug: Failed to kill PID {p.Id}: {ex.Message}", Brushes.Lime);

                    }
                    WriteToConsole($"debug: Killed {BotProcesses.Length - 1} bot process(es)", Brushes.Lime);

                }

                BotStatusText.Text = "Status: Restart";
                try
                {
                    if (botProcess != null && !botProcess.HasExited)
                    {
                        botProcess.Kill();   
                        botProcess.WaitForExit();  
                        CloseBotInputWriter();
                        botProcess.Dispose();  
                        botProcess = null;
                        ConsolePanel.Children.Clear();
                        botActive = false;
                        stopwatch = Stopwatch.StartNew();
                    }

                    StartBotProcess();
                }
                catch (Exception ex)
                {
                    WriteToConsole($"Ошибка при перезапуске бота: {ex.Message}", Brushes.Lime);
                }
            }


            return true;
        }

        public Task<string> GetTotalUsageAsync()
        {
            try
            {
                var usage = CaptureUsageSnapshot();
                if (!usage.botRunning)
                    return Task.FromResult($"GUI CPU/RAM: {usage.guiCpu:F1}% / {usage.guiRamMb:F1} MB | Bot: offline | Free RAM: {usage.freeRamMb:F0} MB");

                return Task.FromResult($"GUI CPU/RAM: {usage.guiCpu:F1}% / {usage.guiRamMb:F1} MB | Bot CPU/RAM: {usage.botCpu:F1}% / {usage.botRamMb:F1} MB | Free RAM: {usage.freeRamMb:F0} MB");
            }
            catch (Exception ex)
            {
                return Task.FromResult($"Ошибка мониторинга: {ex.Message}");
            }
        }

        private (double guiCpu, double botCpu, double guiRamMb, double botRamMb, float freeRamMb, bool botRunning) CaptureUsageSnapshot()
        {
            Process guiProcess = Process.GetCurrentProcess();
            guiProcess.Refresh();

            bool botRunning = botProcess != null && !botProcess.HasExited;
            DateTime now = DateTime.UtcNow;
            double elapsedMs = (now - lastUsageSampleUtc).TotalMilliseconds;

            TimeSpan currentGuiCpu = guiProcess.TotalProcessorTime;
            TimeSpan currentBotCpu = TimeSpan.Zero;
            int currentBotPid = -1;

            double guiRamMb = guiProcess.WorkingSet64 / 1024d / 1024d;
            double botRamMb = 0;

            if (botRunning)
            {
                botProcess.Refresh();
                currentBotCpu = botProcess.TotalProcessorTime;
                currentBotPid = botProcess.Id;
                botRamMb = botProcess.WorkingSet64 / 1024d / 1024d;
            }

            double guiCpu = 0;
            double botCpu = 0;

            if (hasUsageBaseline && elapsedMs > 0)
            {
                guiCpu = CalculateCpuPercent(currentGuiCpu - lastGuiCpuTime, elapsedMs);

                if (botRunning && currentBotPid == lastBotPid)
                {
                    botCpu = CalculateCpuPercent(currentBotCpu - lastBotCpuTime, elapsedMs);
                }
            }

            hasUsageBaseline = true;
            lastUsageSampleUtc = now;
            lastGuiCpuTime = currentGuiCpu;

            if (botRunning)
            {
                lastBotCpuTime = currentBotCpu;
                lastBotPid = currentBotPid;
            }
            else
            {
                lastBotCpuTime = TimeSpan.Zero;
                lastBotPid = -1;
            }

            return (guiCpu, botCpu, guiRamMb, botRamMb, freeRamCounter.NextValue(), botRunning);
        }

        private double CalculateCpuPercent(TimeSpan cpuDelta, double elapsedMs)
        {
            double cpu = cpuDelta.TotalMilliseconds / (elapsedMs * Environment.ProcessorCount) * 100d;
            if (double.IsNaN(cpu) || double.IsInfinity(cpu))
                return 0;

            return Math.Min(100d, Math.Max(0d, cpu));
        }

        private void ResetUsageBaseline()
        {
            hasUsageBaseline = false;
            lastUsageSampleUtc = DateTime.UtcNow;
            lastGuiCpuTime = Process.GetCurrentProcess().TotalProcessorTime;

            if (botProcess != null && !botProcess.HasExited)
            {
                botProcess.Refresh();
                lastBotCpuTime = botProcess.TotalProcessorTime;
                lastBotPid = botProcess.Id;
            }
            else
            {
                lastBotCpuTime = TimeSpan.Zero;
                lastBotPid = -1;
            }
        }



        public void WriteToConsole(string message, Brush brush = null)
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
            if (message.IndexOf("[DB-CLEAR]", StringComparison.OrdinalIgnoreCase) >= 0)
                lastLogColor = Brushes.Cyan;

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
