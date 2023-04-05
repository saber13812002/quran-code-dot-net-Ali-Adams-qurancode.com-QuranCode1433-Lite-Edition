using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Model;

public static class DataAccess
{
    static DataAccess()
    {
        if (!Directory.Exists(Globals.DATA_FOLDER))
        {
            Directory.CreateDirectory(Globals.DATA_FOLDER);
        }

        if (!Directory.Exists(Globals.AUDIO_FOLDER))
        {
            Directory.CreateDirectory(Globals.AUDIO_FOLDER);
        }

        if (!Directory.Exists(Globals.TRANSLATIONS_FOLDER))
        {
            Directory.CreateDirectory(Globals.TRANSLATIONS_FOLDER);
        }
    }

    // quran text from http://tanzil.net
    public static List<string> LoadVerseTexts(string filename)
    {
        List<string> result = new List<string>();
        if (File.Exists(filename))
        {
            using (StreamReader reader = File.OpenText(filename))
            {
                while (!reader.EndOfStream)
                {
                    // skip # comment lines (tanzil copyrights, other meta info, ...)
                    string line = reader.ReadLine();
                    if (!String.IsNullOrEmpty(line))
                    {
                        if (!line.StartsWith("#"))
                        {
                            line = line.Replace("\r", "");
                            line = line.Replace("\n", "");
                            while (line.Contains("  "))
                            {
                                line = line.Replace("  ", " ");
                            }
                            line = line.Trim();
                            result.Add(line);
                        }
                    }
                }
            }
        }
        return result;
    }
    public static void SaveVerseTexts(Book book, string filename)
    {
        if (book != null)
        {
            StringBuilder str = new StringBuilder();
            foreach (Verse verse in book.Verses)
            {
                str.AppendLine(verse.Text);
            }
            FileHelper.SaveText(filename, str.ToString());
        }
    }

