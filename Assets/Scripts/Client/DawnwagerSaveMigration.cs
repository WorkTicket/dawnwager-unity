#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace Wolfsun
{
    public sealed partial class Game
    {
        private static void ImportPreviousWindowsProfile()
        {
            const string profileKey = "wolfsun.profile";
            if (PlayerPrefs.HasKey(profileKey)) return;
            try
            {
                string json = LegacyWindowsProfile.Read(profileKey);
                if (string.IsNullOrWhiteSpace(json) || JsonUtility.FromJson<Profile>(json) == null) return;
                PlayerPrefs.SetString(profileKey, json);
                PlayerPrefs.Save();
            }
            catch (Exception error) when (error is ArgumentException || error is DllNotFoundException ||
                error is EntryPointNotFoundException || error is MarshalDirectiveException)
            {
                Debug.LogWarning("Dawnwager could not import the previous Windows save.");
            }
        }
    }

    internal static class LegacyWindowsProfile
    {
        private const uint QueryValue = 1;
        private const uint StringOrBinary = 0x0000000a;
        private const int MoreData = 234;
        private static readonly IntPtr CurrentUser = new IntPtr(unchecked((int)0x80000001));

        public static string Read(string profileKey)
        {
            if (RegOpenKeyExW(CurrentUser, @"Software\WOLFSUN\WOLFSUN: The Last Dawn", 0, QueryValue, out var key) != 0)
                return null;
            try
            {
                var name = new StringBuilder(16384);
                for (uint index = 0; ; index++)
                {
                    uint nameLength = (uint)name.Capacity;
                    int result = RegEnumValueW(key, index, name, ref nameLength, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                    if (result == MoreData) continue;
                    if (result != 0) return null;
                    string value = name.ToString();
                    if (value != profileKey && !value.StartsWith(profileKey + "_h", StringComparison.Ordinal)) continue;
                    uint bytes = 0;
                    if (RegGetValueW(key, null, value, StringOrBinary, out var type, null, ref bytes) != 0 || bytes == 0 || bytes > 1048576)
                        continue;
                    var data = new byte[bytes];
                    if (RegGetValueW(key, null, value, StringOrBinary, out type, data, ref bytes) == 0)
                        return (type == 1 ? Encoding.Unicode : Encoding.UTF8).GetString(data, 0, (int)bytes).TrimEnd('\0');
                }
            }
            finally
            {
                RegCloseKey(key);
            }
        }

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int RegOpenKeyExW(IntPtr parent, string path, uint options, uint access, out IntPtr key);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int RegEnumValueW(IntPtr key, uint index, StringBuilder name, ref uint nameLength,
            IntPtr reserved, IntPtr type, IntPtr data, IntPtr dataLength);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int RegGetValueW(IntPtr key, string subkey, string value, uint flags, out uint type,
            [Out] byte[] data, ref uint dataLength);
        [DllImport("advapi32.dll", ExactSpelling = true)]
        private static extern int RegCloseKey(IntPtr key);
    }
}
#endif
