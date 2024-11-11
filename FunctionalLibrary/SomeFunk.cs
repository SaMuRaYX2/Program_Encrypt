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
                        new CommonFileDialogFilter("Text files", "*.txt"),
                        new CommonFileDialogFilter("Image Files", "*.jpg;*.jpeg;*.png"),
                        new CommonFileDialogFilter("All Files", "*.*")
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
        public static void EncrypteFiles(this List<string> AllPath, List<string> OutputFile, byte[] key)
        {
            for (int i = 0; i < AllPath.Count; i++)
            {
                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = key;
                    aesAlg.GenerateIV();
                    using (FileStream fsOut = new FileStream(OutputFile[i], FileMode.Create))
                    {
                        fsOut.Write(aesAlg.IV, 0, aesAlg.IV.Length);
                        using (CryptoStream cs = new CryptoStream(fsOut, aesAlg.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            using (FileStream fsIn = new FileStream(AllPath[i], FileMode.Open))
                            {
                                fsIn.CopyTo(cs);
                            }
                        }
                    }
                }
            }
        }
        public static void DecryptFile(this List<string> AllPath, List<string> OutputFile, byte[] key)
        {
            for(int i = 0; i < AllPath.Count; i++)
            {
                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = key;
                    using (FileStream fsIn = new FileStream(AllPath[i], FileMode.Open))
                    {
                        byte[] iv = new byte[aesAlg.BlockSize / 8];
                        fsIn.Read(iv, 0, iv.Length);
                        aesAlg.IV = iv;
                        using (CryptoStream cs = new CryptoStream(fsIn, aesAlg.CreateDecryptor(), CryptoStreamMode.Read))
                        {
                            using(FileStream fsOut = new FileStream(OutputFile[i], FileMode.Create))
                            {
                                cs.CopyTo(fsOut);
                            }
                        }
                    }
                }
            }
        }
        public static bool IsFileEncryptedWithAes(this string filePath)
        {
            try
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    string firstLine = reader.ReadLine();
                    return false;
                }
            }
            catch
            {
                return true;
            }
        }
    }
}
