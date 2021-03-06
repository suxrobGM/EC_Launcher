﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;

namespace EC_Launcher.ViewModels
{
    public class ReportBugPageViewModel : BindableBase
    {
        private readonly IRegionManager regionManager;
        private string senderName;
        private string text;
        private string logFilesPath;
        private string selectedScreenshotFile;
        private bool isAttachedLogFile;
        

        public string SenderName
        {
            get => senderName;
            set
            {
                SetProperty(ref senderName, value);
                SendCommand.RaiseCanExecuteChanged();
            }
        }
        public string Text
        {
            get => text;
            set
            {
                SetProperty(ref text, value);
                SendCommand.RaiseCanExecuteChanged();
            }
        }
        public string SelectedScreenshotFile { get => selectedScreenshotFile; set { SetProperty(ref selectedScreenshotFile, value); } }
        public bool IsAttachedLogFile { get => isAttachedLogFile; set { SetProperty(ref isAttachedLogFile, value); } }
        public ObservableCollection<string> ScreenshotFiles { get; }
        public ObservableCollection<string> ScreenshotFilesPath { get; }
        public DelegateCommand SendCommand { get; }
        public DelegateCommand BackCommand { get; }
        public DelegateCommand AddScreenshootsCommand { get; }
        public DelegateCommand RemoveSelectedFile { get; }


        public ReportBugPageViewModel(IRegionManager regionManager)
        {
            this.regionManager = regionManager;
            logFilesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Paradox Interactive", "Hearts of Iron IV", "logs");
            IsAttachedLogFile = true;
            ScreenshotFiles = new ObservableCollection<string>();
            ScreenshotFilesPath = new ObservableCollection<string>();
            ScreenshotFilesPath.CollectionChanged += ScreenshotFilesPath_CollectionChanged;
            

            AddScreenshootsCommand = new DelegateCommand(() =>
            {
                using (var dialog = new OpenFileDialog())
                {
                    dialog.Multiselect = true;
                    dialog.Filter = "Image files (.png, .jpg)|*.png;*.jpg";
                    dialog.ShowDialog();                    
                    ScreenshotFilesPath.AddRange(dialog.FileNames);                   
                }
            });

            RemoveSelectedFile = new DelegateCommand(() =>
            {             
                ScreenshotFilesPath.Remove(ScreenshotFilesPath.Where(i => i.Contains(SelectedScreenshotFile)).FirstOrDefault());
            });

            SendCommand = new DelegateCommand(() =>
            {
                try
                {
                    SendMessageEmail("DedSec94@mail.ru", "suxrobGM@gmail.com");
                    System.Windows.MessageBox.Show("Your message was successfully sent to suxrobGM@gmail.com Thanks for reported bug, we will fix it soon ;)");
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                
            }, CanExecuteSendCommand);

            BackCommand = new DelegateCommand(() =>
            {
                regionManager.RequestNavigate("ViewsRegion", "BrowserPage");
            });
        }

        private void ScreenshotFilesPath_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                {
                    foreach (var item in e.NewItems)
                    {
                        ScreenshotFiles.Add(Path.GetFileName(item as string));
                    }
                }
            }

            if (e.OldItems != null && e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                foreach (var item in e.OldItems)
                {
                    ScreenshotFiles.Remove(Path.GetFileName(item as string));
                }
            }
        }

        private bool CanExecuteSendCommand()
        {
            return SenderName != null && SenderName != String.Empty && Text != null && Text != String.Empty;
        }

        private void SendMessageEmail(string from, string to)
        {
            using (MailMessage mm = new MailMessage(from, to))
            {
                mm.Subject = "EC: Reported Bug From @" + SenderName;
                mm.Body = Text;
                mm.IsBodyHtml = false;

                if(IsAttachedLogFile)
                {
                    mm.Attachments.Add(new Attachment(GetErrorLogFile())); ;
                    mm.Attachments.Add(new Attachment(GetExceptionsLogFile()));
                }
                
                if(ScreenshotFilesPath.Any())
                {
                    foreach (var filePath in ScreenshotFilesPath)
                    {
                        mm.Attachments.Add(new Attachment(filePath));
                    }                  
                }
                    

                using (SmtpClient sc = new SmtpClient("smtp.mail.ru", 25))
                {
                    sc.EnableSsl = true;
                    sc.DeliveryMethod = SmtpDeliveryMethod.Network;
                    sc.UseDefaultCredentials = false;
                    sc.Credentials = new NetworkCredential(from, GetS());
                    sc.Send(mm);                    
                }
            }
        }

        private string GetErrorLogFile()
        {
            if (!Directory.Exists(logFilesPath))
                throw new Exception("Log files did not found");

            return logFilesPath + "\\error.log";
        }

        private string GetExceptionsLogFile()
        {
            if (!Directory.Exists(logFilesPath))
                throw new Exception("Log files did not found");

            return logFilesPath + "\\exceptions.log";
        }

        private SecureString GetS()
        {
            var secureString = new SecureString();
            secureString.AppendChar('s');
            secureString.AppendChar('u');
            secureString.AppendChar('x');
            secureString.AppendChar('r');
            secureString.AppendChar('o');
            secureString.AppendChar('b');
            secureString.AppendChar('b');
            secureString.AppendChar('e');
            secureString.AppendChar('k');
            return secureString;
        }
    }
}
