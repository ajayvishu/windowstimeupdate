using Microsoft.Win32.TaskScheduler;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WindowsTimeUpdate
{
    public partial class Form1 : Form
    {
        private Timer timer;

        public Form1()
        {
            InitializeComponent();

            //Task Scheduler Create
            try
            {
                CreateTaskScheduler();
            }
            catch { }
        }

        private void SyncLocalTime()
        {
            try
            {
                using (var client = new WebClient())
                {
                    string response = client.DownloadString("https://worldtimeapi.org/api/timezone/Asia/Kolkata");

                    // Parse the JSON response using Newtonsoft.Json
                    dynamic jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(response);
                    string dateTimeString = jsonResponse.datetime;

                    DateTime dateTime = Convert.ToDateTime(dateTimeString).AddMinutes(-330);

                    // Set the system date and time directly
                    SetSystemTime(dateTime);

                    timer.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void SetSystemTime(DateTime newTime)
        {
            SYSTEMTIME systemTime = new SYSTEMTIME
            {
                wYear = (ushort)newTime.Year,
                wMonth = (ushort)newTime.Month,
                wDay = (ushort)newTime.Day,
                wHour = (ushort)newTime.Hour,
                wMinute = (ushort)newTime.Minute,
                wSecond = (ushort)newTime.Second,
                wMilliseconds = 0
            };

            // Set the system time
            if (!SetSystemTime(ref systemTime))
            {
                int errorCode = Marshal.GetLastWin32Error();
                MessageBox.Show($"SetSystemTime failed. Error code: {errorCode}");
            }
            else
            {
                Console.WriteLine("System time updated successfully.");
                //Application.Exit();
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetSystemTime(ref SYSTEMTIME st);

        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEMTIME
        {
            public ushort wYear;
            public ushort wMonth;
            public ushort wDayOfWeek;
            public ushort wDay;
            public ushort wHour;
            public ushort wMinute;
            public ushort wSecond;
            public ushort wMilliseconds;
        }

        private void InitializeClock()
        {
            // Create and start a timer with an interval of 1000 milliseconds (1 second)
            timer = new Timer
            {
                Interval = 1000
            };
            timer.Tick += TimerTick;
            timer.Enabled = false;
            timer.Start();
        }

        private void TimerTick(object sender, EventArgs e)
        {
            // Update the label with the current time
            labelDigitalClock.Text = DateTime.Now.ToString("hh:mm:ss tt");
            this.Close();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Stop the timer when the form is closing
            timer.Stop();
            timer.Dispose();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                // Center the form on the screen
                this.StartPosition = FormStartPosition.CenterScreen;
                this.WindowState = FormWindowState.Minimized;
                // Disable Maximize and Minimize buttons
                this.MaximizeBox = false;
                this.MinimizeBox = true;
                InitializeClock();
                SyncLocalTime();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            try
            {
                if (WindowState == FormWindowState.Minimized)
                {
                    ShowInTaskbar = false;
                    notifyIcon1.Visible = true;
                    //notifyIcon1.ShowBalloonTip(1000);
                }
                else if (WindowState == FormWindowState.Normal)
                {
                    ShowInTaskbar = true;
                }
            }
            catch { }
        }

        private void NotifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            try
            {
                ShowInTaskbar = true;
                notifyIcon1.Visible = false;
                // Center the form on the screen
                Screen screen = Screen.FromControl(this);
                this.Location = new Point(
                    (screen.WorkingArea.Width - this.Width) / 2 + screen.WorkingArea.Left,
                    (screen.WorkingArea.Height - this.Height) / 2 + screen.WorkingArea.Top
                );
                this.WindowState = FormWindowState.Normal;
            }
            catch { }
        }

        private void CreateTaskScheduler()
        {
            try
            {
                // Specify your application's path
                string applicationPath = Process.GetCurrentProcess().MainModule.FileName;

                // Create a new task service
                using (TaskService ts = new TaskService())
                {
                    // Create a new task
                    TaskDefinition td = ts.NewTask();
                    td.RegistrationInfo.Description = "Windows Time Sync";

                    DateTime baseTime = DateTime.Now;
                    TimeSpan offset = TimeSpan.FromMinutes(30);

                    // Set the trigger to run every day, starting from now
                    DailyTrigger dailyTrigger = new DailyTrigger
                    {
                        DaysInterval = 1,
                        StartBoundary = baseTime.Date + offset,
                        Repetition = new RepetitionPattern(TimeSpan.FromMinutes(30), TimeSpan.FromDays(365))
                    };
                    td.Triggers.Add(dailyTrigger);

                    // Set the action to start your application with elevated privileges
                    td.Actions.Add(new ExecAction(applicationPath, null, null));

                    // Register the task, creating or updating if it already exists
                    ts.RootFolder.RegisterTaskDefinition("Windows Time Sync", td, TaskCreation.CreateOrUpdate, null, null);

                    Console.WriteLine("Scheduled task created successfully.");
                }

                // Run your application
                //Process.Start(applicationPath);
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }
    }
}
