using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using FunctionalLibrary;
using System.Windows.Threading;
using System.Text.RegularExpressions;
using System.IO;
using System.Data.SqlTypes;
using System.Security.Cryptography;
using System.ComponentModel;
using System.Threading;

namespace EncryptionProg
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public List<FileItem> List_Source { get; set; }
        private byte[] key_encrypte { get; set; }
        private string output_extension { get; set; }
        private BackgroundWorker backgroundWorkerEncrypt { get; set; }
        private BackgroundWorker backgroundWorkerDecipher { get; set; }
        public string ChoosenOperation { get; set; }
        private System.Timers.Timer _timer { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            _timer = new System.Timers.Timer(5000);
            _timer.Elapsed += _timer_ElapsedAsync;
            _timer.AutoReset = false;
            
            backgroundWorkerEncrypt = new BackgroundWorker();
            backgroundWorkerEncrypt.WorkerReportsProgress = true;
            backgroundWorkerEncrypt.DoWork += BackgroundWorker_DoWork_Encrypt;
            backgroundWorkerEncrypt.RunWorkerCompleted += BackgroundWorkerEncrypt_RunWorkerCompleted;
            backgroundWorkerEncrypt.ProgressChanged += BackgroundWorkerEncrypt_ProgressChanged;
            backgroundWorkerDecipher = new BackgroundWorker();
            backgroundWorkerDecipher.WorkerReportsProgress = true;
            backgroundWorkerDecipher.DoWork += BackgroundWorker_DoWork_Decipher;
            backgroundWorkerDecipher.RunWorkerCompleted += BackgroundWorkerDecipher_RunWorkerCompleted;
            backgroundWorkerDecipher.ProgressChanged += BackgroundWorkerDecipher_ProgressChanged;
            add.MouseDown += Add_MouseDown;
            List_Source = new List<FileItem>();
            list_items.ItemsSource = List_Source;
            remove_all.MouseDown += Remove_all_MouseDown;
            encrypt.Click += Encrypt_Click;
            decipher.Click += Decipher_Click;
            key.LostFocus += Key_LostFocus;
            output_format.LostFocus += Output_format_LostFocus;
        }

        private async void _timer_ElapsedAsync(object sender, System.Timers.ElapsedEventArgs e)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                progress_bar_decipher.Value = 0;
                progress_bar_encrypt.Value = 0;
            });
            
            

        }

        private void BackgroundWorkerDecipher_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progress_bar_decipher.Value = e.ProgressPercentage;
        }

        private void BackgroundWorkerEncrypt_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progress_bar_encrypt.Value = e.ProgressPercentage;
        }

        private void BackgroundWorkerDecipher_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                MessageBox.Show("Program is cancelled :)");
            }
            else if (e.Error != null)
            {
                MessageBox.Show("Program finished with Error :(");
            }
            else
            {
                MessageBox.Show("Program finished succesfull");
            }
        }

        private void BackgroundWorkerEncrypt_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                MessageBox.Show("Program is cancelled :)");
            }
            else if (e.Error != null)
            {
                MessageBox.Show("Program finished with Error :(");
            }
            else
            {
                MessageBox.Show("Program finished succesfull");
            }
        }

        private void BackgroundWorker_DoWork_Decipher(object sender, DoWorkEventArgs e)
        {

            var selectedItems = List_Source;
            if (selectedItems != null)
            {
                List<string> AllPath = new List<string>();
                List<string> OutputPath = new List<string>();
                foreach (var selectedItem in selectedItems)
                {
                    if (selectedItem.Path.IsFileEncryptedWithAes())
                    {
                        AllPath.Add(selectedItem.Path);
                    }
                }
                foreach (string selectedItem in AllPath)
                {
                    string[] partsOfDirection = selectedItem.Split('\\');
                    string partsLast = partsOfDirection[partsOfDirection.Length - 1];
                    string[] parts = partsLast.Split('.');
                    string part1 = parts[0];
                    string part2 = parts[1];
                    string newPath = parts[0] + "." + output_extension;                 
                    string result = "";
                    for (int i = 0; i < partsOfDirection.Length - 1; i++)
                    {
                        result += partsOfDirection[i] + @"\";
                    }
                    result += newPath;
                    OutputPath.Add(result);
                }
                if (AllPath.Count != 0)
                {

                    if (AllPath.DecryptFile(OutputPath, key_encrypte, backgroundWorkerDecipher, _timer))
                    {
                        foreach (string selectedItem in AllPath)
                        {
                            if (File.Exists(selectedItem))
                            {
                                File.Delete(selectedItem);
                            }
                        }
                        List_Source.Clear();
                        Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            CollectionViewSource.GetDefaultView(list_items.ItemsSource).Refresh();
                        });
                        
                    }
                    else
                    {
                        foreach (string createdItem in OutputPath)
                        {
                            if (File.Exists(createdItem))
                            {
                                File.Delete(createdItem);
                            }
                        }
                    }

                }
            }
        }



        private void BackgroundWorker_DoWork_Encrypt(object sender, DoWorkEventArgs e)
        {
            var selectedItems = List_Source;
            if (selectedItems != null)
            {
                List<string> AllPath = new List<string>();
                List<string> OutputPath = new List<string>();
                foreach (var selectedItem in selectedItems)
                {
                    if (!selectedItem.Path.IsFileEncryptedWithAes())
                    {
                        AllPath.Add(selectedItem.Path);
                    }
                }
                foreach (string selectedItem in AllPath)
                {
                    string[] partsOfDirection = selectedItem.Split('\\');
                    string partsLast = partsOfDirection[partsOfDirection.Length - 1];

                    string[] parts = partsLast.Split('.');
                    string part1 = parts[0];
                    string part2 = parts[1];
                    string newPath = parts[0] + "." + output_extension;
                    string result = "";
                    for (int i = 0; i < partsOfDirection.Length - 1; i++)
                    {
                        result += partsOfDirection[i] + @"\";
                    }
                    result += newPath;
                    OutputPath.Add(result);
                }
                if (AllPath.Count != 0)
                {

                    if (AllPath.EncrypteFiles(OutputPath, key_encrypte, backgroundWorkerEncrypt, _timer))
                    {
                        foreach (string selectedItem in AllPath)
                        {
                            if (File.Exists(selectedItem))
                            {
                                File.Delete(selectedItem);
                            }
                        }
                        List_Source.Clear();
                        Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            CollectionViewSource.GetDefaultView(list_items.ItemsSource).Refresh();
                        });
                        
                    }
                    else
                    {
                        foreach (string createdItem in OutputPath)
                        {
                            if (File.Exists(createdItem))
                            {
                                File.Delete(createdItem);
                            }
                        }
                    }
                }
            }

        }

        private void Output_format_LostFocus(object sender, RoutedEventArgs e)
        {
            string pattern = @"[\d\p{IsCyrillic}\.\,\!\@\#\$\%\^\&\*\(\)\-\=\+\~\`\{\}\[\]\|\\/;:'<>,\.\?]";
            if(!Regex.IsMatch(output_format.Text, pattern))
            {
                output_extension = output_format.Text;
            }
            else
            {
                output_format.Text = string.Empty;
                MessageBox.Show("Ви ввели не правильний формат для шифрування є такі види шифрування (crypt,enc,aes,cif,zipx,sfx,safe,dat,secure)");
            }
        }

        private void Key_LostFocus(object sender, RoutedEventArgs e)
        {
            if(key.Text.Length == 16 || key.Text.Length == 24 || key.Text.Length == 32)
            {
                key_encrypte = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(key.Text));
            }
            else
            {
                key.Text = string.Empty;
                MessageBox.Show("Ви ввели не правильний формат для ключа, він повинен бути довжиною 16,24,32 символи!!!");
            }
        }

        private void Decipher_Click(object sender, RoutedEventArgs e)
        {
            
            if (!backgroundWorkerDecipher.IsBusy)
            {
                if (!string.IsNullOrEmpty(key.Text) && !string.IsNullOrEmpty(output_extension))
                {
                    backgroundWorkerDecipher.RunWorkerAsync();
                }
                else
                {
                    if (string.IsNullOrEmpty(key.Text) && !string.IsNullOrEmpty(output_extension))
                    {
                        MessageBox.Show("Введіть ключ для розшифрування");
                        key.Focus();
                    }

                    else if (!string.IsNullOrEmpty(key.Text) && string.IsNullOrEmpty(output_extension))
                    {
                        MessageBox.Show("Введіть розширення для вивеленого файла");
                        output_format.Focus();
                    }
                    else if (string.IsNullOrEmpty(key.Text) && string.IsNullOrEmpty(output_extension))
                    {
                        MessageBox.Show("Введіть значення для ключа розшифрування та розширення для файлу");
                        key.Focus();
                    }
                }
            }
        }

        private void Encrypt_Click(object sender, RoutedEventArgs e)
        {
            ChoosenOperation = "Encrypt";
            if (!backgroundWorkerEncrypt.IsBusy)
            {
                if (!string.IsNullOrEmpty(key.Text) && !string.IsNullOrEmpty(output_extension))
                {
                    backgroundWorkerEncrypt.RunWorkerAsync();
                }
                else
                {
                    if (string.IsNullOrEmpty(key.Text) && !string.IsNullOrEmpty(output_extension))
                    {
                        MessageBox.Show("Введіть ключ для шифрування");
                        key.Focus();
                    }

                    else if (!string.IsNullOrEmpty(key.Text) && string.IsNullOrEmpty(output_extension))
                    {
                        MessageBox.Show("Введіть розширення для вивеленого файла");
                        output_format.Focus();
                    }
                    else if (string.IsNullOrEmpty(key.Text) && string.IsNullOrEmpty(output_extension))
                    {
                        MessageBox.Show("Введіть значення для ключа шифрування та для розширення для файлу");
                        key.Focus();
                    }
                }
            }
        }
        

        private void Remove_all_MouseDown(object sender, MouseButtonEventArgs e)
        {
            List_Source.RemoveAllItems(list_items);
            
        }

        private void Add_MouseDown(object sender, MouseButtonEventArgs e)
        {
            
            TextBlock add_box = sender as TextBlock;
            add_box.FuncAdd(list_items, List_Source);
        }

        private async void delete_button_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var dataGridRow = button?.DataContext as FileItem;
            if(dataGridRow != null)
            {
                var itemsSource = list_items.ItemsSource as List<FileItem>;
                if(itemsSource != null)
                {
                    itemsSource.Remove(dataGridRow);
                    await Dispatcher.CurrentDispatcher.InvokeAsync(() =>
                    {
                        CollectionViewSource.GetDefaultView(list_items.ItemsSource).Refresh();
                    });
                    
                }
            }
        }
    }
}
