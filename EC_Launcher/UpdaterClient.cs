﻿using System;
using Dropbox.Api;
using System.Xml.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Diagnostics;

namespace EC_Launcher
{
    /// <summary>
    /// Update Class used Dropbox API
    /// </summary>
    public class UpdaterClient
    {
        private string tokenDropbox = "JCFYioFBHBAAAAAAAAAAFq4g6p6ZhtsYZJktjnNb_JFknLnJjKEMyASiPO7kKKK5";
        private DropboxClient dbx;
        private XDocument remoteVersionXML;
        private string rootFolder = "/EC_Server_Files"; //Корневой папка сервера
        private string versionXMLFile = "launcher/Version.xml";                      

        public Version RemoteAppVersion { get => VersionXML.ParseAppVersion(remoteVersionXML); }
        public Version RemoteModVersion { get => VersionXML.ParseModVersion(remoteVersionXML); }

        public UpdaterClient()
        {
            try
            {
                dbx = new DropboxClient(tokenDropbox);
                var streamXML = dbx.Files.DownloadAsync(rootFolder + "/" + versionXMLFile).Result.GetContentAsStreamAsync().Result;
                this.remoteVersionXML = XDocument.Load(streamXML);
                this.remoteVersionXML.Save(App.globalVars.CacheFolder + @"\launcher\Version.xml");              
            }
            catch(Exception)
            {
                throw new Exception("Network connection error or Server does not response");
            }
        }

