using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Net.Configuration;
using System.Windows.Data;
using System.Windows.Threading;
using System.Security.Cryptography;
using System.Runtime.InteropServices.WindowsRuntime;
using System.ComponentModel;
using System.Timers;
using System.IO.MemoryMappedFiles;


namespace FunctionalLibrary
{
    public static class SomeFunk
    {
        
        public static void FuncAdd(this TextBlock textBlock, System.Windows.Controls.DataGrid list_items, List<FileItem> list_source)
        {

            try
            {
                
                
                var dialog = new CommonOpenFileDialog
                {
                    Title = "Please select File or Folder",
                    Multiselect = true,
                    Filters =
                    {
                        new CommonFileDialogFilter("All Files", "*.*"),
                        new CommonFileDialogFilter("Text files", "*.txt"),
                        new CommonFileDialogFilter("Image Files", "*.jpg;*.jpeg;*.png") 
                    }

                };
                var result = dialog.ShowDialog();
                if (result == CommonFileDialogResult.Ok)
                {
                    
                    foreach (var file in dialog.FileNames)
                    {
                        FileItem fileItem = new FileItem();
                        FileInfo fileInfo = new FileInfo(file);
                        
                        fileItem.Path = file;
                        fileItem.Name = fileInfo.Name;
                        fileItem.Size = (fileInfo.Length / (1024.0 * 1024.0)).ToString("F3");
                        fileItem.DateCreated = fileInfo.CreationTime.ToString("dd-MM--yyyy HH:mm:ss");
                        fileItem.Id = list_source.Count + 1;
                        list_source.Add(fileItem);
                    }
                    Dispatcher.CurrentDispatcher.InvokeAsync(() =>
                    {
                        CollectionViewSource.GetDefaultView(list_items.ItemsSource).Refresh();
                    });
                    
                }
                else
                {
                    System.Windows.MessageBox.Show("No selected made");
                }
              
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Виконання додавання файлу чи папку пройшло з помилкою", $"{ex.Message}", (MessageBoxButton)MessageBoxButtons.OKCancel, (MessageBoxImage)MessageBoxIcon.Error);
            }
        }
        public static void RemoveAllItems(this List<FileItem> list_source, System.Windows.Controls.DataGrid list_items)
        {
            if (list_source.Count != 0 && list_source != null)
            {
                list_source.Clear();
                Dispatcher.CurrentDispatcher.InvokeAsync(() =>
                {
                    CollectionViewSource.GetDefaultView(list_items.ItemsSource).Refresh();
                });
                System.Windows.MessageBox.Show("Всі елементи видалення з DataGrid");
            }
            else
            {
                System.Windows.MessageBox.Show("Список порожній, додайте файли в список");
            }
        }
        public static bool EncrypteFiles(this List<string> AllPath, List<string> OutputFile, byte[] key, BackgroundWorker backgroundWorkerEncrypt, System.Timers.Timer _timer)
        {
            bool succesfull = false;
            try
            {
                double bytesUsing = 0d;
                double filesSize = 0d;
                for(int i = 0; i < AllPath.Count; i++)
                {
                    FileInfo fileInfo = new FileInfo(AllPath[i]);
                    filesSize += fileInfo.Length;
                }
                for (int i = 0; i < AllPath.Count; i++)
                {
                    using (Aes aesAlg = Aes.Create())
                    {
                        aesAlg.Key = key;
                        aesAlg.GenerateIV();
                        aesAlg.Padding = PaddingMode.PKCS7;
                        using (FileStream fsOut = new FileStream(OutputFile[i], FileMode.Create))
                        {
                            fsOut.Write(aesAlg.IV, 0, aesAlg.IV.Length);
                            using (CryptoStream cs = new CryptoStream(fsOut, aesAlg.CreateEncryptor(), CryptoStreamMode.Write))
                            {

                                using (FileStream fsIn = new FileStream(AllPath[i], FileMode.Open))
                                {
                                    byte[] buffer = new byte[8192];
                                    int byteRead;
                                    while ((byteRead = fsIn.Read(buffer, 0, buffer.Length)) > 0)
                                    {
                                        cs.Write(buffer, 0, byteRead);
                                        bytesUsing += byteRead;
                                        backgroundWorkerEncrypt.ReportProgress((int)((bytesUsing / filesSize) * 100));
                                    }
                                    cs.FlushFinalBlock();
                                }   
                               
                            }
                        }
                    }
                }
                _timer.Start();
                succesfull = true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Неправильно введений ключ", $"Помилка {ex.Message}", MessageBoxButton.OK, MessageBoxImage.Stop);
                _timer.Start();
            }
            return succesfull;
        }
        public static bool DecryptFile(this List<string> AllPath, List<string> OutputFile, byte[] key, BackgroundWorker backgroundWorkerDecipher, System.Timers.Timer _timer)
        {
            bool succesfull = false;
            try
            {
                double bytesUsing = 0d;
                double filesSize = 0d;
                for (int i = 0; i < AllPath.Count; i++)
                {
                    FileInfo fileInfo = new FileInfo(AllPath[i]);
                    filesSize += fileInfo.Length;
                }
                for (int i = 0; i < AllPath.Count; i++)
                {
                    using (Aes aesAlg = Aes.Create())
                    {
                        aesAlg.Key = key;
                        aesAlg.Padding = PaddingMode.PKCS7;
                        using (FileStream fsIn = new FileStream(AllPath[i], FileMode.Open))
                        {
                            byte[] iv = new byte[aesAlg.BlockSize / 8];
                            fsIn.Read(iv, 0, iv.Length);
                            aesAlg.IV = iv;
                            using (CryptoStream cs = new CryptoStream(fsIn, aesAlg.CreateDecryptor(), CryptoStreamMode.Read))
                            {
                                using (FileStream fsOut = new FileStream(OutputFile[i], FileMode.Create))
                                {


                                    byte[] buffer = new byte[8192];
                                    int bytesRead;
                                    while ((bytesRead = cs.Read(buffer, 0, buffer.Length)) > 0)
                                    {
                                        fsOut.Write(buffer, 0, bytesRead);
                                        bytesUsing += bytesRead;
                                        backgroundWorkerDecipher.ReportProgress((int)((bytesUsing / filesSize) * 100));
                                    }


                                }


                            }
                        }
                    }
                }
                backgroundWorkerDecipher.ReportProgress(100);
                succesfull = true;
                _timer.Start();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Неправильно введений ключ", $"Помилка {ex.Message}", MessageBoxButton.OK, MessageBoxImage.Stop);
                _timer.Start();
            }
            return succesfull;
        }
        public static bool IsFileEncryptedWithAes(this string filePath)
        {
            
            bool entropy_test = false;
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile($"{filePath}", FileMode.Open))
            {
                using (MemoryMappedViewStream stream = mmf.CreateViewStream())
                {
                    byte[] buffer = new byte[8192];
                    int bytesRead;
                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                    double entropy = buffer.CalculateEntropy();
                    if (entropy > 5.5)
                    {
                        entropy_test = true;
                        
                    }
                    else
                    {
                        entropy_test = false;
                        
                    }

                }
            }
            return entropy_test;

        }
        public static double CalculateEntropy(this byte[] data)
        {
            int[] byteCount = new int[8192];
            foreach (byte b in data)
            {
                byteCount[b]++;
            }
            double entropy = 0.0;
            int dataSize = data.Length;
            foreach (int count in byteCount)
            {
                if (count == 0)
                    continue;
                double frequency = (double)count / dataSize;
                entropy -= frequency * Math.Log(frequency, 2);
            }
            return entropy;
        }
    }
}
