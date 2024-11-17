using FunctionalLibrary;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace TaskManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<AllTasks> Processes { get; set; }
        private static Mutex mutex = new Mutex(false, "Global\\MyMutex");
        private static SemaphoreSlim _semaphore = new SemaphoreSlim(2, 2);
        private static SemaphoreSlim _semaphore_all_pool = new SemaphoreSlim(3, 3);
        public Timer _timer { get; set; }
        //public BackgroundWorker backgroundWorker { get; set; }
        public readonly object backgroundWorkerLock = new object();
        public string input_text_task { get; private set; }
        public ObservableCollection<AllTasks> ActionProcesses { get; set; }
        private ProcessPriorityClass? priority { get; set; }
        private AllTasks current_row { get; set; }
        //private bool First_finding_all_processes { get; set; } = false;
        public MainWindow()
        {
            InitializeComponent();
            Processes = new ObservableCollection<AllTasks>();
            ActionProcesses = new ObservableCollection<AllTasks>();
            _timer = new Timer(TimerCallback, null, 0, 2000);
            monitoring.ItemsSource = Processes;
            action_tasks.ItemsSource = ActionProcesses;
            this.Closed += MainWindow_Closed;
            input_task.LostFocus += Input_task_LostFocus;
            Enter.Click += Enter_Click; ;
        }

        private async void Enter_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(input_text_task))
            {
                ActionProcesses.Clear();
                await Task.Run(() => BackgroundWorkingAsync());
            }
        }



        private void Input_task_LostFocus(object sender, RoutedEventArgs e)
        {
            var task = sender as TextBox;
            string some_str = task.Text;
            if (!string.IsNullOrEmpty(some_str))
            {
                input_text_task = some_str;
            }

        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            _timer.Dispose();
            this.Close();
        }

        public async Task BackgroundWorkingAsync()
        {
            if (Regex.IsMatch(input_text_task, @"^\D+$"))
            {
                Process[] processes = Process.GetProcessesByName(input_text_task);
                if (processes.Length == 0)
                {
                    MessageBox.Show($"No processes found with the name '{input_text_task}'");
                    return;
                }
                foreach (Process process in processes)
                {


                    AllTasks some_process = await Task.Run(() => FindingInformation(process));
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        ActionProcesses.Add(some_process);
                        some_process.ParentsCollection = ActionProcesses;
                    });


                }
            }

            else if (int.TryParse(input_text_task, out int id_process))
            {
                Process process = Process.GetProcessById(id_process);
                if (process == null)
                {
                    MessageBox.Show($"No processes found with the name '{input_text_task}'");
                    return;
                }
                AllTasks some_process = await Task.Run(() => FindingInformation(process));
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ActionProcesses.Add(some_process);
                    some_process.ParentsCollection = ActionProcesses;
                });
            }
        }
        public AllTasks FindingInformation(Process process)
        {
            if (process == null)
            {

                return new AllTasks { Name = "Unknown", ID = -1, status_prog = true, Memory = "0.0" };

            }
            else
            {
                AllTasks task = new AllTasks();
                try
                {
                    task.ID = process.Id;
                    task.Name = process.ProcessName;
                    task.status_prog = process.HasExited;
                    task.Memory = (process.PrivateMemorySize64 / 1024 / 1024) + " MB";
                    task.Priority = process.PriorityClass.ToString();
                    return task;
                }
                catch (Exception ex)
                {
                    return new AllTasks { Name = $"{ex.Message}", ID = -1, status_prog = true, Memory = "0.0" };
                }
            }
        }
        private async Task BackgroundWorker_DoWorkAsync()
        {

            Process[] processes = Process.GetProcesses();
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            PerformanceCounter bytesSentCounter = new PerformanceCounter();
            PerformanceCounter bytesReceivedCounter = new PerformanceCounter();

            if (networkInterfaces.Length != 0)
            {
                foreach (NetworkInterface some_interface in networkInterfaces)
                {
                    if (some_interface.OperationalStatus == OperationalStatus.Up &&
                        (some_interface.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                        some_interface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211) &&
                        !some_interface.Description.Contains("VirtualBox") &&
                        !some_interface.Description.Contains("VMware") &&
                        !some_interface.Description.Contains("Hyper-V"))
                    {
                        string first_network = some_interface.Description;
                        PerformanceCounterCategory category = new PerformanceCounterCategory("Network Interface");

                        if (Array.Exists(category.GetInstanceNames(), name => name == first_network))
                        {
                            bytesSentCounter = new PerformanceCounter("Network Interface", "Bytes Sent/sec", first_network);
                            bytesReceivedCounter = new PerformanceCounter("Network Interface", "Bytes Received/sec", first_network);
                            break;
                        }
                        else
                        {
                            MessageBox.Show($"Interface {first_network} not exitst in Network Interface");
                        }

                    }
                }


            }
            if (processes.Length != 0)
            {
                await _semaphore.WaitAsync();
                foreach (var process in processes)
                {
                    try
                    {

                        PerformanceCounter cpuCounter = new PerformanceCounter("Process", "% Processor Time", process.ProcessName);
                        PerformanceCounter diskCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");

                        AllTasks allTasks = new AllTasks
                        {
                            ID = process.Id,
                            StartTimer = process.StartTime.ToString("HH:mm:ss"),
                            TotalProcessorTime = process.TotalProcessorTime.ToString(@"hh\:mm\:ss\.fff"),
                            ThreadCount = process.Threads.Count,
                            Name = process.ProcessName,
                            status_prog = process.HasExited,
                            Priority = process.PriorityClass.ToString(),
                            CPU = cpuCounter.NextValue(),
                            Memory = (process.PrivateMemorySize64 / 1024 / 1024) + " MB",
                            Disk = diskCounter.NextValue()
                        };
                        if (networkInterfaces.Length != 0)
                        {
                            allTasks.byteSent = bytesSentCounter.NextValue() / (1024 * 1024);
                            allTasks.byteRecived = bytesReceivedCounter.NextValue() / (1024 * 1024);
                        }
                        else
                        {
                            allTasks.byteSent = 0.0f;
                            allTasks.byteRecived = 0.0f;
                        }
                        
                        if (Application.Current != null && !Application.Current.Dispatcher.HasShutdownStarted)
                        {
                            bool find_task = false;
                            for (int i = 0; i < Processes.Count; i++)
                            {
                                if (allTasks.ID == Processes[i].ID)
                                {
                                    find_task = true;
                                    await Application.Current.Dispatcher.InvokeAsync(() =>
                                    {
                                        if (Processes[i].StartTimer != allTasks.StartTimer)
                                        {
                                            Processes[i].StartTimer = allTasks.StartTimer;
                                        }
                                        if (Processes[i].TotalProcessorTime != allTasks.TotalProcessorTime)
                                        {
                                            Processes[i].TotalProcessorTime = allTasks.TotalProcessorTime;
                                        }
                                        if (Processes[i].ThreadCount != allTasks.ThreadCount)
                                        {
                                            Processes[i].ThreadCount = allTasks.ThreadCount;
                                        }
                                        if (Processes[i].Name != allTasks.Name)
                                        {
                                            Processes[i].Name = allTasks.Name;
                                        }
                                        if (Processes[i].status_prog != allTasks.status_prog)
                                        {
                                            Processes[i].status_prog = allTasks.status_prog;
                                        }
                                        if (Processes[i].Priority != allTasks.Priority)
                                        {
                                            Processes[i].Priority = allTasks.Priority;
                                        }
                                        if (Processes[i].CPU != allTasks.CPU)
                                        {
                                            Processes[i].CPU = allTasks.CPU;
                                        }
                                        if (Processes[i].Memory != allTasks.Memory)
                                        {
                                            Processes[i].Memory = allTasks.Memory;
                                        }
                                        if (Processes[i].Disk != allTasks.Disk)
                                        {
                                            Processes[i].Disk = allTasks.Disk;
                                        }
                                        if (Processes[i].byteSent != allTasks.byteSent)
                                        {
                                            Processes[i].byteSent = allTasks.byteSent;
                                        }
                                        if (Processes[i].byteRecived != allTasks.byteRecived)
                                        {
                                            Processes[i].byteRecived = allTasks.byteRecived;
                                        }
                                        if (Processes[i].Network != allTasks.Network)
                                        {
                                            Processes[i].Network = allTasks.Network;
                                        }
                                    });
                                    

                                }
                            }
                            if (find_task == false)
                            {
                                await Application.Current.Dispatcher.InvokeAsync(() =>
                                {
                                    
                                    Processes.Add(allTasks);
                                    allTasks.ParentsCollection = Processes;
                                });
                                

                            }
                            await Task.Run(() =>
                            {
                                for (int i = Processes.Count - 1; i >= 0; i--)
                                {
                                    int index_processes = i;
                                    bool found = false;
                                    for (int j = processes.Length - 1; j >= 0; j--)
                                    {
                                        if (Processes[i].ID == processes[j].Id)
                                        {
                                            found = true;
                                            break;
                                        }
                                        else
                                        {
                                            found = false;
                                            
                                        }
                                    }
                                    if (!found)
                                    {
                                        Application.Current.Dispatcher.InvokeAsync(() =>
                                        {
                                            Processes.RemoveAt(index_processes);
                                        });
                                        
                                    }
                                }
                            });
                            
                        }
                    }
                    catch (Exception)
                    {
                        
                        //MessageBox.Show($"Сталася помилка під час отримання даних про процеси на цьому комп'ютері {ex.Message}");
                        //return new List<AllTasks> { ID = -1, StartTimer = "00:00:00", TotalProcessorTime = "00:00:00", ThreadCount = 0, Name = "Error", status_prog = true, Priority = "none", CPU = 0.0f, Memory = "0.0 MB", Disk = 0.0f, byteSent = 0.0f, byteRecived = 0.0f };
                    }
                    finally
                    {
                        
                    }
                }
                _semaphore.Release();

            }


        }
        


        
        private async void TimerCallback(object sender)
        {
            await _semaphore_all_pool.WaitAsync();
            var task1 = Task.Run(async () => 
            {
               
                await BackgroundWorker_DoWorkAsync();
               
            });
            _semaphore_all_pool.Release();
        }
        public void PauseTimer()
        {
            _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }
        
        public void ResumeTimer()
        {
            _timer.Change(0, 2000);
        }
        private async void choose_action_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            ComboBoxItem selectedItem = comboBox.SelectedItem as ComboBoxItem;
            var dataGridRow = comboBox?.DataContext as AllTasks;
            current_row = dataGridRow;
            
            if (dataGridRow != null) 
            {
                //var itemsSource = action_tasks.ItemsSource as ObservableCollection<AllTasks>;
                var itemsSource = dataGridRow.ParentsCollection;
                if (itemsSource != null)
                {
                    if (selectedItem != null)
                    {
                        if (selectedItem.Content.ToString() == "End task")
                        {
                            MessageBoxResult result = MessageBox.Show("Ви точно хочете завершити цей процес?", "Виберіть дію", MessageBoxButton.YesNo, MessageBoxImage.Question);
                            if (result == MessageBoxResult.Yes)
                            {
                                int id_process = dataGridRow.ID;
                                bool test_to_end = await Task.Run(() => EndProcess(id_process));
                                if (test_to_end)
                                {
                                    //AllTasks taskRemove = ActionProcesses.FirstOrDefault(t => t.ID == id_process);
                                    //ActionProcesses.Remove(taskRemove);
                                    itemsSource.Remove(dataGridRow);
                                    MessageBox.Show("Process is succesfull finish work!!!:)");
                                }
                                else
                                {
                                    MessageBox.Show("Process isn`t finished!!!:(");
                                }
                            }
                        }
                        else if (selectedItem.Content.ToString() == "Edit priority")
                        {
                            if(selectedItem.ContextMenu != null)
                            {
                                selectedItem.ContextMenu.IsOpen = true;
                            }
                        }
                        else if (selectedItem.Content.ToString() == "Delete")
                        {
                            itemsSource.Remove(dataGridRow);
                        }
                    }
                }
            }
            
        }
        public bool EndProcess(int id)
        {
            bool test = false;
            try
            {
                Process process = Process.GetProcessById(id);
                process.Kill();
                return test = true;
            }
            catch (Exception)
            {
                return test;
            }
        }
        public bool EditPriority(int id)
        {
            bool test = false;
            try
            {
                Process process = Process.GetProcessById(id);
                process.PriorityClass = (ProcessPriorityClass)priority;
                var itemToUpdate = ActionProcesses.FirstOrDefault(t => t.ID == id);
                if(itemToUpdate != null)
                {
                    itemToUpdate.Priority = priority.ToString();
                }
                return test = true;
            }
            catch (Exception)
            {
                return test;
            }
        }
        //SOME_PRIORITY для того щоб змінити пріорітет процеса.

        private async void MenuItem_Click_Priority(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                if (menuItem.Header.ToString() == "Idle")
                {
                    priority = ProcessPriorityClass.Idle;
                }
                else if (menuItem.Header.ToString() == "BelowNormal")
                {
                    priority = ProcessPriorityClass.BelowNormal;
                }
                else if (menuItem.Header.ToString() == "Normal")
                {
                    priority = ProcessPriorityClass.Normal;
                }
                else if (menuItem.Header.ToString() == "AboveNormal")
                {
                    priority = ProcessPriorityClass.AboveNormal;
                }
                else if (menuItem.Header.ToString() == "High")
                {
                    priority = ProcessPriorityClass.High;
                }
                else if (menuItem.Header.ToString() == "RealTime")
                {
                    priority = ProcessPriorityClass.RealTime;
                }
                if (priority != null)
                {
                    //var contextMenu = menuItem?.Parent as ContextMenu;
                    AllTasks dataItem = new AllTasks();
                    dataItem = current_row;
                    if (dataItem != null)
                    {
                        MessageBoxResult result = MessageBox.Show("Ви точно хочете пріоритет цього процесу? ", "Виберіть дію", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.Yes)
                        {
                            int id_process = dataItem.ID;
                            bool test = await Task.Run(() => EditPriority(id_process));
                            if (test)
                            {
                                MessageBox.Show("Process priority is succesfull editing!!!:)");

                            }
                            else
                            {
                                MessageBox.Show("Process priority isn`t editing!!!:(");
                            }
                        }

                    }


                }
                else
                {
                    return;
                }


            }

        }
        private T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if(current is T)
                {
                    return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }
}
