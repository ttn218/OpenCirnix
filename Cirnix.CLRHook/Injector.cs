﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

using Cirnix.CLRHook.Properties;
using Cirnix.Global;

using EasyHook;

namespace Cirnix.CLRHook
{
    public static class Injector
    {
        private static void ForceInstall(string Path, byte[] bytes)
        {
            try
            {
                if (CheckDelete(Path))
                    File.WriteAllBytes(Path, bytes);
            }
            catch { }
        }

        private static void CheckDirectory(string Path)
        {
            if (!Directory.Exists(Path))
                Directory.CreateDirectory(Path);
        }

        private static bool CheckInstall(string Path, byte[] bytes)
        {
            try
            {
                if (!File.Exists(Path))
                    File.WriteAllBytes(Path, bytes);
                else
                    using (var MD5 = new MD5CryptoServiceProvider())
                    {
                        byte[] source = MD5.ComputeHash(bytes);
                        byte[] dest = MD5.ComputeHash(File.ReadAllBytes(Path));
                        if (!source.SequenceEqual(dest))
                            File.WriteAllBytes(Path, bytes);
                        else return false;
                    }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool CheckDelete(string Path)
        {
            try
            {
                if (File.Exists(Path)) 
                    File.Delete(Path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void InstallHookLib()
        {
            string CirnixPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            CheckInstall(Path.Combine(CirnixPath, "EasyHook.dll"), Resources.EasyHook);
            CheckInstall(Path.Combine(CirnixPath, "EasyLoad32.dll"), Resources.EasyLoad32);
        }

        public static int Init(string path, int windowState = 0, bool isInstallM16 = true, bool isDebug = false)
        {
            Global.Registry.Warcraft.SetFullQualityGraphics();
            string EXEPath = Path.Combine(path, "Warcraft III.exe");
            if (!(File.Exists(EXEPath) || File.Exists(EXEPath = Path.Combine(path, "war3.exe"))) 
             || FileVersionInfo.GetVersionInfo(EXEPath).FileVersion != "1.28.5.7680")
                return 0;
            if (!isDebug)
            {
                CheckDelete(Path.Combine(path, "m16l.mix"));
                if (isInstallM16)
                {
                    string M16Mix = Path.Combine(path, "M16.mix");
                    string ServerMD5 = Globals.GetStringFromServer("http://3d83b79312a03679207d5dbd06de14fe.fx.fo/hash").Trim('\n');
                    using (var MD5 = new MD5CryptoServiceProvider())
                    {
                        StringBuilder builder = new StringBuilder();
                        byte[] dest = MD5.ComputeHash(File.ReadAllBytes(M16Mix));
                        for (int i = 0; i < dest.Length; i++)
                            builder.Append(dest[i].ToString("x2"));
                        if (ServerMD5 != builder.ToString())
                            ForceInstall(M16Mix, Globals.GetDataFromServer("http://3d83b79312a03679207d5dbd06de14fe.fx.fo/M16.mix"));
                    }
                }
            }
            string CirnixPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            if (!CirnixPath.Equals(path, StringComparison.OrdinalIgnoreCase))
            {
                CheckDelete(Path.Combine(path, "EasyHook.dll"));
                CheckDelete(Path.Combine(path, "EasyLoad32.dll"));
            }
            string JNServicePath = Path.Combine(Globals.ResourcePath, "JNService");
            string RuntimePath = Path.Combine(JNServicePath, "Cirnix.JassNative.Runtime.dll");
            string JNServicePluginPath = Path.Combine(JNServicePath, "Plugins");
            CheckDirectory(JNServicePath);
            CheckInstall(RuntimePath, Resources.Cirnix_JassNative_Runtime);
            CheckInstall(Path.Combine(JNServicePath, "Cirnix.JassNative.Plugin.dll"), Resources.Cirnix_JassNative_Plugin);
            CheckDirectory(JNServicePluginPath);
            CheckInstall(Path.Combine(JNServicePluginPath, "Cirnix.JassNative.dll"), Resources.Cirnix_JassNative);
            CheckInstall(Path.Combine(JNServicePluginPath, "Cirnix.JassNative.Common.dll"), Resources.Cirnix_JassNative_Common);
            //Global.LogManager.Write($"InEXEPath = {EXEPath}\n"
            //                      + $"InCommandLine = {((windowState == 0) ? "-window" : ((windowState == 1) ? "-nativefullscr" : ""))}\n"
            //                      + $"InProcessCreationFlags = 0\n"
            //                      + $"InLibraryPath_x86 = {RuntimePath}\n"
            //                      + $"InLibraryPath_x64 = {RuntimePath}\n"
            //                      + $"OutProcessId = \n"
            //                      + $"InPassThruArgs[0] = {isDebug}\n"
            //                      + $"InPassThruArgs[1] = {CurrentPath}\n"
            //                      + $"InPassThruArgs[2] = {path}");
            //Config.HelperLibraryLocation = CurrentPath;
            string WindowsStateString;
            switch (windowState)
            {
                case 0: WindowsStateString = "-windows"; break;
                case 1: WindowsStateString = string.Empty; break;
                case 2: WindowsStateString = "-nativefullscr"; break;
                default: return 0;
            }
            try
            {
                RemoteHooking.CreateAndInject(EXEPath, WindowsStateString, 0, RuntimePath, RuntimePath, out int pId, isDebug, JNServicePath, path);
                return pId;
            }
            catch (ArgumentException)
            {
                MetroDialog.OK("오류", "Warcraft III를 실행하지 못했습니다.\nCirnix를 다시 실행시켜주세요.");
                return 0;
            }
        }
    }
}