        /// <summary>
        /// Checks application update in the host of DropBox
        /// </summary>
        /// <returns> true if application has an update otherwise false</returns>
        public bool CheckAppUpdate()
        {          
            if (App.globalVars.ApplicationVersion < RemoteAppVersion)
            {              
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks mod update in the host of DropBox
        /// </summary>
        /// <returns> true if mod has an update otherwise false</returns>
        public bool CheckModUpdate()
        {
            if (App.globalVars.ModVersion < RemoteModVersion)
            {               
                return true;
            }        
            return false;
        }
        
        // Скачать обновление, сначала скачается хеш-файл из сервера, потом сравнивается хеш-файл клиента с сервером
        public async void DownloadModUpdateAsync(IProgress<ProgressData> progress)
        {          
            await Task.Run(async () =>
            {
                try
                {
                    var remoteHashList = new List<KeyValuePair<string, string>>();
                    var localHashList = new List<KeyValuePair<string, string>>();
                    var progressData = new ProgressData
                    {
                        value = 0,
                        max = 1,
                        statusText = "HashList.md5"                      
                    };
                    if (progress != null) //Сообщить пользователя что скачается хеш-файл
                    {
                        progress.Report(progressData);
                    }

                    // Скачать хеш файл сервера
                    var streamRemoteHashFile = await dbx.Files.DownloadAsync(rootFolder + "/launcher/HashList.md5").Result.GetContentAsByteArrayAsync();

                    // Сохранить хеш файл сервера на кеш папке
                    var streamLocalHashFile = new FileStream("HashList.md5", FileMode.Open);                
                    File.WriteAllBytes(App.globalVars.CacheFolder + "\\launcher\\HashList.md5", streamRemoteHashFile);

                    // Список хеш значение сервера
                    remoteHashList = HashFile.GetHashListFromFile(new FileStream(App.globalVars.CacheFolder + "\\launcher\\HashList.md5", FileMode.Open));

                    // Список хеш значение клиента
                    localHashList = HashFile.GetHashListFromFile(streamLocalHashFile);

                    // UNIX path separator '/'
                    // Windows path separator '\\'
                    // All files convert to UNIX path separator
                    // Все пути в хеш файле(HashList.md5) в виде windows разделитела, надо его конвертировать на UNIX разделителя чтобы сервер взаимодействовал с ним
                    
                    // Список изменённые файлы в сервере
                    var ChangedFilesList = localHashList.Except(remoteHashList).Select(item => item.Key.Replace("\\", "/")).ToList();

                    // Список файлы клиента
                    var LocalFilesList = (from item in localHashList select item.Key.Replace("\\", "/")).ToList();

                    // Список файлы сервера
                    var RemoteFilesList = (from item in remoteHashList select item.Key.Replace("\\", "/")).ToList();

                    // Новые файлы который добавлен в сервере
                    var NewFilesList = RemoteFilesList.Except(LocalFilesList).ToList();

                    // Удаленные файлы из сервера
                    var DeletedFilesList = LocalFilesList.Except(RemoteFilesList).ToList();

                    // Общее количество файлов для скачивание
                    progressData.max = ChangedFilesList.Count + NewFilesList.Count;                   

                    foreach (var file in ChangedFilesList)
                    {
                        await DownloadFromDbx(rootFolder, file);
                        progressData.value++;
                        progressData.statusText = Path.GetFileName(file);

                        if (progress != null)
                        {
                            progress.Report(progressData);
                        }                      
                    }

                    foreach (var file in NewFilesList)
                    {
                        await DownloadFromDbx(rootFolder, file);
                        progressData.value++;
                        progressData.statusText = Path.GetFileName(file);

                        if (progress != null)
                        {
                            progress.Report(progressData);
                        }                        
                    }

                    foreach (var file in DeletedFilesList)
                    {
                        string fileNameWindows = file.Replace("/", "\\");
                        File.Delete(App.globalVars.ModDirectory + fileNameWindows);
                        //File.Create(App.globalVars.CacheFolder + fileNameWindows + "_deleted");
                    }

                    // Получаем список скачанных файлов в папке кеша
                    string[] cacheFiles = Directory.GetFiles(App.globalVars.CacheFolder, "*", SearchOption.AllDirectories);
                    string modsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Paradox Interactive", "Hearts of Iron IV", "mod");

                    // Перемещаем файлы из папке кеша на папку мода
                    foreach (var file in cacheFiles)
                    {
                        if(!file.Contains("EC_Launcher.exe") && !file.Contains("Settings.xml"))
                        {
                            // Перемещать файл .mod в MyDocuments\Paradox Interacive\Hearts of Iron IV\mod
                            if (file.Contains("Economic_Crisis.mod")) 
                            {
                                MoveFile(file, modsFolder);
                            }
                            else
                            {
                                MoveFile(file, App.globalVars.ModDirectory);
                            }                           
                        }
                    }
                }
                catch (Exception)
                {
                    throw new Exception("Network connection error or Server does not response");
                }
            });
        }

        // Скачиваем обновленный файл EC_Launcher.exe по частям
        public async void DownloadAppUpdateAsync(IProgress<int> progress)
        {          
            await Task.Run(async () =>
            {
                try
                {
                    using (var response = await dbx.Files.DownloadAsync(rootFolder + "/launcher/EC_Launcher.exe"))
                    {
                        ulong fileSize = response.Response.Size;
                        const int bufferSize = 1024 * 1024;
                        byte[] buffer = new byte[bufferSize];
                        
                        using (var stream = await response.GetContentAsStreamAsync())
                        {
                            using (var file = new FileStream(App.globalVars.CacheFolder + @"\launcher\EC_Launcher.exe", FileMode.OpenOrCreate))
                            {
                                var length = stream.Read(buffer, 0, bufferSize);

                                while (length > 0)
                                {
                                    file.Write(buffer, 0, length);
                                    var percentage = ProgressData.GetPercentage((int)file.Length, (int)fileSize);
                                    length = stream.Read(buffer, 0, bufferSize);

                                    if (progress != null)
                                    {
                                        progress.Report(percentage);
                                    }
                                }
                            }
                        }
                    }
                    MessageToUserAndClose();
                }
                catch (Exception)
                {
                    throw new Exception("Network connection error or Server does not response");
                }               
            });           
        }

        private async Task DownloadFromDbx(string rootFolder, string file)
        {
            using (var response = await dbx.Files.DownloadAsync(rootFolder + file))
            {              
                byte[] data = await response.GetContentAsByteArrayAsync();
                string fileNameWindows = file.Replace("/", "\\");

                // Если не существует такой каталог в папке кеша, тогда создаем новый каталог
                if (!Directory.Exists(App.globalVars.CacheFolder + Path.GetDirectoryName(fileNameWindows)))
                {
                    Directory.CreateDirectory(App.globalVars.CacheFolder + Path.GetDirectoryName(fileNameWindows));
                }

                File.WriteAllBytes(App.globalVars.CacheFolder + fileNameWindows, data);
            }
        }              

        private void MoveFile(string sourceFileFromCacheFolder, string destDirName)
        {           
            int fullLength = sourceFileFromCacheFolder.Length;
            int cacheFolderLength = App.globalVars.CacheFolder.Length;

            string fileName = sourceFileFromCacheFolder.Remove(0, cacheFolderLength);
            string dirName = Path.GetDirectoryName(fileName);

            if (!Directory.Exists(destDirName+dirName))
            {
                Directory.CreateDirectory(destDirName + dirName);
            }

            File.Copy(sourceFileFromCacheFolder, destDirName + fileName, true);           
        }

        // Сообщить пользователя что надо перезагрузить лаунчера после скачивание обновлении лаунчера
        // Активируем наш Updater.exe передаем только одна аршумент который путь к обновленный лаунчер в кеш папке
        private void MessageToUserAndClose()
        {
            MessageBox.Show("The application update has successfully downloaded, please press OK for continue", "Downloaded", MessageBoxButton.OK, MessageBoxImage.Information);
            string arguments = App.globalVars.CacheFolder + @"\launcher\EC_Launcher.exe";
            Process.Start("Updater.exe", arguments);
        }
    }
}
