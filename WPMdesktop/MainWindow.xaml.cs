using System;
using System.Windows;
using System.Windows.Controls;
using DesktopWPFAppLowLevelKeyboardHook;
using System.Windows.Threading;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System.IO;

namespace WPMdesktop
{
    public partial class MainWindow : Window
    {
        private System.Windows.Forms.NotifyIcon notifyIcon = new System.Windows.Forms.NotifyIcon();
        
        private Dictionary<Key, string> operations = new Dictionary<Key, string>();
        private Dictionary<Key, string> operationsShift = new Dictionary<Key, string>();

        private LowLevelKeyboardListener _listener;

        DispatcherTimer dt = new DispatcherTimer();

        Stopwatch stopWatch = new Stopwatch();
        TrayMenu trayMenu = new TrayMenu();
        Settings settings = new Settings();

        Regex regex = new Regex("(\\w+|\\w+\\s+)$+|[^a-zA-Z0-9]$+");

        string currentTime = string.Empty;

        private bool startedTyping = false;
        private bool clickedStart = false;
        private bool isStopped = false;
        private bool timerRestarted = false;
        private bool idlePauseActive;
        private bool usingWordAmount;
        private bool usingTime;
        private bool validationPassed;
        private bool isFinished;

        private int wordAmount;
        private int timeAmount;
        private int wordCount;
        private long lastKeyPressInterval;

        private double inputSize;
        private double wpm = 0;
        

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            addCustomOperations();

        _listener = new LowLevelKeyboardListener();
            this.textBox_DisplayKeyboardInput.IsEnabled = false;
            this.StopButton.IsEnabled = false;
            this.textBoxWordAmount.IsEnabled = false;
            this.textBoxTime.IsEnabled = false;
            _listener.HookKeyboard();
            textBox_DisplayKeyboardInput.IsReadOnly = true;

            dt.Tick += new EventHandler(dt_Tick);
            dt.Interval = new TimeSpan(0, 0, 0, 0, 1);

            //CONTEXT MENU
            
            var myIcon = Properties.Resources.logo;
            System.Windows.Forms.NotifyIcon icon = new System.Windows.Forms.NotifyIcon();
            Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/WPMdesktop;component/Resources/logo.ico")).Stream;
            notifyIcon.Icon = new System.Drawing.Icon(iconStream);
            iconStream.Close();
            //notifyIcon.Icon = new System.Drawing.Icon(Properties.Resources.logo.ToString());

            notifyIcon.Visible = true;

            System.Windows.Forms.ContextMenu notifyIconContextMenu = new System.Windows.Forms.ContextMenu();
            notifyIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(notifyIcon_DoubleClick);

            notifyIconContextMenu.MenuItems.Add("Toggle Traymenu", new EventHandler(HideReveal));
            notifyIconContextMenu.MenuItems.Add("-");
            notifyIconContextMenu.MenuItems.Add("Open", new EventHandler(Open));
            notifyIconContextMenu.MenuItems.Add("Stop", new EventHandler(Stop));
            notifyIconContextMenu.MenuItems.Add("Quit", new EventHandler(Close));
            
            notifyIcon.ContextMenu = notifyIconContextMenu;
        }

        private void HideReveal(object sender, EventArgs e)
        {
            if (settings.trayMenuEnabled && trayMenu.IsLoaded)
            {
                if (!trayMenu.IsVisible)
                {
                    trayMenu.Show();
                }
                else
                {
                    trayMenu.Hide();
                }
            }
        }

        private void Open(object sender, EventArgs e)
        {
            WindowState = WindowState.Normal;
            this.Focus();

        }
        private void Stop(object sender, EventArgs e)
        {
            if (startedTyping)
            {
                stopReading();
            }
        }
        private void Close(object sender, EventArgs e)
        {
            this.Close();
        }

        private void notifyIcon_DoubleClick(object Sender, EventArgs e)
        {
            WindowState = WindowState.Normal;
            this.Focus();
            if (IsVisible)
            {
                Activate();
            }
            else
            {
                Show();
            }
        }


