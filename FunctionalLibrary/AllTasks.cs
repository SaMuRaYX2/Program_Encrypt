using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Collections.ObjectModel;

namespace FunctionalLibrary
{
    public class AllTasks : INotifyPropertyChanged
    {
        private string _startTimer;
        private string _totalProcessorTime;
        private int _threadCount;
        private string _name;
        private bool _statusProg;
        private string _priority;
        private float _cpu;
        private string _memory;
        private float _disk;
        private float _byteSent;
        private float _byteReceived;
        private string _network;
        public ObservableCollection<AllTasks> ParentsCollection { get; set; }
        public int ID { get; set; }
        public string StartTimer
        {
            get
            {
                return _startTimer;
            }
            set
            {
                _startTimer = value;
                OnPropertyChanged();
            }
        }
        public string TotalProcessorTime {
            get
            {
                return _totalProcessorTime;
            }
            set
            {
                _totalProcessorTime = value;
                OnPropertyChanged();
            }
        }
        public int ThreadCount {
            get
            {
                return _threadCount;
            }
            set
            {
                _threadCount = value;
                OnPropertyChanged();
            }
        }
        public string Name {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }
        public bool status_prog {
            get
            {
                return _statusProg;
            }
            set
            {
                _statusProg = value;
                OnPropertyChanged();
            }
        }
        public string Status {
            get
            {
                if (status_prog)
                {
                    return "Don`t Work";
                }
                else{
                    return "IsWorking";
                }
            }
            
        }
        public string Priority
        {
            get
            {
                return _priority;
            }
            set
            {
                _priority = value;
                OnPropertyChanged();
            }
        }
        public float CPU
        {
            get
            {
                return _cpu;
            }
            set
            {
                _cpu = value;
                OnPropertyChanged();
            }
        }
        public string Memory
        {
            get
            {
                return _memory;
            }
            set
            {
                _memory = value;
                OnPropertyChanged();
            }
        }
        public float Disk
        {
            get
            {
                return _disk;
            }
            set
            {
                _disk = value;
                OnPropertyChanged();
            }
        }
        public float byteSent
        {
            get
            {
                return _byteSent;
            }
            set
            {
                _byteSent = value;
                OnPropertyChanged();
            }
        }
        public float byteRecived
        {
            get
            {
                return _byteReceived;
            }
            set
            {
                _byteReceived = value;
                OnPropertyChanged();
            }
        } 
        public string Network
        {
            get { return $"Send - {_byteSent:F2}\nRecived - {_byteReceived:F2}"; }
            set
            {
                _network = value;
                OnPropertyChanged();
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
