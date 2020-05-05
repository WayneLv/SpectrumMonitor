using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using InstrumentDriver.Core.Interfaces;
using InstrumentDriver.Core.Utility;
using Microsoft.Win32;
using SpectrumMonitor.Windows;

namespace SpectrumMonitor.ViewModel
{
    public class ErrorMessageViewModel : AbstractModel
    {
        private readonly SpctrumMonitorViewModel mMainViewModel;
        private readonly IInstrument mInstr;

        private readonly string ERRORTYPE_ALL = "All";
        private readonly string ERRORTYPE_ERROR = "Error";
        private readonly string ERRORTYPE_WARNING = "Warning";
        private readonly string ERRORTYPE_INFO = "Info";

        private ObservableCollection<LogMessage> mLogMessageList = new ObservableCollection<LogMessage>();

        public ErrorMessageViewModel(SpctrumMonitorViewModel viewmodel)
        {
            mMainViewModel = viewmodel;
            mInstr = viewmodel.Instrument;

            MessageTypes = new List<string>(){ ERRORTYPE_ALL, ERRORTYPE_ERROR, ERRORTYPE_WARNING, ERRORTYPE_INFO };
            mSelectedMessageType = MessageTypes[0];

            string folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string path = Path.Combine(folder, "ErrorLog.txt");

            ErrorFilePath = path;
        }

        public ObservableCollection<LogMessage> DisplayLogMessageList { set; get; } = new ObservableCollection<LogMessage>();

        public List<string> MessageTypes
        {
            get;
            set;
        }

        private string mSelectedMessageType;
        public string SelectedMessageType
        {
            get { return mSelectedMessageType; }
            set
            {
                // When switching modules, if the previously selected memory block name
                //  does not exist in the new module, value will be null.  In that case
                //  default to a safe value
                mSelectedMessageType = (value != null) ? value : MessageTypes[0];

                NotifyPropertyChanged((() => SelectedMessageType));

                DoRefreshError();
            }
        }

        RelayCommand mRefreshError;
        public ICommand RefreshError
        {
            get { return mRefreshError ?? (mRefreshError = new RelayCommand(() => DoRefreshError())); }
        }
        public void DoRefreshError()
        {
            DisplayLogMessageList.Clear();
            var errors = mInstr.ErrorLog.ErrorList;
            if (errors.Count == 0)
            {
                DisplayLogMessageList.Add(new LogMessage(ErrorPriorities.Info, 0, "---",
                    "No message"));
            }
            else
            {
                foreach (var err in errors)
                {
                    if (SelectedMessageType == ERRORTYPE_ALL
                        || (SelectedMessageType == ERRORTYPE_ERROR && err.Priority == ErrorPriorities.Error)
                        || (SelectedMessageType == ERRORTYPE_WARNING && err.Priority == ErrorPriorities.Warning)
                        || (SelectedMessageType == ERRORTYPE_INFO && err.Priority == ErrorPriorities.Info)
                    )
                    {
                        DisplayLogMessageList.Add(new LogMessage(err.Priority, err.ErrorCode, err.TimeStamp.ToString(),
                            err.Message));
                    }
                }
            }

            NotifyPropertyChanged((() => DisplayLogMessageList));
        }

        public string ErrorFilePath
        {
            get;
            set;
        }

        RelayCommand mClearError;
        public ICommand ClearError
        {
            get { return mClearError ?? (mClearError = new RelayCommand(() => DoClearError())); }
        }

        public void DoClearError()
        {
            mInstr.ErrorLog.Clear();
            DoRefreshError();
        }

        RelayCommand mDumpError;
        public ICommand DumpError
        {
            get { return mDumpError ?? (mDumpError = new RelayCommand(() => DoDumpError())); }
        }

        public void DoDumpError()
        {
            using (StreamWriter file = new StreamWriter(ErrorFilePath))
            {
                foreach (var error in DisplayLogMessageList)
                {
                    file.WriteLine(String.Format("{0} {1} {2} {3}", error.DateString,error.Type,error.Code,error.Message));
                }

            }
        }

        RelayCommand mBrowseErrorFile;
        public ICommand BrowseErrorFile
        {
            get { return mBrowseErrorFile ?? (mBrowseErrorFile = new RelayCommand(() => DoBrowseErrorFile())); }
        }

        public void DoBrowseErrorFile()
        {
            try
            {
                OpenFileDialog dlg = new OpenFileDialog
                {
                    Title = "Read to file path",
                    InitialDirectory = Path.GetDirectoryName(ErrorFilePath),
                    FileName = Path.GetFileName(ErrorFilePath),
                    DefaultExt = ".txt",
                    Filter = "(*.txt)|*.txt",
                    CheckFileExists = false
                };

                // Show open file dialog box - this dialog checks for an existing document.
                Nullable<bool> result = dlg.ShowDialog();

                // Process save file dialog box results
                if (result == true)
                {
                    ErrorFilePath = dlg.FileName;
                    NotifyPropertyChanged(() => ErrorFilePath);
                }
            }

            catch (Exception ex)
            {
                //ErrorReporter.ReportError(ex);
            }
        }
    }


    public class LogMessage : AbstractModel
    {
        public LogMessage(ErrorPriorities type, int code, string datestr, string message)
        {
            Type = type;
            Code = code;
            DateString = datestr;
            Message = message;
        }
        public ErrorPriorities Type { get; set; }

        public int Code { get; set; }

        public string Message { get; set; }

        public string DateString { get; set; }

    }

    public class MessageEnumToString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return "";
            return ((ErrorPriorities)value).ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }

    public class MessageEnumToColor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Colors.Black;
            ErrorPriorities messageType = (ErrorPriorities)value;
            switch (messageType)
            {
                case ErrorPriorities.Info:
                    return Brushes.Blue;
                case ErrorPriorities.Warning:
                    return Brushes.GreenYellow;
                case ErrorPriorities.Error:
                    return Brushes.Red;
                default:
                    return Brushes.Black;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
}
