using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Net;
using System.Diagnostics;
using winpkg.Properties;
using System.IO;
using System.IO.Compression;
using Microsoft.Win32;
using System.Runtime.InteropServices;

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
            Console.WriteLine("Copyright (C) 2024 WinAPT Organization.");
            Console.WriteLine("Licensed with AGPL license (https://www.gnu.org/licenses/agpl-3.0.en.html#license-text).");
            if (args.Length == 0) 
            {
                //Show help in console
                Console.WriteLine("Available commands:");
                Console.WriteLine("winapt install <package> - Installs a package from the online repository");
                Console.WriteLine("winapt localinstall <packagefile> - Installs a .winapt package from the local system");
                Console.WriteLine("winapt license - Displays the license agreement");
                Console.WriteLine("winapt about - Displays some program information");
                Console.WriteLine("winapt list - Displays a list of all available packages");
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
                    else if (ArchitectureInfo.IsArm64())
                    {
                        arch = "arm64/";
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
                else if (arg1 == "localinstall")
                {
                    string packagefile = args[1].Trim();
                    if (File.Exists(packagefile) && packagefile.EndsWith(".winapt"))
                    {
                        RegistryKey rk = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock");
                        int DevMode = Convert.ToInt32(rk.GetValue("AllowDevelopmentWithoutDevLicense",0));
                        rk.Close();
                        rk = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                        string platform = rk.GetValue("ProductName") as string;
                        if (DevMode == 1) 
                        {
                            Console.WriteLine("Installing packages from unknown sources is a security risk. We reccommend you to install packages from our official repository.");
                            Console.WriteLine("Continue anyway? [y/N] ");
                            var key = Console.ReadKey(true);
                            while (key.Key != ConsoleKey.Y && key.Key != ConsoleKey.N && key.Key != ConsoleKey.Enter)
                            {
                                key = Console.ReadKey(true);
                            }
                            if (key.Key == ConsoleKey.Y)
                            {
                                try
                                {
                                    Console.WriteLine("Found package " + packagefile + ". Extracting...");
                                    ZipFile.ExtractToDirectory(packagefile, Path.GetTempPath() + "/winapt");
                                    if (File.Exists(Path.GetTempPath() + "/winapt/Install.cmd"))
                                    {
                                        Console.WriteLine("Package was extracted. Beginning installation...");
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
                                            Console.WriteLine("Error during the installation of package " + packagefile + "!");
                                            Directory.Delete(Path.GetTempPath() + "/winapt", true);
                                        }
                                        else
                                        {
                                            Console.WriteLine("Successfully installed package " + packagefile + "!");
                                            Directory.Delete(Path.GetTempPath() + "/winapt", true);
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("Package " + packagefile + " does not contain a valid install script.");
                                        Directory.Delete(Path.GetTempPath() + "/winapt", true);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error: {ex.Message}");
                                }
                            }
                            else if (key.Key == ConsoleKey.N || key.Key == ConsoleKey.Enter)
                            {
                                Environment.Exit(0);
                            }
                        }
                        else if (!platform.Contains("Windows 10"))
                        {
                            Console.WriteLine("Installing packages from unknown sources is a security risk. We reccommend you to install packages from our official repository.");
                            Console.WriteLine("Continue anyway? [y/N] ");
                            var key = Console.ReadKey(true);
                            while (key.Key != ConsoleKey.Y && key.Key != ConsoleKey.N && key.Key != ConsoleKey.Enter)
                            {
                                key = Console.ReadKey(true);
                            }
                            if (key.Key == ConsoleKey.Y)
                            {
                                try
                                {
                                    Console.WriteLine("Found package " + packagefile + ". Extracting...");
                                    ZipFile.ExtractToDirectory(packagefile, Path.GetTempPath() + "/winapt");
                                    if (File.Exists(Path.GetTempPath() + "/winapt/Install.cmd"))
                                    {
                                        Console.WriteLine("Package was extracted. Beginning installation...");
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
                                            Console.WriteLine("Error during the installation of package " + packagefile + "!");
                                            Directory.Delete(Path.GetTempPath() + "/winapt", true);
                                        }
                                        else
                                        {
                                            Console.WriteLine("Successfully installed package " + packagefile + "!");
                                            Directory.Delete(Path.GetTempPath() + "/winapt", true);
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("Package " + packagefile + " does not contain a valid install script.");
                                        Directory.Delete(Path.GetTempPath() + "/winapt", true);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error: {ex.Message}");
                                }
                            }
                            else if (key.Key == ConsoleKey.N || key.Key == ConsoleKey.Enter)
                            {
                                Environment.Exit(0);
                            }
                        }
                        else
                        {
                            Console.WriteLine("To protect your computer's security, On Windows 10 and higher you need to enable developer mode to install local packages.");
                            Console.WriteLine("You can enable developer mode from settings.");
                            Console.WriteLine("More info at https://learn.microsoft.com/windows/apps/get-started/enable-your-device-for-development");
                        }
                    }
                    else
                    {
                        Console.WriteLine("The package " + packagefile + " was not found or is not a valid package.");
                    }
                }
                else if (arg1 == "list")
                {
                    try
                    {
                        string arch;
                        if (Environment.Is64BitOperatingSystem)
                        {
                            arch = "x64/";
                        }
                        else if (ArchitectureInfo.IsArm64())
                        {
                            arch = "arm64/";
                        }
                        else
                        {
                            arch = "x86/";
                        }
                        string url = Resources.RepositoryAPI + arch + "list.txt";
                        using (WebClient client = new WebClient())
                        {
                            string responseFromServer = client.DownloadString(url);
                            Console.WriteLine("Available packages for your system:");
                            Console.WriteLine(responseFromServer);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: " + ex.Message);
                    }
                }
                else
                {
                    Console.WriteLine("Unrecognized command: " + arg1);
                }
            }
        }
    }
    public static class ArchitectureInfo
    {
        public static bool IsArm64()
        {
            var handle = Process.GetCurrentProcess().Handle;
            IsWow64Process2(handle, out var processMachine, out var nativeMachine);

            return nativeMachine == 0xaa64;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool IsWow64Process2(
            IntPtr process,
            out ushort processMachine,
            out ushort nativeMachine
        );
    }
}
