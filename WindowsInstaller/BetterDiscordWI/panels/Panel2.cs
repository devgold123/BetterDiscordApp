﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using asardotnet;

namespace BetterDiscordWI.panels {
    public partial class Panel2: UserControl, IPanel {
        private String _dataPath, _tempPath;
        private Utils _utils;

        public Panel2() {
            InitializeComponent();
        }

        public void SetVisible() {
            GetParent().btnBack.Enabled = false;
            GetParent().btnNext.Enabled = false;
            GetParent().btnBack.Visible = false;
            GetParent().btnNext.Visible = false;
            GetParent().btnCancel.Enabled = false;

            _utils = new Utils();

            KillProcessIfInstalling("Discord");
            KillProcessIfInstalling("DiscordCanary");
            KillProcessIfInstalling("DiscordPTB");

            CreateDirectories();
        }

        private void KillProcessIfInstalling(string app) {
            if(GetParent().DiscordPath.Contains(app + "\\")) {
                AppendLog("Killing " + app);
                foreach(var process in Process.GetProcessesByName(app)) {
                    process.Kill();
                }
            }
        }

        private void CreateDirectories() {
            Thread t = new Thread(() => {
                _dataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BetterDiscord";
                _tempPath = _dataPath + "\\temp";
                AppendLog("Deleting old cached files");
                try {
                    if(File.Exists(_dataPath + "\\emotes_bttv.json")) {
                        File.Delete(_dataPath + "\\emotes_bttv.json");
                    }
                    if(File.Exists(_dataPath + "\\emotes_bttv_2.json")) {
                        File.Delete(_dataPath + "\\emotes_bttv_2.json");
                    }
                    if(File.Exists(_dataPath + "\\emotes_ffz.json")) {
                        File.Delete(_dataPath + "\\emotes_ffz.json");
                    }
                    if(File.Exists(_dataPath + "\\emotes_twitch_global.json")) {
                        File.Delete(_dataPath + "\\emotes_twitch_global.json");
                    }
                    if(File.Exists(_dataPath + "\\emotes_twitch_subscriber.json")) {
                        File.Delete(_dataPath + "\\emotes_twitch_subscriber.json");
                    }
                    if(File.Exists(_dataPath + "\\user.json")) {
                        File.Delete(_dataPath + "\\user.json");
                    }
                } catch(Exception e) { AppendLog("Failed to delete one or more cached files"); }


                if(Directory.Exists(_tempPath)) {
                    AppendLog("Deleting temp path");
                    Directory.Delete(_tempPath, true);
                }

                while(Directory.Exists(_tempPath)) {
                    Debug.Print("Waiting for dirdel");
                    Thread.Sleep(100);
                }

                Directory.CreateDirectory(_tempPath);

                DownloadResource("BetterDiscord.zip", "https://github.com/Jiiks/BetterDiscordApp/archive/stable.zip");

                while(!File.Exists(_tempPath + "\\BetterDiscord.zip")) {
                    Debug.Print("Waiting for download");
                    Thread.Sleep(100);
                }

                AppendLog("Extracting BetterDiscord");

                ZipArchive zar =
                    ZipFile.OpenRead(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                                     "\\BetterDiscord\\temp\\BetterDiscord.zip");
                zar.ExtractToDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                                       "\\BetterDiscord\\temp\\");

                DeleteDirs();
            });
            t.Start();

        }


