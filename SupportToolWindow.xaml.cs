﻿using SupportTool.AppVersion;
using SupportTool.Command;
using SupportTool.Dreadnought;
using SupportTool.Logger;
using SupportTool.Ping;
using SupportTool.Tool;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;

namespace SupportTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class SupportToolWindow : Window
    {
        private static readonly short PingDelay = 1000;

        private Config config;
        private VersionChecker versionChecker;
        private FileAggregator fileAggregator;
        private CommandContainer commandContainer;
        private ToolContainer toolContainer;
        private BackgroundWorker CommandWorker = new BackgroundWorker();
        private BackgroundWorker PingWorker = new BackgroundWorker();
        private TextBoxLogger textBoxLogger;
        private Runner runner;
        private PingStorage pingStorage = new PingStorage("172.86.100.9", 240, (byte) (PingDelay / 1000));

        public SupportToolWindow()
        {
            InitializeComponent();
            
            Version versionInfo = Assembly.GetExecutingAssembly().GetName().Version;
            string version = String.Format("{0}.{1}.{2}", versionInfo.Major, versionInfo.Minor, versionInfo.Build);

            Title = String.Format("{0} - {1}", Title, version);
            
            CommandWorker.WorkerReportsProgress = true;
            CommandWorker.WorkerSupportsCancellation = true;
            CommandWorker.DoWork += new DoWorkEventHandler(StartAggregateData);
            CommandWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(FinishAggregateData);
            CommandWorker.ProgressChanged += new ProgressChangedEventHandler(ReportAggregateData);

            PingWorker.WorkerReportsProgress = true;
            PingWorker.WorkerSupportsCancellation = true;
            PingWorker.DoWork += new DoWorkEventHandler(StartPing);
            PingWorker.ProgressChanged += new ProgressChangedEventHandler(ReportPing);

            string home = Environment.GetEnvironmentVariable("userprofile");

            bool isElevated;
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }

            config = new Config(
                version,
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"DreadGame\Saved\Logs"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"DreadGame\Saved\Config\WindowsNoEditor"),
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                "DN_Support.zip",
                "https://raw.githubusercontent.com/dreadnought-friends/tool-versions/master/versions.xml",
                isElevated
            );

#if DEBUG
            config.ShowLogTimes = true;