    // end of verse stopmarks are assumed to be end of sentence (Meem) for now
    public static List<Stopmark> LoadVerseStopmarks()
    {
        List<Stopmark> result = new List<Stopmark>();
        string filename = Globals.DATA_FOLDER + Path.DirectorySeparatorChar + "verse-stopmarks.txt";
        if (File.Exists(filename))
        {
            using (StreamReader reader = File.OpenText(filename))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (!String.IsNullOrEmpty(line))
                    {
                        result.Add(StopmarkHelper.GetStopmark(line));
                    }
                }
            }
        }
        return result;
    }
    public static void SaveVerseStopmarks(Book book, string filename)
    {
        if (book != null)
        {
            StringBuilder str = new StringBuilder();
            foreach (Verse verse in book.Verses)
            {
                str.AppendLine(StopmarkHelper.GetStopmarkText(verse.Stopmark));
            }
            FileHelper.SaveText(filename, str.ToString());
        }
    }

    // recitation infos from http://www.everyayah.com
    public static void LoadRecitationInfos(Book book)
    {
        if (book != null)
        {
            book.RecitationInfos = new Dictionary<string, RecitationInfo>();
            string filename = Globals.AUDIO_FOLDER + Path.DirectorySeparatorChar + "metadata.txt";
            if (File.Exists(filename))
            {
                using (StreamReader reader = File.OpenText(filename))
                {
                    string line = reader.ReadLine(); // skip header row
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] parts = line.Split('\t');
                        if (parts.Length >= 4)
                        {
                            RecitationInfo recitation = new RecitationInfo();
                            recitation.Url = parts[0];
                            recitation.Folder = parts[0];
                            recitation.Language = parts[1];
                            recitation.Reciter = parts[2];
                            int.TryParse(parts[3], out recitation.Quality);
                            recitation.Name = recitation.Language + " - " + recitation.Reciter;
                            book.RecitationInfos.Add(parts[0], recitation);
                        }
                    }
                }
            }
        }
    }

    // translations info from http://tanzil.net
    public static void LoadTranslationInfos(Book book)
    {
        if (book != null)
        {
            book.TranslationInfos = new Dictionary<string, TranslationInfo>();
            string filename = Globals.TRANSLATIONS_OFFLINE_FOLDER + Path.DirectorySeparatorChar + "metadata.txt";
            if (File.Exists(filename))
            {
                using (StreamReader reader = File.OpenText(filename))
                {
                    string line = reader.ReadLine(); // skip header row
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] parts = line.Split('\t');
                        if (parts.Length >= 4)
                        {
                            TranslationInfo translation = new TranslationInfo();
                            translation.Url = "?transID=" + parts[0] + "&type=" + TranslationInfo.FileType;
                            translation.Flag = parts[1];
                            translation.Language = parts[2];
                            translation.Translator = parts[3];
                            translation.Name = parts[2] + " - " + parts[3];
                            book.TranslationInfos.Add(parts[0], translation);
                        }
                    }
                }
            }
        }
    }

    // translation books from http://tanzil.net
    public static void LoadTranslations(Book book)
    {
        if (book != null)
        {
            try
            {
                if (Directory.Exists(Globals.TRANSLATIONS_FOLDER))
                {
                    string[] filenames = Directory.GetFiles(Globals.TRANSLATIONS_FOLDER + "/");
                    foreach (string filename in filenames)
                    {
                        List<string> translated_lines = FileHelper.LoadLines(filename);
                        if (translated_lines != null)
                        {
                            string translation = filename.Substring((Globals.TRANSLATIONS_FOLDER.Length + 1), filename.Length - (Globals.TRANSLATIONS_FOLDER.Length + 1) - 4);
                            if (translation == "metadata.txt") continue;
                            LoadTranslation(book, translation);
                        }
                    }
                }
            }
            catch
            {
                // ignore error
            }
        }
    }
    public static void LoadTranslation(Book book, string translation)
    {
        if (book != null)
        {
            try
            {
                if (Directory.Exists(Globals.TRANSLATIONS_FOLDER))
                {
                    if (Directory.Exists(Globals.TRANSLATIONS_OFFLINE_FOLDER))
                    {
                        string[] filenames = Directory.GetFiles(Globals.TRANSLATIONS_FOLDER + "/");
                        bool already_loaded = false;
                        foreach (string filename in filenames)
                        {
                            if (filename.Contains(translation))
                            {
                                already_loaded = true;
                                break;
                            }
                        }
                        if (!already_loaded)
                        {
                            File.Copy(Globals.TRANSLATIONS_OFFLINE_FOLDER + Path.DirectorySeparatorChar + translation + ".txt", Globals.TRANSLATIONS_FOLDER + Path.DirectorySeparatorChar + translation + ".txt", true);
                        }

                        filenames = Directory.GetFiles(Globals.TRANSLATIONS_FOLDER + "/");
                        foreach (string filename in filenames)
                        {
                            if (filename.Contains(translation))
                            {
                                List<string> translated_lines = FileHelper.LoadLines(filename);
                                if (translated_lines != null)
                                {
                                    if (book.TranslationInfos != null)
                                    {
                                        if (book.TranslationInfos.ContainsKey(translation))
                                        {
                                            if (book.Verses != null)
                                            {
                                                if (book.Verses.Count > 0)
                                                {
                                                    for (int i = 0; i < book.Verses.Count; i++)
                                                    {
                                                        book.Verses[i].Translations[translation] = translated_lines[i];
                                                    }

                                                    // add bismAllah translation to the first verse of each chapter except chapters 1 and 9
                                                    foreach (Chapter chapter in book.Chapters)
                                                    {
                                                        if ((chapter.Number != 1) && (chapter.Number != 9))
                                                        {
                                                            if ((translation != "ar.emlaaei") && (translation != "en.transliteration") && (translation != "en.wordbyword"))
                                                            {
                                                                if (!chapter.Verses[0].Translations[translation].StartsWith(book.Verses[0].Translations[translation]))
                                                                {
                                                                    chapter.Verses[0].Translations[translation] = book.Verses[0].Translations[translation] + " " + chapter.Verses[0].Translations[translation];
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
            }
            catch
            {
                // ignore error
            }
        }
    }
    public static void UnloadTranslation(Book book, string translation)
    {
        if (book != null)
        {
            try
            {
                if (Directory.Exists(Globals.TRANSLATIONS_FOLDER))
                {
                    string[] filenames = Directory.GetFiles(Globals.TRANSLATIONS_FOLDER + "/");
                    foreach (string filename in filenames)
                    {
                        if (filename.Contains(translation))
                        {
                            if (book.TranslationInfos != null)
                            {
                                if (book.TranslationInfos.ContainsKey(translation))
                                {
                                    if (book.Verses.Count > 0)
                                    {
                                        for (int i = 0; i < book.Verses.Count; i++)
                                        {
                                            book.Verses[i].Translations.Remove(translation);
                                        }
                                        book.TranslationInfos.Remove(translation);
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
            }
            catch
            {
                // ignore error
            }
        }
    }
    public static void SaveTranslation(Book book, string translation)
    {
        if (book != null)
        {
            StringBuilder str = new StringBuilder();
            foreach (Verse verse in book.Verses)
            {
                str.AppendLine(verse.Translations[translation]);
            }
            if (Directory.Exists(Globals.TRANSLATIONS_FOLDER))
            {
                string filename = Globals.TRANSLATIONS_FOLDER + Path.DirectorySeparatorChar + translation + ".txt";
                FileHelper.SaveText(filename, str.ToString());
            }
        }
    }

    // word meanings from http://qurandev.appspot.com - modified by Ali Adams
    public static void LoadWordMeanings(Book book)
    {
        if (book != null)
        {
            try
            {
                string filename = Globals.TRANSLATIONS_OFFLINE_FOLDER + Path.DirectorySeparatorChar + "en.wordbyword.txt";
                if (File.Exists(filename))
                {
                    using (StreamReader reader = File.OpenText(filename))
                    {
                        while (!reader.EndOfStream)
                        {
                            if (book.Verses != null)
                            {
                                foreach (Verse verse in book.Verses)
                                {
                                    string line = reader.ReadLine();
                                    if (!String.IsNullOrEmpty(line))
                                    {
                                        string[] parts = line.Split('\t');

                                        int word_count = 0;
                                        if (verse.Words != null)
                                        {
                                            if (verse.Book.WawAsWord)
                                            {
                                                foreach (Word word in verse.Words)
                                                {
                                                    if (word.Text != "و") // WawAsWord
                                                    {
                                                        word_count++;
                                                    }
                                                }
                                            }

                                            int i = 0;
                                            if (!verse.Book.WithBismAllah)
                                            {
                                                if (verse.NumberInChapter == 1)
                                                {
                                                    if ((verse.Chapter.Number != 1) && (verse.Chapter.Number != 9))
                                                    {
                                                        i += 4;
                                                    }
                                                }
                                            }

                                            foreach (Word word in verse.Words)
                                            {
                                                if (word.Text == "و") // WawAsWord
                                                {
                                                    word.Meaning = "and";
                                                }
                                                else
                                                {
                                                    if ((i >= 0) && (i < parts.Length))
                                                    {
                                                        word.Meaning = parts[i];
                                                        i++;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("LoadWordMeanings: " + ex.Message + ".\r\n\r\n" + ex.StackTrace);
            }
        }
    }

    // word and its roots from http://noorsoft.org - reversed and corrected by Ali Adams and Yudi Rohmad
    public static void LoadWordRoots(Book book)
    {
        if (book != null)
        {
            try
            {
                // Id	Chapter	Verse	Word	Text	Roots
                string filename = Globals.DATA_FOLDER + Path.DirectorySeparatorChar + "word-roots.txt";
                if (File.Exists(filename))
                {
                    using (StreamReader reader = File.OpenText(filename))
                    {
                        while (!reader.EndOfStream)
                        {
                            if (book.Verses != null)
                            {
                                foreach (Verse verse in book.Verses)
                                {
                                    // skip bismAllah if book without it
                                    if (!verse.Book.WithBismAllah)
                                    {
                                        if (verse.NumberInChapter == 1)
                                        {
                                            if ((verse.Chapter.Number != 1) && (verse.Chapter.Number != 9))
                                            {
                                                reader.ReadLine();
                                                reader.ReadLine();
                                                reader.ReadLine();
                                                reader.ReadLine();
                                            }
                                        }
                                    }

                                    if (verse.Words != null)
                                    {
                                        foreach (Word word in verse.Words)
                                        {
                                            if (word.Text == "و") // WawAsWord
                                            {
                                                word.Roots = new List<string>() { "و" };
                                            }
                                            else
                                            {
                                                string line = reader.ReadLine();
                                                if (!String.IsNullOrEmpty(line))
                                                {
                                                    string[] parts = line.Split('\t');

                                                    if (parts.Length == 3)
                                                    {
                                                        string text = parts[1];
                                                        string[] subparts = parts[2].Split('|');
                                                        word.Roots = new List<string>(subparts);
                                                    }
                                                    else
                                                    {
                                                        throw new Exception("Invalid Location Format.\r\n" + filename);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("LoadWordRoots: " + ex.Message + ".\r\n\r\n" + ex.StackTrace);
            }

            //SaveWordRoots(book);
        }
    }
    public static void SaveWordRoots(Book book)
    {
        if (book != null)
        {
            try
            {
                if (Directory.Exists(Globals.DATA_FOLDER))
                {
                    string filename = Globals.DATA_FOLDER + Path.DirectorySeparatorChar + "word-roots-aliadams.txt";
                    using (StreamWriter writer = new StreamWriter(filename, false, Encoding.UTF8))
                    {
                        if (book.Verses != null)
                        {
                            StringBuilder str = new StringBuilder();
                            foreach (Verse verse in book.Verses)
                            {
                                if (verse.Words != null)
                                {
                                    foreach (Word word in verse.Words)
                                    {
                                        str.Append(word.Address + "\t");
                                        str.Append(word.Text + "\t");
                                        if (word.Roots != null)
                                        {
                                            foreach (string root in word.Roots)
                                            {
                                                //if (root == "ا")
                                                //{
                                                //    str.Append("ء" + "|");
                                                //}
                                                //else if (root == "انن")
                                                //{
                                                //    if (word.Text.Contains("أَنّ"))
                                                //    {
                                                //        str.Append("أنن" + "|");
                                                //    }
                                                //    else if (word.Text.Contains("إِنّ"))
                                                //    {
                                                //        str.Append("إنن" + "|");
                                                //    }
                                                //}
                                                //else if (root == "ان")
                                                //{
                                                //    if (word.Text.Contains("أَن"))
                                                //    {
                                                //        str.Append("أن" + "|");
                                                //    }
                                                //    else if (word.Text.Contains("إِن"))
                                                //    {
                                                //        str.Append("إِن" + "|");
                                                //    }
                                                //}
                                                //else if (
                                                //    (root == "بدء") ||
                                                //    (root == "برء") ||
                                                //    (root == "بطء") ||
                                                //    (root == "بوء") ||
                                                //    (root == "جفء") ||
                                                //    (root == "حمء") ||
                                                //    (root == "خسء") ||
                                                //    (root == "خطء") ||
                                                //    (root == "دءب") ||
                                                //    (root == "درء") ||
                                                //    (root == "شطء") ||
                                                //    (root == "شنء") ||
                                                //    (root == "ضهء") ||
                                                //    (root == "طفء") ||
                                                //    (root == "ظمء") ||
                                                //    (root == "عبء") ||
                                                //    (root == "فتء") ||
                                                //    (root == "فيء") ||
                                                //    (root == "قثء") ||
                                                //    (root == "قرء") ||
                                                //    (root == "كلء") ||
                                                //    (root == "لجء") ||
                                                //    (root == "مرء") ||
                                                //    (root == "ملء") ||
                                                //    (root == "نسء") ||
                                                //    (root == "نشء") ||
                                                //    (root == "نوء") ||
                                                //    (root == "هزء") ||
                                                //    (root == "هيء") ||
                                                //    (root == "وطء") ||
                                                //    (root == "وكء")
                                                //    )
                                                //{
                                                //    str.Append(root.Replace("ء", "أ") + "|");
                                                //}
                                                //else
                                                //{
                                                str.Append(root + "|");
                                                //}
                                            }
                                        }
                                        str.Remove(str.Length - 1, 1); // "|"
                                        str.AppendLine();
                                    }
                                }
                            }

                            str.Remove(str.Length - 2, 2); // "\r\n"
                            writer.WriteLine(str);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("SaveWordRoots: " + ex.Message + ".\r\n\r\n" + ex.StackTrace);
            }
        }
    }
}
