﻿using System;
using System.Windows;
using System.Net.Mail;
using System.Net;

namespace EC_Launcher
{
    /// <summary>
    /// Логика взаимодействия для ReportBugWindow.xaml
    /// </summary>
    public partial class ReportBugWindow : Window
    {
        public ReportBugWindow()
        {
            InitializeComponent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Visibility = Visibility.Hidden;
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if(Name_TBox.Text == "")
            {
                MessageBox.Show(this.FindResource("m_SetNameText").ToString());
                return;
            }

            if (Body_TBox.Text == "")
            {
                MessageBox.Show(this.FindResource("m_SetDescText").ToString());
                return;
            }

            string to = "suxrobGM@gmail.com";
            string from = "DedSec94@mail.ru";
            string p = "suxrobbek";

            try
            {
                using (MailMessage mm = new MailMessage(from, to))
                {
                    mm.Subject = "Reported Bug From @" + Name_TBox.Text;
                    mm.Body = Body_TBox.Text;
                    mm.IsBodyHtml = false;
                    using (SmtpClient sc = new SmtpClient("smtp.mail.ru", 25))
                    {
                        sc.EnableSsl = true;
                        sc.DeliveryMethod = SmtpDeliveryMethod.Network;
                        sc.UseDefaultCredentials = false;
                        sc.Credentials = new NetworkCredential(from, p);
                        sc.Send(mm);
                        MessageBox.Show(this.FindResource("m_MessageSuccessfullySentText").ToString());
                    }
                }
            }
            catch(Exception)
            {
                MessageBox.Show(this, this.FindResource("m_NetworkErrorText").ToString(), this.FindResource("m_ERROR").ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }      
    }
}