﻿using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EC_Launcher.Models;
using System.Windows;
using System.IO;
using System.Diagnostics;
using System.Reflection;

namespace EC_Launcher.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private IRegionManager regionManager;
        private SingletonModel model;
        private ProgressData progressData;

        public DelegateCommand OpenGameCommand { get; }
        public DelegateCommand CheckUpdateCommand { get; }
        public DelegateCommand ReportBugCommand { get; }
        public DelegateCommand DonateCommand { get; }
        public DelegateCommand SettingsCommand { get; }
        public DelegateCommand GenerateHashCommand { get; }
        public DelegateCommand ExitCommand { get; }
        public DelegateCommand<string> GoToWebSiteCommand { get; }
        public ProgressData ProgressData { get => progressData; set { SetProperty(ref progressData, value); } }
        public Version LauncherVersion { get => Assembly.GetExecutingAssembly().GetName().Version; }
        public Version ModVersion { get => model.VersionXml.ModVersion; }
        public Visibility DevModeVisibility { get; set; }


        public MainWindowViewModel(IRegionManager regionManager)
        {
            this.regionManager = regionManager;
            model = SingletonModel.GetInstance();
            ProgressData = new ProgressData();

            if (model.SettingsXml.AppLanguage == "English")
                App.Language = App.Languages[0];
            else if (model.SettingsXml.AppLanguage == "Russian")
                App.Language = App.Languages[1];


            if (model.DevMode)
                DevModeVisibility = Visibility.Visible;
            else
                DevModeVisibility = Visibility.Hidden;
            
            OpenGameCommand = new DelegateCommand(() =>
            {
                try
                {
                    model.CheckModFile();
                    model.SetTickGameLauncher();

                    if (model.SettingsXml.IsSteamVersion)
                        model.StartSteamGame();
                    else
                        model.StartNonSteamGame();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });

            CheckUpdateCommand = new DelegateCommand(async () =>
            {
                string dropboxToken = "JCFYioFBHBAAAAAAAAAAFq4g6p6ZhtsYZJktjnNb_JFknLnJjKEMyASiPO7kKKK5";
                string serverRootFolder = "/EC_Server_Files"; //Корневой папка мода в сервере              
                ProgressData.StatusText = "Checking update...";

                try
                {
                    var updater = new UpdaterClient(dropboxToken, serverRootFolder, model.CacheFolder, model.SettingsXml.ModPath);
                    var hasLauncherUpdate = await updater.CheckAppUpdateAsync();
                    var hasModUpdate = await updater.CheckModUpdateAsync();

                    if (hasLauncherUpdate)
                    {
                        var remoteLauncherVersion = await updater.GetRemoteAppVersionAsync();
                        var mboxResult = MessageBox.Show($"Available update for launcher. New version is  {remoteLauncherVersion}.\nDo you want to download the update?", "Available update for launcher!", MessageBoxButton.OKCancel, MessageBoxImage.Question);
                        if (mboxResult == MessageBoxResult.OK)
                        {
                            ProgressData = updater.ProgressData;
                            await updater.DownloadAppUpdateAsync();
                        }
                    }
                    if (hasModUpdate)
                    {
                        var remoteModVersion = await updater.GetRemoteModVersionAsync();
                        var mboxResult = MessageBox.Show($"Available update for mod. New version is  {remoteModVersion}.\nDo you want to download the update?", "Available update for mod!", MessageBoxButton.OKCancel, MessageBoxImage.Question);
                        if (mboxResult == MessageBoxResult.OK)
                        {
                            ProgressData = updater.ProgressData;
                            await updater.DownloadModUpdateAsync();
                        }
                    }
                    else
                    {
                        ProgressData.StatusText = "";
                        MessageBox.Show("You are using the last version of Economic Crisis", "Mod does not have update yet", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    ProgressData.StatusText = "Updating has canceled";
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }               
            });

            ReportBugCommand = new DelegateCommand(() =>
            {
                regionManager.RequestNavigate("ViewsRegion", "ReportBugPage");
            });

            DonateCommand = new DelegateCommand(() =>
            {
                regionManager.RequestNavigate("ViewsRegion", "DonatePage");
            });

            SettingsCommand = new DelegateCommand(() =>
            {
                regionManager.RequestNavigate("ViewsRegion", "SettingsPage");
            });

            GenerateHashCommand = new DelegateCommand(async () =>
            {
                try
                {
                    var exceptionFiles = new List<string>()
                    {
                        ".git",
                        "_cache",
                        "Settings.xml",
                        "EC_Launcher.exe"
                    };
                    var hashGenerator = new HashGenerator(model.SettingsXml.ModPath, exceptionFiles);
                    ProgressData = hashGenerator.ProgressData;
                    await hashGenerator.GetGameFileHashesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }               
            });

            ExitCommand = new DelegateCommand(() =>
            {
                try
                {
                    if (Directory.Exists("_cache"))
                        Directory.Delete("_cache", true);
                }
                catch (Exception) { }
                finally
                {
                    Environment.Exit(0);
                }
            });

            GoToWebSiteCommand = new DelegateCommand<string>((webUrl) =>
            {
                Process.Start(webUrl);
            });
        }
    }
}