        private void DeleteDirs() {
            int errors = 0;
            Thread t = new Thread(() => {
                String dir = GetParent().DiscordPath + "\\resources\\app";

                if(Directory.Exists(dir)) {
                    try {
                        AppendLog("Deleting " + dir);
                        Directory.Delete(dir, true);
                    } catch {
                        AppendLog("Error: Failed to Delete the '" + dir + "\\resources\\app' Directory.");
                        errors = 1;
                        Finalize(errors);
                    }
                }

                while(Directory.Exists(dir)) {
                    Debug.Print("Waiting for direl");
                    Thread.Sleep(100);
                }

                dir = GetParent().DiscordPath + "\\resources\\node_modules\\BetterDiscord";


                if(Directory.Exists(dir)) {
                    AppendLog("Deleting " + dir);
                    Directory.Delete(dir, true);
                }

                while(Directory.Exists(dir)) {
                    Debug.Print("Waiting for direl");
                    Thread.Sleep(100);
                }

                AppendLog("Moving BetterDiscord to resources\\node_modules\\");

                Directory.Move(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BetterDiscord\\temp\\BetterDiscordApp-stable", GetParent().DiscordPath + "\\resources\\node_modules\\BetterDiscord");

                try {
                    AppendLog("Extracting app.asar");
					string appAsarPath = GetParent().DiscordPath + "\\resources\\app.asar";

					if(File.Exists(appAsarPath)) {
						AsarArchive archive = new AsarArchive(appAsarPath);

						AsarExtractor extractor = new AsarExtractor();
						//Add extraoffset of 3
						extractor.ExtractAll(archive, GetParent().DiscordPath + "\\resources\\app\\");

						Splice();
					} else {
						AppendLog("Error: app.asar file couldn't be found in 'resources' folder. Installation cannot Continue.");
						errors = 1;
						Finalize(errors);
					}
                } catch {
                    AppendLog("Error: Extracting app.asar: Newtonsoft.Json.dll might not be present in the Installer Folder. Installation cannot Continue.");
                    errors = 1;
                    Finalize(errors);
                }
            });


            t.Start();
        }


        private void DownloadResource(String resource, String url) {
            AppendLog("Downloading Resource: " + resource);

            WebClient webClient = new WebClient();
            webClient.Headers["User-Agent"] = "Mozilla/5.0";

            webClient.DownloadFile(new Uri(url), Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BetterDiscord\\temp\\" + resource);
        }

        private void Splice() {
            String indexloc = GetParent().DiscordPath + "\\resources\\app\\app\\index.js";

            Thread t = new Thread(() => {
                List<String> lines = new List<string>();
                AppendLog("Spicing index");
                using(FileStream fs = new FileStream(indexloc, FileMode.Open)) {
                    using(StreamReader reader = new StreamReader(fs)) {
                        String line = "";
                        while((line = reader.ReadLine()) != null) {
                            //if(GetParent().DiscordPath.Contains("Discord\\")) {
                            //if(GetParent().DiscordPath.Contains("DiscordCanary\\")) {
                            //if(GetParent().DiscordPath.Contains("DiscordPTB\\")) {
                            if(line.Replace(" ", "").Contains("var_fs=")) {
                                lines.Add(line);
                                lines.Add("var _betterDiscord = require('betterdiscord');");
                            } else if(line.Replace(" ", "").Contains("mainWindow=new")) {
                                lines.Add(line);
                                lines.Add(File.ReadAllText("splice"));
                            } else {
                                lines.Add(line);
                            }
                            //}
                        }
                    }
                }

                AppendLog("Writing index");

                File.WriteAllLines(indexloc, lines.ToArray());


                AppendLog("Finished installation, verifying installation...");

                int errors = 0;

                String curPath = GetParent().DiscordPath + "\\resources\\app\\app\\index.js";

                if(!File.Exists(curPath)) {
                    AppendLog("ERROR: FILE: " + curPath + " DOES NOT EXIST!");
                    errors++;
                }

                curPath = GetParent().DiscordPath + "\\resources\\node_modules\\BetterDiscord";

                if(!Directory.Exists(curPath)) {
                    AppendLog("ERROR: DIRECTORY: " + curPath + " DOES NOT EXIST");
                    errors++;
                }


                String basePath = GetParent().DiscordPath + "\\resources\\node_modules\\BetterDiscord";
                String[] bdFiles = { "\\package.json", "\\betterdiscord.js", "\\lib\\BetterDiscord.js", "\\lib\\config.json", "\\lib\\Utils.js" };

                foreach(string s in bdFiles.Where(s => !File.Exists(basePath + s))) {
                    AppendLog("ERROR: FILE: " + basePath + s + " DOES NOT EXIST");
                    errors++;
                }


                Finalize(errors);
            });

            t.Start();
        }

        private void Finalize(int errors) {
            AppendLog("Finished installing BetterDiscord with " + errors + " errors");


            Invoke((MethodInvoker)delegate {
                GetParent().finished = true;
                GetParent().btnCancel.Text = "OK";
                GetParent().btnCancel.Enabled = true;
            });

            if(GetParent().RestartDiscord) {
                if(GetParent().DiscordPath.Contains("\\Discord\\")) {
                    Process.Start(GetParent().DiscordPath + "\\Discord.exe");
                }
                if(GetParent().DiscordPath.Contains("\\DiscordCanary\\")) {
                    Process.Start(GetParent().DiscordPath + "\\DiscordCanary.exe");
                }
                if(GetParent().DiscordPath.Contains("\\DiscordPTB\\")) {
                    Process.Start(GetParent().DiscordPath + "\\DiscordPTB.exe");
                }
            }
        }

        public FormMain GetParent() {
            return (FormMain)ParentForm;
        }

        public void BtnNext() {
            throw new NotImplementedException();
        }

        public void BtnPrev() {
            throw new NotImplementedException();
        }

        private void AppendLog(String message) {
            Invoke((MethodInvoker)delegate {
                rtLog.AppendText(message + "\n");
                rtLog.SelectionStart = rtLog.Text.Length;
                rtLog.ScrollToCaret();
            });

        }
    }
}
