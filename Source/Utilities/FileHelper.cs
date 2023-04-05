using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;

public static class FileHelper
{
    // http://stackoverflow.com/questions/1406808/wait-for-file-to-be-freed-by-process
    public static bool IsFileReady(string path)
    {
        FileStream stream = null;
        try
        {
            stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException)
        {
            //the file is unavailable because it is:
            //still being written to
            //or being processed by another thread
            //or does not exist (has already been processed)
            return false;
        }
        finally
        {
            if (stream != null)
            {
                stream.Close();
            }
        }

        //file is not locked
        return true;
    }
    public static void WaitForReady(string path)
    {
        if (File.Exists(path))
        {
            while (!FileHelper.IsFileReady(path))
            {
                Thread.Sleep(1000);
            }
        }
    }
    public static void DeleteFile(string path)
    {
        if (File.Exists(path))
        {
            WaitForReady(path);
            File.Delete(path);
        }
    }

    public static void AppendLine(string path, string line)
    {
        if (String.IsNullOrEmpty(path)) return;

        try
        {
            path = path.Replace("|", "+"); // remove illegal char | in filename
            using (StreamWriter writer = new StreamWriter(path, true, Encoding.Unicode))
            {
                writer.WriteLine(line);
            }
        }
        catch
        {
            // silence IO error in case running from read-only media (CD/DVD)
        }
    }
    public static void SaveText(string path, string text)
    {
        SaveText(path, text, Encoding.Unicode);
    }
    public static void SaveLetters(string path, char[] characters)
    {
        SaveLetters(path, characters, Encoding.Unicode);
    }
    public static void SaveWords(string path, List<string> words)
    {
        SaveWords(path, words, Encoding.Unicode);
    }
    public static void SaveValues(string path, List<long> values)
    {
        SaveValues(path, values, Encoding.Unicode);
    }
    public static void SaveText(string path, string text, Encoding encoding)
    {
        if (String.IsNullOrEmpty(path)) return;

        try
        {
            path = path.Replace("|", "+"); // remove illegal char | in filename
            using (StreamWriter writer = new StreamWriter(path, false, encoding))
            {
                writer.Write(text);
                writer.Close();
            }
        }
        catch
        {
            // silence IO error in case running from read-only media (CD/DVD)
        }
    }
    public static void SaveLetters(string path, char[] characters, Encoding encoding)
    {
        if (String.IsNullOrEmpty(path)) return;

        try
        {
            path = path.Replace("|", "+"); // remove illegal char | in filename
            using (StreamWriter writer = new StreamWriter(path, false, encoding))
            {
                foreach (char character in characters)
                {
                    if (character == '\0')
                    {
                        continue;
                    }
                    writer.Write(character);
                }
            }
        }
        catch
        {
            // silence IO error in case running from read-only media (CD/DVD)
        }
    }
    public static void SaveWords(string path, List<string> words, Encoding encoding)
    {
        if (String.IsNullOrEmpty(path)) return;

        try
        {
            path = path.Replace("|", "+"); // remove illegal char | in filename
            using (StreamWriter writer = new StreamWriter(path, false, encoding))
            {
                foreach (string word in words)
                {
                    if (String.IsNullOrEmpty(word))
                    {
                        continue;
                    }
                    writer.WriteLine(word);
                }
            }
        }
        catch
        {
            // silence IO error in case running from read-only media (CD/DVD)
        }
    }
    public static void SaveValues(string path, List<long> values, Encoding encoding)
    {
        if (String.IsNullOrEmpty(path)) return;

        try
        {
            path = path.Replace("|", "+"); // remove illegal char | in filename
            using (StreamWriter writer = new StreamWriter(path, false, encoding))
            {
                foreach (long value in values)
                {
                    writer.WriteLine(value.ToString());
                }
            }
        }
        catch
        {
            // silence IO error in case running from read-only media (CD/DVD)
        }
    }

    public static List<string> LoadLines(string path)
    {
        if (String.IsNullOrEmpty(path)) return null;

        List<string> result = new List<string>();
        try
        {
            path = path.Replace("|", "+"); // remove illegal char | in filename
            if (File.Exists(path))
            {
                FileHelper.WaitForReady(path);

                using (StreamReader reader = File.OpenText(path))
                {
                    string line = null;
                    while ((line = reader.ReadLine()) != null)
                    {
                        result.Add(line);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message + ".\r\n\r\n" + ex.StackTrace);
        }
        return result;
    }
    public static string LoadText(string path)
    {
        if (String.IsNullOrEmpty(path)) return null;

        StringBuilder str = new StringBuilder();
        try
        {
            path = path.Replace("|", "+"); // remove illegal char | in filename
            if (File.Exists(path))
            {
                FileHelper.WaitForReady(path);

                using (StreamReader reader = File.OpenText(path))
                {
                    string line = null;
                    while ((line = reader.ReadLine()) != null)
                    {
                        str.AppendLine(line);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message + ".\r\n\r\n" + ex.StackTrace);
        }
        return str.ToString();
    }

    public static void DisplayFile(string path)
    {
        if (String.IsNullOrEmpty(path)) return;

        path = path.Replace("|", "+"); // remove illegal char | in filename
        if (File.Exists(path))
        {
            FileHelper.WaitForReady(path);
            System.Diagnostics.Process.Start(path);
        }
    }
}