            //CUSTOM OPERATIONS
            void addCustomOperations()
        {
            operations.Add(Key.Space, " ");
            operations.Add(Key.OemComma, ",");
            operations.Add(Key.OemPeriod, ".");
            operations.Add(Key.Oem6, "]");
            operations.Add(Key.OemSemicolon, ";");
            operations.Add(Key.OemQuotes, "'");
            operations.Add(Key.OemPipe, "\\");
            operations.Add(Key.OemQuestion,"/");
            operations.Add(Key.OemOpenBrackets, "[");
            operations.Add(Key.OemMinus, "-");
            operations.Add(Key.OemBackslash, "\\");
            operations.Add(Key.OemPlus, "=");
            operations.Add(Key.Oem3, "`");
            for (int i = 0; i <= 9; i++)
            {
                operations.Add(Key.D0 + i, i.ToString());
            }

            operationsShift.Add(Key.OemSemicolon, ":");
            operationsShift.Add(Key.OemOpenBrackets, "{");
            operationsShift.Add(Key.OemCloseBrackets, "}");
            operationsShift.Add(Key.OemQuotes, "\"");
            operationsShift.Add(Key.OemQuestion, "?");
            operationsShift.Add(Key.OemComma, "<");
            operationsShift.Add(Key.OemPeriod, ">");
            operationsShift.Add(Key.D1, "!");
            operationsShift.Add(Key.D2, "@");
            operationsShift.Add(Key.D3, "#");
            operationsShift.Add(Key.D4, "$");
            operationsShift.Add(Key.D5, "%");
            operationsShift.Add(Key.D6, "^");
            operationsShift.Add(Key.D7, "&");
            operationsShift.Add(Key.D8, "*");
            operationsShift.Add(Key.D9, "(");
            operationsShift.Add(Key.D0, ")");
            operationsShift.Add(Key.OemMinus, "_");
            operationsShift.Add(Key.OemPlus, "+");
            operationsShift.Add(Key.OemPipe, "|");
            operationsShift.Add(Key.Oem3, "~");

        }
        //TIMER TICK
        void dt_Tick(object sender, EventArgs e)
        {
            if (stopWatch.IsRunning)
            {
                TimeSpan ts = stopWatch.Elapsed;

                ClockTextBlock.Text = String.Format("{0:00}:{1:00}:{2:00}",
                    ts.Hours, ts.Minutes, ts.Seconds);

                trayMenu.ClockTextBlock.Text = String.Format("{0:00}:{1:00}:{2:00}",
                    ts.Hours, ts.Minutes, ts.Seconds);

            }
            if (usingTime)
            {
                if (stopWatch.ElapsedMilliseconds / 1000 >= timeAmount)
                {
                    this.stopReading();
                    isFinished = true;
                 }
            }
            //IDLEPAUSE
            if(settings.idlePause)
            {
                if(startedTyping)
                {
                    if (stopWatch.ElapsedMilliseconds - lastKeyPressInterval >= settings.idleTimerInterval*1000)
                    {
                        idlePauseActive = true;
                        trayMenu.IdlePauseTimerText.Content = "Idle paused";
                        stopWatch.Stop();
                    }
                }
            }

        }
        //STOP READING
        void stopReading()
        {
            StopButton.IsEnabled = false;
            if (stopWatch.IsRunning)
            {
                stopWatch.Stop();
            }
            this.textBox_DisplayKeyboardInput.IsEnabled = false;
            this.StartButton.IsEnabled = true;
            _listener.OnKeyPressed -= _listener_OnKeyPressed;
            isStopped = true;

            if (textBox_DisplayKeyboardInput.Text.Length / 5 >= 1)
            {
                wpm = (this.textBox_DisplayKeyboardInput.Text.Length / 5) * (60f / (stopWatch.ElapsedMilliseconds / 1000f));

                this.label_status_text.Content = "Stopped. " + stopWatch.ElapsedMilliseconds / 1000 + " seconds WPM: "  + Math.Round((Decimal)wpm, 1, MidpointRounding.AwayFromZero) + " (" + textBox_DisplayKeyboardInput.Text.Length / 5 + " counted words)";
                trayMenu.WPMLabel.Content = Math.Round((Decimal)wpm, 1, MidpointRounding.AwayFromZero);
                trayMenu.WordLabel.Content = textBox_DisplayKeyboardInput.Text.Length / 5;

                startedTyping = false;
            }
            else
            {
                this.label_status_text.Content = "Stopped. You need to type at least 5 characters!";
            }
            if (!isFinished)
            {
                string[] words = textBox_DisplayKeyboardInput.Text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string word in words)
                {
                    wordCount++;
                }
                wordCountLabel.Content = wordCount;
            }
            trayMenu.IdlePauseTimerText.Content = "Paused";
        }
        //START READING
        void startReading()
        {
            if (isFinished)
            {
                textBox_DisplayKeyboardInput.Clear();
            }
            stopWatch.Stop();
            if (timerRestarted)
            {
                if (!startedTyping)
                {
                    stopWatch.Reset();
                    stopWatch.Stop();
                    timerRestarted = false;

                }
            }

            StopButton.IsEnabled = true;
            
            this.StartButton.IsEnabled = false;
            this.textBoxWordAmount.IsEnabled = false;
            this.checkBoxWordAmount.IsEnabled = false;
            this.textBoxTime.IsEnabled = false;
            this.checkBoxTime.IsEnabled = false;
            this.textBox_DisplayKeyboardInput.IsEnabled = true;
            _listener.OnKeyPressed += _listener_OnKeyPressed;
            clickedStart = true;
            this.label_status_text.Content = "Waiting for the user to start typing.";
            if (startedTyping)
            {
                if (isStopped)
                {
                    stopWatch.Start();
                    isStopped = false;
                    this.StopButton.IsEnabled = true;
                }
            }
        }
        //START BUTTON CLICK
        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (validationPassed)
            {   
                startReading();
                //trayMenu.ClockTextBlock.Visibility = Visibility.Visible;
                if (settings.trayMenuEnabled)
                {
                    var desktopWorkingArea = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
                    trayMenu.Left = desktopWorkingArea.Right - trayMenu.Width;
                    trayMenu.Top = desktopWorkingArea.Bottom - trayMenu.Height;
                    trayMenu.Topmost = true;
                    trayMenu.IdlePauseTimerText.Content = "Idle";
                    trayMenu.Show();
                }

            }
            else
            {
                label_status_text.Content = "You need to validate your method of counting!";
            }
        }
        //STOP BUTTON CLICK
        public void StopButton_Click(object sender, RoutedEventArgs e)
        {
            stopReading();
        }
        //RESET BUTTON CLICK
        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            _listener.OnKeyPressed -= _listener_OnKeyPressed;
            if (stopWatch.IsRunning)
            {
                stopReading();
            }
            stopWatch.Reset();
            trayMenu.ClockTextBlock.Visibility = Visibility.Collapsed;
            ClockTextBlock.Visibility = Visibility.Collapsed;


            trayMenu.WordLabel.Content = 0;
            trayMenu.WPMLabel.Content = 0;

            this.checkBoxWordAmount.IsEnabled = true;
            this.checkBoxTime.IsEnabled = true;
            this.textBox_DisplayKeyboardInput.Clear();
            this.textBox_DisplayKeyboardInput.IsEnabled = false;
            this.StopButton.IsEnabled = false;
            this.StartButton.IsEnabled = true;
            this.checkBoxTime.IsChecked = false;
            this.checkBoxWordAmount.IsChecked = false;
            this.label_status_text.Content = "Reset.";
            wordCount = 0;
            this.wordCountLabel.Content = "";
            this.textBoxWordAmount.IsEnabled = false;
            this.textBoxTime.IsEnabled = false;
            this.textBoxTime.Clear();
            this.textBoxWordAmount.Clear();

            trayMenu.IdlePauseTimerText.Content = "Idle";

            validationPassed = false;


            isStopped = true;
            clickedStart = false;
            timerRestarted = true;
            startedTyping = false;
            isFinished = false;
            textBox_DisplayKeyboardInput.IsReadOnly = true;

            wpm = 0;
        }

        //KEYPRESS READING
        void _listener_OnKeyPressed(object sender, KeyPressedArgs e)
        {
            if (e.downPress)
            {
                lastKeyPressInterval = stopWatch.ElapsedMilliseconds;
                //IF idlePause ACTIVE
                if (idlePauseActive)
                {
                    idlePauseActive = false;
                    trayMenu.IdlePauseTimerText.Content = "Running";
                    stopWatch.Start();
                }
                if (!idlePauseActive)
                {
                    if (e.KeyPressed.Count != 0)
                    {
                        if (e.KeyPressed.Last() == Key.Return)
                        {
                            this.textBox_DisplayKeyboardInput.AppendText(Environment.NewLine);

                        }
                    }

                    if (e.KeyPressed.Count != 0)
                    {
                        if (e.KeyPressed.Last() == Key.Back)
                        {
                            //CTRL + BACKSPACE
                            if (e.Modifiers.Count != 0 && e.Modifiers.Last() == Key.LeftCtrl)
                            {
                                
                                this.textBox_DisplayKeyboardInput.Text = textBox_DisplayKeyboardInput.Text.Substring(0, textBox_DisplayKeyboardInput.Text.Length - regex.Match(textBox_DisplayKeyboardInput.Text).Length);
                            }
                            //BACKSPACE ONLY
                            if (textBox_DisplayKeyboardInput.Text.Length >= 1)
                            {
                                this.textBox_DisplayKeyboardInput.Text = textBox_DisplayKeyboardInput.Text.Substring(0, textBox_DisplayKeyboardInput.Text.Length - 1);
                            }
                        }
                    }
                    //KEY AND NO MODIFIER
                    if (e.KeyPressed.Count != 0)
                    {
                        if (e.KeyPressed.Last() != Key.Back && e.KeyPressed.Last() != Key.Return && e.KeyPressed.Last() != Key.F2)
                        {
                            if (operations.ContainsKey(e.KeyPressed.Last()))
                            {
                                if (operationsShift.ContainsKey(e.KeyPressed.Last()) && e.Modifiers.Count != 0)
                                {

                                }
                                else
                                {
                                    this.textBox_DisplayKeyboardInput.Text += operations[e.KeyPressed.Last()];
                                }
                            }
                            else
                            {
                                if (e.Modifiers.Count != 0)
                                {
                                    if (!e.lastKeyisModifier && (e.Modifiers.Last() == Key.LeftShift || e.Modifiers.Last() == Key.RightShift))
                                    {
                                        this.textBox_DisplayKeyboardInput.Text += e.KeyPressed.Last().ToString();
                                    }
                                }
                                else
                                {
                                    this.textBox_DisplayKeyboardInput.Text += e.KeyPressed.Last().ToString().ToLower();
                                }
                            }
                        }
                        if (e.KeyPressed.Last() == Key.F2)
                        {
                            stopReading();
                        }
                    }
                    //MODIFIER KEY PLUS KEY
                    if (e.KeyPressed.Count != 0 && e.Modifiers.Count != 0)
                    {
                        if (e.KeyPressed.Last() != Key.Back && e.KeyPressed.Last() != Key.Return)
                        {
                            if (operationsShift.ContainsKey(e.KeyPressed.Last()))
                            {
                                {
                                    this.textBox_DisplayKeyboardInput.Text += operationsShift[e.KeyPressed.Last()];
                                    if (e.Modifiers.Last() == (Key.LeftAlt | Key.RightAlt))
                                    {
                                        e.KeyPressed.Clear();
                                    }
                                }
                            }
                        }
                        
                    }

                    //TRAYMENU READING
                    inputSize = textBox_DisplayKeyboardInput.Text.Length;

                    if (inputSize / 5 != 0)
                    {
                        wpm = (inputSize / 5) * (60f / (stopWatch.ElapsedMilliseconds / 1000f));
                        trayMenu.WPMLabel.Content = Math.Round(wpm, MidpointRounding.AwayFromZero);
                        trayMenu.WordLabel.Content = textBox_DisplayKeyboardInput.Text.Length / 5;
                        ClockTextBlock.Visibility = Visibility.Visible;
                        trayMenu.ClockTextBlock.Visibility = Visibility.Visible;
                    }

                    //VALIDATE
                    if (usingWordAmount)
                    {
                        if (inputSize / 5 >= wordAmount)
                        {
                            this.stopReading();
                            isFinished = true;
                            this.StartButton.IsEnabled = false;
                        }
                    }

                }

            }
        }

        //READ TEXTBOX CONTENT
        private void textBox_DisplayKeyboardInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (clickedStart)
            {
                if (!startedTyping)
                {
                    stopWatch.Start();
                    dt.Start();
                    startedTyping = true;
                    this.label_status_text.Content = "Running.";
                    trayMenu.IdlePauseTimerText.Content = "Running";
                }
            }
        }
        //TIME AND WORD AMOUNT TEXT AND CHECKBOX CONTENT
        private void checkBoxWordAmount_Checked(object sender, RoutedEventArgs e)
        {
            usingWordAmount = true;
            usingTime = false;
            checkBoxTime.IsEnabled = false;
            checkBoxTime.IsChecked = false;
            textBoxWordAmount.IsEnabled = true;
            textBoxTime.IsEnabled = false;

            textBoxTime.Clear();
            timeValidationLabel.Content = "";

            textBoxWordAmount.Focus();


        }

        private void checkBoxTime_Checked(object sender, RoutedEventArgs e)
        {
            usingTime = true;
            usingWordAmount = false;
            checkBoxWordAmount.IsEnabled = false;
            checkBoxWordAmount.IsChecked = false;
            textBoxTime.IsEnabled = true;
            textBoxWordAmount.IsEnabled = false;

            textBoxWordAmount.Clear();
            wordAmountValidationLabel.Content = "";

            textBoxTime.Focus();
        }

        private void textBoxWordAmount_LostFocus(object sender, RoutedEventArgs e)
        {
            wordAmountValidationLabel.Content = "";
            if (usingWordAmount)
            {
                validateTextBox("usingWordAmount");
                if (textBoxWordAmount.Text.Length > 0 && validationPassed)
                {
                    wordAmount = int.Parse(textBoxWordAmount.Text);
                }
            }
        }

        private void textBoxTime_LostFocus(object sender, RoutedEventArgs e)
        {
            timeValidationLabel.Content = "";
            if (usingTime)
            {
                validateTextBox("usingTime");
                if (textBoxTime.Text.Length > 0 && validationPassed)
                {
                    timeAmount = int.Parse(textBoxTime.Text);
                }
            }
        }

        private void validateTextBox(string caller)
        {
            int parsedValue;

            if (caller == "usingWordAmount")
            {
                if (int.TryParse(textBoxWordAmount.Text, out parsedValue))
                {
                    if (Convert.ToInt32(textBoxWordAmount.Text) <= 0 ) {
                        wordAmountValidationLabel.Content = "Value is 0 or less!";
                    }
                    else
                    {
                        validationPassed = true;
                    }
                    
                }
                else
                {
                    {
                        wordAmountValidationLabel.Content = "Must contain a value of numbers!";
                    }
                }
            }
            else if (caller == "usingTime")
            {
                if (int.TryParse(textBoxTime.Text, out parsedValue))
                {
                    if (Convert.ToInt32(textBoxTime.Text) <= 0)
                    {
                        timeValidationLabel.Content = "Value is 0 or less!";
                    }
                    else
                    {
                        validationPassed = true;
                    }
                }
                else
                {
                    timeValidationLabel.Content = "Must contain a value of numbers!";
                }
            }

        }

        private void checkBoxWordAmount_Unchecked(object sender, RoutedEventArgs e)
        {
            usingWordAmount = false;
            checkBoxTime.IsEnabled = true;
            validationPassed = false;
            textBoxWordAmount.IsEnabled = false;
        }

        private void checkBoxTime_Unchecked(object sender, RoutedEventArgs e)
        {
            usingTime = false;
            checkBoxWordAmount.IsEnabled = true;
            validationPassed = false;
            textBoxTime.IsEnabled = false;
        }

        private void aboutButton_Click(object sender, RoutedEventArgs e)
        {
            About about = new About();
            about.ShowDialog();
        }

        //WINDOW CLOSING
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
            notifyIcon.Visible = false;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void Title_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void SettingsIcon_Click(object sender, RoutedEventArgs e)
        {
            settings.Show();
        }
    }
}