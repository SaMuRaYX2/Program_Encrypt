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
        public MainWindow()
        {
            InitializeComponent();
            add.MouseDown += Add_MouseDown;
            List_Source = new List<FileItem>();
            list_items.ItemsSource = List_Source;
            remove_all.MouseDown += Remove_all_MouseDown;
            encrypt.Click += Encrypt_Click;
            decipher.Click += Decipher_Click;
            key.LostFocus += Key_LostFocus;
            output_format.LostFocus += Output_format_LostFocus;
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
            if (!string.IsNullOrEmpty(key.Text) && !string.IsNullOrEmpty(output_extension))
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
                        //string[] newOutput = new string[partsOfDirection.Length - 1];
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
                        AllPath.DecryptFile(OutputPath, key_encrypte);
                        foreach (string selectedItem in AllPath)
                        {
                            if (File.Exists(selectedItem))
                            {
                                File.Delete(selectedItem);
                            }
                        }
                    }
                }
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

        private void Encrypt_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(key.Text) && !string.IsNullOrEmpty(output_extension))
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
                        //string[] newOutput = new string[partsOfDirection.Length - 1];
                        string result = "";
                        for(int i = 0; i < partsOfDirection.Length - 1; i++)
                        {
                            result += partsOfDirection[i] + @"\";
                        }
                        result += newPath;
                        OutputPath.Add(result);
                    }
                    if (AllPath.Count != 0)
                    {
                        AllPath.EncrypteFiles(OutputPath, key_encrypte);
                        foreach(string selectedItem in AllPath)
                        {
                            if (File.Exists(selectedItem))
                            {
                                File.Delete(selectedItem);
                            }
                        }
                    }
                }
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
                else if(string.IsNullOrEmpty(key.Text) && string.IsNullOrEmpty(output_extension))
                {
                    MessageBox.Show("Введіть значення для ключа шифрування та для розширення для файлу");
                    key.Focus();
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

        private void delete_button_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var dataGridRow = button?.DataContext as FileItem;
            if(dataGridRow != null)
            {
                var itemsSource = list_items.ItemsSource as List<FileItem>;
                if(itemsSource != null)
                {
                    itemsSource.Remove(dataGridRow);
                    Dispatcher.CurrentDispatcher.InvokeAsync(() =>
                    {
                        CollectionViewSource.GetDefaultView(list_items.ItemsSource).Refresh();
                    });
                    
                }
            }
        }
    }
}
