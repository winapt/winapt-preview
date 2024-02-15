using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Net;
using System.Diagnostics;
using winpkg.Properties;
using System.IO;
using System.IO.Compression;

namespace winpkg
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Console.WriteLine("APT for Windows");
            Console.WriteLine("Copyright (C) 2024 Useful Stuffs.");
            Console.WriteLine("Licensed with AGPL license (https://www.gnu.org/licenses/agpl-3.0.en.html#license-text).");
            if (args.Length == 0) 
            {
                //Show help in console
                Console.WriteLine("Available commands:");
                Console.WriteLine("winapt install <package> - Installs a package from the online repository");
                Console.WriteLine("winapt settings - Allows changing winapt settings");
                Console.WriteLine("winapt license - Displays the license agreement");
            }
            else
            {
                string arg1 = args[0].Trim();
                if (arg1 == "install")
                {
                    //Install a package
                    string arch;
                    if (Environment.Is64BitOperatingSystem) 
                    {
                        arch = "x64/";
                    }
                    else
                    {
                        arch = "x86/";
                    }
                    string package = args[1].Trim();
                    string packagedir = Resources.RepositoryAPI + arch + package;
                    string url = packagedir + "/package.txt";
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

                    try
                    {
                        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            Console.WriteLine("Package " + package + " found in the repository. Downloading...");
                            using (var client = new WebClient())
                            {
                                client.DownloadFile(packagedir + "/package.winapt", Path.GetTempPath() + "/package.winapt");
                            }
                            Console.WriteLine("Download done. Starting installation...");
                            ZipFile.ExtractToDirectory(Path.GetTempPath() + "/package.winapt", Path.GetTempPath() + "/winapt");
                            Process info = new Process();
                            info.StartInfo.FileName = "cmd.exe";
                            info.StartInfo.Arguments = "/c " + Path.GetTempPath() + "/winapt/Install.cmd";
                            info.StartInfo.CreateNoWindow = true;
                            info.StartInfo.RedirectStandardOutput = true;
                            info.StartInfo.UseShellExecute = false;
                            info.Start();
                            string output = info.StandardOutput.ReadToEnd();
                            Console.WriteLine(output);
                            if (info.ExitCode != 0)
                            {
                                Console.WriteLine("Error during the installation of package " + package + "!");
                                Directory.Delete(Path.GetTempPath() + "/winapt",true);
                                File.Delete(Path.GetTempPath() + "/package.winapt");
                            }
                            else
                            {
                                Console.WriteLine("Successfully installed package " + package + "!");
                                Directory.Delete(Path.GetTempPath() + "/winapt", true);
                                File.Delete(Path.GetTempPath() + "/package.winapt");
                            }
                        }
                        else
                        {
                            Console.WriteLine("No package " + package + " was found in the repository!");
                        }
                    }
                    catch (WebException ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                    }
                }
                else if (arg1 == "settings")
                {
                    //Show settings
                    Console.WriteLine("This feature is still in development! Thank you for your interest.");
                }
                else if (arg1 == "license")
                {
                    try
                    {
                        Process.Start("https://www.gnu.org/licenses/agpl-3.0.en.html#license-text");
                    }
                    catch 
                    {
                        Console.WriteLine("Unable to launch browser to view license. Please install and choose a default browser from settings/control panel.");
                    }
                }
                else if (arg1 == "about")
                {
                    Console.WriteLine("Program version: " + Application.ProductVersion);
                    Console.WriteLine("Created by Useful Stuffs - https://github.com/usefulstuffs");
                    Console.WriteLine("GitHub Page: https://github.com/winapt");
                }
                else
                {
                    Console.WriteLine("Unrecognized command: " + arg1);
                }
            }
        }
    }
}