#endif

            InMemoryLogger inMemoryLogger = new InMemoryLogger();
            textBoxLogger = new TextBoxLogger(config, inMemoryLogger, ExecutionOutput);
            versionChecker = new VersionChecker(config);
            BackgroundReportLogger backgroundReportLogger = new BackgroundReportLogger(config, inMemoryLogger, CommandWorker);
            fileAggregator = new FileAggregator(Path.Combine(Path.GetTempPath() + "DN_Support"));
            runner = new Runner(config, fileAggregator, backgroundReportLogger);

            commandContainer = new CommandContainer(config, ConfigurationOptions);

            commandContainer.Add(new TempDirectoryPreparation());
            commandContainer.Add(new HostDeveloper());
            commandContainer.Add(new CustomerSupportReadme());
            commandContainer.Add(new DxDiag());
            commandContainer.Add(new MsInfo());
            commandContainer.Add(new Connection(pingStorage));
            commandContainer.Add(new DreadnoughtLogs());
            commandContainer.Add(new DreadnoughtSettings());
            commandContainer.Add(new DreadnoughtCrashDumps());
            commandContainer.Add(new AggregatedFileCollector());
            commandContainer.Add(new Archiver());

            toolContainer = new ToolContainer(config, ToolsMenuItem);
            toolContainer.RegisterTool(new Tool.KeyboardSettings.ToolData(config, textBoxLogger));
            toolContainer.RegisterTool(new Tool.ChangeInstallationDirectory.ToolData(textBoxLogger));
            toolContainer.RegisterTool(new Tool.PingExport.ToolData(pingStorage, config, textBoxLogger));

            updateLatestInfo();
            RunPings();
        }

        private void updateLatestInfo()
        {
            DownloadNewVersionText.Text = "";
            textBoxLogger.Clear();

            try
            {
                VersionInfo info = versionChecker.getLatestVersionInfo();

                textBoxLogger.Log(info.MotdTitle);
                textBoxLogger.Log(info.MotdBody);

                if (!info.IsUpToDate)
                {
                    DownloadNewVersionLink.NavigateUri = new Uri(info.Url);
                    DownloadNewVersionText.Text = String.Format("Version {0} is available!", info.Version);
                }
            }
            catch (Exception e)
            {
                textBoxLogger.Log(String.Format("Unable to check for a new version: {0}", e.Message));
            }
        }

        private async Task RunPings()
        {
            while (true)
            {
                if (!PingWorker.IsBusy)
                {
                    PingWorker.RunWorkerAsync();
                }
                
                await Task.Delay(PingDelay);
            }
        }

        private void StartPing(object sender, DoWorkEventArgs e)
        { 
            PingWorker.ReportProgress(1, pingStorage.Ping());
        }

        private void ReportPing(object sender, ProgressChangedEventArgs e)
        {
            var pingResult = (PingResult)e.UserState;

            if (pingResult.Successful)
            {
                DisplayPing.Header = String.Format("Ping: {0}ms", pingResult.AveragePing);
                return;
            }

            DisplayPing.Header = "Ping: Unknown";
        }

        private void StartAggregateData(object sender, DoWorkEventArgs e)
        {
            runner.Run(commandContainer.Commands);
        }

        private void FinishAggregateData(object sender, RunWorkerCompletedEventArgs e)
        {
            StartAggregation.IsEnabled = true;
            commandContainer.Enable();
        }

        private void ReportAggregateData(object sender, ProgressChangedEventArgs e)
        {
            ExecutionOutput.AppendText(e.UserState.ToString() + Environment.NewLine);
            ExecutionOutput.ScrollToEnd();
        }

        private void StartAggregation_Click(object sender, RoutedEventArgs e)
        {
            textBoxLogger.Clear();
            commandContainer.Disable();
            StartAggregation.IsEnabled = false;
            CommandWorker.RunWorkerAsync();
        }

        private void OpenAggregatedFiles_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(fileAggregator.TempDir))
            {
                textBoxLogger.Log("No aggregated files found to show");
                return;
            }

            Process.Start(fileAggregator.TempDir);
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
        }

        private void OpenDreadnoughtInstallationDirectory_Click(object sender, RoutedEventArgs e)
        {
            if (null == config.DnInstallationDirectory)
            {
                textBoxLogger.Log("Could not reliably find the Dreadnought installation directory (Hint: try Tools > Change Installation Directory)");
                return;
            }

            var process = Process.Start(new ProcessStartInfo()
            {
                FileName = config.DnInstallationDirectory,
                UseShellExecute = true,
                Verb = "Open"
            });
        }

        private void OpenDocumentation_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/dreadnought-friends/support-tool/wiki"));
        }

        private void ShowMessageOfTheDay_Click(object sender, RoutedEventArgs e)
        {
            updateLatestInfo();
        }

        private void RunDreadnoughtDebugLauncher_Click(object sender, RoutedEventArgs e)
        {
            FileInfo launcherExecutable = new FileInfo(Path.Combine(config.DnInstallationDirectory, "DreadnoughtLauncher.exe"));

            if (!launcherExecutable.Exists)
            {
                textBoxLogger.Log("Could not reliably find the Dreadnought installation directory (Hint: try Tools > Change Installation Directory)");
                return;
            }

            Process process = DebugLauncher.CreateProcess(launcherExecutable.FullName);

            try
            {
                process.Start();
            }
            catch (Win32Exception ex)
            {
                if (ex.NativeErrorCode == 1223) // operation cancelled by user
                {
                    textBoxLogger.Log("Starting the debug launcher requires administrative permissions.");
                    return;
                }
            }
        }
    }
}
