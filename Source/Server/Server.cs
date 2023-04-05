using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using Model;

public class Server
{
    public const string DEFAULT_RECITATION = "Alafasy_64kbps";
    public const string DEFAULT_QURAN_TEXT = "quran-uthmani";
    public const string DEFAULT_TRANSLITERATION = "en.transliteration";

    static Server()
    {
        if (!Directory.Exists(Globals.STATISTICS_FOLDER))
        {
            Directory.CreateDirectory(Globals.STATISTICS_FOLDER);
        }

        if (!Directory.Exists(Globals.RULES_FOLDER))
        {
            Directory.CreateDirectory(Globals.RULES_FOLDER);
        }

        if (!Directory.Exists(Globals.VALUES_FOLDER))
        {
            Directory.CreateDirectory(Globals.VALUES_FOLDER);
        }

        if (!Directory.Exists(Globals.HELP_FOLDER))
        {
            Directory.CreateDirectory(Globals.HELP_FOLDER);
        }

        // load simplification systems
        LoadSimplificationSystems();

        // load numerology systems
        LoadNumerologySystems();

        // load help messages
        LoadHelpMessages();
    }

    // the book [DYNAMIC]
    private static Book s_book = null;
    public static Book Book
    {
        get { return s_book; }
    }

    // loaded simplification systems [STATIC]
    private static Dictionary<string, SimplificationSystem> s_loaded_simplification_systems = null;
    public static Dictionary<string, SimplificationSystem> LoadedSimplificationSystems
    {
        get { return s_loaded_simplification_systems; }
    }
    private static void LoadSimplificationSystems()
    {
        if (s_loaded_simplification_systems == null)
        {
            s_loaded_simplification_systems = new Dictionary<string, SimplificationSystem>();
        }

        if (s_loaded_simplification_systems != null)
        {
            s_loaded_simplification_systems.Clear();

            string path = Globals.RULES_FOLDER;
            DirectoryInfo folder = new DirectoryInfo(path);
            if (folder != null)
            {
                FileInfo[] files = folder.GetFiles("*.txt");
                if ((files != null) && (files.Length > 0))
                {
                    foreach (FileInfo file in files)
                    {
                        string text_mode = file.Name.Remove(file.Name.Length - 4, 4);
                        if (!String.IsNullOrEmpty(text_mode))
                        {
                            LoadSimplificationSystem(text_mode);
                        }
                    }

                    // start with default simplification system
                    if (s_loaded_simplification_systems.ContainsKey(SimplificationSystem.DEFAULT_NAME))
                    {
                        s_simplification_system = new SimplificationSystem(s_loaded_simplification_systems[SimplificationSystem.DEFAULT_NAME]);
                    }
                    else
                    {
                        //TODO Cannot carry exception over Interface notification
                        //throw new Exception("ERROR: No default simplification system was found.");
                    }
                }
            }
        }
    }
    // simplification system [DYNAMIC]
    private static SimplificationSystem s_simplification_system = null;
    public static SimplificationSystem SimplificationSystem
    {
        get { return s_simplification_system; }
    }
    public static void LoadSimplificationSystem(string text_mode)
    {
        if (String.IsNullOrEmpty(text_mode)) return;

        if (s_loaded_simplification_systems != null)
        {
            // remove and rebuild on the fly without restarting application
            if (s_loaded_simplification_systems.ContainsKey(text_mode))
            {
                s_loaded_simplification_systems.Remove(text_mode);
            }

            string filename = Globals.RULES_FOLDER + Path.DirectorySeparatorChar + text_mode + ".txt";
            if (File.Exists(filename))
            {
                List<string> lines = FileHelper.LoadLines(filename);

                SimplificationSystem simplification_system = new SimplificationSystem(text_mode);
                if (simplification_system != null)
                {
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("#")) continue;

                        string[] parts = line.Split('\t');
                        if (parts.Length == 2)
                        {
                            SimplificationRule rule = new SimplificationRule(parts[0], parts[1]);
                            if (rule != null)
                            {
                                simplification_system.Rules.Add(rule);
                            }
                        }
                        else
                        {
                            //TODO Cannot carry exception over Interface notification
                            //throw new Exception(filename + " file format must be:\r\n\tText TAB Replacement");
                        }
                    }
                }

                try
                {
                    // add to dictionary
                    s_loaded_simplification_systems.Add(simplification_system.Name, simplification_system);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + ".\r\n\r\n" + ex.StackTrace);
                }
            }

            // set current simplification system
            if (s_loaded_simplification_systems.ContainsKey(text_mode))
            {
                s_simplification_system = s_loaded_simplification_systems[text_mode];
            }
        }
    }
    public static void BuildSimplifiedBook(string text_mode, bool with_diacritics, bool with_bism_Allah, bool waw_as_word, bool shadda_as_letter, bool hamza_above_horizontal_line_as_letter, bool elf_above_horizontal_line_as_letter, bool yaa_above_horizontal_line_as_letter, bool noon_above_horizontal_line_as_letter)
    {
        if (!String.IsNullOrEmpty(text_mode))
        {
            if (s_loaded_simplification_systems != null)
            {
                if (s_loaded_simplification_systems.ContainsKey(text_mode))
                {
                    s_simplification_system = s_loaded_simplification_systems[text_mode];
                    if (s_simplification_system != null)
                    {
                        // reload original Quran text
                        string filename = Globals.DATA_FOLDER + Path.DirectorySeparatorChar + DEFAULT_QURAN_TEXT + ".txt";
                        List<string> lines = DataAccess.LoadVerseTexts(filename);

                        // generate Quranmarks/Stopmarks word numbers
                        //int[] numbers = new int[]
                        //{
                        //    7, 286, 200, 176, 120, 165, 206, 75, 129, 109,
                        //    123, 111, 43, 52, 99, 128, 111, 110, 98, 135,
                        //    112, 78, 118, 64, 77, 227, 93, 88, 69, 60,
                        //    34, 30, 73, 54, 45, 83, 182, 88, 75, 85,
                        //    54, 53, 89, 59, 37, 35, 38, 29, 18, 45,
                        //    60, 49, 62, 55, 78, 96, 29, 22, 24, 13,
                        //    14, 11, 11, 18, 12, 12, 30, 52, 52, 44,
                        //    28, 28, 20, 56, 40, 31, 50, 40, 46, 42,
                        //    29, 19, 36, 25, 22, 17, 19, 26, 30, 20,
                        //    15, 21, 11, 8, 8, 19, 5, 8, 8, 11,
                        //    11, 8, 3, 9, 5, 4, 7, 3, 6, 3,
                        //    5, 4, 5, 6
                        //};

                        //str.AppendLine
                        //    (
                        //        "#" + "\t" +
                        //        "Chapter" + "\t" +
                        //        "Verse" + "\t" +
                        //        "Word" + "\t" +
                        //        "Stopmark"
                        //      );

                        //int count = 0;
                        //for (int v = 0; v < lines.Count; v++)
                        //{
                        //    string[] words = lines[v].Split();

                        //    int vv = v + 1;
                        //    int cc = 0 + 1;
                        //    foreach (int n in numbers)
                        //    {
                        //        if (vv <= n) break;

                        //        cc++;
                        //        vv -= n;
                        //    }

                        //    for (int w = 0; w < words.Length; w++)
                        //    {
                        //        if (words[w].Length == 1)
                        //        {
                        //            if (
                        //                Constants.STOPMARKS.Contains(words[w][0])
                        //                ||
                        //                Constants.QURANMARKS.Contains(words[w][0]))
                        //            {
                        //                count++;
                        //                str.AppendLine(
                        //                    count + "\t" +
                        //                    cc + "\t" +
                        //                    vv + "\t" +
                        //                    (w + 1) + "\t" +
                        //                    words[w][0]
                        //                  );
                        //            }
                        //        }
                        //    }
                        //}
                        //string filename = "Stopmarks" + ".txt";
                        //if (Directory.Exists(Globals.DATA_FOLDER))
                        //{
                        //    string filepath = Globals.DATA_FOLDER + Path.DirectorySeparatorChar + filename;
                        //    FileHelper.SaveText(filepath, str.ToString());
                        //    FileHelper.DisplayFile(filepath);
                        //}

                        List<Stopmark> verse_stopmarks = DataAccess.LoadVerseStopmarks();

                        // remove bismAllah from 112 chapters
                        if (!with_bism_Allah)
                        {
                            string bimsAllah_text1 = "بِسْمِ ٱللَّهِ ٱلرَّحْمَـٰنِ ٱلرَّحِيمِ ";
                            string bimsAllah_text2 = "بِّسْمِ ٱللَّهِ ٱلرَّحْمَـٰنِ ٱلرَّحِيمِ "; // shadda on baa for chapter 95 and 97
                            for (int i = 0; i < lines.Count; i++)
                            {
                                if (lines[i].StartsWith(bimsAllah_text1))
                                {
                                    lines[i] = lines[i].Replace(bimsAllah_text1, "");
                                }
                                else if (lines[i].StartsWith(bimsAllah_text2))
                                {
                                    lines[i] = lines[i].Replace(bimsAllah_text2, "");
                                }
                            }
                        }

                        // Load WawAsWord words
                        if (waw_as_word)
                        {
                            LoadWawWords();

                            if (s_waw_words != null)
                            {
                                // replace shadda with previous letter and to waw exception list
                                if (shadda_as_letter)
                                {
                                    for (int i = 0; i < lines.Count; i++)
                                    {
                                        string[] word_texts = lines[i].Split();
                                        foreach (string word_text in word_texts)
                                        {
                                            if (s_waw_words.Contains(s_simplification_system.Simplify(word_text)))
                                            {
                                                if (word_text.Contains("ّ"))
                                                {
                                                    string shadda_waw_word = null;
                                                    for (int j = 1; j < word_text.Length; j++)
                                                    {
                                                        if (word_text[j] == 'ّ')
                                                        {
                                                            shadda_waw_word = word_text.Insert(j, word_text[j - 1].ToString());
                                                            s_waw_words.Add(s_simplification_system.Simplify(shadda_waw_word));
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        // replace shadda with previous letter before any simplification
                        if (shadda_as_letter)
                        {
                            for (int i = 0; i < lines.Count; i++)
                            {
                                StringBuilder str = new StringBuilder(lines[i]);
                                for (int j = 1; j < str.Length; j++)
                                {
                                    if (str[j] == 'ّ')
                                    {
                                        str[j] = str[j - 1];
                                    }
                                }
                                lines[i] = str.ToString();
                            }
                        }

                        // convert superscript above horizontal line to letter
                        for (int i = 0; i < lines.Count; i++)
                        {
                            if (hamza_above_horizontal_line_as_letter)
                            {
                                lines[i] = lines[i].Replace("ـٔ", "ء");
                            }

                            if (elf_above_horizontal_line_as_letter)
                            {
                                lines[i] = lines[i].Replace("ـٰ", "ا");
                            }

                            if (yaa_above_horizontal_line_as_letter)
                            {
                                lines[i] = lines[i].Replace("ـۧ", "ي");
                            }

                            if (noon_above_horizontal_line_as_letter)
                            {
                                lines[i] = lines[i].Replace("ـۨ", "ن");
                            }
                        }

                        // simplify verse texts
                        List<string> verse_texts = new List<string>();
                        foreach (string line in lines)
                        {
                            string verse_text = s_simplification_system.Simplify(line);
                            verse_texts.Add(verse_text);
                        }

                        // build verses
                        List<Verse> verses = new List<Verse>();
                        for (int i = 0; i < verse_texts.Count; i++)
                        {
                            Verse verse = new Verse(i + 1, verse_texts[i], verse_stopmarks[i]);
                            if (verse != null)
                            {
                                verses.Add(verse);
                                verse.ApplyWordStopmarks(lines[i]);
                            }
                        }

                        if (s_numerology_system != null)
                        {
                            s_book = new Book(text_mode, verses, with_diacritics);
                            if (s_book != null)
                            {
                                s_book.WithBismAllah = with_bism_Allah;
                                s_book.WawAsWord = waw_as_word;
                                s_book.ShaddaAsLetter = shadda_as_letter;
                                s_book.HamzaAboveHorizontalLineAsLetter = hamza_above_horizontal_line_as_letter;
                                s_book.ElfAboveHorizontalLineAsLetter = elf_above_horizontal_line_as_letter;
                                s_book.YaaAboveHorizontalLineAsLetter = yaa_above_horizontal_line_as_letter;
                                s_book.NoonAboveHorizontalLineAsLetter = noon_above_horizontal_line_as_letter;

                                // build words before DataAccess.Loads
                                if (waw_as_word)
                                {
                                    SplitWawPrefixsAsWords(s_book, text_mode);

                                    // update verses/words/letters numbers and distances
                                    if (s_numerology_system != null)
                                    {
                                        s_book.SetupNumbers();
                                        s_book.SetupOccurrencesFrequencies(with_diacritics);
                                    }
                                }
                                DataAccess.LoadRecitationInfos(s_book);
                                DataAccess.LoadTranslationInfos(s_book);
                                DataAccess.LoadTranslations(s_book);
                                DataAccess.LoadWordMeanings(s_book);
                                DataAccess.LoadWordRoots(s_book);

                                CalculateValue(s_book);
                            }
                        }
                    }
                }
            }
        }
    }
    private static List<string> s_waw_words = null;
    private static void LoadWawWords()
    {
        string filename = Globals.DATA_FOLDER + Path.DirectorySeparatorChar + "waw-words.txt";
        if (File.Exists(filename))
        {
            s_waw_words = new List<string>();
            if (s_waw_words != null)
            {
                List<string> lines = FileHelper.LoadLines(filename);
                foreach (string line in lines)
                {
                    string[] parts = line.Split('\t');
                    if (parts.Length > 0)
                    {
                        s_waw_words.Add(parts[0]);
                    }
                }
            }
        }
    }
    private static void SplitWawPrefixsAsWords(Book book, string text_mode)
    {
        if (book != null)
        {
            string filename = Globals.DATA_FOLDER + Path.DirectorySeparatorChar + "waw-words.txt";
            if (File.Exists(filename))
            {
                // same spelling waw-words but their waw is prefix
                Dictionary<string, List<Verse>> non_exception_words_in_verses = new Dictionary<string, List<Verse>>();

                List<string> lines = FileHelper.LoadLines(filename);
                foreach (string line in lines)
                {
                    string[] parts = line.Split('\t');
                    if (parts.Length > 0)
                    {
                        string exception_word = parts[0];
                        if (parts.Length > 1)
                        {
                            string[] sub_parts = parts[1].Split(',');
                            foreach (string sub_part in sub_parts)
                            {
                                string[] verse_address_parts = sub_part.Split(':');
                                if (verse_address_parts.Length == 2)
                                {
                                    try
                                    {
                                        Chapter chapter = book.Chapters[int.Parse(verse_address_parts[0]) - 1];
                                        if (chapter != null)
                                        {
                                            Verse verse = chapter.Verses[int.Parse(verse_address_parts[1]) - 1];
                                            if (verse != null)
                                            {
                                                if (non_exception_words_in_verses.ContainsKey(exception_word))
                                                {
                                                    List<Verse> verses = non_exception_words_in_verses[exception_word];
                                                    if (verses != null)
                                                    {
                                                        verses.Add(verse);
                                                    }
                                                }
                                                else
                                                {
                                                    List<Verse> verses = new List<Verse>();
                                                    if (verses != null)
                                                    {
                                                        verses.Add(verse);
                                                        non_exception_words_in_verses.Add(exception_word, verses);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    catch
                                    {
                                        // skip error
                                    }
                                }
                            }
                        }
                    }
                }

                if (s_waw_words != null)
                {
                    foreach (Verse verse in book.Verses)
                    {
                        StringBuilder str = new StringBuilder();
                        if (verse.Words.Count > 0)
                        {
                            for (int i = 0; i < verse.Words.Count; i++)
                            {
                                if (verse.Words[i].Text.StartsWith("و"))
                                {
                                    if (!s_waw_words.Contains(verse.Words[i].Text))
                                    {
                                        str.Append(verse.Words[i].Text.Insert(1, " ") + " ");
                                    }
                                    else // don't split exception words unless they are in non_exception_words_in_verses
                                    {
                                        if (non_exception_words_in_verses.ContainsKey(verse.Words[i].Text))
                                        {
                                            if (non_exception_words_in_verses[verse.Words[i].Text].Contains(verse))
                                            {
                                                str.Append(verse.Words[i].Text.Insert(1, " ") + " ");
                                            }
                                            else
                                            {
                                                str.Append(verse.Words[i].Text + " ");
                                            }
                                        }
                                        else
                                        {
                                            str.Append(verse.Words[i].Text + " ");
                                        }
                                    }
                                }
                                else
                                {
                                    str.Append(verse.Words[i].Text + " ");
                                }
                            }
                            if (str.Length > 1)
                            {
                                str.Remove(str.Length - 1, 1); // " "
                            }
                        }

                        // re-create new Words with word stopmarks
                        verse.RecreateWordsApplyStopmarks(str.ToString());
                    }
                }
            }
        }
    }

    // loaded numerology systems [STATIC]
    private static Dictionary<string, NumerologySystem> s_loaded_numerology_systems = null;
    public static Dictionary<string, NumerologySystem> LoadedNumerologySystems
    {
        get { return s_loaded_numerology_systems; }
    }
    private static void LoadNumerologySystems()
    {
        if (s_loaded_numerology_systems == null)
        {
            s_loaded_numerology_systems = new Dictionary<string, NumerologySystem>();
        }

        if (s_loaded_numerology_systems != null)
        {
            s_loaded_numerology_systems.Clear();

            if (Directory.Exists(Globals.VALUES_FOLDER))
            {
                string path = Globals.VALUES_FOLDER;
                DirectoryInfo folder = new DirectoryInfo(path);
                if (folder != null)
                {
                    FileInfo[] files = folder.GetFiles("*.txt");
                    if ((files != null) && (files.Length > 0))
                    {
                        foreach (FileInfo file in files)
                        {
                            string numerology_system_name = file.Name.Remove(file.Name.Length - 4, 4);

                            if (!String.IsNullOrEmpty(numerology_system_name))
                            {
                                string[] parts = numerology_system_name.Split('_');
                                if (parts.Length == 3)
                                {
                                    if (s_loaded_simplification_systems.ContainsKey(parts[0]))
                                    {
                                        LoadNumerologySystem(numerology_system_name);
                                    }
                                }
                                else
                                {
                                    if (!file.Name.StartsWith("_"))
                                    {
                                        //TODO Cannot carry exception over Interface notification
                                        //throw new Exception("ERROR: " + file.FullName + " must contain 3 parts separated by \"_\".");
                                    }
                                }
                            }
                        }

                        // start with default numerology system
                        if (s_loaded_numerology_systems.ContainsKey(NumerologySystem.DEFAULT_NAME))
                        {
                            s_numerology_system = new NumerologySystem(s_loaded_numerology_systems[NumerologySystem.DEFAULT_NAME]);
                        }
                        else
                        {
                            //TODO Cannot carry exception over Interface notification
                            //throw new Exception("ERROR: No default numerology system was found.");
                        }
                    }
                }
            }
        }
    }
    // numerology system [DYNAMIC]
    private static NumerologySystem s_numerology_system = null;
    public static NumerologySystem NumerologySystem
    {
        get { return s_numerology_system; }
        set { s_numerology_system = value; }
    }
    public static void LoadNumerologySystem(string numerology_system_name)
    {
        if (String.IsNullOrEmpty(numerology_system_name)) return;
        if (numerology_system_name.Contains("DNA")) return;

        if (s_loaded_numerology_systems != null)
        {
            string filename = Globals.VALUES_FOLDER + Path.DirectorySeparatorChar + numerology_system_name + ".txt";
            if (File.Exists(filename))
            {
                List<string> lines = FileHelper.LoadLines(filename);

                NumerologySystem numerology_system = new NumerologySystem(numerology_system_name);
                if (numerology_system != null)
                {
                    numerology_system.LetterValues.Clear();
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("#")) continue;

                        string[] parts = line.Split('\t');
                        if (parts.Length == 2)
                        {
                            try
                            {
                                numerology_system.LetterValues.Add(parts[0][0], long.Parse(parts[1]));
                            }
                            catch
                            {
                                //TODO Cannot carry exception over Interface notification
                                //throw new Exception(filename + " file format must be:\r\n\tLetter TAB Value");
                            }
                        }
                        else
                        {
                            //TODO Cannot carry exception over Interface notification
                            //throw new Exception(filename + " file format must be:\r\n\tLetter TAB Value");
                        }
                    }
                }

                if (s_loaded_numerology_systems.ContainsKey(numerology_system_name))
                {
                    s_loaded_numerology_systems[numerology_system.Name] = numerology_system;
                }
                else
                {
                    s_loaded_numerology_systems.Add(numerology_system.Name, numerology_system);
                }

                // set current numerology system
                if (s_loaded_numerology_systems.ContainsKey(numerology_system_name))
                {
                    s_numerology_system = new NumerologySystem(s_loaded_numerology_systems[numerology_system_name]);
                }
            }
        }

    }
    public static void SaveNumerologySystem(string numerology_system_name)
    {
        if (String.IsNullOrEmpty(numerology_system_name)) return;
        if (numerology_system_name.Contains("DNA")) return;
        if (numerology_system_name.Contains("ResearchEdition")) return;

        if (s_loaded_numerology_systems != null)
        {
            if (s_loaded_numerology_systems.ContainsKey(numerology_system_name))
            {
                NumerologySystem numerology_system = s_loaded_numerology_systems[numerology_system_name];
                if (numerology_system != null)
                {
                    if (Directory.Exists(Globals.VALUES_FOLDER))
                    {
                        string filename = Globals.VALUES_FOLDER + Path.DirectorySeparatorChar + numerology_system.Name + ".txt";
                        try
                        {
                            using (StreamWriter writer = new StreamWriter(filename, false, Encoding.Unicode))
                            {
                                foreach (char key in numerology_system.Keys)
                                {
                                    writer.WriteLine(key + "\t" + numerology_system[key].ToString());
                                }
                            }
                        }
                        catch
                        {
                            // silence IO error in case running from read-only media (CD/DVD)
                        }
                    }
                }
            }
        }
    }
    public static void UpdateNumerologySystem(string text)
    {
        if (s_numerology_system != null)
        {
            if (!String.IsNullOrEmpty(text))
            {
                text = text.Replace("\r", "");
                text = text.Replace("\n", " ");
                text = text.Replace("\t", "");
                text = text.Replace("_", "");
                //text = text.Replace(" ", "");
                text = text.Replace(Constants.ORNATE_RIGHT_PARENTHESIS, "");
                text = text.Replace(Constants.ORNATE_LEFT_PARENTHESIS, "");
                foreach (char character in Constants.INDIAN_DIGITS)
                {
                    text = text.Replace(character.ToString(), "");
                }
                foreach (char character in Constants.ARABIC_DIGITS)
                {
                    text = text.Replace(character.ToString(), "");
                }
                foreach (char character in Constants.SYMBOLS)
                {
                    text = text.Replace(character.ToString(), "");
                }
                foreach (char character in Constants.STOPMARKS)
                {
                    text = text.Replace(character.ToString(), "");
                }
                foreach (char character in Constants.QURANMARKS)
                {
                    text = text.Replace(character.ToString(), "");
                }
                foreach (char character in Constants.DIACRITICS)
                {
                    text = text.Replace(character.ToString(), "");
                }

                BuildLetterStatistics(text);

                BuildNumerologySystem(text);

                if (s_book != null)
                {
                    if (s_book.Verses != null)
                    {
                        foreach (Verse verse in s_book.Verses)
                        {
                            CalculateValue(verse);
                        }
                    }
                }
            }
        }
    }
    private static void BuildNumerologySystem(string text)
    {
        if (s_loaded_numerology_systems != null)
        {
            if (s_numerology_system != null)
            {
                if (text != null)
                {
                    // build letter_order using letters in text only
                    string numerology_system_name = s_numerology_system.Name;
                    if (s_loaded_numerology_systems.ContainsKey(numerology_system_name))
                    {
                        NumerologySystem loaded_numerology_system = s_loaded_numerology_systems[numerology_system_name];

                        // re-generate numerology systems
                        List<char> letter_order = new List<char>();
                        List<long> letter_values = new List<long>();

                        switch (s_numerology_system.LetterOrder)
                        {
                            case "Alphabet":
                            case "Alphabet▲":
                                {
                                    LetterStatistic.CompareBy = StatisticCompareBy.Letter;
                                    LetterStatistic.CompareOrder = StatisticCompareOrder.Ascending;
                                    s_letter_statistics.Sort();
                                    foreach (LetterStatistic letter_statistic in s_letter_statistics)
                                    {
                                        letter_order.Add(letter_statistic.Letter);
                                    }
                                }
                                break;
                            case "Alphabet▼":
                                {
                                    LetterStatistic.CompareBy = StatisticCompareBy.Letter;
                                    LetterStatistic.CompareOrder = StatisticCompareOrder.Descending;
                                    s_letter_statistics.Sort();
                                    foreach (LetterStatistic letter_statistic in s_letter_statistics)
                                    {
                                        letter_order.Add(letter_statistic.Letter);
                                    }
                                }
                                break;
                            case "Appearance":
                            case "Appearance▲":
                                {
                                    LetterStatistic.CompareBy = StatisticCompareBy.Order;
                                    LetterStatistic.CompareOrder = StatisticCompareOrder.Ascending;
                                    s_letter_statistics.Sort();
                                    foreach (LetterStatistic letter_statistic in s_letter_statistics)
                                    {
                                        letter_order.Add(letter_statistic.Letter);
                                    }
                                }
                                break;
                            case "Appearance▼":
                                {
                                    LetterStatistic.CompareBy = StatisticCompareBy.Order;
                                    LetterStatistic.CompareOrder = StatisticCompareOrder.Descending;
                                    s_letter_statistics.Sort();
                                    foreach (LetterStatistic letter_statistic in s_letter_statistics)
                                    {
                                        letter_order.Add(letter_statistic.Letter);
                                    }
                                }
                                break;
                            case "Frequency▲":
                                {
                                    LetterStatistic.CompareBy = StatisticCompareBy.Frequency;
                                    LetterStatistic.CompareOrder = StatisticCompareOrder.Ascending;
                                    s_letter_statistics.Sort();
                                    foreach (LetterStatistic letter_statistic in s_letter_statistics)
                                    {
                                        letter_order.Add(letter_statistic.Letter);
                                    }
                                }
                                break;
                            case "Frequency▼":
                            case "Frequency":
                                {
                                    LetterStatistic.CompareBy = StatisticCompareBy.Frequency;
                                    LetterStatistic.CompareOrder = StatisticCompareOrder.Descending;
                                    s_letter_statistics.Sort();
                                    foreach (LetterStatistic letter_statistic in s_letter_statistics)
                                    {
                                        letter_order.Add(letter_statistic.Letter);
                                    }
                                }
                                break;
                            default: // use static numerology system
                                {
                                    foreach (char character in loaded_numerology_system.LetterValues.Keys)
                                    {
                                        if (text.Contains(character.ToString()))
                                        {
                                            letter_order.Add(character);
                                        }
                                    }
                                }
                                break;
                        }

                        if (letter_order.Count > 0)
                        {
                            if (s_numerology_system.Name.EndsWith("Linear"))
                            {
                                for (int i = 0; i < letter_order.Count; i++)
                                {
                                    letter_values.Add(i + 1L);
                                }
                            }
                            else if (s_numerology_system.Name.EndsWith("NonAdditivePrimes1"))
                            {
                                letter_values.Add(1L);
                                for (int i = 0; i < letter_order.Count - 1; i++)
                                {
                                    letter_values.Add(Numbers.NonAdditivePrimes[i]);
                                }
                            }
                            else if (s_numerology_system.Name.EndsWith("NonAdditivePrimes"))
                            {
                                for (int i = 0; i < letter_order.Count; i++)
                                {
                                    letter_values.Add(Numbers.NonAdditivePrimes[i]);
                                }
                            }
                            else if (s_numerology_system.Name.EndsWith("AdditivePrimes1"))
                            {
                                letter_values.Add(1L);
                                for (int i = 0; i < letter_order.Count - 1; i++)
                                {
                                    letter_values.Add(Numbers.AdditivePrimes[i]);
                                }
                            }
                            else if (s_numerology_system.Name.EndsWith("AdditivePrimes"))
                            {
                                for (int i = 0; i < letter_order.Count; i++)
                                {
                                    letter_values.Add(Numbers.AdditivePrimes[i]);
                                }
                            }
                            else if (s_numerology_system.Name.EndsWith("Primes1"))
                            {
                                letter_values.Add(1L);
                                for (int i = 0; i < letter_order.Count - 1; i++)
                                {
                                    letter_values.Add(Numbers.Primes[i]);
                                }
                            }
                            else if (s_numerology_system.Name.EndsWith("Primes"))
                            {
                                for (int i = 0; i < letter_order.Count; i++)
                                {
                                    letter_values.Add(Numbers.Primes[i]);
                                }
                            }
                            else if (s_numerology_system.Name.EndsWith("NonAdditiveComposites"))
                            {
                                for (int i = 0; i < letter_order.Count; i++)
                                {
                                    letter_values.Add(Numbers.NonAdditiveComposites[i]);
                                }
                            }
                            else if (s_numerology_system.Name.EndsWith("AdditiveComposites"))
                            {
                                for (int i = 0; i < letter_order.Count; i++)
                                {
                                    letter_values.Add(Numbers.AdditiveComposites[i]);
                                }
                            }
                            else if (s_numerology_system.Name.EndsWith("Composites"))
                            {
                                for (int i = 0; i < letter_order.Count; i++)
                                {
                                    letter_values.Add(Numbers.Composites[i]);
                                }
                            }
                            else if (s_numerology_system.Name.EndsWith("Frequency▲"))
                            {
                                // letter-frequency mismacth: different letters for different frequencies
                                LetterStatistic.CompareBy = StatisticCompareBy.Frequency;
                                LetterStatistic.CompareOrder = StatisticCompareOrder.Ascending;
                                s_letter_statistics.Sort();
                                foreach (LetterStatistic letter_statistic in s_letter_statistics)
                                {
                                    letter_values.Add(letter_statistic.Frequency);
                                }
                            }
                            else if (s_numerology_system.Name.EndsWith("Frequency"))
                            {
                                letter_order.Clear();
                                foreach (LetterStatistic letter_statistic in s_letter_statistics)
                                {
                                    letter_order.Add(letter_statistic.Letter);
                                    letter_values.Add(letter_statistic.Frequency);
                                }
                            }
                            else if (s_numerology_system.Name.EndsWith("Frequency▼"))
                            {
                                // letter-frequency mismacth: different letters for different frequencies
                                LetterStatistic.CompareBy = StatisticCompareBy.Frequency;
                                LetterStatistic.CompareOrder = StatisticCompareOrder.Descending;
                                s_letter_statistics.Sort();
                                foreach (LetterStatistic letter_statistic in s_letter_statistics)
                                {
                                    letter_values.Add(letter_statistic.Frequency);
                                }
                            }
                            else // if not defined in Numbers
                            {
                                // use loadeded numerology system instead
                                foreach (long value in loaded_numerology_system.LetterValues.Values)
                                {
                                    letter_values.Add(value);
                                }
                            }
                        }
                        else // if not defined in Numbers
                        {
                            // use loadeded numerology system instead
                            foreach (long value in loaded_numerology_system.LetterValues.Values)
                            {
                                letter_values.Add(value);
                            }
                        }

                        // rebuild the current numerology system
                        s_numerology_system.Clear();
                        for (int i = 0; i < letter_order.Count; i++)
                        {
                            s_numerology_system.Add(letter_order[i], letter_values[i]);
                        }
                    }
                }
            }
        }
    }

    // letter statistics [DYNAMIC]
    private static List<LetterStatistic> s_letter_statistics = new List<LetterStatistic>();
    private static void BuildLetterStatistics(string text)
    {
        if (text == null) // null means Book scope
        {
            if (s_book != null)
            {
                text = s_book.Text;
            }
        }
        if (text != null)
        {
            if (s_letter_statistics != null)
            {
                s_letter_statistics.Clear();
                for (int i = 0; i < text.Length; i++)
                {
                    // calculate letter frequency
                    bool is_found = false;
                    for (int j = 0; j < s_letter_statistics.Count; j++)
                    {
                        if (text[i] == s_letter_statistics[j].Letter)
                        {
                            s_letter_statistics[j].Frequency++;
                            is_found = true;
                            break;
                        }
                    }

                    // add entry into dictionary
                    if (!is_found)
                    {
                        LetterStatistic letter_statistic = new LetterStatistic();
                        letter_statistic.Order = s_letter_statistics.Count + 1;
                        letter_statistic.Letter = text[i];
                        letter_statistic.Frequency++;
                        s_letter_statistics.Add(letter_statistic);
                    }
                }
            }
        }
    }

    // for user text or Quran highlighted text
    public static long CalculateValue(char character)
    {
        if (character == '\0') return 0L;
        if (character == '\r') return 0L;
        if (character == '\n') return 0L;
        if (s_numerology_system == null) return 0L;

        return s_numerology_system.CalculateValue(character);
    }
    public static long CalculateValue(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0L;

        long result = 0L;

        if (s_numerology_system != null)
        {
            text = text.Replace("\r\n", "\n");
            char[] separators1 = { '\n' };
            string[] line_texts = text.Split(separators1, StringSplitOptions.RemoveEmptyEntries);
            char[] separators2 = { '\n', ' ' };
            string[] word_texts = text.Split(separators2, StringSplitOptions.RemoveEmptyEntries);

            foreach (string word_text in word_texts)
            {
                foreach (char c in word_text)
                {
                    result += s_numerology_system.CalculateValue(c);
                }
            }
        }

        return result;
    }
    // for Quran text
    public static long CalculateValue(Letter letter)
    {
        if (letter == null) return 0L;

        long result = 0L;

        if (s_numerology_system != null)
        {
            result = s_numerology_system.CalculateValue(letter.Character);
        }

        letter.Value = result; // update value for CompareBy.Value

        return result;
    }
    public static long CalculateValue(List<Letter> letters)
    {
        if (letters == null) return 0L;
        if (letters.Count == 0) return 0L;

        long result = 0L;

        if (s_numerology_system != null)
        {
            foreach (Letter letter in letters)
            {
                result += CalculateValue(letter);
            }
        }

        return result;
    }
    public static long CalculateValue(Word word)
    {
        if (word == null) return 0L;

        long result = 0L;

        if (s_numerology_system != null)
        {
            foreach (Letter letter in word.Letters)
            {
                result += CalculateValue(letter);
            }

            word.Value = result; // update value for CompareBy.Value
        }

        return result;
    }
    public static long CalculateValue(List<Word> words)
    {
        if (words == null) return 0L;
        if (words.Count == 0) return 0L;

        long result = 0L;

        if (s_numerology_system != null)
        {
            foreach (Word word in words)
            {
                result += CalculateValue(word);
            }
        }

        return result;
    }
    public static long CalculateValue(Sentence sentence)
    {
        if (sentence == null) return 0L;

        long result = 0L;

        if (s_numerology_system != null)
        {
            List<Word> words = s_book.GetCompleteWords(sentence);
            if (words != null)
            {
                foreach (Word word in words)
                {
                    result += CalculateValue(word);
                }
            }
        }

        return result;
    }
    public static long CalculateValue(Verse verse)
    {
        if (verse == null) return 0L;

        long result = 0L;

        if (s_numerology_system != null)
        {
            foreach (Word word in verse.Words)
            {
                result += CalculateValue(word);
            }
        }

        return result;
    }
    public static long CalculateValue(Verse verse, Letter from_letter, Letter to_letter)
    {
        if (verse == null) return 0L;
        if (from_letter == null) return 0L;
        if (to_letter == null) return 0L;

        long result = 0L;

        if (s_numerology_system != null)
        {
            int word_index = -1;   // in verse
            int letter_index = -1; // in verse
            bool done = false;
            foreach (Word word in verse.Words)
            {
                word_index++;

                if ((word.Letters != null) && (word.Letters.Count > 0))
                {
                    foreach (Letter letter in word.Letters)
                    {
                        letter_index++;

                        if (letter_index < from_letter.NumberInVerse - 1) continue;
                        if (letter_index > to_letter.NumberInVerse - 1)
                        {
                            done = true;
                            break;
                        }

                        result += CalculateValue(letter);
                    }
                }

                if (done) break;
            }
        }

        return result;
    }
    public static long CalculateValue(List<Verse> verses)
    {
        if (verses == null) return 0L;
        if (verses.Count == 0) return 0L;

        long result = 0L;

        if (s_numerology_system != null)
        {
            foreach (Verse verse in verses)
            {
                result += CalculateValue(verse);
            }
        }

        return result;
    }
    public static long CalculateValue(List<Verse> verses, Letter start_letter, Letter end_letter)
    {
        if (verses == null) return 0L;
        if (verses.Count == 0) return 0L;
        if (start_letter == null) return 0L;
        if (end_letter == null) return 0L;

        long result = 0L;

        if (s_numerology_system != null)
        {
            if (verses.Count == 1)
            {
                result += CalculateValue(verses[0], start_letter, end_letter);
            }
            else if (verses.Count == 2)
            {
                Word first_verse_end_word = verses[0].Words[verses[0].Words.Count - 1];
                if (first_verse_end_word != null)
                {
                    if (first_verse_end_word.Letters.Count > 0)
                    {
                        Letter first_verse_end_letter = first_verse_end_word.Letters[first_verse_end_word.Letters.Count - 1];
                        if (first_verse_end_letter != null)
                        {
                            result += CalculateValue(verses[0], start_letter, first_verse_end_letter);
                        }
                    }
                }

                Word last_verse_start_word = verses[1].Words[0];
                if (last_verse_start_word != null)
                {
                    if (last_verse_start_word.Letters.Count > 0)
                    {
                        Letter last_verse_start_letter = last_verse_start_word.Letters[0];
                        if (last_verse_start_letter != null)
                        {
                            result += CalculateValue(verses[1], last_verse_start_letter, end_letter);
                        }
                    }
                }
            }
            else //if (verses.Count > 2)
            {
                // WARNING: no null check ???
                bool first_verse_is_fully_selected = (start_letter.NumberInChapter == 1);
                bool last_verse_is_fully_selected = (end_letter.NumberInChapter == end_letter.Word.Verse.Chapter.LetterCount);
                Chapter first_chapter = start_letter.Word.Verse.Chapter;
                Chapter last_chapter = end_letter.Word.Verse.Chapter;

                // first verse
                Word first_verse_end_word = verses[0].Words[verses[0].Words.Count - 1];
                if (first_verse_end_word != null)
                {
                    if (first_verse_end_word.Letters.Count > 0)
                    {
                        Letter first_verse_end_letter = first_verse_end_word.Letters[first_verse_end_word.Letters.Count - 1];
                        if (first_verse_end_letter != null)
                        {
                            result += CalculateValue(verses[0], start_letter, first_verse_end_letter);
                        }
                    }
                }

                // middle verses
                for (int i = 1; i < verses.Count - 1; i++)
                {
                    result += CalculateValue(verses[i]);
                }

                // last verse
                Word last_verse_start_word = verses[verses.Count - 1].Words[0];
                if (last_verse_start_word != null)
                {
                    if (last_verse_start_word.Letters.Count > 0)
                    {
                        Letter last_verse_start_letter = last_verse_start_word.Letters[0];
                        if (last_verse_start_letter != null)
                        {
                            result += CalculateValue(verses[verses.Count - 1], last_verse_start_letter, end_letter);
                        }
                    }
                }
            }
        }

        return result;
    }
    public static long CalculateValue(Chapter chapter)
    {
        if (chapter == null) return 0L;

        long result = 0L;

        if (s_numerology_system != null)
        {
            result += CalculateValue(chapter.Verses);
        }

        return result;
    }
    public static long CalculateValue(List<Chapter> chapters)
    {
        if (chapters == null) return 0L;
        if (chapters.Count == 0) return 0L;

        long result = 0L;

        if (s_numerology_system != null)
        {
            foreach (Chapter chapter in chapters)
            {
                result += CalculateValue(chapter);
            }
        }

        return result;
    }
    public static long CalculateValue(Book book)
    {
        if (book == null) return 0L;

        long result = 0L;

        if (s_numerology_system != null)
        {
            result += CalculateValue(book.Chapters);
        }

        return result;
    }


    // helper methods for finds
    private static List<Verse> GetVerses(List<Word> words)
    {
        List<Verse> result = new List<Verse>();

        if (words != null)
        {
            foreach (Word word in words)
            {
                if (word != null)
                {
                    if (!result.Contains(word.Verse))
                    {
                        result.Add(word.Verse);
                    }
                }
            }
        }

        return result;
    }
    private static List<Verse> GetVerses(List<Phrase> phrases)
    {
        List<Verse> result = new List<Verse>();

        if (phrases != null)
        {
            foreach (Phrase phrase in phrases)
            {
                if (phrase != null)
                {
                    if (!result.Contains(phrase.Verse))
                    {
                        result.Add(phrase.Verse);
                    }
                }
            }
        }

        return result;
    }
    private static List<Chapter> GetSourceChapters(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses)
    {
        List<Chapter> result = new List<Chapter>();

        if (s_book != null)
        {
            if (search_scope == SearchScope.Book)
            {
                result = s_book.Chapters;
            }
            else if (search_scope == SearchScope.Selection)
            {
                result = current_selection.Chapters;
            }
            else if (search_scope == SearchScope.Result)
            {
                if (previous_verses != null)
                {
                    result = s_book.GetChapters(previous_verses);
                }
            }
        }

        return result;
    }
    private static List<Chapter> GetChapters(List<Phrase> phrases)
    {
        List<Chapter> result = new List<Chapter>();

        if (phrases != null)
        {
            foreach (Phrase phrase in phrases)
            {
                if (phrase != null)
                {
                    if (!result.Contains(phrase.Verse.Chapter))
                    {
                        result.Add(phrase.Verse.Chapter);
                    }
                }
            }
        }

        return result;
    }
    private static List<Phrase> BuildPhrases(Verse verse, MatchCollection matches, bool with_diacritics)
    {
        List<Phrase> result = new List<Phrase>();

        foreach (Match match in matches)
        {
            foreach (Capture capture in match.Captures)
            {
                string text = capture.Value;
                int position = capture.Index;
                if (s_numerology_system != null)
                {
                    if (s_numerology_system.TextMode == "Original")
                    {
                        if (with_diacritics)
                        {
                            result.Add(new Phrase(verse, position, text));
                        }
                        else
                        {
                            result.Add(OriginifyPhrase(new Phrase(verse, position, text)));
                        }
                    }
                    else
                    {
                        result.Add(new Phrase(verse, position, text));
                    }
                }
            }
        }

        return result;
    }
    private static Phrase OriginifyPhrase(Phrase phrase)
    {
        if (phrase != null)
        {
            Verse verse = phrase.Verse;
            if (verse != null)
            {
                string text = phrase.Text;
                int position = phrase.Position;

                int start = 0;
                for (int i = 0; i < verse.Text.Length; i++)
                {
                    char character = verse.Text[i];

                    if ((character == ' ') || (Constants.ARABIC_LETTERS.Contains(character)))
                    {
                        start++;
                    }
                    else if ((Constants.STOPMARKS.Contains(character)) || (Constants.QURANMARKS.Contains(character)))
                    {
                        // superscript Seen letter in words وَيَبْصُۜطُ and بَصْۜطَةًۭ are not stopmarks
                        // Quran 2:245  مَّن ذَا ٱلَّذِى يُقْرِضُ ٱللَّهَ قَرْضًا حَسَنًۭا فَيُضَٰعِفَهُۥ لَهُۥٓ أَضْعَافًۭا كَثِيرَةًۭ ۚ وَٱللَّهُ يَقْبِضُ وَيَبْصُۜطُ وَإِلَيْهِ تُرْجَعُونَ
                        // Quran 7:69  أَوَعَجِبْتُمْ أَن جَآءَكُمْ ذِكْرٌۭ مِّن رَّبِّكُمْ عَلَىٰ رَجُلٍۢ مِّنكُمْ لِيُنذِرَكُمْ ۚ وَٱذْكُرُوٓا۟ إِذْ جَعَلَكُمْ خُلَفَآءَ مِنۢ بَعْدِ قَوْمِ نُوحٍۢ وَزَادَكُمْ فِى ٱلْخَلْقِ بَصْۜطَةًۭ ۖ فَٱذْكُرُوٓا۟ ءَالَآءَ ٱللَّهِ لَعَلَّكُمْ تُفْلِحُونَ
                        if
                            (
                                (character == 'ۜ') &&  // superscript Seen
                                (
                                    ((verse.Chapter.Number == 2) && (verse.NumberInChapter == 245))
                                    ||
                                    ((verse.Chapter.Number == 7) && (verse.NumberInChapter == 69))
                                )
                            )
                        {
                            // not a stopmark but a Seen above Ssad so treat as part in its word
                        }
                        else
                        {
                            start--; // skip the space after stopmark
                        }
                    }
                    else
                    {
                        // treat character as part of its word
                    }

                    // i has reached phrase start
                    if (start > position)
                    {
                        int phrase_length = text.Trim().Length;
                        StringBuilder str = new StringBuilder();

                        int length = 0;
                        for (int j = i; j < verse.Text.Length; j++)
                        {
                            character = verse.Text[j];
                            str.Append(character);

                            if ((character == ' ') || (Constants.ARABIC_LETTERS.Contains(character)))
                            {
                                length++;
                            }
                            else if ((Constants.STOPMARKS.Contains(character)) || (Constants.QURANMARKS.Contains(character)))
                            {
                                length--; // ignore space after stopmark
                                if (length < 0)
                                {
                                    length = 0;
                                }
                            }

                            // j has reached phrase end
                            if (length == phrase_length)
                            {
                                return new Phrase(verse, i, str.ToString());
                            }
                        }
                    }
                }
            }
        }
        return null;
    }
    //??? Doesn't work with FindByNumbers "L" in Original text_mode
    public static Phrase SwitchTextMode(Phrase phrase, string to_text_mode)
    {
        if (phrase != null)
        {
            Verse phrase_verse = phrase.Verse;
            int phrase_position = phrase.Position;
            string phrase_text = phrase.Text;
            if (phrase_text != null)
            {
                int phrase_length = phrase_text.Length;

                if (phrase_verse != null)
                {
                    if (to_text_mode == "Original")
                    {
                        Verse original_verse = phrase_verse;
                        int letter_count = 0;
                        int position = 0;
                        foreach (char c in original_verse.Text)
                        {
                            position++;
                            if ((c == ' ') || (Constants.ARABIC_LETTERS.Contains(c)))
                            {
                                letter_count++;
                            }

                            if (letter_count == phrase_position)
                            {
                                break;
                            }
                        }

                        foreach (char c in original_verse.Text)
                        {
                            position++;
                            if ((c == ' ') || (Constants.ARABIC_LETTERS.Contains(c)))
                            {
                                letter_count++;
                            }

                            if (letter_count == phrase_position)
                            {
                                break;
                            }
                        }

                        letter_count = 0;
                        StringBuilder str = new StringBuilder();
                        for (int i = position; i < phrase_verse.Text.Length; i++)
                        {
                            char character = phrase_verse.Text[i];
                            str.Append(character);

                            if ((character == ' ') || (Constants.ARABIC_LETTERS.Contains(character)))
                            {
                                letter_count++;
                            }
                            else if (Constants.STOPMARKS.Contains(character))
                            {
                                letter_count--; // decrement space after stopmark as it will be incremented above
                                if (letter_count < 0)
                                {
                                    letter_count = 0;
                                }
                            }
                            else if (Constants.QURANMARKS.Contains(character))
                            {
                                letter_count--; // decrement space after quranmark as it will be incremented above
                            }

                            // check if finished
                            if (letter_count == phrase_length)
                            {
                                // skip any non-letter at start
                                int index = position;
                                if ((index > 0) && (index < phrase_verse.Text.Length))
                                {
                                    character = phrase_verse.Text[index];
                                    if (!Constants.ARABIC_LETTERS.Contains(character))
                                    {
                                        position++;
                                        str.Append(" "); // increment length
                                    }
                                }

                                // skip any non-letter at end
                                index = position + str.Length - 1;
                                if ((index > 0) && (position + str.Length < phrase_verse.Text.Length))
                                {
                                    character = phrase_verse.Text[index];
                                    if (!Constants.ARABIC_LETTERS.Contains(character))
                                    {
                                        str.Append(" "); // increment length
                                    }
                                }

                                return new Phrase(phrase_verse, position, str.ToString());
                            }
                        }
                    }
                    else // if (to_text_mode != "Original")
                    {
                        // simplify text
                        phrase_text = phrase_text.Simplify(to_text_mode);
                        phrase_text = phrase_text.Trim();
                        if (!String.IsNullOrEmpty(phrase_text)) // re-test in case text was just harakaat which is simplifed to nothing
                        {
                            // simplify phrase
                            string verse_text = phrase_verse.Text.Simplify(to_text_mode);
                            phrase_position = verse_text.IndexOf(phrase_text);  //????? will ONLY build first phrase occurrence in verse

                            // build simplified phrase
                            return new Phrase(phrase_verse, phrase_position, phrase_text);
                        }
                    }
                }
            }
        }
        return null;
    }
    public static List<Verse> GetSourceVerses(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, TextLocationInChapter text_location_in_chapter)
    {
        List<Verse> result = new List<Verse>();

        List<Verse> verses = new List<Verse>();
        if (s_book != null)
        {
            if (search_scope == SearchScope.Book)
            {
                verses = s_book.Verses;
            }
            else if (search_scope == SearchScope.Selection)
            {
                verses = current_selection.Verses;
            }
            else if (search_scope == SearchScope.Result)
            {
                if (previous_verses != null)
                {
                    verses = new List<Verse>(previous_verses);
                }
            }
        }

        switch (text_location_in_chapter)
        {
            case TextLocationInChapter.AtStart:
                {
                    foreach (Verse verse in verses)
                    {
                        if (verse.NumberInChapter == 1)
                        {
                            result.Add(verse);
                        }
                    }
                }
                break;
            case TextLocationInChapter.AtMiddle:
                {
                    foreach (Verse verse in verses)
                    {
                        if ((verse.NumberInChapter != 1) && (verse.NumberInChapter != verse.Chapter.Verses.Count))
                        {
                            result.Add(verse);
                        }
                    }
                }
                break;
            case TextLocationInChapter.AtEnd:
                {
                    foreach (Verse verse in verses)
                    {
                        if (verse.NumberInChapter == verse.Chapter.Verses.Count)
                        {
                            result.Add(verse);
                        }
                    }
                }
                break;
            case TextLocationInChapter.Any:
            default:
                {
                    result = verses;
                }
                break;
        }

        return result;
    }

    // find by text - Exact
    private static string BuildPattern(string text, TextLocationInVerse text_location_in_verse, TextLocationInWord text_location_in_word, TextWordness text_wordness)
    {
        if ((text_location_in_verse == TextLocationInVerse.Any) && (text_location_in_word == TextLocationInWord.Any))
        {
            return BuildPatternByText(text, text_location_in_verse, text_location_in_word, text_wordness);
        }
        else
        {
            return BuildPatternByWords(text, text_location_in_verse, text_location_in_word, text_wordness);
        }
    }
    private static string BuildPatternByText(string text, TextLocationInVerse text_location_in_verse, TextLocationInWord text_location_in_word, TextWordness text_wordness)
    {
        string pattern = null;

        if (String.IsNullOrEmpty(text)) return text;

        text = Regex.Replace(text, @"\s+", " "); // remove double space or higher if any

        //// don't allow user to specify space before or after search term
        //text = text.Trim();

        // search for Quran markers, stopmarks, numbers, etc.
        if (text.Length == 1)
        {
            if (!Constants.ARABIC_LETTERS.Contains(text[0]))
            {
                return text;
            }
        }

        /*
        =====================================================================
        Regular Expressions
        =====================================================================
        Best Reference: http://www.regular-expressions.info/
        RegEx Utility:  http://sourceforge.net/projects/regulator/
        RegEx Tester:   http://regexstorm.net/tester
        =====================================================================
        Matches	Characters
        x	character x
        \\	backslash character
        \0n	character with octal value 0n (0 <= n <= 7)
        \0nn	character with octal value 0nn (0 <= n <= 7)
        \0mnn	character with octal value 0mnn (0 <= m <= 3, 0 <= n <= 7)
        \xhh	character with hexadecimal value 0xhh
        \uhhhh	character with hexadecimal value 0xhhhh
        \t	tab character ('\u0009')
        \n	newline (line feed) character ('\u000A')
        \r	carriage-return character ('\u000D')
        \f	form-feed character ('\u000C')
        \a	alert (bell) character ('\u0007')
        \e	escape character ('\u001B')
        \cx	control character corresponding to x

        Character Classes
        [abc]		    a, b, or c				                    (simple class)
        [^abc]		    any character except a, b, or c		        (negation)
        [a-zA-Z]	    a through z or A through Z, inclusive	    (range)
        [a-d[m-p]]	    a through d, or m through p: [a-dm-p]	    (union)
        [a-z&&[def]]	d, e, or f				                    (intersection)
        [a-z&&[^bc]]	a through z, except for b and c: [ad-z]	    (subtraction)
        [a-z&&[^m-p]]	a through z, and not m through p: [a-lq-z]  (subtraction)

        Predefined
        .	any character (inc line terminators) except newline
        \d	digit				            [0-9]
        \D	non-digit			            [^0-9]
        \s	whitespace character		    [ \t\n\x0B\f\r]
        \S	non-whitespace character	    [^\s]
        \w	word character (alphanumeric)	[a-zA-Z_0-9]
        \W	non-word character		        [^\w]

        Boundary Matchers
        ^	beginning of a line	(in Multiline)
        $	end of a line  		(in Multiline)
        \b	word boundary, including line start and line end
        \B	non-word boundary
        \A	beginning of the input
        \G	end of the previous match
        \Z	end of the input but for the final terminator, if any
        \z	end of the input

        Greedy quantifiers
        X?	     X 0 or 1    times
        X*	     X 0 or more times
        X+	     X 1 or more times
        X{n}	 X n         times
        X{n,}	 X n or more times
        X{n,m}	 X n to m    times

        Reluctant quantifiers
        X??	     X 0 or 1    times
        X*?	     X 0 or more times
        X+?	     X 1 or more times
        X{n}?	 X n         times
        X{n,}?	 X n or more times
        X{n,m}?	 X n to m    times

        Possessive quantifiers
        X?+	     X 0 or 1    times
        X*+	     X 0 or more times
        X++	     X 1 or more times
        X{n}+	 X n         times
        X{n,}+	 X n or more times
        X{n,m}+	 X n to m    times

        (?=text)  positive lookahead
        (?!text)  negative lookahead           // not at end of line  (?!$)
        (?<=text) positive lookbehind
        (?<!text) negative lookbehind          // not at start of line (?<!^)
        =====================================================================
        */

        string pattern_empty_line = @"(^$)";
        string pattern_whole_line = @"(^" + text + @"$)";
        string pattern_whole_word = @"(\b" + text + @"\b)";
        string pattern_prefix = @"(\b" + text + @"\B)";
        string pattern_midfix = @"(\B" + text + @"\B)";
        string pattern_suffix = @"(\B" + text + @"\b)";


        ///////////////////////////////////////////////////////////////////////////////////////
        // WORD LOCATION = ANYWHERE
        ///////////////////////////////////////////////////////////////////////////////////////
        // Any Wordness
        string pattern_anywordness_vstart_wanywhere = @"(" + pattern_whole_line + "|"
                                             + @"(" + @"^" + pattern_whole_word + @")" + "|"
                                             + @"(" + @"^" + pattern_prefix + @")" + "|"
                                             + @"(" + @"^" + pattern_midfix + @")" + "|"
                                             + @"(" + @"^" + pattern_suffix + @")"
                                             + @")";
        string pattern_anywordness_vmiddle_wanywhere = @"(" + @"(?<!^)" + pattern_whole_line + @"(?!$)" + "|"
                                                     + @"(" + @"(?<!^)" + pattern_whole_word + @"(?!$)" + @")" + "|"
                                                     + @"(" + @"(?<!^)" + pattern_prefix + @"(?!$)" + @")" + "|"
                                                     + @"(" + @"(?<!^)" + pattern_midfix + @"(?!$)" + @")" + "|"
                                                     + @"(" + @"(?<!^)" + pattern_suffix + @"(?!$)" + @")"
                                                     + @")";
        string pattern_anywordness_vend_wanywhere = @"(" + pattern_whole_line + "|"
                                                  + @"(" + pattern_whole_word + @"$" + @")" + "|"
                                                  + @"(" + pattern_prefix + @"$" + @")" + "|"
                                                  + @"(" + pattern_midfix + @"$" + @")" + "|"
                                                  + @"(" + pattern_suffix + @"$" + @")"
                                                  + @")";
        string pattern_anywordness_vanywhere_wanywhere = @"(" + pattern_anywordness_vstart_wanywhere + "|" + pattern_anywordness_vmiddle_wanywhere + "|" + pattern_anywordness_vend_wanywhere + @")";

        // Part of word
        string pattern_partword_vstart_wanywhere = @"("
                                          + @"(" + @"^" + pattern_prefix + @")" + "|"
                                          + @"(" + @"^" + pattern_midfix + @")" + "|"
                                          + @"(" + @"^" + pattern_suffix + @")"
                                          + @")";
        string pattern_partword_vmiddle_wanywhere = @"("
                                                  + @"(" + @"(?<!^)" + pattern_prefix + @"(?!$)" + @")" + "|"
                                                  + @"(" + @"(?<!^)" + pattern_midfix + @"(?!$)" + @")" + "|"
                                                  + @"(" + @"(?<!^)" + pattern_suffix + @"(?!$)" + @")"
                                                  + @")";
        string pattern_partword_vend_wanywhere = @"("
                                               + @"(" + pattern_prefix + @"$" + @")" + "|"
                                               + @"(" + pattern_midfix + @"$" + @")" + "|"
                                               + @"(" + pattern_suffix + @"$" + @")"
                                               + @")";
        string pattern_partword_vanywhere_wanywhere = @"(" + pattern_partword_vstart_wanywhere + "|" + pattern_partword_vmiddle_wanywhere + "|" + pattern_partword_vend_wanywhere + @")";

        // Whole word
        string pattern_wholeword_vstart_wanywhere = @"(" + pattern_whole_line + "|"
                                           + @"(" + @"^" + pattern_whole_word + @")"
                                           + @")";
        string pattern_wholeword_vmiddle_wanywhere = @"(" + @"(?<!^)" + pattern_whole_line + @"(?!$)" + "|"
                                                   + @"(" + @"(?<!^)" + pattern_whole_word + @"(?!$)" + @")"
                                                   + @")";
        string pattern_wholeword_vend_wanywhere = @"(" + pattern_whole_line + "|"
                                                + @"(" + pattern_whole_word + @"$" + @")"
                                                + @")";
        string pattern_wholeword_vanywhere_wanywhere = @"(" + pattern_wholeword_vstart_wanywhere + "|" + pattern_wholeword_vmiddle_wanywhere + "|" + pattern_wholeword_vend_wanywhere + @")";
        ///////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////
        // WORD LOCATION = START
        ///////////////////////////////////////////////////////////////////////////////////////
        // Any Wordness
        string pattern_anywordness_vstart_wstart = @"(" + pattern_whole_line + "|"
                                          + @"(" + @"^" + pattern_whole_word + @")" + "|"
                                          + @"(" + @"^" + pattern_prefix + @")"
                                          + @")";
        string pattern_anywordness_vmiddle_wstart = @"(" + @"(?<!^)" + pattern_whole_line + @"(?!$)" + "|"
                                                  + @"(" + @"(?<!^)" + pattern_whole_word + @"(?!$)" + @")" + "|"
                                                  + @"(" + @"(?<!^)" + pattern_prefix + @"(?!$)" + @")"
                                                  + @")";
        string pattern_anywordness_vend_wstart = @"(" + pattern_whole_line + "|"
                                               + @"(" + pattern_whole_word + @"$" + @")" + "|"
                                               + @"(" + pattern_prefix + @"$" + @")"
                                               + @")";
        string pattern_anywordness_vanywhere_wstart = @"(" + pattern_anywordness_vstart_wstart + "|" + pattern_anywordness_vmiddle_wstart + "|" + pattern_anywordness_vend_wstart + @")";

        // Part of word
        string pattern_partword_vstart_wstart = @"("
                                              + @"(" + @"^" + pattern_prefix + @")"
                                              + @")";
        string pattern_partword_vmiddle_wstart = @"("
                                               + @"(" + @"(?<!^)" + pattern_prefix + @"(?!$)" + @")"
                                               + @")";
        string pattern_partword_vend_wstart = @"("
                                            + @"(" + pattern_prefix + @"$" + @")"
                                            + @")";
        string pattern_partword_vanywhere_wstart = @"(" + pattern_partword_vstart_wstart + "|" + pattern_partword_vmiddle_wstart + "|" + pattern_partword_vend_wstart + @")";

        // Whole word
        string pattern_wholeword_vstart_wstart = @"(" + pattern_whole_line + "|"
                                               + @"(" + @"^" + pattern_whole_word + @")"
                                               + @")";
        string pattern_wholeword_vmiddle_wstart = @"(" + @"(?<!^)" + pattern_whole_line + @"(?!$)" + "|"
                                                + @"(" + @"(?<!^)" + pattern_whole_word + @"(?!$)" + @")"
                                                + @")";
        string pattern_wholeword_vend_wstart = @"(" + pattern_whole_line + "|"
                                             + @"(" + pattern_whole_word + @"$" + @")"
                                             + @")";
        string pattern_wholeword_vanywhere_wstart = @"(" + pattern_wholeword_vstart_wstart + "|" + pattern_wholeword_vmiddle_wstart + "|" + pattern_wholeword_vend_wstart + @")";
        ///////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////
        // WORD LOCATION = MIDDLE
        ///////////////////////////////////////////////////////////////////////////////////////
        // Any Wordness
        string pattern_anywordness_vstart_wmiddle = @"(" + @"^" + pattern_midfix + @")";
        string pattern_anywordness_vmiddle_wmiddle = @"(" + @"(?<!^)" + pattern_midfix + @"(?!$)" + @")";
        string pattern_anywordness_vend_wmiddle = @"(" + pattern_midfix + @"$" + @")";
        string pattern_anywordness_vanywhere_wmiddle = @"(" + pattern_anywordness_vstart_wmiddle + "|" + pattern_anywordness_vmiddle_wmiddle + "|" + pattern_anywordness_vend_wmiddle + @")";

        // Part of word
        string pattern_partword_vstart_wmiddle = @"(" + pattern_midfix + @")";
        string pattern_partword_vmiddle_wmiddle = @"(" + @"(?<!^)" + pattern_midfix + @"(?!$)" + @")";
        string pattern_partword_vend_wmiddle = @"(" + pattern_midfix + @"$" + @")";
        string pattern_partword_vanywhere_wmiddle = @"(" + pattern_partword_vstart_wmiddle + "|" + pattern_partword_vmiddle_wmiddle + "|" + pattern_partword_vend_wmiddle + @")";

        // Whole word
        string pattern_wholeword_vstart_wmiddle = @"(" + "^Dummy text not to be found.$" + @")";
        string pattern_wholeword_vmiddle_wmiddle = @"(" + "^Dummy text not to be found.$" + @")";
        string pattern_wholeword_vend_wmiddle = @"(" + "^Dummy text not to be found.$" + @")";
        string pattern_wholeword_vanywhere_wmiddle = @"(" + pattern_wholeword_vstart_wmiddle + "|" + pattern_wholeword_vmiddle_wmiddle + "|" + pattern_wholeword_vend_wmiddle + @")";
        ///////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////
        // WORD LOCATION = END
        ///////////////////////////////////////////////////////////////////////////////////////
        // Any Wordness
        string pattern_anywordness_vstart_wend = @"(" + pattern_whole_line + "|"
                                        + @"(" + @"^" + pattern_whole_word + @")" + "|"
                                        + @"(" + @"^" + pattern_suffix + @")"
                                        + @")";
        string pattern_anywordness_vmiddle_wend = @"(" + @"(?<!^)" + pattern_whole_line + @"(?!$)" + "|"
                                                + @"(" + @"(?<!^)" + pattern_whole_word + @"(?!$)" + @")" + "|"
                                                + @"(" + @"(?<!^)" + pattern_suffix + @"(?!$)" + @")"
                                                + @")";
        string pattern_anywordness_vend_wend = @"(" + pattern_whole_line + "|"
                                             + @"(" + pattern_whole_word + @"$" + @")" + "|"
                                             + @"(" + pattern_suffix + @"$" + @")"
                                             + @")";
        string pattern_anywordness_vanywhere_wend = @"(" + pattern_anywordness_vstart_wend + "|" + pattern_anywordness_vmiddle_wend + "|" + pattern_anywordness_vend_wend + @")";

        // Part of word
        string pattern_partword_vstart_wend = @"("
                                     + @"(" + @"^" + pattern_suffix + @")"
                                     + @")";
        string pattern_partword_vmiddle_wend = @"("
                                      + @"(" + @"(?<!^)" + pattern_suffix + @"(?!$)" + @")"
                                      + @")";
        string pattern_partword_vend_wend = @"("
                                          + @"(" + pattern_suffix + @"$" + @")"
                                          + @")";
        string pattern_partword_vanywhere_wend = @"(" + pattern_partword_vstart_wend + "|" + pattern_partword_vmiddle_wend + "|" + pattern_partword_vend_wend + @")";

        // Whole word
        string pattern_wholeword_vstart_wend = @"(" + pattern_whole_line + "|"
                                      + @"(" + @"^" + pattern_whole_word + @")"
                                      + @")";
        string pattern_wholeword_vmiddle_wend = @"(" + @"(?<!^)" + pattern_whole_line + @"(?!$)" + "|"
                                              + @"(" + @"(?<!^)" + pattern_whole_word + @"(?!$)" + @")"
                                              + @")";
        string pattern_wholeword_vend_wend = @"(" + pattern_whole_line + "|"
                                           + @"(" + pattern_whole_word + @"$" + @")"
                                           + @")";
        string pattern_wholeword_vanywhere_wend = @"(" + pattern_wholeword_vstart_wend + "|" + pattern_wholeword_vmiddle_wend + "|" + pattern_wholeword_vend_wend + @")";
        ///////////////////////////////////////////////////////////////////////////////////////



        switch (text_wordness)
        {
            case TextWordness.Any:
                {
                    switch (text_location_in_verse)
                    {
                        case TextLocationInVerse.Any:
                            {
                                switch (text_location_in_word)
                                {
                                    case TextLocationInWord.Any:
                                        {
                                            pattern = pattern_anywordness_vanywhere_wanywhere;
                                        }
                                        break;
                                    case TextLocationInWord.AtStart:
                                        {
                                            pattern = pattern_anywordness_vanywhere_wstart;
                                        }
                                        break;
                                    case TextLocationInWord.AtMiddle:
                                        {
                                            pattern = pattern_anywordness_vanywhere_wmiddle;
                                        }
                                        break;
                                    case TextLocationInWord.AtEnd:
                                        {
                                            pattern = pattern_anywordness_vanywhere_wend;
                                        }
                                        break;
                                }
                            }
                            break;
                        case TextLocationInVerse.AtStart:
                            {
                                switch (text_location_in_word)
                                {
                                    case TextLocationInWord.Any:
                                        {
                                            pattern = pattern_anywordness_vstart_wanywhere;
                                        }
                                        break;
                                    case TextLocationInWord.AtStart:
                                        {
                                            pattern = pattern_anywordness_vstart_wstart;
                                        }
                                        break;
                                    case TextLocationInWord.AtMiddle:
                                        {
                                            pattern = pattern_anywordness_vstart_wmiddle;
                                        }
                                        break;
                                    case TextLocationInWord.AtEnd:
                                        {
                                            pattern = pattern_anywordness_vstart_wend;
                                        }
                                        break;
                                }
                            }
                            break;
                        case TextLocationInVerse.AtMiddle:
                            {
                                switch (text_location_in_word)
                                {
                                    case TextLocationInWord.Any:
                                        {
                                            pattern = pattern_anywordness_vmiddle_wanywhere;
                                        }
                                        break;
                                    case TextLocationInWord.AtStart:
                                        {
                                            pattern = pattern_anywordness_vmiddle_wstart;
                                        }
                                        break;
                                    case TextLocationInWord.AtMiddle:
                                        {
                                            pattern = pattern_anywordness_vmiddle_wmiddle;
                                        }
                                        break;
                                    case TextLocationInWord.AtEnd:
                                        {
                                            pattern = pattern_anywordness_vmiddle_wend;
                                        }
                                        break;
                                }
                            }
                            break;
                        case TextLocationInVerse.AtEnd:
                            {
                                switch (text_location_in_word)
                                {
                                    case TextLocationInWord.Any:
                                        {
                                            pattern = pattern_anywordness_vend_wanywhere;
                                        }
                                        break;
                                    case TextLocationInWord.AtStart:
                                        {
                                            pattern = pattern_anywordness_vend_wstart;
                                        }
                                        break;
                                    case TextLocationInWord.AtMiddle:
                                        {
                                            pattern = pattern_anywordness_vend_wmiddle;
                                        }
                                        break;
                                    case TextLocationInWord.AtEnd:
                                        {
                                            pattern = pattern_anywordness_vend_wend;
                                        }
                                        break;
                                }
                            }
                            break;
                    }
                }
                break;
            case TextWordness.PartOfWord:
                {
                    switch (text_location_in_verse)
                    {
                        case TextLocationInVerse.Any:
                            {
                                switch (text_location_in_word)
                                {
                                    case TextLocationInWord.Any:
                                        {
                                            pattern = pattern_partword_vanywhere_wanywhere;
                                        }
                                        break;
                                    case TextLocationInWord.AtStart:
                                        {
                                            pattern = pattern_partword_vanywhere_wstart;
                                        }
                                        break;
                                    case TextLocationInWord.AtMiddle:
                                        {
                                            pattern = pattern_partword_vanywhere_wmiddle;
                                        }
                                        break;
                                    case TextLocationInWord.AtEnd:
                                        {
                                            pattern = pattern_partword_vanywhere_wend;
                                        }
                                        break;
                                }
                            }
                            break;
                        case TextLocationInVerse.AtStart:
                            {
                                switch (text_location_in_word)
                                {
                                    case TextLocationInWord.Any:
                                        {
                                            pattern = pattern_partword_vstart_wanywhere;
                                        }
                                        break;
                                    case TextLocationInWord.AtStart:
                                        {
                                            pattern = pattern_partword_vstart_wstart;
                                        }
                                        break;
                                    case TextLocationInWord.AtMiddle:
                                        {
                                            pattern = pattern_partword_vstart_wmiddle;
                                        }
                                        break;
                                    case TextLocationInWord.AtEnd:
                                        {
                                            pattern = pattern_partword_vstart_wend;
                                        }
                                        break;
                                }
                            }
                            break;
                        case TextLocationInVerse.AtMiddle:
                            {
                                switch (text_location_in_word)
                                {
                                    case TextLocationInWord.Any:
                                        {
                                            pattern = pattern_partword_vmiddle_wanywhere;
                                        }
                                        break;
                                    case TextLocationInWord.AtStart:
                                        {
                                            pattern = pattern_partword_vmiddle_wstart;
                                        }
                                        break;
                                    case TextLocationInWord.AtMiddle:
                                        {
                                            pattern = pattern_partword_vmiddle_wmiddle;
                                        }
                                        break;
                                    case TextLocationInWord.AtEnd:
                                        {
                                            pattern = pattern_partword_vmiddle_wend;
                                        }
                                        break;
                                }
                            }
                            break;
                        case TextLocationInVerse.AtEnd:
                            {
                                switch (text_location_in_word)
                                {
                                    case TextLocationInWord.Any:
                                        {
                                            pattern = pattern_partword_vend_wanywhere;
                                        }
                                        break;
                                    case TextLocationInWord.AtStart:
                                        {
                                            pattern = pattern_partword_vend_wstart;
                                        }
                                        break;
                                    case TextLocationInWord.AtMiddle:
                                        {
                                            pattern = pattern_partword_vend_wmiddle;
                                        }
                                        break;
                                    case TextLocationInWord.AtEnd:
                                        {
                                            pattern = pattern_partword_vend_wend;
                                        }
                                        break;
                                }
                            }
                            break;
                    }
                }
                break;
            case TextWordness.WholeWord:
                {
                    switch (text_location_in_verse)
                    {
                        case TextLocationInVerse.Any:
                            {
                                switch (text_location_in_word)
                                {
                                    case TextLocationInWord.Any:
                                        {
                                            pattern = pattern_wholeword_vanywhere_wanywhere;
                                        }
                                        break;
                                    case TextLocationInWord.AtStart:
                                        {
                                            pattern = pattern_wholeword_vanywhere_wstart;
                                        }
                                        break;
                                    case TextLocationInWord.AtMiddle:
                                        {
                                            pattern = pattern_wholeword_vanywhere_wmiddle;
                                        }
                                        break;
                                    case TextLocationInWord.AtEnd:
                                        {
                                            pattern = pattern_wholeword_vanywhere_wend;
                                        }
                                        break;
                                }
                            }
                            break;
                        case TextLocationInVerse.AtStart:
                            {
                                switch (text_location_in_word)
                                {
                                    case TextLocationInWord.Any:
                                        {
                                            pattern = pattern_wholeword_vstart_wanywhere;
                                        }
                                        break;
                                    case TextLocationInWord.AtStart:
                                        {
                                            pattern = pattern_wholeword_vstart_wstart;
                                        }
                                        break;
                                    case TextLocationInWord.AtMiddle:
                                        {
                                            pattern = pattern_wholeword_vstart_wmiddle;
                                        }
                                        break;
                                    case TextLocationInWord.AtEnd:
                                        {
                                            pattern = pattern_wholeword_vstart_wend;
                                        }
                                        break;
                                }
                            }
                            break;
                        case TextLocationInVerse.AtMiddle:
                            {
                                switch (text_location_in_word)
                                {
                                    case TextLocationInWord.Any:
                                        {
                                            pattern = pattern_wholeword_vmiddle_wanywhere;
                                        }
                                        break;
                                    case TextLocationInWord.AtStart:
                                        {
                                            pattern = pattern_wholeword_vmiddle_wstart;
                                        }
                                        break;
                                    case TextLocationInWord.AtMiddle:
                                        {
                                            pattern = pattern_wholeword_vmiddle_wmiddle;
                                        }
                                        break;
                                    case TextLocationInWord.AtEnd:
                                        {
                                            pattern = pattern_wholeword_vmiddle_wend;
                                        }
                                        break;
                                }
                            }
                            break;
                        case TextLocationInVerse.AtEnd:
                            {
                                switch (text_location_in_word)
                                {
                                    case TextLocationInWord.Any:
                                        {
                                            pattern = pattern_wholeword_vend_wanywhere;
                                        }
                                        break;
                                    case TextLocationInWord.AtStart:
                                        {
                                            pattern = pattern_wholeword_vend_wstart;
                                        }
                                        break;
                                    case TextLocationInWord.AtMiddle:
                                        {
                                            pattern = pattern_wholeword_vend_wmiddle;
                                        }
                                        break;
                                    case TextLocationInWord.AtEnd:
                                        {
                                            pattern = pattern_wholeword_vend_wend;
                                        }
                                        break;
                                }
                            }
                            break;
                    }
                }
                break;
            default:
                {
                    pattern = pattern_empty_line;
                }
                break;
        }

        return pattern;
    }
    private static string BuildPatternByWords(string text, TextLocationInVerse text_location_in_verse, TextLocationInWord text_location_in_word, TextWordness text_wordness)
    {
        // Last correct BuildPattern in QuranCode1433_7.29.139.0271.Source.zip
        string pattern = null;

        if (String.IsNullOrEmpty(text)) return text;

        text = Regex.Replace(text, @"\s+", " "); // remove double space or higher if any

        // search for Quran markers, stopmarks, numbers, etc.
        if (text.Length == 1)
        {
            if (!Constants.ARABIC_LETTERS.Contains(text[0]))
            {
                return text;
            }
        }

        /*
        =====================================================================
        Regular Expressions
        =====================================================================
        Best Reference: http://www.regular-expressions.info/
        RegEx Utility:  http://sourceforge.net/projects/regulator/
        RegEx Tester:   http://regexstorm.net/tester
        =====================================================================
        Matches	Characters
        x	character x
        \\	backslash character
        \0n	character with octal value 0n (0 <= n <= 7)
        \0nn	character with octal value 0nn (0 <= n <= 7)
        \0mnn	character with octal value 0mnn (0 <= m <= 3, 0 <= n <= 7)
        \xhh	character with hexadecimal value 0xhh
        \uhhhh	character with hexadecimal value 0xhhhh
        \t	tab character ('\u0009')
        \n	newline (line feed) character ('\u000A')
        \r	carriage-return character ('\u000D')
        \f	form-feed character ('\u000C')
        \a	alert (bell) character ('\u0007')
        \e	escape character ('\u001B')
        \cx	control character corresponding to x

        Character Classes
        [abc]		    a, b, or c				                    (simple class)
        [^abc]		    any character except a, b, or c		        (negation)
        [a-zA-Z]	    a through z or A through Z, inclusive	    (range)
        [a-d[m-p]]	    a through d, or m through p: [a-dm-p]	    (union)
        [a-z&&[def]]	d, e, or f				                    (intersection)
        [a-z&&[^bc]]	a through z, except for b and c: [ad-z]	    (subtraction)
        [a-z&&[^m-p]]	a through z, and not m through p: [a-lq-z]  (subtraction)

        Predefined
        .	any character (inc line terminators) except newline
        \d	digit				            [0-9]
        \D	non-digit			            [^0-9]
        \s	whitespace character		    [ \t\n\x0B\f\r]
        \S	non-whitespace character	    [^\s]
        \w	word character (alphanumeric)	[a-zA-Z_0-9]
        \W	non-word character		        [^\w]

        Boundary Matchers
        ^	beginning of a line	(in Multiline)
        $	end of a line  		(in Multiline)
        \b	word boundary, including line start and line end
        \B	non-word boundary
        \A	beginning of the input
        \G	end of the previous match
        \Z	end of the input but for the final terminator, if any
        \z	end of the input

        Greedy quantifiers
        X?	     X 0 or 1    times
        X*	     X 0 or more times
        X+	     X 1 or more times
        X{n}	 X n         times
        X{n,}	 X n or more times
        X{n,m}	 X n to m    times

        Reluctant quantifiers
        X??	     X 0 or 1    times
        X*?	     X 0 or more times
        X+?	     X 1 or more times
        X{n}?	 X n         times
        X{n,}?	 X n or more times
        X{n,m}?	 X n to m    times

        Possessive quantifiers
        X?+	     X 0 or 1    times
        X*+	     X 0 or more times
        X++	     X 1 or more times
        X{n}+	 X n         times
        X{n,}+	 X n or more times
        X{n,m}+	 X n to m    times

        (?=text)  positive lookahead
        (?!text)  negative lookahead           // not at end of line  (?!$)
        (?<=text) positive lookbehind
        (?<!text) negative lookbehind          // not at start of line (?<!^)
        =====================================================================
        */

        string pattern_empty_line = @"(" + @"^$" + @")";
        string pattern_whole_line = @"(" + @"^" + text + @"$" + @")";

        string pattern_whole_word = @"(" + @"\b" + text + @"\b" + @")";
        string pattern_prefix = @"(" + @"\b" + @"\S+?" + text + @"\b" + @")";
        string pattern_suffix = @"(" + @"\b" + text + @"\S+?" + @"\b" + @")";
        string pattern_prefix_and_suffix = @"(" + @"\b" + @"\S+?" + text + @"\S+?" + @"\b" + @")";


        ///////////////////////////////////////////////////////////////////////////////////////
        // WORD LOCATION = ANYWHERE
        ///////////////////////////////////////////////////////////////////////////////////////
        // Any Wordness
        string pattern_anywordness_vstart_wanywhere = @"(" + pattern_whole_line + "|"
                                             + @"(" + @"^" + pattern_whole_word + @")" + "|"
                                             + @"(" + @"^" + pattern_prefix_and_suffix + @")" + "|"
                                             + @"(" + @"^" + pattern_suffix + @")" + "|"
                                             + @"(" + @"^" + pattern_prefix + @")"
                                             + @")";
        string pattern_anywordness_vmiddle_wanywhere = @"(" + @"(?<!^)" + pattern_whole_line + @"(?!$)" + "|"
                                                     + @"(" + @"(?<!^)" + pattern_whole_word + @"(?!$)" + @")" + "|"
                                                     + @"(" + @"(?<!^)" + pattern_prefix_and_suffix + @"(?!$)" + @")" + "|"
                                                     + @"(" + @"(?<!^)" + pattern_suffix + @"(?!$)" + @")" + "|"
                                                     + @"(" + @"(?<!^)" + pattern_prefix + @"(?!$)" + @")"
                                                     + @")";
        string pattern_anywordness_vend_wanywhere = @"(" + pattern_whole_line + "|"
                                                  + @"(" + pattern_whole_word + @"$" + @")" + "|"
                                                  + @"(" + pattern_prefix_and_suffix + @"$" + @")" + "|"
                                                  + @"(" + pattern_suffix + @"$" + @")" + "|"
                                                  + @"(" + pattern_prefix + @"$" + @")"
                                                  + @")";
        string pattern_anywordness_not_vmiddle_wanywhere = @"(" + pattern_whole_line + "|" + pattern_anywordness_vstart_wanywhere + "|" + pattern_anywordness_vend_wanywhere + @")";
        string pattern_anywordness_vanywhere_wanywhere = @"(" + pattern_anywordness_vstart_wanywhere + "|" + pattern_anywordness_vmiddle_wanywhere + "|" + pattern_anywordness_vend_wanywhere + @")";

        // Part of word
        string pattern_partword_vstart_wanywhere = @"("
                                          + @"(" + @"^" + pattern_prefix_and_suffix + @")" + "|"
                                          + @"(" + @"^" + pattern_suffix + @")" + "|"
                                          + @"(" + @"^" + pattern_prefix + @")"
                                          + @")";
        string pattern_partword_vmiddle_wanywhere = @"("
                                                  + @"(" + @"(?<!^)" + pattern_prefix_and_suffix + @"(?!$)" + @")" + "|"
                                                  + @"(" + @"(?<!^)" + pattern_suffix + @"(?!$)" + @")" + "|"
                                                  + @"(" + @"(?<!^)" + pattern_prefix + @"(?!$)" + @")"
                                                  + @")";
        string pattern_partword_vend_wanywhere = @"("
                                               + @"(" + pattern_prefix_and_suffix + @"$" + @")" + "|"
                                               + @"(" + pattern_suffix + @"$" + @")" + "|"
                                               + @"(" + pattern_prefix + @"$" + @")"
                                               + @")";
        string pattern_partword_not_vmiddle_wanywhere = @"(" + pattern_whole_line + "|" + pattern_partword_vstart_wanywhere + "|" + pattern_partword_vend_wanywhere + @")";
        string pattern_partword_vanywhere_wanywhere = @"(" + pattern_partword_vstart_wanywhere + "|" + pattern_partword_vmiddle_wanywhere + "|" + pattern_partword_vend_wanywhere + @")";

        // Whole word
        string pattern_wholeword_vstart_wanywhere = @"(" + pattern_whole_line + "|"
                                           + @"(" + @"^" + pattern_whole_word + @")"
                                           + @")";
        string pattern_wholeword_vmiddle_wanywhere = @"(" + @"(?<!^)" + pattern_whole_line + @"(?!$)" + "|"
                                                   + @"(" + @"(?<!^)" + pattern_whole_word + @"(?!$)" + @")"
                                                   + @")";
        string pattern_wholeword_vend_wanywhere = @"(" + pattern_whole_line + "|"
                                                + @"(" + pattern_whole_word + @"$" + @")"
                                                + @")";
        string pattern_wholeword_not_vmiddle_wanywhere = @"(" + pattern_whole_line + "|" + pattern_wholeword_vstart_wanywhere + "|" + pattern_wholeword_vend_wanywhere + @")";
        string pattern_wholeword_vanywhere_wanywhere = @"(" + pattern_wholeword_vstart_wanywhere + "|" + pattern_wholeword_vmiddle_wanywhere + "|" + pattern_wholeword_vend_wanywhere + @")";
        ///////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////
        // WORD LOCATION = START
        ///////////////////////////////////////////////////////////////////////////////////////
        // Any Wordness
        string pattern_anywordness_vstart_wstart = @"(" + pattern_whole_line + "|"
                                          + @"(" + @"^" + pattern_whole_word + @")" + "|"
                                          + @"(" + @"^" + pattern_suffix + @")"
                                          + @")";
        string pattern_anywordness_vmiddle_wstart = @"(" + @"(?<!^)" + pattern_whole_line + @"(?!$)" + "|"
                                                  + @"(" + @"(?<!^)" + pattern_whole_word + @"(?!$)" + @")" + "|"
                                                  + @"(" + @"(?<!^)" + pattern_suffix + @"(?!$)" + @")"
                                                  + @")";
        string pattern_anywordness_vend_wstart = @"(" + pattern_whole_line + "|"
                                               + @"(" + pattern_whole_word + @"$" + @")" + "|"
                                               + @"(" + pattern_suffix + @"$" + @")"
                                               + @")";
        string pattern_anywordness_not_vmiddle_wstart = @"(" + pattern_whole_line + "|" + pattern_anywordness_vstart_wstart + "|" + pattern_anywordness_vend_wstart + @")";
        string pattern_anywordness_vanywhere_wstart = @"(" + pattern_anywordness_vstart_wstart + "|" + pattern_anywordness_vmiddle_wstart + "|" + pattern_anywordness_vend_wstart + @")";

        // Part of word
        string pattern_partword_vstart_wstart = @"("
                                              + @"(" + @"^" + pattern_suffix + @")"
                                              + @")";
        string pattern_partword_vmiddle_wstart = @"("
                                               + @"(" + @"(?<!^)" + pattern_suffix + @"(?!$)" + @")"
                                               + @")";
        string pattern_partword_vend_wstart = @"("
                                            + @"(" + pattern_suffix + @"$" + @")"
                                            + @")";
        string pattern_partword_not_vmiddle_wstart = @"(" + pattern_whole_line + "|" + pattern_partword_vstart_wstart + "|" + pattern_partword_vend_wstart + @")";
        string pattern_partword_vanywhere_wstart = @"(" + pattern_partword_vstart_wstart + "|" + pattern_partword_vmiddle_wstart + "|" + pattern_partword_vend_wstart + @")";

        // Whole word
        string pattern_wholeword_vstart_wstart = @"(" + pattern_whole_line + "|"
                                               + @"(" + @"^" + pattern_whole_word + @")"
                                               + @")";
        string pattern_wholeword_vmiddle_wstart = @"(" + @"(?<!^)" + pattern_whole_line + @"(?!$)" + "|"
                                                + @"(" + @"(?<!^)" + pattern_whole_word + @"(?!$)" + @")"
                                                + @")";
        string pattern_wholeword_vend_wstart = @"(" + pattern_whole_line + "|"
                                             + @"(" + pattern_whole_word + @"$" + @")"
                                             + @")";
        string pattern_wholeword_not_vmiddle_wstart = @"(" + pattern_whole_line + "|" + pattern_wholeword_vstart_wstart + "|" + pattern_wholeword_vend_wstart + @")";
        string pattern_wholeword_vanywhere_wstart = @"(" + pattern_wholeword_vstart_wstart + "|" + pattern_wholeword_vmiddle_wstart + "|" + pattern_wholeword_vend_wstart + @")";
        ///////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////
        // WORD LOCATION = MIDDLE
        ///////////////////////////////////////////////////////////////////////////////////////
        // Any Wordness
        string pattern_anywordness_vstart_wmiddle = @"(" + @"^" + pattern_prefix_and_suffix + @")";
        string pattern_anywordness_vmiddle_wmiddle = @"(" + @"(?<!^)" + pattern_prefix_and_suffix + @"(?!$)" + @")";
        string pattern_anywordness_vend_wmiddle = @"(" + pattern_prefix_and_suffix + @"$" + @")";
        string pattern_anywordness_not_vmiddle_wmiddle = @"(" + pattern_whole_line + "|" + pattern_anywordness_vstart_wmiddle + "|" + pattern_anywordness_vend_wmiddle + @")";
        string pattern_anywordness_vanywhere_wmiddle = @"(" + pattern_anywordness_vstart_wmiddle + "|" + pattern_anywordness_vmiddle_wmiddle + "|" + pattern_anywordness_vend_wmiddle + @")";

        // Part of word
        string pattern_partword_vstart_wmiddle = @"(" + pattern_prefix_and_suffix + @")";
        string pattern_partword_vmiddle_wmiddle = @"(" + @"(?<!^)" + pattern_prefix_and_suffix + @"(?!$)" + @")";
        string pattern_partword_vend_wmiddle = @"(" + pattern_prefix_and_suffix + @"$" + @")";
        string pattern_partword_not_vmiddle_wmiddle = @"(" + pattern_whole_line + "|" + pattern_partword_vstart_wmiddle + "|" + pattern_partword_vend_wmiddle + @")";
        string pattern_partword_vanywhere_wmiddle = @"(" + pattern_partword_vstart_wmiddle + "|" + pattern_partword_vmiddle_wmiddle + "|" + pattern_partword_vend_wmiddle + @")";

        // Whole word
        string pattern_wholeword_vstart_wmiddle = @"(" + "^Dummy text not to be found.$" + @")";
        string pattern_wholeword_vmiddle_wmiddle = @"(" + "^Dummy text not to be found.$" + @")";
        string pattern_wholeword_vend_wmiddle = @"(" + "^Dummy text not to be found.$" + @")";
        string pattern_wholeword_not_vmiddle_wmiddle = @"(" + pattern_whole_line + "|" + pattern_wholeword_vstart_wmiddle + "|" + pattern_wholeword_vend_wmiddle + @")";
        string pattern_wholeword_vanywhere_wmiddle = @"(" + pattern_wholeword_vstart_wmiddle + "|" + pattern_wholeword_vmiddle_wmiddle + "|" + pattern_wholeword_vend_wmiddle + @")";
        ///////////////////////////////////////////////////////////////////////////////////////


        ///////////////////////////////////////////////////////////////////////////////////////
        // WORD LOCATION = END
        ///////////////////////////////////////////////////////////////////////////////////////
        // Any Wordness
        string pattern_anywordness_vstart_wend = @"(" + pattern_whole_line + "|"
                                        + @"(" + @"^" + pattern_whole_word + @")" + "|"
                                        + @"(" + @"^" + pattern_prefix + @")"
                                        + @")";
        string pattern_anywordness_vmiddle_wend = @"(" + @"(?<!^)" + pattern_whole_line + @"(?!$)" + "|"
                                                + @"(" + @"(?<!^)" + pattern_whole_word + @"(?!$)" + @")" + "|"
                                                + @"(" + @"(?<!^)" + pattern_prefix + @"(?!$)" + @")"
                                                + @")";
        string pattern_anywordness_vend_wend = @"(" + pattern_whole_line + "|"
                                             + @"(" + pattern_whole_word + @"$" + @")" + "|"
                                             + @"(" + pattern_prefix + @"$" + @")"
                                             + @")";
        string pattern_anywordness_not_vmiddle_wend = @"(" + pattern_whole_line + "|" + pattern_anywordness_vstart_wend + "|" + pattern_anywordness_vend_wend + @")";
        string pattern_anywordness_vanywhere_wend = @"(" + pattern_anywordness_vstart_wend + "|" + pattern_anywordness_vmiddle_wend + "|" + pattern_anywordness_vend_wend + @")";

        // Part of word
        string pattern_partword_vstart_wend = @"("
                                     + @"(" + @"^" + pattern_prefix + @")"
                                     + @")";
        string pattern_partword_vmiddle_wend = @"("
                                      + @"(" + @"(?<!^)" + pattern_prefix + @"(?!$)" + @")"
                                      + @")";
        string pattern_partword_vend_wend = @"("
                                          + @"(" + pattern_prefix + @"$" + @")"
                                          + @")";
        string pattern_partword_not_vmiddle_wend = @"(" + pattern_whole_line + "|" + pattern_partword_vstart_wend + "|" + pattern_partword_vend_wend + @")";
        string pattern_partword_vanywhere_wend = @"(" + pattern_partword_vstart_wend + "|" + pattern_partword_vmiddle_wend + "|" + pattern_partword_vend_wend + @")";

        // Whole word
        string pattern_wholeword_vstart_wend = @"(" + pattern_whole_line + "|"
                                      + @"(" + @"^" + pattern_whole_word + @")"
                                      + @")";
        string pattern_wholeword_vmiddle_wend = @"(" + @"(?<!^)" + pattern_whole_line + @"(?!$)" + "|"
                                              + @"(" + @"(?<!^)" + pattern_whole_word + @"(?!$)" + @")"
                                              + @")";
        string pattern_wholeword_vend_wend = @"(" + pattern_whole_line + "|"
                                           + @"(" + pattern_whole_word + @"$" + @")"
                                           + @")";
        string pattern_wholeword_not_vmiddle_wend = @"(" + pattern_whole_line + "|" + pattern_wholeword_vstart_wend + "|" + pattern_wholeword_vend_wend + @")";
        string pattern_wholeword_vanywhere_wend = @"(" + pattern_wholeword_vstart_wend + "|" + pattern_wholeword_vmiddle_wend + "|" + pattern_wholeword_vend_wend + @")";
        ///////////////////////////////////////////////////////////////////////////////////////



        switch (text_wordness)
        {
            case TextWordness.Any:
                {
                    switch (text_location_in_verse)
                    {
                        case TextLocationInVerse.Any:
                            {
                                switch (text_location_in_word)
                                {
                                    case TextLocationInWord.Any:
                                        {
                                            pattern = pattern_anywordness_vanywhere_wanywhere;
                                        }
                                        break;
                                    case TextLocationInWord.AtStart:
                                        {
                                            pattern = pattern_anywordness_vanywhere_wstart;
                                        }
                                        break;
                                    case TextLocationInWord.AtMiddle:
                                        {
                                            pattern = pattern_anywordness_vanywhere_wmiddle;
                                        }
                                        break;
                                    case TextLocationInWord.AtEnd:
                                        {
                                            pattern = pattern_anywordness_vanywhere_wend;
                                        }
                                        break;
                                }
                            }
                            break;
                        case TextLocationInVerse.AtStart:
                            {
                                switch (text_location_in_word)
                                {
                                    case TextLocationInWord.Any:
                                        {
                                            pattern = pattern_anywordness_vstart_wanywhere;
                                        }
                                        break;
                                    case TextLocationInWord.AtStart:
                                        {
                                            pattern = pattern_anywordness_vstart_wstart;
                                        }
                                        break;
                                    case TextLocationInWord.AtMiddle:
                                        {
                                            pattern = pattern_anywordness_vstart_wmiddle;
                                        }
                                        break;
                                    case TextLocationInWord.AtEnd:
                                        {
                                            pattern = pattern_anywordness_vstart_wend;
                                        }
                                        break;
                                }
                            }
                            break;
                        case TextLocationInVerse.AtMiddle:
                            {
                                switch (text_location_in_word)
                                {
                                    case TextLocationInWord.Any:
                                        {
                                            pattern = pattern_anywordness_vmiddle_wanywhere;
                                        }
                                        break;
                                    case TextLocationInWord.AtStart:
                                        {
                                            pattern = pattern_anywordness_vmiddle_wstart;
                                        }
                                        break;
                                    case TextLocationInWord.AtMiddle:
                                        {
                                            pattern = pattern_anywordness_vmiddle_wmiddle;
                                        }
                                        break;
                                    case TextLocationInWord.AtEnd:
                                        {
                                            pattern = pattern_anywordness_vmiddle_wend;
                                        }
                                        break;
                                }
                            }
                            break;
                        case TextLocationInVerse.AtEnd:
                            {
                                switch (text_location_in_word)
                                {
                                    case TextLocationInWord.Any:
                                        {
                                            pattern = pattern_anywordness_vend_wanywhere;
                                        }
                                        break;
                                    case TextLocationInWord.AtStart:
                                        {
                                            pattern = pattern_anywordness_vend_wstart;
                                        }
                                        break;
                                    case TextLocationInWord.AtMiddle:
                                        {
                                            pattern = pattern_anywordness_vend_wmiddle;
                                        }
                                        break;
                                    case TextLocationInWord.AtEnd:
                                        {
                                            pattern = pattern_anywordness_vend_wend;
                                        }
                                        break;
                                }
                            }
                            break;
                    }
                }
                break;
            case TextWordness.PartOfWord:
                {
                    switch (text_location_in_verse)
                    {
                        case TextLocationInVerse.Any:
                            {
                                switch (text_location_in_word)
                                {
                                    case TextLocationInWord.Any:
                                        {
                                            pattern = pattern_partword_vanywhere_wanywhere;
                                        }
                                        break;
                                    case TextLocationInWord.AtStart:
                                        {
                                            pattern = pattern_partword_vanywhere_wstart;
                                        }
                                        break;
                                    case TextLocationInWord.AtMiddle:
                                        {
                                            pattern = pattern_partword_vanywhere_wmiddle;
                                        }
                                        break;
                                    case TextLocationInWord.AtEnd:
                                        {
                                            pattern = pattern_partword_vanywhere_wend;
                                        }
                                        break;
                                }
                            }
                            break;
                        case TextLocationInVerse.AtStart:
                            {
                                switch (text_location_in_word)
                                {
                                    case TextLocationInWord.Any:
                                        {
                                            pattern = pattern_partword_vstart_wanywhere;
                                        }
                                        break;
                                    case TextLocationInWord.AtStart:
                                        {
                                            pattern = pattern_partword_vstart_wstart;
                                        }
                                        break;
                                    case TextLocationInWord.AtMiddle:
                                        {
                                            pattern = pattern_partword_vstart_wmiddle;
                                        }
                                        break;
                                    case TextLocationInWord.AtEnd:
                                        {
                                            pattern = pattern_partword_vstart_wend;
                                        }
                                        break;
                                }
                            }
                            break;
                        case TextLocationInVerse.AtMiddle:
                            {
                                switch (text_location_in_word)
                                {
                                    case TextLocationInWord.Any:
                                        {
                                            pattern = pattern_partword_vmiddle_wanywhere;
                                        }
                                        break;
                                    case TextLocationInWord.AtStart:
                                        {
                                            pattern = pattern_partword_vmiddle_wstart;
                                        }
                                        break;
                                    case TextLocationInWord.AtMiddle:
                                        {
                                            pattern = pattern_partword_vmiddle_wmiddle;
                                        }
                                        break;
                                    case TextLocationInWord.AtEnd:
                                        {
                                            pattern = pattern_partword_vmiddle_wend;
                                        }
                                        break;
                                }
                            }
                            break;
                        case TextLocationInVerse.AtEnd:
                            {
                                switch (text_location_in_word)
                                {
                                    case TextLocationInWord.Any:
                                        {
                                            pattern = pattern_partword_vend_wanywhere;
                                        }
                                        break;
                                    case TextLocationInWord.AtStart:
                                        {
                                            pattern = pattern_partword_vend_wstart;
                                        }
                                        break;
                                    case TextLocationInWord.AtMiddle:
                                        {
                                            pattern = pattern_partword_vend_wmiddle;
                                        }
                                        break;
                                    case TextLocationInWord.AtEnd:
                                        {
                                            pattern = pattern_partword_vend_wend;
                                        }
                                        break;
                                }
                            }
                            break;
                    }
                }
                break;
            case TextWordness.WholeWord:
                {
                    switch (text_location_in_verse)
                    {
                        case TextLocationInVerse.Any:
                            {
                                switch (text_location_in_word)
                                {
                                    case TextLocationInWord.Any:
                                        {
                                            pattern = pattern_wholeword_vanywhere_wanywhere;
                                        }
                                        break;
                                    case TextLocationInWord.AtStart:
                                        {
                                            pattern = pattern_wholeword_vanywhere_wstart;
                                        }
                                        break;
                                    case TextLocationInWord.AtMiddle:
                                        {
                                            pattern = pattern_wholeword_vanywhere_wmiddle;
                                        }
                                        break;
                                    case TextLocationInWord.AtEnd:
                                        {
                                            pattern = pattern_wholeword_vanywhere_wend;
                                        }
                                        break;
                                }
                            }
                            break;
                        case TextLocationInVerse.AtStart:
                            {
                                switch (text_location_in_word)
                                {
                                    case TextLocationInWord.Any:
                                        {
                                            pattern = pattern_wholeword_vstart_wanywhere;
                                        }
                                        break;
                                    case TextLocationInWord.AtStart:
                                        {
                                            pattern = pattern_wholeword_vstart_wstart;
                                        }
                                        break;
                                    case TextLocationInWord.AtMiddle:
                                        {
                                            pattern = pattern_wholeword_vstart_wmiddle;
                                        }
                                        break;
                                    case TextLocationInWord.AtEnd:
                                        {
                                            pattern = pattern_wholeword_vstart_wend;
                                        }
                                        break;
                                }
                            }
                            break;
                        case TextLocationInVerse.AtMiddle:
                            {
                                switch (text_location_in_word)
                                {
                                    case TextLocationInWord.Any:
                                        {
                                            pattern = pattern_wholeword_vmiddle_wanywhere;
                                        }
                                        break;
                                    case TextLocationInWord.AtStart:
                                        {
                                            pattern = pattern_wholeword_vmiddle_wstart;
                                        }
                                        break;
                                    case TextLocationInWord.AtMiddle:
                                        {
                                            pattern = pattern_wholeword_vmiddle_wmiddle;
                                        }
                                        break;
                                    case TextLocationInWord.AtEnd:
                                        {
                                            pattern = pattern_wholeword_vmiddle_wend;
                                        }
                                        break;
                                }
                            }
                            break;
                        case TextLocationInVerse.AtEnd:
                            {
                                switch (text_location_in_word)
                                {
                                    case TextLocationInWord.Any:
                                        {
                                            pattern = pattern_wholeword_vend_wanywhere;
                                        }
                                        break;
                                    case TextLocationInWord.AtStart:
                                        {
                                            pattern = pattern_wholeword_vend_wstart;
                                        }
                                        break;
                                    case TextLocationInWord.AtMiddle:
                                        {
                                            pattern = pattern_wholeword_vend_wmiddle;
                                        }
                                        break;
                                    case TextLocationInWord.AtEnd:
                                        {
                                            pattern = pattern_wholeword_vend_wend;
                                        }
                                        break;
                                }
                            }
                            break;
                    }
                }
                break;
            default:
                {
                    pattern = pattern_empty_line;
                }
                break;
        }

        return pattern;
    }
    public static List<Phrase> FindPhrases(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, TextSearchBlockSize text_search_block_size, string text, LanguageType language_type, TextLocationInChapter text_location_in_chapter, TextLocationInVerse text_location_in_verse, TextLocationInWord text_location_in_word, TextWordness text_wordness, bool case_sensitive, bool with_diacritics, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Phrase> result = new List<Phrase>();

        if (language_type == LanguageType.RightToLeft)
        {
            result = DoFindPhrases(search_scope, current_selection, previous_verses, text_search_block_size, text, language_type, text_location_in_chapter, text_location_in_verse, text_location_in_word, text_wordness, case_sensitive, with_diacritics, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
        }
        else if (language_type == LanguageType.LeftToRight)
        {
            result = DoFindPhrases(search_scope, current_selection, previous_verses, text_search_block_size, text, language_type, text_location_in_chapter, text_location_in_verse, text_location_in_word, text_wordness, case_sensitive, with_diacritics, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
        }

        return result;
    }
    private static List<Phrase> DoFindPhrases(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, TextSearchBlockSize text_search_block_size, string text, LanguageType language_type, TextLocationInChapter text_location_in_chapter, TextLocationInVerse text_location_in_verse, TextLocationInWord text_location_in_word, TextWordness text_wordness, bool case_sensitive, bool with_diacritics, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Verse> verses = new List<Verse>();
        switch (text_search_block_size)
        {
            case TextSearchBlockSize.Verse:
                {
                    List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_verses, text_location_in_chapter);
                    if (language_type == LanguageType.RightToLeft)
                    {
                        return DoFindPhrases(source, current_selection, previous_verses, text, text_location_in_verse, text_location_in_word, text_wordness, case_sensitive, with_diacritics, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
                    }
                    else //if (language_type == FindByTextLanguageType.LeftToRight)
                    {
                        return DoFindPhrases(source, current_selection, previous_verses, text, text_location_in_verse, text_location_in_word, text_wordness, case_sensitive, with_diacritics, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
                    }
                }
            case TextSearchBlockSize.Chapter:
                {
                    List<Chapter> chapters = DoFindChapters(text, text_location_in_verse, text_location_in_word, text_wordness, case_sensitive, with_diacritics, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
                    if (chapters != null)
                    {
                        foreach (Chapter chapter in chapters)
                        {
                            if (chapter != null)
                            {
                                verses.AddRange(chapter.Verses);
                            }
                        }

                        List<Verse> source = GetSourceVerses(SearchScope.Result, current_selection, verses, text_location_in_chapter);
                        if (language_type == LanguageType.RightToLeft)
                        {
                            return DoFindPhrases(source, current_selection, previous_verses, text, text_location_in_verse, text_location_in_word, text_wordness, case_sensitive, with_diacritics, -1, NumberType.None, ComparisonOperator.Equal, -1);
                        }
                        else //if (language_type == FindByTextLanguageType.LeftToRight)
                        {
                            return DoFindPhrases(source, current_selection, previous_verses, text, text_location_in_verse, text_location_in_word, text_wordness, case_sensitive, with_diacritics, -1, NumberType.None, ComparisonOperator.Equal, -1);
                        }
                    }
                    return null;
                }
            case TextSearchBlockSize.Page:
                {
                    List<Page> pages = DoFindPages(text, text_location_in_verse, text_location_in_word, text_wordness, case_sensitive, with_diacritics, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
                    if (pages != null)
                    {
                        foreach (Page page in pages)
                        {
                            if (page != null)
                            {
                                verses.AddRange(page.Verses);
                            }
                        }

                        List<Verse> source = GetSourceVerses(SearchScope.Result, current_selection, verses, text_location_in_chapter);
                        if (language_type == LanguageType.RightToLeft)
                        {
                            return DoFindPhrases(source, current_selection, previous_verses, text, text_location_in_verse, text_location_in_word, text_wordness, case_sensitive, with_diacritics, -1, NumberType.None, ComparisonOperator.Equal, -1);
                        }
                        else //if (language_type == FindByTextLanguageType.LeftToRight)
                        {
                            return DoFindPhrases(source, current_selection, previous_verses, text, text_location_in_verse, text_location_in_word, text_wordness, case_sensitive, with_diacritics, -1, NumberType.None, ComparisonOperator.Equal, -1);
                        }
                    }
                    return null;
                }
            case TextSearchBlockSize.Station:
                {
                    List<Station> stations = DoFindStations(text, text_location_in_verse, text_location_in_word, text_wordness, case_sensitive, with_diacritics, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
                    if (stations != null)
                    {
                        foreach (Station station in stations)
                        {
                            if (station != null)
                            {
                                verses.AddRange(station.Verses);
                            }
                        }

                        List<Verse> source = GetSourceVerses(SearchScope.Result, current_selection, verses, text_location_in_chapter);
                        if (language_type == LanguageType.RightToLeft)
                        {
                            return DoFindPhrases(source, current_selection, previous_verses, text, text_location_in_verse, text_location_in_word, text_wordness, case_sensitive, with_diacritics, -1, NumberType.None, ComparisonOperator.Equal, -1);
                        }
                        else //if (language_type == FindByTextLanguageType.LeftToRight)
                        {
                            return DoFindPhrases(source, current_selection, previous_verses, text, text_location_in_verse, text_location_in_word, text_wordness, case_sensitive, with_diacritics, -1, NumberType.None, ComparisonOperator.Equal, -1);
                        }
                    }
                    return null;
                }
            case TextSearchBlockSize.Part:
                {
                    List<Part> parts = DoFindParts(text, text_location_in_verse, text_location_in_word, text_wordness, case_sensitive, with_diacritics, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
                    if (parts != null)
                    {
                        foreach (Part part in parts)
                        {
                            if (part != null)
                            {
                                verses.AddRange(part.Verses);
                            }
                        }

                        List<Verse> source = GetSourceVerses(SearchScope.Result, current_selection, verses, text_location_in_chapter);
                        if (language_type == LanguageType.RightToLeft)
                        {
                            return DoFindPhrases(source, current_selection, previous_verses, text, text_location_in_verse, text_location_in_word, text_wordness, case_sensitive, with_diacritics, -1, NumberType.None, ComparisonOperator.Equal, -1);
                        }
                        else //if (language_type == FindByTextLanguageType.LeftToRight)
                        {
                            return DoFindPhrases(source, current_selection, previous_verses, text, text_location_in_verse, text_location_in_word, text_wordness, case_sensitive, with_diacritics, -1, NumberType.None, ComparisonOperator.Equal, -1);
                        }
                    }
                    return null;
                }
            case TextSearchBlockSize.Group:
                {
                    List<Model.Group> groups = DoFindGroups(text, text_location_in_verse, text_location_in_word, text_wordness, case_sensitive, with_diacritics, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
                    if (groups != null)
                    {
                        foreach (Model.Group group in groups)
                        {
                            if (group != null)
                            {
                                verses.AddRange(group.Verses);
                            }
                        }

                        List<Verse> source = GetSourceVerses(SearchScope.Result, current_selection, verses, text_location_in_chapter);
                        if (language_type == LanguageType.RightToLeft)
                        {
                            return DoFindPhrases(source, current_selection, previous_verses, text, text_location_in_verse, text_location_in_word, text_wordness, case_sensitive, with_diacritics, -1, NumberType.None, ComparisonOperator.Equal, -1);
                        }
                        else //if (language_type == FindByTextLanguageType.LeftToRight)
                        {
                            return DoFindPhrases(source, current_selection, previous_verses, text, text_location_in_verse, text_location_in_word, text_wordness, case_sensitive, with_diacritics, -1, NumberType.None, ComparisonOperator.Equal, -1);
                        }
                    }
                    return null;
                }
            case TextSearchBlockSize.Half:
                {
                    List<Half> halfs = DoFindHalfs(text, text_location_in_verse, text_location_in_word, text_wordness, case_sensitive, with_diacritics, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
                    if (halfs != null)
                    {
                        foreach (Half half in halfs)
                        {
                            if (half != null)
                            {
                                verses.AddRange(half.Verses);
                            }
                        }

                        List<Verse> source = GetSourceVerses(SearchScope.Result, current_selection, verses, text_location_in_chapter);
                        if (language_type == LanguageType.RightToLeft)
                        {
                            return DoFindPhrases(source, current_selection, previous_verses, text, text_location_in_verse, text_location_in_word, text_wordness, case_sensitive, with_diacritics, -1, NumberType.None, ComparisonOperator.Equal, -1);
                        }
                        else //if (language_type == FindByTextLanguageType.LeftToRight)
                        {
                            return DoFindPhrases(source, current_selection, previous_verses, text, text_location_in_verse, text_location_in_word, text_wordness, case_sensitive, with_diacritics, -1, NumberType.None, ComparisonOperator.Equal, -1);
                        }
                    }
                    return null;
                }
            case TextSearchBlockSize.Quarter:
                {
                    List<Quarter> quarters = DoFindQuarters(text, text_location_in_verse, text_location_in_word, text_wordness, case_sensitive, with_diacritics, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
                    if (quarters != null)
                    {
                        foreach (Quarter quarter in quarters)
                        {
                            if (quarter != null)
                            {
                                verses.AddRange(quarter.Verses);
                            }
                        }

                        List<Verse> source = GetSourceVerses(SearchScope.Result, current_selection, verses, text_location_in_chapter);
                        if (language_type == LanguageType.RightToLeft)
                        {
                            return DoFindPhrases(source, current_selection, previous_verses, text, text_location_in_verse, text_location_in_word, text_wordness, case_sensitive, with_diacritics, -1, NumberType.None, ComparisonOperator.Equal, -1);
                        }
                        else //if (language_type == FindByTextLanguageType.LeftToRight)
                        {
                            return DoFindPhrases(source, current_selection, previous_verses, text, text_location_in_verse, text_location_in_word, text_wordness, case_sensitive, with_diacritics, -1, NumberType.None, ComparisonOperator.Equal, -1);
                        }
                    }
                    return null;
                }
            case TextSearchBlockSize.Bowing:
                {
                    List<Bowing> bowings = DoFindBowings(text, text_location_in_verse, text_location_in_word, text_wordness, case_sensitive, with_diacritics, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
                    if (bowings != null)
                    {
                        foreach (Bowing bowing in bowings)
                        {
                            if (bowing != null)
                            {
                                verses.AddRange(bowing.Verses);
                            }
                        }

                        List<Verse> source = GetSourceVerses(SearchScope.Result, current_selection, verses, text_location_in_chapter);
                        if (language_type == LanguageType.RightToLeft)
                        {
                            return DoFindPhrases(source, current_selection, previous_verses, text, text_location_in_verse, text_location_in_word, text_wordness, case_sensitive, with_diacritics, -1, NumberType.None, ComparisonOperator.Equal, -1);
                        }
                        else //if (language_type == FindByTextLanguageType.LeftToRight)
                        {
                            return DoFindPhrases(source, current_selection, previous_verses, text, text_location_in_verse, text_location_in_word, text_wordness, case_sensitive, with_diacritics, -1, NumberType.None, ComparisonOperator.Equal, -1);
                        }
                    }
                    return null;
                }
            default:
                return null;
        }
    }
    private static List<Phrase> DoFindPhrases(List<Verse> source, Selection current_selection, List<Verse> previous_verses, string text, TextLocationInVerse text_location_in_verse, TextLocationInWord text_location_in_word, TextWordness text_wordness, bool case_sensitive, bool with_diacritics, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Phrase> result = new List<Phrase>();

        if (source != null)
        {
            if (source.Count > 0)
            {
                if (!String.IsNullOrEmpty(text))
                {
                    text = Regex.Replace(text, @"\s+", " "); // remove double space or higher if any
                    RegexOptions regex_options = case_sensitive ? RegexOptions.None : RegexOptions.IgnoreCase;

                    if (with_diacritics)
                    {
                        string pattern = BuildPattern(text, text_location_in_verse, text_location_in_word, text_wordness);
                        if (!String.IsNullOrEmpty(pattern))
                        {
                            foreach (Verse verse in source)
                            {
                                string verse_text = s_book.RemoveQuranmarksAndStopmarks(text, verse.Text);

                                MatchCollection matches = Regex.Matches(verse_text, pattern, regex_options);
                                if (multiplicity == 0) // contains no matches
                                {
                                    if (matches.Count == 0)
                                    {
                                        result.Add(new Phrase(verse, 0, ""));
                                    }
                                }
                                else // contains wildcard (-1) or exact multiplicity or number_type multiplicity
                                {
                                    if (matches.Count > 0)
                                    {
                                        if (
                                             ((multiplicity_number_type == NumberType.None) && (multiplicity == -1)) ||
                                             ((multiplicity_number_type == NumberType.Natural) && (matches.Count == multiplicity)) ||
                                             (Compare(matches.Count, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder))
                                           )
                                        {
                                            result.AddRange(BuildPhrases(verse, matches, with_diacritics));
                                        }
                                    }
                                }
                            } // end for
                        }
                    }
                    else // without diacritics
                    {
                        if (s_numerology_system != null)
                        {
                            text = text.Simplify(s_numerology_system.TextMode);
                            if (!String.IsNullOrEmpty(text)) // re-test in case text was just harakaat which is simplifed to nothing
                            {
                                string pattern = BuildPattern(text, text_location_in_verse, text_location_in_word, text_wordness);
                                if (!String.IsNullOrEmpty(pattern))
                                {
                                    foreach (Verse verse in source)
                                    {
                                        string verse_text = verse.Text;
                                        if (s_numerology_system.TextMode == "Original")
                                        {
                                            verse_text = verse_text.Simplify29().Trim();
                                        }
                                        MatchCollection matches = Regex.Matches(verse_text, pattern, regex_options);
                                        if (multiplicity == 0) // contains no matches
                                        {
                                            if (matches.Count == 0)
                                            {
                                                result.Add(new Phrase(verse, 0, ""));
                                            }
                                        }
                                        else // contains wildcard (-1) or exact multiplicity or number_type multiplicity
                                        {
                                            if (matches.Count > 0)
                                            {
                                                if (
                                                     ((multiplicity_number_type == NumberType.None) && (multiplicity == -1)) ||
                                                     ((multiplicity_number_type == NumberType.Natural) && (matches.Count == multiplicity)) ||
                                                     (Compare(matches.Count, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder))
                                                   )
                                                {
                                                    result.AddRange(BuildPhrases(verse, matches, with_diacritics));
                                                }
                                            }
                                        }
                                    } // end for
                                }
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
    private static List<Chapter> DoFindChapters(string text, TextLocationInVerse text_location_in_verse, TextLocationInWord text_location_in_word, TextWordness text_wordness, bool case_sensitive, bool with_diacritics, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Chapter> result = new List<Chapter>();

        if (!String.IsNullOrEmpty(text))
        {
            text = Regex.Replace(text, @"\s+", " "); // remove double space or higher if any
            RegexOptions regex_options = case_sensitive ? RegexOptions.None : RegexOptions.IgnoreCase;

            if (s_book != null)
            {
                if (with_diacritics)
                {
                    if (!String.IsNullOrEmpty(text))
                    {
                        string pattern = BuildPattern(text, text_location_in_verse, text_location_in_word, text_wordness);
                        if (!String.IsNullOrEmpty(pattern))
                        {
                            foreach (Chapter chapter in s_book.Chapters)
                            {
                                string chapter_text = s_book.RemoveQuranmarksAndStopmarks(text, chapter.Text);

                                MatchCollection matches = Regex.Matches(chapter_text, pattern, regex_options);
                                if (
                                     ((multiplicity == 0) && (matches.Count == 0)) ||
                                     ((multiplicity_number_type == NumberType.None) && (multiplicity == -1)) ||
                                     ((multiplicity_number_type == NumberType.Natural) && (matches.Count == multiplicity)) ||
                                     (Compare(matches.Count, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder))
                                   )
                                {
                                    if (!result.Contains(chapter))
                                    {
                                        result.Add(chapter);
                                    }
                                }
                            }
                        } // end for
                    }
                }
                else // without diacritics
                {
                    if (s_numerology_system != null)
                    {
                        text = text.Simplify(s_numerology_system.TextMode);
                        if (!String.IsNullOrEmpty(text))
                        {
                            string pattern = BuildPattern(text, text_location_in_verse, text_location_in_word, text_wordness);
                            if (!String.IsNullOrEmpty(pattern))
                            {
                                foreach (Chapter chapter in s_book.Chapters)
                                {
                                    string chapter_text = chapter.Text;
                                    if (s_numerology_system.TextMode == "Original")
                                    {
                                        chapter_text = chapter_text.Simplify29().Trim();
                                    }
                                    MatchCollection matches = Regex.Matches(chapter_text, pattern, regex_options);
                                    if (
                                         ((multiplicity == 0) && (matches.Count == 0)) ||
                                         ((multiplicity_number_type == NumberType.None) && (multiplicity == -1)) ||
                                         ((multiplicity_number_type == NumberType.Natural) && (matches.Count == multiplicity)) ||
                                         (Compare(matches.Count, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder))
                                       )
                                    {
                                        if (!result.Contains(chapter))
                                        {
                                            result.Add(chapter);
                                        }
                                    }
                                } // end for
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
    private static List<Page> DoFindPages(string text, TextLocationInVerse text_location_in_verse, TextLocationInWord text_location_in_word, TextWordness text_wordness, bool case_sensitive, bool with_diacritics, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Page> result = new List<Page>();

        if (!String.IsNullOrEmpty(text))
        {
            text = Regex.Replace(text, @"\s+", " "); // remove double space or higher if any
            RegexOptions regex_options = case_sensitive ? RegexOptions.None : RegexOptions.IgnoreCase;

            if (s_book != null)
            {
                if (with_diacritics)
                {
                    if (!String.IsNullOrEmpty(text))
                    {
                        string pattern = BuildPattern(text, text_location_in_verse, text_location_in_word, text_wordness);
                        if (!String.IsNullOrEmpty(pattern))
                        {
                            foreach (Page page in s_book.Pages)
                            {
                                string page_text = s_book.RemoveQuranmarksAndStopmarks(text, page.Text);

                                MatchCollection matches = Regex.Matches(page_text, pattern, regex_options);
                                if (
                                     ((multiplicity == 0) && (matches.Count == 0)) ||
                                     ((multiplicity_number_type == NumberType.None) && (multiplicity == -1)) ||
                                     ((multiplicity_number_type == NumberType.Natural) && (matches.Count == multiplicity)) ||
                                     (Compare(matches.Count, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder))
                                   )
                                {
                                    if (!result.Contains(page))
                                    {
                                        result.Add(page);
                                    }
                                }
                            }
                        } // end for
                    }
                }
                else // without diacritics
                {
                    if (s_numerology_system != null)
                    {
                        text = text.Simplify(s_numerology_system.TextMode);
                        if (!String.IsNullOrEmpty(text))
                        {
                            string pattern = BuildPattern(text, text_location_in_verse, text_location_in_word, text_wordness);
                            if (!String.IsNullOrEmpty(pattern))
                            {
                                foreach (Page page in s_book.Pages)
                                {
                                    string page_text = page.Text;
                                    if (s_numerology_system.TextMode == "Original")
                                    {
                                        page_text = page_text.Simplify29().Trim();
                                    }
                                    MatchCollection matches = Regex.Matches(page_text, pattern, regex_options);
                                    if (
                                         ((multiplicity == 0) && (matches.Count == 0)) ||
                                         ((multiplicity_number_type == NumberType.None) && (multiplicity == -1)) ||
                                         ((multiplicity_number_type == NumberType.Natural) && (matches.Count == multiplicity)) ||
                                         (Compare(matches.Count, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder))
                                       )
                                    {
                                        if (!result.Contains(page))
                                        {
                                            result.Add(page);
                                        }
                                    }
                                } // end for
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
    private static List<Station> DoFindStations(string text, TextLocationInVerse text_location_in_verse, TextLocationInWord text_location_in_word, TextWordness text_wordness, bool case_sensitive, bool with_diacritics, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Station> result = new List<Station>();

        if (!String.IsNullOrEmpty(text))
        {
            text = Regex.Replace(text, @"\s+", " "); // remove double space or higher if any
            RegexOptions regex_options = case_sensitive ? RegexOptions.None : RegexOptions.IgnoreCase;

            if (s_book != null)
            {
                if (with_diacritics)
                {
                    if (!String.IsNullOrEmpty(text))
                    {
                        string pattern = BuildPattern(text, text_location_in_verse, text_location_in_word, text_wordness);
                        if (!String.IsNullOrEmpty(pattern))
                        {
                            foreach (Station station in s_book.Stations)
                            {
                                string station_text = s_book.RemoveQuranmarksAndStopmarks(text, station.Text);

                                MatchCollection matches = Regex.Matches(station_text, pattern, regex_options);
                                if (
                                     ((multiplicity == 0) && (matches.Count == 0)) ||
                                     ((multiplicity_number_type == NumberType.None) && (multiplicity == -1)) ||
                                     ((multiplicity_number_type == NumberType.Natural) && (matches.Count == multiplicity)) ||
                                     (Compare(matches.Count, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder))
                                   )
                                {
                                    if (!result.Contains(station))
                                    {
                                        result.Add(station);
                                    }
                                }
                            }
                        } // end for
                    }
                }
                else // without diacritics
                {
                    if (s_numerology_system != null)
                    {
                        text = text.Simplify(s_numerology_system.TextMode);
                        if (!String.IsNullOrEmpty(text))
                        {
                            string pattern = BuildPattern(text, text_location_in_verse, text_location_in_word, text_wordness);
                            if (!String.IsNullOrEmpty(pattern))
                            {
                                foreach (Station station in s_book.Stations)
                                {
                                    string station_text = station.Text;
                                    if (s_numerology_system.TextMode == "Original")
                                    {
                                        station_text = station_text.Simplify29().Trim();
                                    }
                                    MatchCollection matches = Regex.Matches(station_text, pattern, regex_options);
                                    if (
                                         ((multiplicity == 0) && (matches.Count == 0)) ||
                                         ((multiplicity_number_type == NumberType.None) && (multiplicity == -1)) ||
                                         ((multiplicity_number_type == NumberType.Natural) && (matches.Count == multiplicity)) ||
                                         (Compare(matches.Count, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder))
                                       )
                                    {
                                        if (!result.Contains(station))
                                        {
                                            result.Add(station);
                                        }
                                    }
                                } // end for
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
    private static List<Part> DoFindParts(string text, TextLocationInVerse text_location_in_verse, TextLocationInWord text_location_in_word, TextWordness text_wordness, bool case_sensitive, bool with_diacritics, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Part> result = new List<Part>();

        if (!String.IsNullOrEmpty(text))
        {
            text = Regex.Replace(text, @"\s+", " "); // remove double space or higher if any
            RegexOptions regex_options = case_sensitive ? RegexOptions.None : RegexOptions.IgnoreCase;

            if (s_book != null)
            {
                if (with_diacritics)
                {
                    if (!String.IsNullOrEmpty(text))
                    {
                        string pattern = BuildPattern(text, text_location_in_verse, text_location_in_word, text_wordness);
                        if (!String.IsNullOrEmpty(pattern))
                        {
                            foreach (Part part in s_book.Parts)
                            {
                                string part_text = s_book.RemoveQuranmarksAndStopmarks(text, part.Text);

                                MatchCollection matches = Regex.Matches(part_text, pattern, regex_options);
                                if (
                                     ((multiplicity == 0) && (matches.Count == 0)) ||
                                     ((multiplicity_number_type == NumberType.None) && (multiplicity == -1)) ||
                                     ((multiplicity_number_type == NumberType.Natural) && (matches.Count == multiplicity)) ||
                                     (Compare(matches.Count, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder))
                                   )
                                {
                                    if (!result.Contains(part))
                                    {
                                        result.Add(part);
                                    }
                                }
                            }
                        } // end for
                    }
                }
                else // without diacritics
                {
                    if (s_numerology_system != null)
                    {
                        text = text.Simplify(s_numerology_system.TextMode);
                        if (!String.IsNullOrEmpty(text))
                        {
                            string pattern = BuildPattern(text, text_location_in_verse, text_location_in_word, text_wordness);
                            if (!String.IsNullOrEmpty(pattern))
                            {
                                foreach (Part part in s_book.Parts)
                                {
                                    string part_text = part.Text;
                                    if (s_numerology_system.TextMode == "Original")
                                    {
                                        part_text = part_text.Simplify29().Trim();
                                    }
                                    MatchCollection matches = Regex.Matches(part_text, pattern, regex_options);
                                    if (
                                         ((multiplicity == 0) && (matches.Count == 0)) ||
                                         ((multiplicity_number_type == NumberType.None) && (multiplicity == -1)) ||
                                         ((multiplicity_number_type == NumberType.Natural) && (matches.Count == multiplicity)) ||
                                         (Compare(matches.Count, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder))
                                       )
                                    {
                                        if (!result.Contains(part))
                                        {
                                            result.Add(part);
                                        }
                                    }
                                } // end for
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
    private static List<Model.Group> DoFindGroups(string text, TextLocationInVerse text_location_in_verse, TextLocationInWord text_location_in_word, TextWordness text_wordness, bool case_sensitive, bool with_diacritics, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Model.Group> result = new List<Model.Group>();

        if (!String.IsNullOrEmpty(text))
        {
            text = Regex.Replace(text, @"\s+", " "); // remove double space or higher if any
            RegexOptions regex_options = case_sensitive ? RegexOptions.None : RegexOptions.IgnoreCase;

            if (s_book != null)
            {
                if (with_diacritics)
                {
                    if (!String.IsNullOrEmpty(text))
                    {
                        string pattern = BuildPattern(text, text_location_in_verse, text_location_in_word, text_wordness);
                        if (!String.IsNullOrEmpty(pattern))
                        {
                            foreach (Model.Group group in s_book.Groups)
                            {
                                string group_text = s_book.RemoveQuranmarksAndStopmarks(text, group.Text);

                                MatchCollection matches = Regex.Matches(group_text, pattern, regex_options);
                                if (
                                     ((multiplicity == 0) && (matches.Count == 0)) ||
                                     ((multiplicity_number_type == NumberType.None) && (multiplicity == -1)) ||
                                     ((multiplicity_number_type == NumberType.Natural) && (matches.Count == multiplicity)) ||
                                     (Compare(matches.Count, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder))
                                   )
                                {
                                    if (!result.Contains(group))
                                    {
                                        result.Add(group);
                                    }
                                }
                            }
                        } // end for
                    }
                }
                else // without diacritics
                {
                    if (s_numerology_system != null)
                    {
                        text = text.Simplify(s_numerology_system.TextMode);
                        if (!String.IsNullOrEmpty(text))
                        {
                            string pattern = BuildPattern(text, text_location_in_verse, text_location_in_word, text_wordness);
                            if (!String.IsNullOrEmpty(pattern))
                            {
                                foreach (Model.Group group in s_book.Groups)
                                {
                                    string group_text = group.Text;
                                    if (s_numerology_system.TextMode == "Original")
                                    {
                                        group_text = group_text.Simplify29().Trim();
                                    }
                                    MatchCollection matches = Regex.Matches(group_text, pattern, regex_options);
                                    if (
                                         ((multiplicity == 0) && (matches.Count == 0)) ||
                                         ((multiplicity_number_type == NumberType.None) && (multiplicity == -1)) ||
                                         ((multiplicity_number_type == NumberType.Natural) && (matches.Count == multiplicity)) ||
                                         (Compare(matches.Count, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder))
                                       )
                                    {
                                        if (!result.Contains(group))
                                        {
                                            result.Add(group);
                                        }
                                    }
                                } // end for
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
    private static List<Half> DoFindHalfs(string text, TextLocationInVerse text_location_in_verse, TextLocationInWord text_location_in_word, TextWordness text_wordness, bool case_sensitive, bool with_diacritics, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Half> result = new List<Half>();

        if (!String.IsNullOrEmpty(text))
        {
            text = Regex.Replace(text, @"\s+", " "); // remove double space or higher if any
            RegexOptions regex_options = case_sensitive ? RegexOptions.None : RegexOptions.IgnoreCase;

            if (s_book != null)
            {
                if (with_diacritics)
                {
                    if (!String.IsNullOrEmpty(text))
                    {
                        string pattern = BuildPattern(text, text_location_in_verse, text_location_in_word, text_wordness);
                        if (!String.IsNullOrEmpty(pattern))
                        {
                            foreach (Half half in s_book.Halfs)
                            {
                                string half_text = s_book.RemoveQuranmarksAndStopmarks(text, half.Text);

                                MatchCollection matches = Regex.Matches(half_text, pattern, regex_options);
                                if (
                                     ((multiplicity == 0) && (matches.Count == 0)) ||
                                     ((multiplicity_number_type == NumberType.None) && (multiplicity == -1)) ||
                                     ((multiplicity_number_type == NumberType.Natural) && (matches.Count == multiplicity)) ||
                                     (Compare(matches.Count, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder))
                                   )
                                {
                                    if (!result.Contains(half))
                                    {
                                        result.Add(half);
                                    }
                                }
                            }
                        } // end for
                    }
                }
                else // without diacritics
                {
                    if (s_numerology_system != null)
                    {
                        text = text.Simplify(s_numerology_system.TextMode);
                        if (!String.IsNullOrEmpty(text))
                        {
                            string pattern = BuildPattern(text, text_location_in_verse, text_location_in_word, text_wordness);
                            if (!String.IsNullOrEmpty(pattern))
                            {
                                foreach (Half half in s_book.Halfs)
                                {
                                    string half_text = half.Text;
                                    if (s_numerology_system.TextMode == "Original")
                                    {
                                        half_text = half_text.Simplify29().Trim();
                                    }
                                    MatchCollection matches = Regex.Matches(half_text, pattern, regex_options);
                                    if (
                                         ((multiplicity == 0) && (matches.Count == 0)) ||
                                         ((multiplicity_number_type == NumberType.None) && (multiplicity == -1)) ||
                                         ((multiplicity_number_type == NumberType.Natural) && (matches.Count == multiplicity)) ||
                                         (Compare(matches.Count, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder))
                                       )
                                    {
                                        if (!result.Contains(half))
                                        {
                                            result.Add(half);
                                        }
                                    }
                                } // end for
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
    private static List<Quarter> DoFindQuarters(string text, TextLocationInVerse text_location_in_verse, TextLocationInWord text_location_in_word, TextWordness text_wordness, bool case_sensitive, bool with_diacritics, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Quarter> result = new List<Quarter>();

        if (!String.IsNullOrEmpty(text))
        {
            text = Regex.Replace(text, @"\s+", " "); // remove double space or higher if any
            RegexOptions regex_options = case_sensitive ? RegexOptions.None : RegexOptions.IgnoreCase;

            if (s_book != null)
            {
                if (with_diacritics)
                {
                    if (!String.IsNullOrEmpty(text))
                    {
                        string pattern = BuildPattern(text, text_location_in_verse, text_location_in_word, text_wordness);
                        if (!String.IsNullOrEmpty(pattern))
                        {
                            foreach (Quarter quarter in s_book.Quarters)
                            {
                                string quarter_text = s_book.RemoveQuranmarksAndStopmarks(text, quarter.Text);

                                MatchCollection matches = Regex.Matches(quarter_text, pattern, regex_options);
                                if (
                                     ((multiplicity == 0) && (matches.Count == 0)) ||
                                     ((multiplicity_number_type == NumberType.None) && (multiplicity == -1)) ||
                                     ((multiplicity_number_type == NumberType.Natural) && (matches.Count == multiplicity)) ||
                                     (Compare(matches.Count, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder))
                                   )
                                {
                                    if (!result.Contains(quarter))
                                    {
                                        result.Add(quarter);
                                    }
                                }
                            }
                        } // end for
                    }
                }
                else // without diacritics
                {
                    if (s_numerology_system != null)
                    {
                        text = text.Simplify(s_numerology_system.TextMode);
                        if (!String.IsNullOrEmpty(text))
                        {
                            string pattern = BuildPattern(text, text_location_in_verse, text_location_in_word, text_wordness);
                            if (!String.IsNullOrEmpty(pattern))
                            {
                                foreach (Quarter quarter in s_book.Quarters)
                                {
                                    string quarter_text = quarter.Text;
                                    if (s_numerology_system.TextMode == "Original")
                                    {
                                        quarter_text = quarter_text.Simplify29().Trim();
                                    }
                                    MatchCollection matches = Regex.Matches(quarter_text, pattern, regex_options);
                                    if (
                                         ((multiplicity == 0) && (matches.Count == 0)) ||
                                         ((multiplicity_number_type == NumberType.None) && (multiplicity == -1)) ||
                                         ((multiplicity_number_type == NumberType.Natural) && (matches.Count == multiplicity)) ||
                                         (Compare(matches.Count, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder))
                                       )
                                    {
                                        if (!result.Contains(quarter))
                                        {
                                            result.Add(quarter);
                                        }
                                    }
                                } // end for
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
    private static List<Bowing> DoFindBowings(string text, TextLocationInVerse text_location_in_verse, TextLocationInWord text_location_in_word, TextWordness text_wordness, bool case_sensitive, bool with_diacritics, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Bowing> result = new List<Bowing>();

        if (!String.IsNullOrEmpty(text))
        {
            text = Regex.Replace(text, @"\s+", " "); // remove double space or higher if any
            RegexOptions regex_options = case_sensitive ? RegexOptions.None : RegexOptions.IgnoreCase;

            if (s_book != null)
            {
                if (with_diacritics)
                {
                    if (!String.IsNullOrEmpty(text))
                    {
                        string pattern = BuildPattern(text, text_location_in_verse, text_location_in_word, text_wordness);
                        if (!String.IsNullOrEmpty(pattern))
                        {
                            foreach (Bowing bowing in s_book.Bowings)
                            {
                                string bowing_text = s_book.RemoveQuranmarksAndStopmarks(text, bowing.Text);

                                MatchCollection matches = Regex.Matches(bowing_text, pattern, regex_options);
                                if (
                                     ((multiplicity == 0) && (matches.Count == 0)) ||
                                     ((multiplicity_number_type == NumberType.None) && (multiplicity == -1)) ||
                                     ((multiplicity_number_type == NumberType.Natural) && (matches.Count == multiplicity)) ||
                                     (Compare(matches.Count, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder))
                                   )
                                {
                                    if (!result.Contains(bowing))
                                    {
                                        result.Add(bowing);
                                    }
                                }
                            }
                        } // end for
                    }
                }
                else // without diacritics
                {
                    if (s_numerology_system != null)
                    {
                        text = text.Simplify(s_numerology_system.TextMode);
                        if (!String.IsNullOrEmpty(text))
                        {
                            string pattern = BuildPattern(text, text_location_in_verse, text_location_in_word, text_wordness);
                            if (!String.IsNullOrEmpty(pattern))
                            {
                                foreach (Bowing bowing in s_book.Bowings)
                                {
                                    string bowing_text = bowing.Text;
                                    if (s_numerology_system.TextMode == "Original")
                                    {
                                        bowing_text = bowing_text.Simplify29().Trim();
                                    }
                                    MatchCollection matches = Regex.Matches(bowing_text, pattern, regex_options);
                                    if (
                                         ((multiplicity == 0) && (matches.Count == 0)) ||
                                         ((multiplicity_number_type == NumberType.None) && (multiplicity == -1)) ||
                                         ((multiplicity_number_type == NumberType.Natural) && (matches.Count == multiplicity)) ||
                                         (Compare(matches.Count, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder))
                                       )
                                    {
                                        if (!result.Contains(bowing))
                                        {
                                            result.Add(bowing);
                                        }
                                    }
                                } // end for
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
    // find by text - Proximity
    private static void BuildWordLists(string text, out List<string> unsigned_words, out List<string> positive_words, out List<string> negative_words)
    {
        unsigned_words = new List<string>();
        positive_words = new List<string>();
        negative_words = new List<string>();

        if (String.IsNullOrEmpty(text)) return;
        text = Regex.Replace(text, @"\s+", " "); // remove double space or higher if any

        string[] text_words = text.Split(text.Contains("|") ? '|' : ' ');
        foreach (string text_word in text_words)
        {
            if (text_word.StartsWith("-"))
            {
                negative_words.Add(text_word.Substring(1));
            }
            else if (text_word.EndsWith("-"))
            {
                negative_words.Add(text_word.Substring(0, text_word.Length - 1));
            }
            else if (text_word.StartsWith("+"))
            {
                positive_words.Add(text_word.Substring(1));
            }
            else if (text_word.EndsWith("+"))
            {
                positive_words.Add(text_word.Substring(0, text_word.Length - 1));
            }
            else
            {
                unsigned_words.Add(text_word);
            }
        }
    }
    public static List<Word> FindWords(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, TextSearchBlockSize text_search_block_size, string text, LanguageType language_type, TextWordGrouping text_word_grouping, TextWordness text_wordness, bool case_sensitive, bool with_diacritics)
    {
        List<Word> result = new List<Word>();

        if (language_type == LanguageType.RightToLeft)
        {
            result = DoFindWords(search_scope, current_selection, previous_verses, text_search_block_size, text, language_type, text_word_grouping, text_wordness, case_sensitive, with_diacritics);
        }
        else if (language_type == LanguageType.LeftToRight)
        {
            result = DoFindWords(search_scope, current_selection, previous_verses, text_search_block_size, text, language_type, text_word_grouping, text_wordness, case_sensitive, with_diacritics);
        }

        Word.CompareBy = WordCompareBy.Number;
        Word.CompareOrder = WordCompareOrder.Ascending;
        result.Sort();

        return result;
    }
    private static List<Word> DoFindWords(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, TextSearchBlockSize text_search_block_size, string text, LanguageType language_type, TextWordGrouping text_word_grouping, TextWordness text_wordness, bool case_sensitive, bool with_diacritics)
    {
        List<Verse> result = new List<Verse>();
        switch (text_search_block_size)
        {
            case TextSearchBlockSize.Verse:
                {
                    List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_verses, TextLocationInChapter.Any);
                    if (language_type == LanguageType.RightToLeft)
                    {
                        return DoFindWords(source, current_selection, previous_verses, text, text_word_grouping, text_wordness, case_sensitive, with_diacritics);
                    }
                    else //if (language_type == FindByTextLanguageType.LeftToRight)
                    {
                        return DoFindWords(source, current_selection, previous_verses, text, text_word_grouping, text_wordness, case_sensitive, with_diacritics);
                    }
                }
            case TextSearchBlockSize.Chapter:
                {
                    List<Chapter> chapters = DoFindChapters(text, text_word_grouping, with_diacritics);
                    if (chapters != null)
                    {
                        foreach (Chapter chapter in chapters)
                        {
                            if (chapter != null)
                            {
                                result.AddRange(chapter.Verses);
                            }
                        }
                    }
                }
                break;
            case TextSearchBlockSize.Page:
                {
                    List<Page> pages = DoFindPages(text, text_word_grouping, with_diacritics);
                    if (pages != null)
                    {
                        foreach (Page page in pages)
                        {
                            if (page != null)
                            {
                                result.AddRange(page.Verses);
                            }
                        }
                    }
                }
                break;
            case TextSearchBlockSize.Station:
                {
                    List<Station> stations = DoFindStations(text, text_word_grouping, with_diacritics);
                    if (stations != null)
                    {
                        foreach (Station station in stations)
                        {
                            if (station != null)
                            {
                                result.AddRange(station.Verses);
                            }
                        }
                    }
                }
                break;
            case TextSearchBlockSize.Part:
                {
                    List<Part> parts = DoFindParts(text, text_word_grouping, with_diacritics);
                    if (parts != null)
                    {
                        foreach (Part part in parts)
                        {
                            if (part != null)
                            {
                                result.AddRange(part.Verses);
                            }
                        }
                    }
                }
                break;
            case TextSearchBlockSize.Group:
                {
                    List<Model.Group> groups = DoFindGroups(text, text_word_grouping, with_diacritics);
                    if (groups != null)
                    {
                        foreach (Model.Group group in groups)
                        {
                            if (group != null)
                            {
                                result.AddRange(group.Verses);
                            }
                        }
                    }
                }
                break;
            case TextSearchBlockSize.Half:
                {
                    List<Half> halfs = DoFindHalfs(text, text_word_grouping, with_diacritics);
                    if (halfs != null)
                    {
                        foreach (Half half in halfs)
                        {
                            if (half != null)
                            {
                                result.AddRange(half.Verses);
                            }
                        }
                    }
                }
                break;
            case TextSearchBlockSize.Quarter:
                {
                    List<Quarter> quarters = DoFindQuarters(text, text_word_grouping, with_diacritics);
                    if (quarters != null)
                    {
                        foreach (Quarter quarter in quarters)
                        {
                            if (quarter != null)
                            {
                                result.AddRange(quarter.Verses);
                            }
                        }
                    }
                }
                break;
            case TextSearchBlockSize.Bowing:
                {
                    List<Bowing> bowings = DoFindBowings(text, text_word_grouping, with_diacritics);
                    if (bowings != null)
                    {
                        foreach (Bowing bowing in bowings)
                        {
                            if (bowing != null)
                            {
                                result.AddRange(bowing.Verses);
                            }
                        }
                    }
                }
                break;
            default:
                break;
        }
        if (language_type == LanguageType.RightToLeft)
        {
            return DoFindWords(result, current_selection, null, text, TextWordGrouping.Or, TextWordness.Any, false, false);
        }
        else //if (language_type == FindByTextLanguageType.LeftToRight)
        {
            return DoFindWords(result, current_selection, null, text, TextWordGrouping.Or, TextWordness.Any, false, false);
        }
    }
    private static List<Word> DoFindWords(List<Verse> source, Selection current_selection, List<Verse> previous_verses, string text, TextWordGrouping text_word_grouping, TextWordness text_wordness, bool case_sensitive, bool with_diacritics)
    {
        List<Word> result = new List<Word>();
        if (source != null)
        {
            if (source.Count > 0)
            {
                if (!String.IsNullOrEmpty(text))
                {
                    text = Regex.Replace(text, @"\s+", " "); // remove double space or higher if any

                    List<string> unsigned_words = null;
                    List<string> positive_words = null;
                    List<string> negative_words = null;

                    if (with_diacritics)
                    {
                        BuildWordLists(text, out unsigned_words, out positive_words, out negative_words);

                        foreach (Verse verse in source)
                        {
                            /////////////////////////
                            // process negative_words
                            /////////////////////////
                            if (negative_words.Count > 0)
                            {
                                bool found = false;
                                foreach (string negative_word in negative_words)
                                {
                                    foreach (Word word in verse.Words)
                                    {
                                        string word_text = word.Text;
                                        if (text_wordness == TextWordness.Any)
                                        {
                                            if (word_text.Contains(negative_word))
                                            {
                                                found = true; // next verse
                                                break;
                                            }
                                        }
                                        else if (text_wordness == TextWordness.PartOfWord)
                                        {
                                            if ((word_text.Contains(negative_word)) && (word_text.Length > negative_word.Length))
                                            {
                                                found = true; // next verse
                                                break;
                                            }
                                        }
                                        else if (text_wordness == TextWordness.WholeWord)
                                        {
                                            if (word_text == negative_word)
                                            {
                                                found = true; // next verse
                                                break;
                                            }
                                        }
                                    }
                                    if (found)
                                    {
                                        break;
                                    }
                                }
                                if (found) continue; // next verse
                            }

                            /////////////////////////
                            // process positive_words
                            /////////////////////////
                            if (positive_words.Count > 0)
                            {
                                int match_count = 0;
                                foreach (string positive_word in positive_words)
                                {
                                    foreach (Word word in verse.Words)
                                    {
                                        string word_text = word.Text;
                                        if (text_wordness == TextWordness.Any)
                                        {
                                            if (word_text.Contains(positive_word))
                                            {
                                                match_count++;
                                                break; // next positive_word
                                            }
                                        }
                                        else if (text_wordness == TextWordness.PartOfWord)
                                        {
                                            if ((word_text.Contains(positive_word)) && (word_text.Length > positive_word.Length))
                                            {
                                                match_count++;
                                                break; // next positive_word
                                            }
                                        }
                                        else if (text_wordness == TextWordness.WholeWord)
                                        {
                                            if (word_text == positive_word)
                                            {
                                                match_count++;
                                                break; // next positive_word
                                            }
                                        }
                                    }
                                }

                                // verse failed test, so skip it
                                if (match_count < positive_words.Count)
                                {
                                    continue; // next verse
                                }
                            }

                            //////////////////////////////////////////////////////
                            // both negative and positive conditions have been met
                            //////////////////////////////////////////////////////

                            /////////////////////////
                            // process unsigned_words
                            /////////////////////////
                            //////////////////////////////////////////////////////////
                            // FindByText WORDS Any
                            //////////////////////////////////////////////////////////
                            if ((text.Contains("|")) || (text_word_grouping == TextWordGrouping.Or))
                            {
                                bool found = false;
                                foreach (string unsigned_word in unsigned_words)
                                {
                                    foreach (Word word in verse.Words)
                                    {
                                        string word_text = word.Text;
                                        if (text_wordness == TextWordness.Any)
                                        {
                                            if (word_text.Contains(unsigned_word))
                                            {
                                                found = true;
                                                break; // no need to continue even if there are more matches
                                            }
                                        }
                                        else if (text_wordness == TextWordness.PartOfWord)
                                        {
                                            if ((word_text.Contains(unsigned_word)) && (word_text.Length > unsigned_word.Length))
                                            {
                                                found = true;
                                                break; // no need to continue even if there are more matches
                                            }
                                        }
                                        else if (text_wordness == TextWordness.WholeWord)
                                        {
                                            if (word_text == unsigned_word)
                                            {
                                                found = true;
                                                break; // no need to continue even if there are more matches
                                            }
                                        }
                                    }
                                    if (found)
                                    {
                                        break;
                                    }
                                }

                                if (found) // found 1 unsigned word in verse, which is enough
                                {
                                    ///////////////////////////////////////////////////////////////
                                    // all negative, positive and unsigned conditions have been met
                                    ///////////////////////////////////////////////////////////////

                                    // add positive matches
                                    foreach (string positive_word in positive_words)
                                    {
                                        foreach (Word word in verse.Words)
                                        {
                                            string word_text = word.Text;
                                            if (text_wordness == TextWordness.Any)
                                            {
                                                if (word_text.Contains(positive_word))
                                                {
                                                    result.Add(word);
                                                    //break; // no break in case there are more matches
                                                }
                                            }
                                            else if (text_wordness == TextWordness.PartOfWord)
                                            {
                                                if ((word_text.Contains(positive_word)) && (word_text.Length > positive_word.Length))
                                                {
                                                    result.Add(word);
                                                    //break; // no break in case there are more matches
                                                }
                                            }
                                            else if (text_wordness == TextWordness.WholeWord)
                                            {
                                                if (word_text == positive_word)
                                                {
                                                    result.Add(word);
                                                    //break; // no break in case there are more matches
                                                }
                                            }
                                        }
                                    }

                                    // add unsigned matches
                                    foreach (string unsigned_word in unsigned_words)
                                    {
                                        foreach (Word word in verse.Words)
                                        {
                                            string word_text = word.Text;
                                            if (text_wordness == TextWordness.Any)
                                            {
                                                if (word_text.Contains(unsigned_word))
                                                {
                                                    result.Add(word);
                                                    //break; // no break in case there are more matches
                                                }
                                            }
                                            else if (text_wordness == TextWordness.PartOfWord)
                                            {
                                                if ((word_text.Contains(unsigned_word)) && (word_text.Length > unsigned_word.Length))
                                                {
                                                    result.Add(word);
                                                    //break; // no break in case there are more matches
                                                }
                                            }
                                            else if (text_wordness == TextWordness.WholeWord)
                                            {
                                                if (word_text == unsigned_word)
                                                {
                                                    result.Add(word);
                                                    //break; // no break in case there are more matches
                                                }
                                            }
                                        }
                                    }
                                }
                                else // verse failed test, so skip it
                                {
                                    continue; // next verse
                                }
                            }
                            //////////////////////////////////////////////////////////
                            // FindByText WORDS All
                            //////////////////////////////////////////////////////////
                            else if (text_word_grouping == TextWordGrouping.And)
                            {
                                int match_count = 0;
                                foreach (string unsigned_word in unsigned_words)
                                {
                                    foreach (Word word in verse.Words)
                                    {
                                        string word_text = word.Text;
                                        if (text_wordness == TextWordness.Any)
                                        {
                                            if (word_text.Contains(unsigned_word))
                                            {
                                                match_count++;
                                                break; // no need to continue even if there are more matches
                                            }
                                        }
                                        else if (text_wordness == TextWordness.PartOfWord)
                                        {
                                            if ((word_text.Contains(unsigned_word)) && (word_text.Length > unsigned_word.Length))
                                            {
                                                match_count++;
                                                break; // no need to continue even if there are more matches
                                            }
                                        }
                                        else if (text_wordness == TextWordness.WholeWord)
                                        {
                                            if (word_text == unsigned_word)
                                            {
                                                match_count++;
                                                break; // no need to continue even if there are more matches
                                            }
                                        }
                                    }
                                }

                                if (match_count == unsigned_words.Count)
                                {
                                    ///////////////////////////////////////////////////////////////
                                    // all negative, positive and unsigned conditions have been met
                                    ///////////////////////////////////////////////////////////////

                                    // add positive matches
                                    foreach (string positive_word in positive_words)
                                    {
                                        foreach (Word word in verse.Words)
                                        {
                                            string word_text = word.Text;
                                            if (text_wordness == TextWordness.Any)
                                            {
                                                if (word_text.Contains(positive_word))
                                                {
                                                    result.Add(word);
                                                    //break; // no break in case there are more matches
                                                }
                                            }
                                            else if (text_wordness == TextWordness.PartOfWord)
                                            {
                                                if ((word_text.Contains(positive_word)) && (word_text.Length > positive_word.Length))
                                                {
                                                    result.Add(word);
                                                    //break; // no break in case there are more matches
                                                }
                                            }
                                            else if (text_wordness == TextWordness.WholeWord)
                                            {
                                                if (word_text == positive_word)
                                                {
                                                    result.Add(word);
                                                    //break; // no break in case there are more matches
                                                }
                                            }
                                        }
                                    }

                                    // add unsigned matches
                                    foreach (string unsigned_word in unsigned_words)
                                    {
                                        foreach (Word word in verse.Words)
                                        {
                                            string word_text = word.Text;
                                            if (text_wordness == TextWordness.Any)
                                            {
                                                if (word_text.Contains(unsigned_word))
                                                {
                                                    result.Add(word);
                                                    //break; // no break in case there are more matches
                                                }
                                            }
                                            else if (text_wordness == TextWordness.PartOfWord)
                                            {
                                                if ((word_text.Contains(unsigned_word)) && (word_text.Length > unsigned_word.Length))
                                                {
                                                    result.Add(word);
                                                    //break; // no break in case there are more matches
                                                }
                                            }
                                            else if (text_wordness == TextWordness.WholeWord)
                                            {
                                                if (word_text == unsigned_word)
                                                {
                                                    result.Add(word);
                                                    //break; // no break in case there are more matches
                                                }
                                            }
                                        }
                                    }
                                }
                                else // verse failed test, so skip it
                                {
                                    continue; // next verse
                                }
                            }
                        } // end for
                    }
                    else // without diacritics
                    {
                        if (s_numerology_system != null)
                        {
                            text = text.Simplify(s_numerology_system.TextMode);
                            if (!String.IsNullOrEmpty(text)) // re-test in case text was just harakaat which is simplifed to nothing
                            {
                                BuildWordLists(text, out unsigned_words, out positive_words, out negative_words);

                                foreach (Verse verse in source)
                                {
                                    /////////////////////////
                                    // process negative_words
                                    /////////////////////////
                                    if (negative_words.Count > 0)
                                    {
                                        bool found = false;
                                        foreach (string negative_word in negative_words)
                                        {
                                            foreach (Word word in verse.Words)
                                            {
                                                string word_text = word.Text;
                                                if (s_numerology_system.TextMode == "Original")
                                                {
                                                    word_text = word_text.Simplify29().Trim();
                                                }
                                                if (text_wordness == TextWordness.Any)
                                                {
                                                    if (word_text.Contains(negative_word))
                                                    {
                                                        found = true; // next verse
                                                        break;
                                                    }
                                                }
                                                else if (text_wordness == TextWordness.PartOfWord)
                                                {
                                                    if ((word_text.Contains(negative_word)) && (word_text.Length > negative_word.Length))
                                                    {
                                                        found = true; // next verse
                                                        break;
                                                    }
                                                }
                                                else if (text_wordness == TextWordness.WholeWord)
                                                {
                                                    if (word_text == negative_word)
                                                    {
                                                        found = true; // next verse
                                                        break;
                                                    }
                                                }
                                            }
                                            if (found)
                                            {
                                                break;
                                            }
                                        }
                                        if (found) continue; // next verse
                                    }

                                    /////////////////////////
                                    // process positive_words
                                    /////////////////////////
                                    if (positive_words.Count > 0)
                                    {
                                        int match_count = 0;
                                        foreach (string positive_word in positive_words)
                                        {
                                            foreach (Word word in verse.Words)
                                            {
                                                string word_text = word.Text;
                                                if (s_numerology_system.TextMode == "Original")
                                                {
                                                    word_text = word_text.Simplify29().Trim();
                                                }
                                                if (text_wordness == TextWordness.Any)
                                                {
                                                    if (word_text.Contains(positive_word))
                                                    {
                                                        match_count++;
                                                        break; // next positive_word
                                                    }
                                                }
                                                else if (text_wordness == TextWordness.PartOfWord)
                                                {
                                                    if ((word_text.Contains(positive_word)) && (word_text.Length > positive_word.Length))
                                                    {
                                                        match_count++;
                                                        break; // next positive_word
                                                    }
                                                }
                                                else if (text_wordness == TextWordness.WholeWord)
                                                {
                                                    if (word_text == positive_word)
                                                    {
                                                        match_count++;
                                                        break; // next positive_word
                                                    }
                                                }
                                            }
                                        }

                                        // verse failed test, so skip it
                                        if (match_count < positive_words.Count)
                                        {
                                            continue; // next verse
                                        }
                                    }

                                    //////////////////////////////////////////////////////
                                    // both negative and positive conditions have been met
                                    //////////////////////////////////////////////////////

                                    /////////////////////////
                                    // process unsigned_words
                                    /////////////////////////
                                    //////////////////////////////////////////////////////////
                                    // FindByText WORDS Any
                                    //////////////////////////////////////////////////////////
                                    if ((text.Contains("|")) || (text_word_grouping == TextWordGrouping.Or))
                                    {
                                        bool found = false;
                                        foreach (string unsigned_word in unsigned_words)
                                        {
                                            foreach (Word word in verse.Words)
                                            {
                                                string word_text = word.Text;
                                                if (s_numerology_system.TextMode == "Original")
                                                {
                                                    word_text = word_text.Simplify29().Trim();
                                                }
                                                if (text_wordness == TextWordness.Any)
                                                {
                                                    if (word_text.Contains(unsigned_word))
                                                    {
                                                        found = true;
                                                        break; // no need to continue even if there are more matches
                                                    }
                                                }
                                                else if (text_wordness == TextWordness.PartOfWord)
                                                {
                                                    if ((word_text.Contains(unsigned_word)) && (word_text.Length > unsigned_word.Length))
                                                    {
                                                        found = true;
                                                        break; // no need to continue even if there are more matches
                                                    }
                                                }
                                                else if (text_wordness == TextWordness.WholeWord)
                                                {
                                                    if (word_text == unsigned_word)
                                                    {
                                                        found = true;
                                                        break; // no need to continue even if there are more matches
                                                    }
                                                }
                                            }
                                            if (found)
                                            {
                                                break;
                                            }
                                        }

                                        if (found) // found 1 unsigned word in verse, which is enough
                                        {
                                            ///////////////////////////////////////////////////////////////
                                            // all negative, positive and unsigned conditions have been met
                                            ///////////////////////////////////////////////////////////////

                                            // add positive matches
                                            foreach (string positive_word in positive_words)
                                            {
                                                foreach (Word word in verse.Words)
                                                {
                                                    string word_text = word.Text;
                                                    if (s_numerology_system.TextMode == "Original")
                                                    {
                                                        word_text = word_text.Simplify29().Trim();
                                                    }
                                                    if (text_wordness == TextWordness.Any)
                                                    {
                                                        if (word_text.Contains(positive_word))
                                                        {
                                                            result.Add(word);
                                                            //break; // no break in case there are more matches
                                                        }
                                                    }
                                                    else if (text_wordness == TextWordness.PartOfWord)
                                                    {
                                                        if ((word_text.Contains(positive_word)) && (word_text.Length > positive_word.Length))
                                                        {
                                                            result.Add(word);
                                                            //break; // no break in case there are more matches
                                                        }
                                                    }
                                                    else if (text_wordness == TextWordness.WholeWord)
                                                    {
                                                        if (word_text == positive_word)
                                                        {
                                                            result.Add(word);
                                                            //break; // no break in case there are more matches
                                                        }
                                                    }
                                                }
                                            }

                                            // add unsigned matches
                                            foreach (string unsigned_word in unsigned_words)
                                            {
                                                foreach (Word word in verse.Words)
                                                {
                                                    string word_text = word.Text;
                                                    if (s_numerology_system.TextMode == "Original")
                                                    {
                                                        word_text = word_text.Simplify29().Trim();
                                                    }
                                                    if (text_wordness == TextWordness.Any)
                                                    {
                                                        if (word_text.Contains(unsigned_word))
                                                        {
                                                            result.Add(word);
                                                            //break; // no break in case there are more matches
                                                        }
                                                    }
                                                    else if (text_wordness == TextWordness.PartOfWord)
                                                    {
                                                        if ((word_text.Contains(unsigned_word)) && (word_text.Length > unsigned_word.Length))
                                                        {
                                                            result.Add(word);
                                                            //break; // no break in case there are more matches
                                                        }
                                                    }
                                                    else if (text_wordness == TextWordness.WholeWord)
                                                    {
                                                        if (word_text == unsigned_word)
                                                        {
                                                            result.Add(word);
                                                            //break; // no break in case there are more matches
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else // verse failed test, so skip it
                                        {
                                            continue; // next verse
                                        }
                                    }
                                    //////////////////////////////////////////////////////////
                                    // FindByText WORDS All
                                    //////////////////////////////////////////////////////////
                                    else if (text_word_grouping == TextWordGrouping.And)
                                    {
                                        int match_count = 0;
                                        foreach (string unsigned_word in unsigned_words)
                                        {
                                            foreach (Word word in verse.Words)
                                            {
                                                string word_text = word.Text;
                                                if (s_numerology_system.TextMode == "Original")
                                                {
                                                    word_text = word_text.Simplify29().Trim();
                                                }
                                                if (text_wordness == TextWordness.Any)
                                                {
                                                    if (word_text.Contains(unsigned_word))
                                                    {
                                                        match_count++;
                                                        break; // no need to continue even if there are more matches
                                                    }
                                                }
                                                else if (text_wordness == TextWordness.PartOfWord)
                                                {
                                                    if ((word_text.Contains(unsigned_word)) && (word_text.Length > unsigned_word.Length))
                                                    {
                                                        match_count++;
                                                        break; // no need to continue even if there are more matches
                                                    }
                                                }
                                                else if (text_wordness == TextWordness.WholeWord)
                                                {
                                                    if (word_text == unsigned_word)
                                                    {
                                                        match_count++;
                                                        break; // no need to continue even if there are more matches
                                                    }
                                                }
                                            }
                                        }

                                        if (match_count == unsigned_words.Count)
                                        {
                                            ///////////////////////////////////////////////////////////////
                                            // all negative, positive and unsigned conditions have been met
                                            ///////////////////////////////////////////////////////////////

                                            // add positive matches
                                            foreach (string positive_word in positive_words)
                                            {
                                                foreach (Word word in verse.Words)
                                                {
                                                    string word_text = word.Text;
                                                    if (s_numerology_system.TextMode == "Original")
                                                    {
                                                        word_text = word_text.Simplify29().Trim();
                                                    }
                                                    if (text_wordness == TextWordness.Any)
                                                    {
                                                        if (word_text.Contains(positive_word))
                                                        {
                                                            result.Add(word);
                                                            //break; // no break in case there are more matches
                                                        }
                                                    }
                                                    else if (text_wordness == TextWordness.PartOfWord)
                                                    {
                                                        if ((word_text.Contains(positive_word)) && (word_text.Length > positive_word.Length))
                                                        {
                                                            result.Add(word);
                                                            //break; // no break in case there are more matches
                                                        }
                                                    }
                                                    else if (text_wordness == TextWordness.WholeWord)
                                                    {
                                                        if (word_text == positive_word)
                                                        {
                                                            result.Add(word);
                                                            //break; // no break in case there are more matches
                                                        }
                                                    }
                                                }
                                            }

                                            // add unsigned matches
                                            foreach (string unsigned_word in unsigned_words)
                                            {
                                                foreach (Word word in verse.Words)
                                                {
                                                    string word_text = word.Text;
                                                    if (s_numerology_system.TextMode == "Original")
                                                    {
                                                        word_text = word_text.Simplify29().Trim();
                                                    }
                                                    if (text_wordness == TextWordness.Any)
                                                    {
                                                        if (word_text.Contains(unsigned_word))
                                                        {
                                                            result.Add(word);
                                                            //break; // no break in case there are more matches
                                                        }
                                                    }
                                                    else if (text_wordness == TextWordness.PartOfWord)
                                                    {
                                                        if ((word_text.Contains(unsigned_word)) && (word_text.Length > unsigned_word.Length))
                                                        {
                                                            result.Add(word);
                                                            //break; // no break in case there are more matches
                                                        }
                                                    }
                                                    else if (text_wordness == TextWordness.WholeWord)
                                                    {
                                                        if (word_text == unsigned_word)
                                                        {
                                                            result.Add(word);
                                                            //break; // no break in case there are more matches
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else // verse failed test, so skip it
                                        {
                                            continue; // next verse
                                        }
                                    }
                                } // end for
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
    private static List<Chapter> DoFindChapters(string text, TextWordGrouping text_word_grouping, bool with_diacritics)
    {
        List<Chapter> result = new List<Chapter>();

        if (!String.IsNullOrEmpty(text))
        {
            if (s_numerology_system != null)
            {
                if ((!with_diacritics) && (s_numerology_system.TextMode == "Original"))
                {
                    text = text.Simplify29().Trim();
                }
                while (text.Contains("  "))
                {
                    text = text.Replace("  ", " ");
                }

                List<string> negative_words = new List<string>();
                List<string> positive_words = new List<string>();
                List<string> unsigned_words = new List<string>();
                BuildWordLists(text, out unsigned_words, out positive_words, out negative_words);

                if (s_book != null)
                {
                    foreach (Chapter chapter in s_book.Chapters)
                    {
                        string chapter_text = chapter.Text.Trim();
                        if ((!with_diacritics) && (s_numerology_system.TextMode == "Original"))
                        {
                            chapter_text = chapter_text.Simplify29().Trim();
                        }
                        chapter_text = chapter_text.Trim();

                        if ((text.Contains("|")) || (text_word_grouping == TextWordGrouping.Or))
                        {
                            bool found = false;
                            foreach (string word in negative_words)
                            {
                                if (chapter_text.Contains(word))
                                {
                                    found = true;
                                    break;
                                }
                            }
                            if (found) continue;

                            foreach (string positive_word in positive_words)
                            {
                                if (!chapter_text.Contains(positive_word))
                                {
                                    found = true;
                                    break;
                                }
                            }
                            if (found) continue;

                            if (
                                 (negative_words.Count > 0) ||
                                 (positive_words.Count > 0) ||
                                 (
                                   (unsigned_words.Count == 0) ||
                                   (chapter_text.ContainsWord(unsigned_words))
                                 )
                               )
                            {
                                if (!result.Contains(chapter))
                                {
                                    result.Add(chapter);
                                }
                            }
                        }
                        else if (text_word_grouping == TextWordGrouping.And)
                        {
                            bool found = false;
                            foreach (string word in negative_words)
                            {
                                if (chapter_text.Contains(word))
                                {
                                    found = true; // next chapter
                                    break;
                                }
                            }
                            if (found) continue;

                            foreach (string word in positive_words)
                            {
                                if (!chapter_text.Contains(word))
                                {
                                    found = true; // next chapter
                                    break;
                                }
                            }
                            if (found) continue;

                            if (
                                 (unsigned_words.Count == 0) ||
                                 (chapter_text.ContainsWords(unsigned_words))
                               )
                            {
                                if (!result.Contains(chapter))
                                {
                                    result.Add(chapter);
                                }
                            }
                        }
                    } // end for
                }
            }
        }

        return result;
    }
    private static List<Page> DoFindPages(string text, TextWordGrouping text_word_grouping, bool with_diacritics)
    {
        List<Page> result = new List<Page>();

        if (!String.IsNullOrEmpty(text))
        {
            if ((!with_diacritics) && (s_numerology_system.TextMode == "Original"))
            {
                text = text.Simplify29().Trim();
            }
            while (text.Contains("  "))
            {
                text = text.Replace("  ", " ");
            }

            List<string> negative_words = new List<string>();
            List<string> positive_words = new List<string>();
            List<string> unsigned_words = new List<string>();
            BuildWordLists(text, out unsigned_words, out positive_words, out negative_words);

            if (s_book != null)
            {
                foreach (Page page in s_book.Pages)
                {
                    string page_text = page.Text.Trim();
                    if ((!with_diacritics) && (s_numerology_system.TextMode == "Original"))
                    {
                        page_text = page_text.Simplify29().Trim();
                    }
                    page_text = page_text.Trim();

                    if ((text.Contains("|")) || (text_word_grouping == TextWordGrouping.Or))
                    {
                        bool found = false;
                        foreach (string word in negative_words)
                        {
                            if (page_text.Contains(word))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (found) continue;

                        foreach (string positive_word in positive_words)
                        {
                            if (!page_text.Contains(positive_word))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (found) continue;

                        if (
                             (negative_words.Count > 0) ||
                             (positive_words.Count > 0) ||
                             (
                               (unsigned_words.Count == 0) ||
                               (page_text.ContainsWord(unsigned_words))
                             )
                           )
                        {
                            if (!result.Contains(page))
                            {
                                result.Add(page);
                            }
                        }
                    }
                    else if (text_word_grouping == TextWordGrouping.And)
                    {
                        bool found = false;
                        foreach (string word in negative_words)
                        {
                            if (page_text.Contains(word))
                            {
                                found = true; // next page
                                break;
                            }
                        }
                        if (found) continue;

                        foreach (string word in positive_words)
                        {
                            if (!page_text.Contains(word))
                            {
                                found = true; // next page
                                break;
                            }
                        }
                        if (found) continue;

                        if (
                             (unsigned_words.Count == 0) ||
                             (page_text.ContainsWords(unsigned_words))
                           )
                        {
                            if (!result.Contains(page))
                            {
                                result.Add(page);
                            }
                        }
                    }
                } // end for
            }
        }

        return result;
    }
    private static List<Station> DoFindStations(string text, TextWordGrouping text_word_grouping, bool with_diacritics)
    {
        List<Station> result = new List<Station>();

        if (!String.IsNullOrEmpty(text))
        {
            if ((!with_diacritics) && (s_numerology_system.TextMode == "Original"))
            {
                text = text.Simplify29().Trim();
            }
            while (text.Contains("  "))
            {
                text = text.Replace("  ", " ");
            }

            List<string> negative_words = new List<string>();
            List<string> positive_words = new List<string>();
            List<string> unsigned_words = new List<string>();
            BuildWordLists(text, out unsigned_words, out positive_words, out negative_words);

            if (s_book != null)
            {
                foreach (Station station in s_book.Stations)
                {
                    string station_text = station.Text.Trim();
                    if ((!with_diacritics) && (s_numerology_system.TextMode == "Original"))
                    {
                        station_text = station_text.Simplify29().Trim();
                    }
                    station_text = station_text.Trim();

                    if ((text.Contains("|")) || (text_word_grouping == TextWordGrouping.Or))
                    {
                        bool found = false;
                        foreach (string word in negative_words)
                        {
                            if (station_text.Contains(word))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (found) continue;

                        foreach (string positive_word in positive_words)
                        {
                            if (!station_text.Contains(positive_word))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (found) continue;

                        if (
                             (negative_words.Count > 0) ||
                             (positive_words.Count > 0) ||
                             (
                               (unsigned_words.Count == 0) ||
                               (station_text.ContainsWord(unsigned_words))
                             )
                           )
                        {
                            if (!result.Contains(station))
                            {
                                result.Add(station);
                            }
                        }
                    }
                    else if (text_word_grouping == TextWordGrouping.And)
                    {
                        bool found = false;
                        foreach (string word in negative_words)
                        {
                            if (station_text.Contains(word))
                            {
                                found = true; // next station
                                break;
                            }
                        }
                        if (found) continue;

                        foreach (string word in positive_words)
                        {
                            if (!station_text.Contains(word))
                            {
                                found = true; // next station
                                break;
                            }
                        }
                        if (found) continue;

                        if (
                             (unsigned_words.Count == 0) ||
                             (station_text.ContainsWords(unsigned_words))
                           )
                        {
                            if (!result.Contains(station))
                            {
                                result.Add(station);
                            }
                        }
                    }
                } // end for
            }
        }

        return result;
    }
    private static List<Part> DoFindParts(string text, TextWordGrouping text_word_grouping, bool with_diacritics)
    {
        List<Part> result = new List<Part>();

        if (!String.IsNullOrEmpty(text))
        {
            if ((!with_diacritics) && (s_numerology_system.TextMode == "Original"))
            {
                text = text.Simplify29().Trim();
            }
            while (text.Contains("  "))
            {
                text = text.Replace("  ", " ");
            }

            List<string> negative_words = new List<string>();
            List<string> positive_words = new List<string>();
            List<string> unsigned_words = new List<string>();
            BuildWordLists(text, out unsigned_words, out positive_words, out negative_words);

            if (s_book != null)
            {
                foreach (Part part in s_book.Parts)
                {
                    string part_text = part.Text.Trim();
                    if ((!with_diacritics) && (s_numerology_system.TextMode == "Original"))
                    {
                        part_text = part_text.Simplify29().Trim();
                    }
                    part_text = part_text.Trim();

                    if ((text.Contains("|")) || (text_word_grouping == TextWordGrouping.Or))
                    {
                        bool found = false;
                        foreach (string word in negative_words)
                        {
                            if (part_text.Contains(word))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (found) continue;

                        foreach (string positive_word in positive_words)
                        {
                            if (!part_text.Contains(positive_word))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (found) continue;

                        if (
                             (negative_words.Count > 0) ||
                             (positive_words.Count > 0) ||
                             (
                               (unsigned_words.Count == 0) ||
                               (part_text.ContainsWord(unsigned_words))
                             )
                           )
                        {
                            if (!result.Contains(part))
                            {
                                result.Add(part);
                            }
                        }
                    }
                    else if (text_word_grouping == TextWordGrouping.And)
                    {
                        bool found = false;
                        foreach (string word in negative_words)
                        {
                            if (part_text.Contains(word))
                            {
                                found = true; // next part
                                break;
                            }
                        }
                        if (found) continue;

                        foreach (string word in positive_words)
                        {
                            if (!part_text.Contains(word))
                            {
                                found = true; // next part
                                break;
                            }
                        }
                        if (found) continue;

                        if (
                             (unsigned_words.Count == 0) ||
                             (part_text.ContainsWords(unsigned_words))
                           )
                        {
                            if (!result.Contains(part))
                            {
                                result.Add(part);
                            }
                        }
                    }
                } // end for
            }
        }

        return result;
    }
    private static List<Model.Group> DoFindGroups(string text, TextWordGrouping text_word_grouping, bool with_diacritics)
    {
        List<Model.Group> result = new List<Model.Group>();

        if (!String.IsNullOrEmpty(text))
        {
            if ((!with_diacritics) && (s_numerology_system.TextMode == "Original"))
            {
                text = text.Simplify29().Trim();
            }
            while (text.Contains("  "))
            {
                text = text.Replace("  ", " ");
            }

            List<string> negative_words = new List<string>();
            List<string> positive_words = new List<string>();
            List<string> unsigned_words = new List<string>();
            BuildWordLists(text, out unsigned_words, out positive_words, out negative_words);

            if (s_book != null)
            {
                foreach (Model.Group group in s_book.Groups)
                {
                    string group_text = group.Text.Trim();
                    if ((!with_diacritics) && (s_numerology_system.TextMode == "Original"))
                    {
                        group_text = group_text.Simplify29().Trim();
                    }
                    group_text = group_text.Trim();

                    if ((text.Contains("|")) || (text_word_grouping == TextWordGrouping.Or))
                    {
                        bool found = false;
                        foreach (string word in negative_words)
                        {
                            if (group_text.Contains(word))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (found) continue;

                        foreach (string positive_word in positive_words)
                        {
                            if (!group_text.Contains(positive_word))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (found) continue;

                        if (
                             (negative_words.Count > 0) ||
                             (positive_words.Count > 0) ||
                             (
                               (unsigned_words.Count == 0) ||
                               (group_text.ContainsWord(unsigned_words))
                             )
                           )
                        {
                            if (!result.Contains(group))
                            {
                                result.Add(group);
                            }
                        }
                    }
                    else if (text_word_grouping == TextWordGrouping.And)
                    {
                        bool found = false;
                        foreach (string word in negative_words)
                        {
                            if (group_text.Contains(word))
                            {
                                found = true; // next group
                                break;
                            }
                        }
                        if (found) continue;

                        foreach (string word in positive_words)
                        {
                            if (!group_text.Contains(word))
                            {
                                found = true; // next group
                                break;
                            }
                        }
                        if (found) continue;

                        if (
                             (unsigned_words.Count == 0) ||
                             (group_text.ContainsWords(unsigned_words))
                           )
                        {
                            if (!result.Contains(group))
                            {
                                result.Add(group);
                            }
                        }
                    }
                } // end for
            }
        }

        return result;
    }
    private static List<Half> DoFindHalfs(string text, TextWordGrouping text_word_grouping, bool with_diacritics)
    {
        List<Half> result = new List<Half>();

        if (!String.IsNullOrEmpty(text))
        {
            if ((!with_diacritics) && (s_numerology_system.TextMode == "Original"))
            {
                text = text.Simplify29().Trim();
            }
            while (text.Contains("  "))
            {
                text = text.Replace("  ", " ");
            }

            List<string> negative_words = new List<string>();
            List<string> positive_words = new List<string>();
            List<string> unsigned_words = new List<string>();
            BuildWordLists(text, out unsigned_words, out positive_words, out negative_words);

            if (s_book != null)
            {
                foreach (Half half in s_book.Halfs)
                {
                    string half_text = half.Text.Trim();
                    if ((!with_diacritics) && (s_numerology_system.TextMode == "Original"))
                    {
                        half_text = half_text.Simplify29().Trim();
                    }
                    half_text = half_text.Trim();

                    if ((text.Contains("|")) || (text_word_grouping == TextWordGrouping.Or))
                    {
                        bool found = false;
                        foreach (string word in negative_words)
                        {
                            if (half_text.Contains(word))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (found) continue;

                        foreach (string positive_word in positive_words)
                        {
                            if (!half_text.Contains(positive_word))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (found) continue;

                        if (
                             (negative_words.Count > 0) ||
                             (positive_words.Count > 0) ||
                             (
                               (unsigned_words.Count == 0) ||
                               (half_text.ContainsWord(unsigned_words))
                             )
                           )
                        {
                            if (!result.Contains(half))
                            {
                                result.Add(half);
                            }
                        }
                    }
                    else if (text_word_grouping == TextWordGrouping.And)
                    {
                        bool found = false;
                        foreach (string word in negative_words)
                        {
                            if (half_text.Contains(word))
                            {
                                found = true; // next half
                                break;
                            }
                        }
                        if (found) continue;

                        foreach (string word in positive_words)
                        {
                            if (!half_text.Contains(word))
                            {
                                found = true; // next half
                                break;
                            }
                        }
                        if (found) continue;

                        if (
                             (unsigned_words.Count == 0) ||
                             (half_text.ContainsWords(unsigned_words))
                           )
                        {
                            if (!result.Contains(half))
                            {
                                result.Add(half);
                            }
                        }
                    }
                } // end for
            }
        }

        return result;
    }
    private static List<Quarter> DoFindQuarters(string text, TextWordGrouping text_word_grouping, bool with_diacritics)
    {
        List<Quarter> result = new List<Quarter>();

        if (!String.IsNullOrEmpty(text))
        {
            if ((!with_diacritics) && (s_numerology_system.TextMode == "Original"))
            {
                text = text.Simplify29().Trim();
            }
            while (text.Contains("  "))
            {
                text = text.Replace("  ", " ");
            }

            List<string> negative_words = new List<string>();
            List<string> positive_words = new List<string>();
            List<string> unsigned_words = new List<string>();
            BuildWordLists(text, out unsigned_words, out positive_words, out negative_words);

            if (s_book != null)
            {
                foreach (Quarter quarter in s_book.Quarters)
                {
                    string quarter_text = quarter.Text.Trim();
                    if ((!with_diacritics) && (s_numerology_system.TextMode == "Original"))
                    {
                        quarter_text = quarter_text.Simplify29().Trim();
                    }
                    quarter_text = quarter_text.Trim();

                    if ((text.Contains("|")) || (text_word_grouping == TextWordGrouping.Or))
                    {
                        bool found = false;
                        foreach (string word in negative_words)
                        {
                            if (quarter_text.Contains(word))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (found) continue;

                        foreach (string positive_word in positive_words)
                        {
                            if (!quarter_text.Contains(positive_word))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (found) continue;

                        if (
                             (negative_words.Count > 0) ||
                             (positive_words.Count > 0) ||
                             (
                               (unsigned_words.Count == 0) ||
                               (quarter_text.ContainsWord(unsigned_words))
                             )
                           )
                        {
                            if (!result.Contains(quarter))
                            {
                                result.Add(quarter);
                            }
                        }
                    }
                    else if (text_word_grouping == TextWordGrouping.And)
                    {
                        bool found = false;
                        foreach (string word in negative_words)
                        {
                            if (quarter_text.Contains(word))
                            {
                                found = true; // next quarter
                                break;
                            }
                        }
                        if (found) continue;

                        foreach (string word in positive_words)
                        {
                            if (!quarter_text.Contains(word))
                            {
                                found = true; // next quarter
                                break;
                            }
                        }
                        if (found) continue;

                        if (
                             (unsigned_words.Count == 0) ||
                             (quarter_text.ContainsWords(unsigned_words))
                           )
                        {
                            if (!result.Contains(quarter))
                            {
                                result.Add(quarter);
                            }
                        }
                    }
                } // end for
            }
        }

        return result;
    }
    private static List<Bowing> DoFindBowings(string text, TextWordGrouping text_word_grouping, bool with_diacritics)
    {
        List<Bowing> result = new List<Bowing>();

        if (!String.IsNullOrEmpty(text))
        {
            if ((!with_diacritics) && (s_numerology_system.TextMode == "Original"))
            {
                text = text.Simplify29().Trim();
            }
            while (text.Contains("  "))
            {
                text = text.Replace("  ", " ");
            }

            List<string> negative_words = new List<string>();
            List<string> positive_words = new List<string>();
            List<string> unsigned_words = new List<string>();
            BuildWordLists(text, out unsigned_words, out positive_words, out negative_words);

            if (s_book != null)
            {
                foreach (Bowing bowing in s_book.Bowings)
                {
                    string bowing_text = bowing.Text.Trim();
                    if ((!with_diacritics) && (s_numerology_system.TextMode == "Original"))
                    {
                        bowing_text = bowing_text.Simplify29().Trim();
                    }
                    bowing_text = bowing_text.Trim();

                    if ((text.Contains("|")) || (text_word_grouping == TextWordGrouping.Or))
                    {
                        bool found = false;
                        foreach (string word in negative_words)
                        {
                            if (bowing_text.Contains(word))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (found) continue;

                        foreach (string positive_word in positive_words)
                        {
                            if (!bowing_text.Contains(positive_word))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (found) continue;

                        if (
                             (negative_words.Count > 0) ||
                             (positive_words.Count > 0) ||
                             (
                               (unsigned_words.Count == 0) ||
                               (bowing_text.ContainsWord(unsigned_words))
                             )
                           )
                        {
                            if (!result.Contains(bowing))
                            {
                                result.Add(bowing);
                            }
                        }
                    }
                    else if (text_word_grouping == TextWordGrouping.And)
                    {
                        bool found = false;
                        foreach (string word in negative_words)
                        {
                            if (bowing_text.Contains(word))
                            {
                                found = true; // next bowing
                                break;
                            }
                        }
                        if (found) continue;

                        foreach (string word in positive_words)
                        {
                            if (!bowing_text.Contains(word))
                            {
                                found = true; // next bowing
                                break;
                            }
                        }
                        if (found) continue;

                        if (
                             (unsigned_words.Count == 0) ||
                             (bowing_text.ContainsWords(unsigned_words))
                           )
                        {
                            if (!result.Contains(bowing))
                            {
                                result.Add(bowing);
                            }
                        }
                    }
                } // end for
            }
        }

        return result;
    }
    // find by text - Root
    private static void MergeWords(List<Word> current_words, ref List<Verse> previous_verses, ref List<Word> previous_words)
    {
        if (current_words != null)
        {
            if (previous_words != null)
            {
                // extract verses from current words
                previous_verses = new List<Verse>(GetVerses(current_words));
                if (previous_verses != null)
                {
                    if (previous_words.Count == 0)
                    {
                        previous_words = current_words;
                    }
                    else
                    {
                        // add current words
                        List<Word> total = new List<Word>(current_words);

                        // add previous words if their verses exist in current words
                        foreach (Word previous_word in previous_words)
                        {
                            if (previous_word != null)
                            {
                                if (previous_verses.Contains(previous_word.Verse))
                                {
                                    total.Add(previous_word);
                                }
                            }
                        }
                        previous_words = total;
                    }
                }
            }
        }
    }
    public static List<Word> FindWords(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, TextSearchBlockSize text_search_block_size, string texts, TextWordGrouping text_word_grouping, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Word> result = new List<Word>();

        // first call only
        if (previous_verses == null)
        {
            previous_verses = new List<Verse>();
        }

        if (String.IsNullOrEmpty(texts)) return null;
        texts = texts.Simplify36();   // texts use 36 letters
        while (texts.Contains("  "))
        {
            texts = texts.Replace("  ", " ");
        }

        List<Word> current_result = null;
        List<string> negative_words = new List<string>();
        List<string> positive_words = new List<string>();
        List<string> unsigned_words = new List<string>();
        BuildWordLists(texts, out unsigned_words, out positive_words, out negative_words);

        foreach (string negative_word in negative_words)
        {
            current_result = DoFindWords(search_scope, current_selection, previous_verses, text_search_block_size, negative_word, 0, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder); // multiplicity = 0 for exclude
            if (text_word_grouping == TextWordGrouping.Or)
            {
                result.AddRange(current_result);
                previous_verses.Union(GetVerses(current_result));
            }
            else if (text_word_grouping == TextWordGrouping.And)
            {
                MergeWords(current_result, ref previous_verses, ref result);
                search_scope = SearchScope.Result;
            }
        }

        foreach (string positive_word in positive_words)
        {
            current_result = DoFindWords(search_scope, current_selection, previous_verses, text_search_block_size, positive_word, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
            if (text_word_grouping == TextWordGrouping.Or)
            {
                result.AddRange(current_result);
                previous_verses.Union(GetVerses(current_result));
            }
            else if (text_word_grouping == TextWordGrouping.And)
            {
                MergeWords(current_result, ref previous_verses, ref result);
                search_scope = SearchScope.Result;
            }
        }

        foreach (string unsigned_word in unsigned_words)
        {
            current_result = DoFindWords(search_scope, current_selection, previous_verses, text_search_block_size, unsigned_word, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
            if (text_word_grouping == TextWordGrouping.Or)
            {
                result.AddRange(current_result);
                previous_verses.Union(GetVerses(current_result));
            }
            else if (text_word_grouping == TextWordGrouping.And)
            {
                MergeWords(current_result, ref previous_verses, ref result);
                search_scope = SearchScope.Result;
            }
        }

        return result;
    }
    private static List<Word> DoFindWords(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, TextSearchBlockSize text_search_block_size, string text, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Verse> result = new List<Verse>();
        switch (text_search_block_size)
        {
            case TextSearchBlockSize.Verse:
                {
                    List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_verses, TextLocationInChapter.Any);
                    return DoFindWords(source, text, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
                }
            case TextSearchBlockSize.Chapter:
                {
                    List<Chapter> chapters = DoFindChapters(text, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
                    if (chapters != null)
                    {
                        foreach (Chapter chapter in chapters)
                        {
                            if (chapter != null)
                            {
                                result.AddRange(chapter.Verses);
                            }
                        }
                    }
                }
                break;
            case TextSearchBlockSize.Page:
                {
                    List<Page> pages = DoFindPages(text, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
                    if (pages != null)
                    {
                        foreach (Page page in pages)
                        {
                            if (page != null)
                            {
                                result.AddRange(page.Verses);
                            }
                        }
                    }
                }
                break;
            case TextSearchBlockSize.Station:
                {
                    List<Station> stations = DoFindStations(text, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
                    if (stations != null)
                    {
                        foreach (Station station in stations)
                        {
                            if (station != null)
                            {
                                result.AddRange(station.Verses);
                            }
                        }
                    }
                }
                break;
            case TextSearchBlockSize.Part:
                {
                    List<Part> parts = DoFindParts(text, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
                    if (parts != null)
                    {
                        foreach (Part part in parts)
                        {
                            if (part != null)
                            {
                                result.AddRange(part.Verses);
                            }
                        }
                    }
                }
                break;
            case TextSearchBlockSize.Group:
                {
                    List<Model.Group> groups = DoFindGroups(text, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
                    if (groups != null)
                    {
                        foreach (Model.Group group in groups)
                        {
                            if (group != null)
                            {
                                result.AddRange(group.Verses);
                            }
                        }
                    }
                }
                break;
            case TextSearchBlockSize.Half:
                {
                    List<Half> halfs = DoFindHalfs(text, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
                    if (halfs != null)
                    {
                        foreach (Half half in halfs)
                        {
                            if (half != null)
                            {
                                result.AddRange(half.Verses);
                            }
                        }
                    }
                }
                break;
            case TextSearchBlockSize.Quarter:
                {
                    List<Quarter> quarters = DoFindQuarters(text, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
                    if (quarters != null)
                    {
                        foreach (Quarter quarter in quarters)
                        {
                            if (quarter != null)
                            {
                                result.AddRange(quarter.Verses);
                            }
                        }
                    }
                }
                break;
            case TextSearchBlockSize.Bowing:
                {
                    List<Bowing> bowings = DoFindBowings(text, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
                    if (bowings != null)
                    {
                        foreach (Bowing bowing in bowings)
                        {
                            if (bowing != null)
                            {
                                result.AddRange(bowing.Verses);
                            }
                        }
                    }
                }
                break;
            default:
                break;
        }
        return DoFindWords(result, text, -1, NumberType.None, ComparisonOperator.Equal, -1);
    }
    private static List<Word> DoFindWords(List<Verse> source, string text, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Word> result = new List<Word>();

        if (source != null)
        {
            if (source.Count > 0)
            {
                if (!String.IsNullOrEmpty(text))
                {
                    if (s_book != null)
                    {
                        SortedDictionary<string, List<Word>> root_words_dictionary = s_book.RootWords;
                        if (root_words_dictionary != null)
                        {
                            List<Word> root_words = null;
                            if (root_words_dictionary.ContainsKey(text))
                            {
                                // get all pre-identified root_words
                                root_words = root_words_dictionary[text];
                            }
                            if (root_words != null)
                            {
                                result = GetWordsWithRootWords(source, root_words, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
                            }
                            else // text is a text, not a real text
                            {
                                foreach (Verse verse in s_book.Verses)
                                {
                                    foreach (Word word in verse.Words)
                                    {
                                        string best_root = s_book.GetBestRoot(text);
                                        return DoFindWords(source, best_root, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
                                    }
                                }

                                // must not come here !!!
                                throw new Exception(text + " is not a text and no best text found!");
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
    private static List<Word> GetWordsWithRootWords(List<Verse> source, List<Word> root_words, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Word> result = new List<Word>();

        if (source != null)
        {
            if (source.Count > 0)
            {
                if (s_book != null)
                {
                    Dictionary<Verse, int> multiplicity_dictionary = new Dictionary<Verse, int>();
                    foreach (Word word in root_words)
                    {
                        Verse verse = s_book.Verses[word.Verse.Number - 1];
                        if (source.Contains(verse))
                        {
                            if (multiplicity_dictionary.ContainsKey(verse))
                            {
                                multiplicity_dictionary[verse]++;
                            }
                            else // first found
                            {
                                multiplicity_dictionary.Add(verse, 1);
                            }
                        }
                    }

                    if (multiplicity == 0) // contains no matches
                    {
                        foreach (Verse verse in source)
                        {
                            if (!multiplicity_dictionary.ContainsKey(verse))
                            {
                                //result.Add(word); //???
                            }
                        }
                    }
                    else // contains wildcard (-1) or exact multiplicity or number_type multiplicity
                    {
                        foreach (Word word in root_words)
                        {
                            int verse_index = word.Verse.Number - 1;
                            if ((verse_index >= 0) && (verse_index < s_book.Verses.Count))
                            {
                                Verse verse = s_book.Verses[verse_index];
                                if (multiplicity_dictionary.ContainsKey(verse))
                                {
                                    if (((multiplicity_number_type == NumberType.None) && (multiplicity == -1)) || (Compare(multiplicity_dictionary[verse], multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder)))
                                    {
                                        if (source.Contains(verse))
                                        {
                                            int word_index = word.NumberInVerse - 1;
                                            if ((word_index >= 0) && (word_index < verse.Words.Count))
                                            {
                                                Word verse_word = verse.Words[word_index];
                                                string word_text = verse_word.Text;
                                                int word_position = verse_word.Position;
                                                result.Add(word);
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

        return result;
    }
    private static List<Chapter> DoFindChapters(string texts, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Chapter> result = new List<Chapter>();

        if (s_book != null)
        {
            List<Chapter> source = s_book.Chapters;

            if (String.IsNullOrEmpty(texts)) return null;
            while (texts.Contains("  "))
            {
                texts = texts.Replace("  ", " ");
            }

            string[] parts = texts.Split();
            if (parts.Length > 0) // enable nested searches
            {
                List<string> negative_words = new List<string>();
                List<string> positive_words = new List<string>();
                List<string> unsigned_words = new List<string>();
                BuildWordLists(texts, out unsigned_words, out positive_words, out negative_words);

                if (negative_words.Count > 0)
                {
                    foreach (string word in negative_words)
                    {
                        List<Chapter> temp = DoFindChapters(source, word, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);

                        result = new List<Chapter>(source);
                        foreach (Chapter chapter in temp)
                        {
                            result.Remove(chapter);
                        }

                        source = new List<Chapter>(result);
                    }
                }

                if (positive_words.Count > 0)
                {
                    foreach (string word in positive_words)
                    {
                        List<Chapter> temp = DoFindChapters(source, word, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);

                        result.Clear();
                        foreach (Chapter chapter in temp)
                        {
                            if (!result.Contains(chapter))
                            {
                                result.Add(chapter);
                            }
                        }

                        source = new List<Chapter>(result);
                    }
                }

                if (unsigned_words.Count > 0)
                {
                    foreach (string word in unsigned_words)
                    {
                        List<Chapter> temp = DoFindChapters(source, word, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);

                        result.Clear();
                        foreach (Chapter chapter in temp)
                        {
                            if (!result.Contains(chapter))
                            {
                                result.Add(chapter);
                            }
                        }

                        source = new List<Chapter>(result);
                    }
                }
            }
        }

        return result;
    }
    private static List<Chapter> DoFindChapters(List<Chapter> source, string texts, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Chapter> result = new List<Chapter>();

        if (!String.IsNullOrEmpty(texts))
        {
            if (s_book != null)
            {
                SortedDictionary<string, List<Word>> root_words_dictionary = s_book.RootWords;
                if (root_words_dictionary != null)
                {
                    List<Word> root_words = null;
                    if (root_words_dictionary.ContainsKey(texts))
                    {
                        // get all pre-identified root_words
                        root_words = root_words_dictionary[texts];
                    }
                    if (root_words != null)
                    {
                        result = GetChaptersWithRootWords(source, root_words, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
                    }
                    else // text is a text, not a real text
                    {
                        foreach (Verse verse in s_book.Verses)
                        {
                            foreach (Word word in verse.Words)
                            {
                                string best_root = s_book.GetBestRoot(texts);
                                return DoFindChapters(source, best_root, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
                            }
                        }

                        // must not come here !!!
                        throw new Exception(texts + " is not a text and no best text found!");
                    }
                }
            }
        }

        return result;
    }
    private static List<Chapter> GetChaptersWithRootWords(List<Chapter> source, List<Word> root_words, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Chapter> result = new List<Chapter>();

        if (source != null)
        {
            Dictionary<Chapter, int> multiplicity_dictionary = new Dictionary<Chapter, int>();
            foreach (Word word in root_words)
            {
                Chapter chapter = word.Verse.Chapter;
                if (multiplicity_dictionary.ContainsKey(chapter))
                {
                    multiplicity_dictionary[chapter]++;
                }
                else // first found
                {
                    multiplicity_dictionary.Add(chapter, 1);
                }
            }

            if (multiplicity == 0) // chapter contains no matches
            {
                foreach (Chapter chapter in source)
                {
                    if (!multiplicity_dictionary.ContainsKey(chapter))
                    {
                        if (!result.Contains(chapter))
                        {
                            result.Add(chapter);
                        }
                    }
                }
            }
            else // contains wildcard (-1) or exact multiplicity or number_type multiplicity
            {
                foreach (Word word in root_words)
                {
                    Chapter chapter = word.Verse.Chapter;
                    if (source.Contains(chapter))
                    {
                        if (multiplicity_dictionary.ContainsKey(chapter))
                        {
                            if (((multiplicity_number_type == NumberType.None) && (multiplicity == -1)) || (Compare(multiplicity_dictionary[chapter], multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder)))
                            {
                                if (!result.Contains(chapter))
                                {
                                    result.Add(chapter);
                                }
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
    private static List<Page> DoFindPages(string texts, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Page> result = new List<Page>();

        if (s_book != null)
        {
            List<Page> source = s_book.Pages;

            if (String.IsNullOrEmpty(texts)) return null;
            while (texts.Contains("  "))
            {
                texts = texts.Replace("  ", " ");
            }

            string[] parts = texts.Split();
            if (parts.Length > 0) // enable nested searches
            {
                List<string> negative_words = new List<string>();
                List<string> positive_words = new List<string>();
                List<string> unsigned_words = new List<string>();
                BuildWordLists(texts, out unsigned_words, out positive_words, out negative_words);

                if (negative_words.Count > 0)
                {
                    foreach (string word in negative_words)
                    {
                        List<Page> temp = DoFindPages(source, word, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);

                        result = new List<Page>(source);
                        foreach (Page page in temp)
                        {
                            result.Remove(page);
                        }

                        source = new List<Page>(result);
                    }
                }

                if (positive_words.Count > 0)
                {
                    foreach (string word in positive_words)
                    {
                        List<Page> temp = DoFindPages(source, word, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);

                        result.Clear();
                        foreach (Page page in temp)
                        {
                            if (!result.Contains(page))
                            {
                                result.Add(page);
                            }
                        }

                        source = new List<Page>(result);
                    }
                }

                if (unsigned_words.Count > 0)
                {
                    foreach (string word in unsigned_words)
                    {
                        List<Page> temp = DoFindPages(source, word, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);

                        result.Clear();
                        foreach (Page page in temp)
                        {
                            if (!result.Contains(page))
                            {
                                result.Add(page);
                            }
                        }

                        source = new List<Page>(result);
                    }
                }
            }
        }

        return result;
    }
    private static List<Page> DoFindPages(List<Page> source, string texts, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Page> result = new List<Page>();

        if (!String.IsNullOrEmpty(texts))
        {
            if (s_book != null)
            {
                SortedDictionary<string, List<Word>> root_words_dictionary = s_book.RootWords;
                if (root_words_dictionary != null)
                {
                    List<Word> root_words = null;
                    if (root_words_dictionary.ContainsKey(texts))
                    {
                        // get all pre-identified root_words
                        root_words = root_words_dictionary[texts];
                    }
                    if (root_words != null)
                    {
                        result = GetPagesWithRootWords(source, root_words, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
                    }
                    else // text is a text, not a real text
                    {
                        foreach (Verse verse in s_book.Verses)
                        {
                            foreach (Word word in verse.Words)
                            {
                                string best_root = s_book.GetBestRoot(texts);
                                return DoFindPages(source, best_root, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
                            }
                        }

                        // must not come here !!!
                        throw new Exception(texts + " is not a text and no best text found!");
                    }
                }
            }
        }

        return result;
    }
    private static List<Page> GetPagesWithRootWords(List<Page> source, List<Word> root_words, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Page> result = new List<Page>();

        if (source != null)
        {
            Dictionary<Page, int> multiplicity_dictionary = new Dictionary<Page, int>();
            foreach (Word word in root_words)
            {
                Page page = word.Verse.Page;
                if (multiplicity_dictionary.ContainsKey(page))
                {
                    multiplicity_dictionary[page]++;
                }
                else // first found
                {
                    multiplicity_dictionary.Add(page, 1);
                }
            }

            if (multiplicity == 0) // page contains no matches
            {
                foreach (Page page in source)
                {
                    if (!multiplicity_dictionary.ContainsKey(page))
                    {
                        if (!result.Contains(page))
                        {
                            result.Add(page);
                        }
                    }
                }
            }
            else // contains wildcard (-1) or exact multiplicity or number_type multiplicity
            {
                foreach (Word word in root_words)
                {
                    Page page = word.Verse.Page;
                    if (source.Contains(page))
                    {
                        if (multiplicity_dictionary.ContainsKey(page))
                        {
                            if (((multiplicity_number_type == NumberType.None) && (multiplicity == -1)) || (Compare(multiplicity_dictionary[page], multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder)))
                            {
                                if (!result.Contains(page))
                                {
                                    result.Add(page);
                                }
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
    private static List<Station> DoFindStations(string texts, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Station> result = new List<Station>();

        if (s_book != null)
        {
            List<Station> source = s_book.Stations;

            if (String.IsNullOrEmpty(texts)) return null;
            while (texts.Contains("  "))
            {
                texts = texts.Replace("  ", " ");
            }

            string[] parts = texts.Split();
            if (parts.Length > 0) // enable nested searches
            {
                List<string> negative_words = new List<string>();
                List<string> positive_words = new List<string>();
                List<string> unsigned_words = new List<string>();
                BuildWordLists(texts, out unsigned_words, out positive_words, out negative_words);

                if (negative_words.Count > 0)
                {
                    foreach (string word in negative_words)
                    {
                        List<Station> temp = DoFindStations(source, word, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);

                        result = new List<Station>(source);
                        foreach (Station station in temp)
                        {
                            result.Remove(station);
                        }

                        source = new List<Station>(result);
                    }
                }

                if (positive_words.Count > 0)
                {
                    foreach (string word in positive_words)
                    {
                        List<Station> temp = DoFindStations(source, word, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);

                        result.Clear();
                        foreach (Station station in temp)
                        {
                            if (!result.Contains(station))
                            {
                                result.Add(station);
                            }
                        }

                        source = new List<Station>(result);
                    }
                }

                if (unsigned_words.Count > 0)
                {
                    foreach (string word in unsigned_words)
                    {
                        List<Station> temp = DoFindStations(source, word, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);

                        result.Clear();
                        foreach (Station station in temp)
                        {
                            if (!result.Contains(station))
                            {
                                result.Add(station);
                            }
                        }

                        source = new List<Station>(result);
                    }
                }
            }
        }

        return result;
    }
    private static List<Station> DoFindStations(List<Station> source, string texts, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Station> result = new List<Station>();

        if (!String.IsNullOrEmpty(texts))
        {
            if (s_book != null)
            {
                SortedDictionary<string, List<Word>> root_words_dictionary = s_book.RootWords;
                if (root_words_dictionary != null)
                {
                    List<Word> root_words = null;
                    if (root_words_dictionary.ContainsKey(texts))
                    {
                        // get all pre-identified root_words
                        root_words = root_words_dictionary[texts];
                    }
                    if (root_words != null)
                    {
                        result = GetStationsWithRootWords(source, root_words, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
                    }
                    else // text is a text, not a real text
                    {
                        foreach (Verse verse in s_book.Verses)
                        {
                            foreach (Word word in verse.Words)
                            {
                                string best_root = s_book.GetBestRoot(texts);
                                return DoFindStations(source, best_root, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
                            }
                        }

                        // must not come here !!!
                        throw new Exception(texts + " is not a text and no best text found!");
                    }
                }
            }
        }

        return result;
    }
    private static List<Station> GetStationsWithRootWords(List<Station> source, List<Word> root_words, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Station> result = new List<Station>();

        if (source != null)
        {
            Dictionary<Station, int> multiplicity_dictionary = new Dictionary<Station, int>();
            foreach (Word word in root_words)
            {
                Station station = word.Verse.Station;
                if (multiplicity_dictionary.ContainsKey(station))
                {
                    multiplicity_dictionary[station]++;
                }
                else // first found
                {
                    multiplicity_dictionary.Add(station, 1);
                }
            }

            if (multiplicity == 0) // station contains no matches
            {
                foreach (Station station in source)
                {
                    if (!multiplicity_dictionary.ContainsKey(station))
                    {
                        if (!result.Contains(station))
                        {
                            result.Add(station);
                        }
                    }
                }
            }
            else // contains wildcard (-1) or exact multiplicity or number_type multiplicity
            {
                foreach (Word word in root_words)
                {
                    Station station = word.Verse.Station;
                    if (source.Contains(station))
                    {
                        if (multiplicity_dictionary.ContainsKey(station))
                        {
                            if (((multiplicity_number_type == NumberType.None) && (multiplicity == -1)) || (Compare(multiplicity_dictionary[station], multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder)))
                            {
                                if (!result.Contains(station))
                                {
                                    result.Add(station);
                                }
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
    private static List<Part> DoFindParts(string texts, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Part> result = new List<Part>();

        if (s_book != null)
        {
            List<Part> source = s_book.Parts;

            if (String.IsNullOrEmpty(texts)) return null;
            while (texts.Contains("  "))
            {
                texts = texts.Replace("  ", " ");
            }

            string[] parts = texts.Split();
            if (parts.Length > 0) // enable nested searches
            {
                List<string> negative_words = new List<string>();
                List<string> positive_words = new List<string>();
                List<string> unsigned_words = new List<string>();
                BuildWordLists(texts, out unsigned_words, out positive_words, out negative_words);

                if (negative_words.Count > 0)
                {
                    foreach (string word in negative_words)
                    {
                        List<Part> temp = DoFindParts(source, word, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);

                        result = new List<Part>(source);
                        foreach (Part part in temp)
                        {
                            result.Remove(part);
                        }

                        source = new List<Part>(result);
                    }
                }

                if (positive_words.Count > 0)
                {
                    foreach (string word in positive_words)
                    {
                        List<Part> temp = DoFindParts(source, word, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);

                        result.Clear();
                        foreach (Part part in temp)
                        {
                            if (!result.Contains(part))
                            {
                                result.Add(part);
                            }
                        }

                        source = new List<Part>(result);
                    }
                }

                if (unsigned_words.Count > 0)
                {
                    foreach (string word in unsigned_words)
                    {
                        List<Part> temp = DoFindParts(source, word, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);

                        result.Clear();
                        foreach (Part part in temp)
                        {
                            if (!result.Contains(part))
                            {
                                result.Add(part);
                            }
                        }

                        source = new List<Part>(result);
                    }
                }
            }
        }

        return result;
    }
    private static List<Part> DoFindParts(List<Part> source, string texts, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Part> result = new List<Part>();

        if (!String.IsNullOrEmpty(texts))
        {
            if (s_book != null)
            {
                SortedDictionary<string, List<Word>> root_words_dictionary = s_book.RootWords;
                if (root_words_dictionary != null)
                {
                    List<Word> root_words = null;
                    if (root_words_dictionary.ContainsKey(texts))
                    {
                        // get all pre-identified root_words
                        root_words = root_words_dictionary[texts];
                    }
                    if (root_words != null)
                    {
                        result = GetPartsWithRootWords(source, root_words, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
                    }
                    else // text is a text, not a real text
                    {
                        foreach (Verse verse in s_book.Verses)
                        {
                            foreach (Word word in verse.Words)
                            {
                                string best_root = s_book.GetBestRoot(texts);
                                return DoFindParts(source, best_root, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
                            }
                        }

                        // must not come here !!!
                        throw new Exception(texts + " is not a text and no best text found!");
                    }
                }
            }
        }

        return result;
    }
    private static List<Part> GetPartsWithRootWords(List<Part> source, List<Word> root_words, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Part> result = new List<Part>();

        if (source != null)
        {
            Dictionary<Part, int> multiplicity_dictionary = new Dictionary<Part, int>();
            foreach (Word word in root_words)
            {
                Part part = word.Verse.Part;
                if (multiplicity_dictionary.ContainsKey(part))
                {
                    multiplicity_dictionary[part]++;
                }
                else // first found
                {
                    multiplicity_dictionary.Add(part, 1);
                }
            }

            if (multiplicity == 0) // part contains no matches
            {
                foreach (Part part in source)
                {
                    if (!multiplicity_dictionary.ContainsKey(part))
                    {
                        if (!result.Contains(part))
                        {
                            result.Add(part);
                        }
                    }
                }
            }
            else // contains wildcard (-1) or exact multiplicity or number_type multiplicity
            {
                foreach (Word word in root_words)
                {
                    Part part = word.Verse.Part;
                    if (source.Contains(part))
                    {
                        if (multiplicity_dictionary.ContainsKey(part))
                        {
                            if (((multiplicity_number_type == NumberType.None) && (multiplicity == -1)) || (Compare(multiplicity_dictionary[part], multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder)))
                            {
                                if (!result.Contains(part))
                                {
                                    result.Add(part);
                                }
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
    private static List<Model.Group> DoFindGroups(string texts, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Model.Group> result = new List<Model.Group>();

        if (s_book != null)
        {
            List<Model.Group> source = s_book.Groups;

            if (String.IsNullOrEmpty(texts)) return null;
            while (texts.Contains("  "))
            {
                texts = texts.Replace("  ", " ");
            }

            string[] parts = texts.Split();
            if (parts.Length > 0) // enable nested searches
            {
                List<string> negative_words = new List<string>();
                List<string> positive_words = new List<string>();
                List<string> unsigned_words = new List<string>();
                BuildWordLists(texts, out unsigned_words, out positive_words, out negative_words);

                if (negative_words.Count > 0)
                {
                    foreach (string word in negative_words)
                    {
                        List<Model.Group> temp = DoFindGroups(source, word, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);

                        result = new List<Model.Group>(source);
                        foreach (Model.Group group in temp)
                        {
                            result.Remove(group);
                        }

                        source = new List<Model.Group>(result);
                    }
                }

                if (positive_words.Count > 0)
                {
                    foreach (string word in positive_words)
                    {
                        List<Model.Group> temp = DoFindGroups(source, word, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);

                        result.Clear();
                        foreach (Model.Group group in temp)
                        {
                            if (!result.Contains(group))
                            {
                                result.Add(group);
                            }
                        }

                        source = new List<Model.Group>(result);
                    }
                }

                if (unsigned_words.Count > 0)
                {
                    foreach (string word in unsigned_words)
                    {
                        List<Model.Group> temp = DoFindGroups(source, word, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);

                        result.Clear();
                        foreach (Model.Group group in temp)
                        {
                            if (!result.Contains(group))
                            {
                                result.Add(group);
                            }
                        }

                        source = new List<Model.Group>(result);
                    }
                }
            }
        }

        return result;
    }
    private static List<Model.Group> DoFindGroups(List<Model.Group> source, string texts, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Model.Group> result = new List<Model.Group>();

        if (!String.IsNullOrEmpty(texts))
        {
            if (s_book != null)
            {
                SortedDictionary<string, List<Word>> root_words_dictionary = s_book.RootWords;
                if (root_words_dictionary != null)
                {
                    List<Word> root_words = null;
                    if (root_words_dictionary.ContainsKey(texts))
                    {
                        // get all pre-identified root_words
                        root_words = root_words_dictionary[texts];
                    }
                    if (root_words != null)
                    {
                        result = GetGroupsWithRootWords(source, root_words, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
                    }
                    else // text is a text, not a real text
                    {
                        foreach (Verse verse in s_book.Verses)
                        {
                            foreach (Word word in verse.Words)
                            {
                                string best_root = s_book.GetBestRoot(texts);
                                return DoFindGroups(source, best_root, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
                            }
                        }

                        // must not come here !!!
                        throw new Exception(texts + " is not a text and no best text found!");
                    }
                }
            }
        }

        return result;
    }
    private static List<Model.Group> GetGroupsWithRootWords(List<Model.Group> source, List<Word> root_words, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Model.Group> result = new List<Model.Group>();

        if (source != null)
        {
            Dictionary<Model.Group, int> multiplicity_dictionary = new Dictionary<Model.Group, int>();
            foreach (Word word in root_words)
            {
                Model.Group group = word.Verse.Group;
                if (multiplicity_dictionary.ContainsKey(group))
                {
                    multiplicity_dictionary[group]++;
                }
                else // first found
                {
                    multiplicity_dictionary.Add(group, 1);
                }
            }

            if (multiplicity == 0) // group contains no matches
            {
                foreach (Model.Group group in source)
                {
                    if (!multiplicity_dictionary.ContainsKey(group))
                    {
                        if (!result.Contains(group))
                        {
                            result.Add(group);
                        }
                    }
                }
            }
            else // contains wildcard (-1) or exact multiplicity or number_type multiplicity
            {
                foreach (Word word in root_words)
                {
                    Model.Group group = word.Verse.Group;
                    if (source.Contains(group))
                    {
                        if (multiplicity_dictionary.ContainsKey(group))
                        {
                            if (((multiplicity_number_type == NumberType.None) && (multiplicity == -1)) || (Compare(multiplicity_dictionary[group], multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder)))
                            {
                                if (!result.Contains(group))
                                {
                                    result.Add(group);
                                }
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
    private static List<Half> DoFindHalfs(string texts, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Half> result = new List<Half>();

        if (s_book != null)
        {
            List<Half> source = s_book.Halfs;

            if (String.IsNullOrEmpty(texts)) return null;
            while (texts.Contains("  "))
            {
                texts = texts.Replace("  ", " ");
            }

            string[] parts = texts.Split();
            if (parts.Length > 0) // enable nested searches
            {
                List<string> negative_words = new List<string>();
                List<string> positive_words = new List<string>();
                List<string> unsigned_words = new List<string>();
                BuildWordLists(texts, out unsigned_words, out positive_words, out negative_words);

                if (negative_words.Count > 0)
                {
                    foreach (string word in negative_words)
                    {
                        List<Half> temp = DoFindHalfs(source, word, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);

                        result = new List<Half>(source);
                        foreach (Half half in temp)
                        {
                            result.Remove(half);
                        }

                        source = new List<Half>(result);
                    }
                }

                if (positive_words.Count > 0)
                {
                    foreach (string word in positive_words)
                    {
                        List<Half> temp = DoFindHalfs(source, word, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);

                        result.Clear();
                        foreach (Half half in temp)
                        {
                            if (!result.Contains(half))
                            {
                                result.Add(half);
                            }
                        }

                        source = new List<Half>(result);
                    }
                }

                if (unsigned_words.Count > 0)
                {
                    foreach (string word in unsigned_words)
                    {
                        List<Half> temp = DoFindHalfs(source, word, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);

                        result.Clear();
                        foreach (Half half in temp)
                        {
                            if (!result.Contains(half))
                            {
                                result.Add(half);
                            }
                        }

                        source = new List<Half>(result);
                    }
                }
            }
        }

        return result;
    }
    private static List<Half> DoFindHalfs(List<Half> source, string texts, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Half> result = new List<Half>();

        if (!String.IsNullOrEmpty(texts))
        {
            if (s_book != null)
            {
                SortedDictionary<string, List<Word>> root_words_dictionary = s_book.RootWords;
                if (root_words_dictionary != null)
                {
                    List<Word> root_words = null;
                    if (root_words_dictionary.ContainsKey(texts))
                    {
                        // get all pre-identified root_words
                        root_words = root_words_dictionary[texts];
                    }
                    if (root_words != null)
                    {
                        result = GetHalfsWithRootWords(source, root_words, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
                    }
                    else // text is a text, not a real text
                    {
                        foreach (Verse verse in s_book.Verses)
                        {
                            foreach (Word word in verse.Words)
                            {
                                string best_root = s_book.GetBestRoot(texts);
                                return DoFindHalfs(source, best_root, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
                            }
                        }

                        // must not come here !!!
                        throw new Exception(texts + " is not a text and no best text found!");
                    }
                }
            }
        }

        return result;
    }
    private static List<Half> GetHalfsWithRootWords(List<Half> source, List<Word> root_words, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Half> result = new List<Half>();

        if (source != null)
        {
            Dictionary<Half, int> multiplicity_dictionary = new Dictionary<Half, int>();
            foreach (Word word in root_words)
            {
                Half half = word.Verse.Half;
                if (multiplicity_dictionary.ContainsKey(half))
                {
                    multiplicity_dictionary[half]++;
                }
                else // first found
                {
                    multiplicity_dictionary.Add(half, 1);
                }
            }

            if (multiplicity == 0) // half contains no matches
            {
                foreach (Half half in source)
                {
                    if (!multiplicity_dictionary.ContainsKey(half))
                    {
                        if (!result.Contains(half))
                        {
                            result.Add(half);
                        }
                    }
                }
            }
            else // contains wildcard (-1) or exact multiplicity or number_type multiplicity
            {
                foreach (Word word in root_words)
                {
                    Half half = word.Verse.Half;
                    if (source.Contains(half))
                    {
                        if (multiplicity_dictionary.ContainsKey(half))
                        {
                            if (((multiplicity_number_type == NumberType.None) && (multiplicity == -1)) || (Compare(multiplicity_dictionary[half], multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder)))
                            {
                                if (!result.Contains(half))
                                {
                                    result.Add(half);
                                }
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
    private static List<Quarter> DoFindQuarters(string texts, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Quarter> result = new List<Quarter>();

        if (s_book != null)
        {
            List<Quarter> source = s_book.Quarters;

            if (String.IsNullOrEmpty(texts)) return null;
            while (texts.Contains("  "))
            {
                texts = texts.Replace("  ", " ");
            }

            string[] parts = texts.Split();
            if (parts.Length > 0) // enable nested searches
            {
                List<string> negative_words = new List<string>();
                List<string> positive_words = new List<string>();
                List<string> unsigned_words = new List<string>();
                BuildWordLists(texts, out unsigned_words, out positive_words, out negative_words);

                if (negative_words.Count > 0)
                {
                    foreach (string word in negative_words)
                    {
                        List<Quarter> temp = DoFindQuarters(source, word, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);

                        result = new List<Quarter>(source);
                        foreach (Quarter quarter in temp)
                        {
                            result.Remove(quarter);
                        }

                        source = new List<Quarter>(result);
                    }
                }

                if (positive_words.Count > 0)
                {
                    foreach (string word in positive_words)
                    {
                        List<Quarter> temp = DoFindQuarters(source, word, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);

                        result.Clear();
                        foreach (Quarter quarter in temp)
                        {
                            if (!result.Contains(quarter))
                            {
                                result.Add(quarter);
                            }
                        }

                        source = new List<Quarter>(result);
                    }
                }

                if (unsigned_words.Count > 0)
                {
                    foreach (string word in unsigned_words)
                    {
                        List<Quarter> temp = DoFindQuarters(source, word, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);

                        result.Clear();
                        foreach (Quarter quarter in temp)
                        {
                            if (!result.Contains(quarter))
                            {
                                result.Add(quarter);
                            }
                        }

                        source = new List<Quarter>(result);
                    }
                }
            }
        }

        return result;
    }
    private static List<Quarter> DoFindQuarters(List<Quarter> source, string texts, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Quarter> result = new List<Quarter>();

        if (!String.IsNullOrEmpty(texts))
        {
            if (s_book != null)
            {
                SortedDictionary<string, List<Word>> root_words_dictionary = s_book.RootWords;
                if (root_words_dictionary != null)
                {
                    List<Word> root_words = null;
                    if (root_words_dictionary.ContainsKey(texts))
                    {
                        // get all pre-identified root_words
                        root_words = root_words_dictionary[texts];
                    }
                    if (root_words != null)
                    {
                        result = GetQuartersWithRootWords(source, root_words, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
                    }
                    else // text is a text, not a real text
                    {
                        foreach (Verse verse in s_book.Verses)
                        {
                            foreach (Word word in verse.Words)
                            {
                                string best_root = s_book.GetBestRoot(texts);
                                return DoFindQuarters(source, best_root, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
                            }
                        }

                        // must not come here !!!
                        throw new Exception(texts + " is not a text and no best text found!");
                    }
                }
            }
        }

        return result;
    }
    private static List<Quarter> GetQuartersWithRootWords(List<Quarter> source, List<Word> root_words, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Quarter> result = new List<Quarter>();

        if (source != null)
        {
            Dictionary<Quarter, int> multiplicity_dictionary = new Dictionary<Quarter, int>();
            foreach (Word word in root_words)
            {
                Quarter quarter = word.Verse.Quarter;
                if (multiplicity_dictionary.ContainsKey(quarter))
                {
                    multiplicity_dictionary[quarter]++;
                }
                else // first found
                {
                    multiplicity_dictionary.Add(quarter, 1);
                }
            }

            if (multiplicity == 0) // quarter contains no matches
            {
                foreach (Quarter quarter in source)
                {
                    if (!multiplicity_dictionary.ContainsKey(quarter))
                    {
                        if (!result.Contains(quarter))
                        {
                            result.Add(quarter);
                        }
                    }
                }
            }
            else // contains wildcard (-1) or exact multiplicity or number_type multiplicity
            {
                foreach (Word word in root_words)
                {
                    Quarter quarter = word.Verse.Quarter;
                    if (source.Contains(quarter))
                    {
                        if (multiplicity_dictionary.ContainsKey(quarter))
                        {
                            if (((multiplicity_number_type == NumberType.None) && (multiplicity == -1)) || (Compare(multiplicity_dictionary[quarter], multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder)))
                            {
                                if (!result.Contains(quarter))
                                {
                                    result.Add(quarter);
                                }
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
    private static List<Bowing> DoFindBowings(string texts, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Bowing> result = new List<Bowing>();

        if (s_book != null)
        {
            List<Bowing> source = s_book.Bowings;

            if (String.IsNullOrEmpty(texts)) return null;
            while (texts.Contains("  "))
            {
                texts = texts.Replace("  ", " ");
            }

            string[] parts = texts.Split();
            if (parts.Length > 0) // enable nested searches
            {
                List<string> negative_words = new List<string>();
                List<string> positive_words = new List<string>();
                List<string> unsigned_words = new List<string>();
                BuildWordLists(texts, out unsigned_words, out positive_words, out negative_words);

                if (negative_words.Count > 0)
                {
                    foreach (string word in negative_words)
                    {
                        List<Bowing> temp = DoFindBowings(source, word, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);

                        result = new List<Bowing>(source);
                        foreach (Bowing bowing in temp)
                        {
                            result.Remove(bowing);
                        }

                        source = new List<Bowing>(result);
                    }
                }

                if (positive_words.Count > 0)
                {
                    foreach (string word in positive_words)
                    {
                        List<Bowing> temp = DoFindBowings(source, word, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);

                        result.Clear();
                        foreach (Bowing bowing in temp)
                        {
                            if (!result.Contains(bowing))
                            {
                                result.Add(bowing);
                            }
                        }

                        source = new List<Bowing>(result);
                    }
                }

                if (unsigned_words.Count > 0)
                {
                    foreach (string word in unsigned_words)
                    {
                        List<Bowing> temp = DoFindBowings(source, word, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);

                        result.Clear();
                        foreach (Bowing bowing in temp)
                        {
                            if (!result.Contains(bowing))
                            {
                                result.Add(bowing);
                            }
                        }

                        source = new List<Bowing>(result);
                    }
                }
            }
        }

        return result;
    }
    private static List<Bowing> DoFindBowings(List<Bowing> source, string texts, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Bowing> result = new List<Bowing>();

        if (!String.IsNullOrEmpty(texts))
        {
            if (s_book != null)
            {
                SortedDictionary<string, List<Word>> root_words_dictionary = s_book.RootWords;
                if (root_words_dictionary != null)
                {
                    List<Word> root_words = null;
                    if (root_words_dictionary.ContainsKey(texts))
                    {
                        // get all pre-identified root_words
                        root_words = root_words_dictionary[texts];
                    }
                    if (root_words != null)
                    {
                        result = GetBowingsWithRootWords(source, root_words, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
                    }
                    else // text is a text, not a real text
                    {
                        foreach (Verse verse in s_book.Verses)
                        {
                            foreach (Word word in verse.Words)
                            {
                                string best_root = s_book.GetBestRoot(texts);
                                return DoFindBowings(source, best_root, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
                            }
                        }

                        // must not come here !!!
                        throw new Exception(texts + " is not a text and no best text found!");
                    }
                }
            }
        }

        return result;
    }
    private static List<Bowing> GetBowingsWithRootWords(List<Bowing> source, List<Word> root_words, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Bowing> result = new List<Bowing>();

        if (source != null)
        {
            Dictionary<Bowing, int> multiplicity_dictionary = new Dictionary<Bowing, int>();
            foreach (Word word in root_words)
            {
                Bowing bowing = word.Verse.Bowing;
                if (multiplicity_dictionary.ContainsKey(bowing))
                {
                    multiplicity_dictionary[bowing]++;
                }
                else // first found
                {
                    multiplicity_dictionary.Add(bowing, 1);
                }
            }

            if (multiplicity == 0) // bowing contains no matches
            {
                foreach (Bowing bowing in source)
                {
                    if (!multiplicity_dictionary.ContainsKey(bowing))
                    {
                        if (!result.Contains(bowing))
                        {
                            result.Add(bowing);
                        }
                    }
                }
            }
            else // contains wildcard (-1) or exact multiplicity or number_type multiplicity
            {
                foreach (Word word in root_words)
                {
                    Bowing bowing = word.Verse.Bowing;
                    if (source.Contains(bowing))
                    {
                        if (multiplicity_dictionary.ContainsKey(bowing))
                        {
                            if (((multiplicity_number_type == NumberType.None) && (multiplicity == -1)) || (Compare(multiplicity_dictionary[bowing], multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder)))
                            {
                                if (!result.Contains(bowing))
                                {
                                    result.Add(bowing);
                                }
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
    // find by text - Related words
    public static List<Verse> FindRelatedVerses(SearchScope search_scope, Selection current_selection, List<Verse> previous_result, Verse verse)
    {
        return DoFindRelatedVerses(search_scope, current_selection, previous_result, verse);
    }
    private static List<Verse> DoFindRelatedVerses(SearchScope search_scope, Selection current_selection, List<Verse> previous_result, Verse verse)
    {
        List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_result, TextLocationInChapter.Any);
        return DoFindRelatedVerses(source, current_selection, previous_result, verse);
    }
    private static List<Verse> DoFindRelatedVerses(List<Verse> source, Selection current_selection, List<Verse> previous_result, Verse verse)
    {
        List<Verse> result = new List<Verse>();

        if (source != null)
        {
            if (source.Count > 0)
            {
                if (verse != null)
                {
                    for (int j = 0; j < source.Count; j++)
                    {
                        if (verse.HasRelatedWordsTo(source[j]))
                        {
                            result.Add(source[j]);
                        }
                    }
                }
            }
        }

        return result;
    }
    // find by text - Repetition
    public static List<Word> FindConsecutivelyRepeatedWords(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder, bool with_diacritics)
    {
        List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_verses, TextLocationInChapter.Any);
        return DoFindConsecutivelyRepeatedWords(source, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder, with_diacritics);
    }
    public static List<Word> FindConsecutivelyRepeatedRoots(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_verses, TextLocationInChapter.Any);
        return DoFindConsecutivelyRepeatedRoots(source, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
    }
    private static List<Word> DoFindConsecutivelyRepeatedWords(List<Verse> source, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder, bool with_diacritics)
    {
        List<Word> result = new List<Word>();

        if (source != null)
        {
            if (source.Count > 0)
            {
                if (multiplicity > 0)
                {
                    List<Word> words = new List<Word>();
                    foreach (Verse verse in source)
                    {
                        words.AddRange(verse.Words);
                    }

                    for (int i = 0; i < words.Count - 2 * multiplicity; i++)
                    {
                        bool found = true;
                        for (int j = 0; j < multiplicity; j++)
                        {
                            string word_text_i = words[i + j].Text;
                            string word_text_j = words[i + j + multiplicity].Text;

                            if (!with_diacritics)
                            {
                                if ((!with_diacritics) && (s_numerology_system.TextMode == "Original"))
                                {
                                    word_text_i = word_text_i.Simplify29().Trim();
                                    word_text_j = word_text_j.Simplify29().Trim();
                                }
                            }
                            if (word_text_i != word_text_j)
                            {
                                found = false;
                                break;
                            }
                        }
                        if (found)
                        {
                            for (int j = 0; j < 2 * multiplicity; j++)
                            {
                                result.Add(words[i + j]);
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
    private static List<Word> DoFindConsecutivelyRepeatedRoots(List<Verse> source, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        List<Word> result = new List<Word>();

        if (source != null)
        {
            if (source.Count > 0)
            {
                if (multiplicity > 0)
                {
                    List<Word> words = new List<Word>();
                    foreach (Verse verse in source)
                    {
                        words.AddRange(verse.Words);
                    }

                    for (int i = 0; i < words.Count - 2 * multiplicity; i++)
                    {
                        bool found = true;
                        for (int j = 0; j < multiplicity; j++)
                        {
                            string word_text_i = words[i + j].Root;
                            string word_text_j = words[i + j + multiplicity].Root;

                            if (word_text_i != word_text_j)
                            {
                                found = false;
                                break;
                            }
                        }
                        if (found)
                        {
                            for (int j = 0; j < 2 * multiplicity; j++)
                            {
                                result.Add(words[i + j]);
                            }
                        }
                    }
                }
            }
        }

        return result;
    }


    // find by numbers - helper methods
    private static void CalculateSums(Word word, out int letter_sum)
    {
        letter_sum = 0;
        if (word != null)
        {
            if ((word.Letters != null) && (word.Letters.Count > 0))
            {
                foreach (Letter letter in word.Letters)
                {
                    letter_sum += letter.NumberInWord;
                }
            }
        }
    }
    private static void CalculateSums(List<Word> words, out int word_sum, out int letter_sum)
    {
        word_sum = 0;
        letter_sum = 0;
        if (words != null)
        {
            foreach (Word word in words)
            {
                word_sum += word.NumberInVerse;

                if ((word.Letters != null) && (word.Letters.Count > 0))
                {
                    foreach (Letter letter in word.Letters)
                    {
                        letter_sum += letter.NumberInWord;
                    }
                }
            }
        }
    }
    private static void CalculateSums(Verse verse, out int word_sum, out int letter_sum)
    {
        word_sum = 0;
        letter_sum = 0;
        if (verse != null)
        {
            if (verse.Words != null)
            {
                foreach (Word word in verse.Words)
                {
                    word_sum += word.NumberInVerse;

                    if ((word.Letters != null) && (word.Letters.Count > 0))
                    {
                        foreach (Letter letter in word.Letters)
                        {
                            letter_sum += letter.NumberInWord;
                        }
                    }
                }
            }
        }
    }
    private static void CalculateSums(List<Verse> verses, out int chapter_sum, out int verse_sum, out int word_sum, out int letter_sum)
    {
        chapter_sum = 0;
        verse_sum = 0;
        word_sum = 0;
        letter_sum = 0;
        if (verses != null)
        {
            if (s_book != null)
            {
                List<Chapter> chapters = s_book.GetChapters(verses);
                if (chapters != null)
                {
                    foreach (Chapter chapter in chapters)
                    {
                        if (chapter != null)
                        {
                            chapter_sum += chapter.Number;
                        }
                    }

                    foreach (Verse verse in verses)
                    {
                        verse_sum += verse.NumberInChapter;
                        if (verse.Words != null)
                        {
                            foreach (Word word in verse.Words)
                            {
                                word_sum += word.NumberInVerse;

                                if ((word.Letters != null) && (word.Letters.Count > 0))
                                {
                                    foreach (Letter letter in word.Letters)
                                    {
                                        letter_sum += letter.NumberInWord;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    private static void CalculateSums(Chapter chapter, out int verse_sum, out int word_sum, out int letter_sum)
    {
        verse_sum = 0;
        word_sum = 0;
        letter_sum = 0;
        if (chapter != null)
        {
            foreach (Verse verse in chapter.Verses)
            {
                verse_sum += verse.NumberInChapter;
                if (verse.Words != null)
                {
                    foreach (Word word in verse.Words)
                    {
                        word_sum += word.NumberInVerse;

                        if ((word.Letters != null) && (word.Letters.Count > 0))
                        {
                            foreach (Letter letter in word.Letters)
                            {
                                letter_sum += letter.NumberInWord;
                            }
                        }
                    }
                }
            }
        }
    }
    private static void CalculateSums(Page page, out int verse_sum, out int word_sum, out int letter_sum)
    {
        verse_sum = 0;
        word_sum = 0;
        letter_sum = 0;
        if (page != null)
        {
            foreach (Verse verse in page.Verses)
            {
                verse_sum += verse.NumberInChapter;
                if (verse.Words != null)
                {
                    foreach (Word word in verse.Words)
                    {
                        word_sum += word.NumberInVerse;

                        if ((word.Letters != null) && (word.Letters.Count > 0))
                        {
                            foreach (Letter letter in word.Letters)
                            {
                                letter_sum += letter.NumberInWord;
                            }
                        }
                    }
                }
            }
        }
    }
    private static void CalculateSums(Station station, out int verse_sum, out int word_sum, out int letter_sum)
    {
        verse_sum = 0;
        word_sum = 0;
        letter_sum = 0;
        if (station != null)
        {
            foreach (Verse verse in station.Verses)
            {
                verse_sum += verse.NumberInChapter;
                if (verse.Words != null)
                {
                    foreach (Word word in verse.Words)
                    {
                        word_sum += word.NumberInVerse;

                        if ((word.Letters != null) && (word.Letters.Count > 0))
                        {
                            foreach (Letter letter in word.Letters)
                            {
                                letter_sum += letter.NumberInWord;
                            }
                        }
                    }
                }
            }
        }
    }
    private static void CalculateSums(Part part, out int verse_sum, out int word_sum, out int letter_sum)
    {
        verse_sum = 0;
        word_sum = 0;
        letter_sum = 0;
        if (part != null)
        {
            foreach (Verse verse in part.Verses)
            {
                verse_sum += verse.NumberInChapter;
                if (verse.Words != null)
                {
                    foreach (Word word in verse.Words)
                    {
                        word_sum += word.NumberInVerse;

                        if ((word.Letters != null) && (word.Letters.Count > 0))
                        {
                            foreach (Letter letter in word.Letters)
                            {
                                letter_sum += letter.NumberInWord;
                            }
                        }
                    }
                }
            }
        }
    }
    private static void CalculateSums(Model.Group group, out int verse_sum, out int word_sum, out int letter_sum)
    {
        verse_sum = 0;
        word_sum = 0;
        letter_sum = 0;
        if (group != null)
        {
            foreach (Verse verse in group.Verses)
            {
                verse_sum += verse.NumberInChapter;
                if (verse.Words != null)
                {
                    foreach (Word word in verse.Words)
                    {
                        word_sum += word.NumberInVerse;

                        if ((word.Letters != null) && (word.Letters.Count > 0))
                        {
                            foreach (Letter letter in word.Letters)
                            {
                                letter_sum += letter.NumberInWord;
                            }
                        }
                    }
                }
            }
        }
    }
    private static void CalculateSums(Half half, out int verse_sum, out int word_sum, out int letter_sum)
    {
        verse_sum = 0;
        word_sum = 0;
        letter_sum = 0;
        if (half != null)
        {
            foreach (Verse verse in half.Verses)
            {
                verse_sum += verse.NumberInChapter;
                if (verse.Words != null)
                {
                    foreach (Word word in verse.Words)
                    {
                        word_sum += word.NumberInVerse;

                        if ((word.Letters != null) && (word.Letters.Count > 0))
                        {
                            foreach (Letter letter in word.Letters)
                            {
                                letter_sum += letter.NumberInWord;
                            }
                        }
                    }
                }
            }
        }
    }
    private static void CalculateSums(Quarter quarter, out int verse_sum, out int word_sum, out int letter_sum)
    {
        verse_sum = 0;
        word_sum = 0;
        letter_sum = 0;
        if (quarter != null)
        {
            foreach (Verse verse in quarter.Verses)
            {
                verse_sum += verse.NumberInChapter;
                if (verse.Words != null)
                {
                    foreach (Word word in verse.Words)
                    {
                        word_sum += word.NumberInVerse;

                        if ((word.Letters != null) && (word.Letters.Count > 0))
                        {
                            foreach (Letter letter in word.Letters)
                            {
                                letter_sum += letter.NumberInWord;
                            }
                        }
                    }
                }
            }
        }
    }
    private static void CalculateSums(Bowing bowing, out int verse_sum, out int word_sum, out int letter_sum)
    {
        verse_sum = 0;
        word_sum = 0;
        letter_sum = 0;
        if (bowing != null)
        {
            foreach (Verse verse in bowing.Verses)
            {
                verse_sum += verse.NumberInChapter;
                if (verse.Words != null)
                {
                    foreach (Word word in verse.Words)
                    {
                        word_sum += word.NumberInVerse;

                        if ((word.Letters != null) && (word.Letters.Count > 0))
                        {
                            foreach (Letter letter in word.Letters)
                            {
                                letter_sum += letter.NumberInWord;
                            }
                        }
                    }
                }
            }
        }
    }
    private static void CalculateSums(List<Chapter> chapters, out int chapter_sum, out int verse_sum, out int word_sum, out int letter_sum)
    {
        chapter_sum = 0;
        verse_sum = 0;
        word_sum = 0;
        letter_sum = 0;
        if (chapters != null)
        {
            foreach (Chapter chapter in chapters)
            {
                if (chapter != null)
                {
                    chapter_sum += chapter.Number;

                    foreach (Verse verse in chapter.Verses)
                    {
                        verse_sum += verse.NumberInChapter;
                        if (verse.Words != null)
                        {
                            foreach (Word word in verse.Words)
                            {
                                word_sum += word.NumberInVerse;

                                if ((word.Letters != null) && (word.Letters.Count > 0))
                                {
                                    foreach (Letter letter in word.Letters)
                                    {
                                        letter_sum += letter.NumberInWord;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    private static void CalculateSums(List<Page> pages, out int page_sum, out int verse_sum, out int word_sum, out int letter_sum)
    {
        page_sum = 0;
        verse_sum = 0;
        word_sum = 0;
        letter_sum = 0;
        if (pages != null)
        {
            foreach (Page page in pages)
            {
                if (page != null)
                {
                    page_sum += page.Number;

                    foreach (Verse verse in page.Verses)
                    {
                        verse_sum += verse.NumberInChapter;
                        if (verse.Words != null)
                        {
                            foreach (Word word in verse.Words)
                            {
                                word_sum += word.NumberInVerse;

                                if ((word.Letters != null) && (word.Letters.Count > 0))
                                {
                                    foreach (Letter letter in word.Letters)
                                    {
                                        letter_sum += letter.NumberInWord;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    private static void CalculateSums(List<Station> stations, out int station_sum, out int verse_sum, out int word_sum, out int letter_sum)
    {
        station_sum = 0;
        verse_sum = 0;
        word_sum = 0;
        letter_sum = 0;
        if (stations != null)
        {
            foreach (Station station in stations)
            {
                if (station != null)
                {
                    station_sum += station.Number;

                    foreach (Verse verse in station.Verses)
                    {
                        verse_sum += verse.NumberInChapter;
                        if (verse.Words != null)
                        {
                            foreach (Word word in verse.Words)
                            {
                                word_sum += word.NumberInVerse;

                                if ((word.Letters != null) && (word.Letters.Count > 0))
                                {
                                    foreach (Letter letter in word.Letters)
                                    {
                                        letter_sum += letter.NumberInWord;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    private static void CalculateSums(List<Part> parts, out int part_sum, out int verse_sum, out int word_sum, out int letter_sum)
    {
        part_sum = 0;
        verse_sum = 0;
        word_sum = 0;
        letter_sum = 0;
        if (parts != null)
        {
            foreach (Part part in parts)
            {
                if (part != null)
                {
                    part_sum += part.Number;

                    foreach (Verse verse in part.Verses)
                    {
                        verse_sum += verse.NumberInChapter;
                        if (verse.Words != null)
                        {
                            foreach (Word word in verse.Words)
                            {
                                word_sum += word.NumberInVerse;

                                if ((word.Letters != null) && (word.Letters.Count > 0))
                                {
                                    foreach (Letter letter in word.Letters)
                                    {
                                        letter_sum += letter.NumberInWord;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    private static void CalculateSums(List<Model.Group> groups, out int group_sum, out int verse_sum, out int word_sum, out int letter_sum)
    {
        group_sum = 0;
        verse_sum = 0;
        word_sum = 0;
        letter_sum = 0;
        if (groups != null)
        {
            foreach (Model.Group group in groups)
            {
                if (group != null)
                {
                    group_sum += group.Number;

                    foreach (Verse verse in group.Verses)
                    {
                        verse_sum += verse.NumberInChapter;
                        if (verse.Words != null)
                        {
                            foreach (Word word in verse.Words)
                            {
                                word_sum += word.NumberInVerse;

                                if ((word.Letters != null) && (word.Letters.Count > 0))
                                {
                                    foreach (Letter letter in word.Letters)
                                    {
                                        letter_sum += letter.NumberInWord;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    private static void CalculateSums(List<Half> halfs, out int half_sum, out int verse_sum, out int word_sum, out int letter_sum)
    {
        half_sum = 0;
        verse_sum = 0;
        word_sum = 0;
        letter_sum = 0;
        if (halfs != null)
        {
            foreach (Half half in halfs)
            {
                if (half != null)
                {
                    half_sum += half.Number;

                    foreach (Verse verse in half.Verses)
                    {
                        verse_sum += verse.NumberInChapter;
                        if (verse.Words != null)
                        {
                            foreach (Word word in verse.Words)
                            {
                                word_sum += word.NumberInVerse;

                                if ((word.Letters != null) && (word.Letters.Count > 0))
                                {
                                    foreach (Letter letter in word.Letters)
                                    {
                                        letter_sum += letter.NumberInWord;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    private static void CalculateSums(List<Quarter> quarters, out int quarter_sum, out int verse_sum, out int word_sum, out int letter_sum)
    {
        quarter_sum = 0;
        verse_sum = 0;
        word_sum = 0;
        letter_sum = 0;
        if (quarters != null)
        {
            foreach (Quarter quarter in quarters)
            {
                if (quarter != null)
                {
                    quarter_sum += quarter.Number;

                    foreach (Verse verse in quarter.Verses)
                    {
                        verse_sum += verse.NumberInChapter;
                        if (verse.Words != null)
                        {
                            foreach (Word word in verse.Words)
                            {
                                word_sum += word.NumberInVerse;

                                if ((word.Letters != null) && (word.Letters.Count > 0))
                                {
                                    foreach (Letter letter in word.Letters)
                                    {
                                        letter_sum += letter.NumberInWord;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    private static void CalculateSums(List<Bowing> bowings, out int bowing_sum, out int verse_sum, out int word_sum, out int letter_sum)
    {
        bowing_sum = 0;
        verse_sum = 0;
        word_sum = 0;
        letter_sum = 0;
        if (bowings != null)
        {
            foreach (Bowing bowing in bowings)
            {
                if (bowing != null)
                {
                    bowing_sum += bowing.Number;

                    foreach (Verse verse in bowing.Verses)
                    {
                        verse_sum += verse.NumberInChapter;
                        if (verse.Words != null)
                        {
                            foreach (Word word in verse.Words)
                            {
                                word_sum += word.NumberInVerse;

                                if ((word.Letters != null) && (word.Letters.Count > 0))
                                {
                                    foreach (Letter letter in word.Letters)
                                    {
                                        letter_sum += letter.NumberInWord;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    private static bool Compare(Letter letter, NumberQuery query)
    {
        if (letter != null)
        {
            int number = 0;
            switch (query.NumberScope)
            {
                case NumberScope.Number:
                    number = letter.Number;
                    break;
                case NumberScope.NumberInChapter:
                    number = letter.NumberInChapter;
                    break;
                case NumberScope.NumberInVerse:
                    number = letter.NumberInVerse;
                    break;
                case NumberScope.NumberInWord:
                    number = letter.NumberInWord;
                    break;
                default:
                    number = letter.NumberInWord;
                    break;
            }
            if (query.NumberNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(number, number, query.NumberComparisonOperator, query.NumberRemainder))
                {
                    return false;
                }
            }
            else if (query.NumberNumberType == NumberType.None)
            {
                if (query.Number != 0)
                {
                    if (query.Number < 0)
                    {
                        switch (query.NumberScope)
                        {
                            case NumberScope.Number:
                                query.Number = letter.Word.Verse.Chapter.Book.LetterCount + query.Number + 1;
                                break;
                            case NumberScope.NumberInChapter:
                                query.Number = letter.Word.Verse.Chapter.LetterCount + query.Number + 1;
                                break;
                            case NumberScope.NumberInVerse:
                                query.Number = letter.Word.Verse.LetterCount + query.Number + 1;
                                break;
                            case NumberScope.NumberInWord:
                                query.Number = letter.Word.Letters.Count + query.Number + 1;
                                break;
                            default:
                                query.Number = letter.Word.Letters.Count + query.Number + 1;
                                break;
                        }
                    }

                    if (query.Number > 0)
                    {
                        if (!Numbers.Compare(number, query.Number, query.NumberComparisonOperator, query.NumberRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false; // number_out_of_range
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(number, query.NumberNumberType))
                {
                    return false;
                }
            }

            long value = CalculateValue(letter);
            if (query.ValueNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(value, number, query.ValueComparisonOperator, query.ValueRemainder))
                {
                    return false;
                }
            }
            else if (query.ValueNumberType == NumberType.None)
            {
                if (query.Value != 0)
                {
                    if (!Numbers.Compare(value, query.Value, query.ValueComparisonOperator, query.ValueRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(value, query.ValueNumberType))
                {
                    return false;
                }
            }

            int frequency = letter.Frequency;
            if (query.FrequencyNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(frequency, number, query.FrequencyComparisonOperator, query.FrequencyRemainder))
                {
                    return false;
                }
            }
            else if (query.FrequencyNumberType == NumberType.None)
            {
                if (query.Frequency != 0)
                {
                    if (!Numbers.Compare(frequency, query.Frequency, query.FrequencyComparisonOperator, query.FrequencyRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(frequency, query.FrequencyNumberType))
                {
                    return false;
                }
            }

            int occurrence = letter.Occurrence;
            if (query.OccurrenceNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(occurrence, number, query.OccurrenceComparisonOperator, query.OccurrenceRemainder))
                {
                    return false;
                }
            }
            else if (query.OccurrenceNumberType == NumberType.None)
            {
                if (query.Occurrence != 0)
                {
                    if (!Numbers.Compare(occurrence, query.Occurrence, query.OccurrenceComparisonOperator, query.OccurrenceRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(occurrence, query.OccurrenceNumberType))
                {
                    return false;
                }
            }
        }

        // passed all tests successfully
        return true;
    }
    private static bool Compare(Word word, NumberQuery query)
    {
        if (word != null)
        {
            int number = 0;
            switch (query.NumberScope)
            {
                case NumberScope.Number:
                    number = word.Number;
                    break;
                case NumberScope.NumberInChapter:
                    number = word.NumberInChapter;
                    break;
                case NumberScope.NumberInVerse:
                    number = word.NumberInVerse;
                    break;
                default:
                    number = word.NumberInVerse;
                    break;
            }
            if (query.NumberNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(number, number, query.NumberComparisonOperator, query.NumberRemainder))
                {
                    return false;
                }
            }
            else if (query.NumberNumberType == NumberType.None)
            {
                if (query.Number != 0)
                {
                    if (query.Number < 0)
                    {
                        switch (query.NumberScope)
                        {
                            case NumberScope.Number:
                                query.Number = word.Verse.Chapter.Book.WordCount + query.Number + 1;
                                break;
                            case NumberScope.NumberInChapter:
                                query.Number = word.Verse.Chapter.WordCount + query.Number + 1;
                                break;
                            case NumberScope.NumberInVerse:
                                query.Number = word.Verse.Words.Count + query.Number + 1;
                                break;
                            default:
                                query.Number = word.Verse.Words.Count + query.Number + 1;
                                break;
                        }
                    }

                    if (query.Number > 0)
                    {
                        if (!Numbers.Compare(number, query.Number, query.NumberComparisonOperator, query.NumberRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false; // number_out_of_range
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(number, query.NumberNumberType))
                {
                    return false;
                }
            }

            if (query.LetterCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(word.Letters.Count, number, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                {
                    return false;
                }
            }
            else if (query.LetterCountNumberType == NumberType.None)
            {
                if (query.LetterCount > 0)
                {
                    if (query.LetterCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        int letter_sum;
                        CalculateSums(word, out letter_sum);
                        if (!Numbers.Compare(letter_sum, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(word.Letters.Count, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.LetterCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    int letter_sum;
                    CalculateSums(word, out letter_sum);
                    if (!Numbers.IsNumberType(letter_sum, query.LetterCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(word.Letters.Count, query.LetterCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.UniqueLetterCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(word.UniqueLetters.Count, number, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                {
                    return false;
                }
            }
            else if (query.UniqueLetterCountNumberType == NumberType.None)
            {
                if (query.UniqueLetterCount > 0)
                {
                    if (!Numbers.Compare(word.UniqueLetters.Count, query.UniqueLetterCount, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(word.UniqueLetters.Count, query.UniqueLetterCountNumberType))
                {
                    return false;
                }
            }

            long value = CalculateValue(word);
            if (query.ValueNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(value, number, query.ValueComparisonOperator, query.ValueRemainder))
                {
                    return false;
                }
            }
            else if (query.ValueNumberType == NumberType.None)
            {
                if (query.Value != 0)
                {
                    if (!Numbers.Compare(value, query.Value, query.ValueComparisonOperator, query.ValueRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(value, query.ValueNumberType))
                {
                    return false;
                }
            }

            int frequency = word.Frequency;
            if (query.FrequencyNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(frequency, number, query.FrequencyComparisonOperator, query.FrequencyRemainder))
                {
                    return false;
                }
            }
            else if (query.FrequencyNumberType == NumberType.None)
            {
                if (query.Frequency != 0)
                {
                    if (!Numbers.Compare(frequency, query.Frequency, query.FrequencyComparisonOperator, query.FrequencyRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(frequency, query.FrequencyNumberType))
                {
                    return false;
                }
            }

            int occurrence = word.Occurrence;
            if (query.OccurrenceNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(occurrence, number, query.OccurrenceComparisonOperator, query.OccurrenceRemainder))
                {
                    return false;
                }
            }
            else if (query.OccurrenceNumberType == NumberType.None)
            {
                if (query.Occurrence != 0)
                {
                    if (!Numbers.Compare(occurrence, query.Occurrence, query.OccurrenceComparisonOperator, query.OccurrenceRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(occurrence, query.OccurrenceNumberType))
                {
                    return false;
                }
            }
        }

        // passed all tests successfully
        return true;
    }
    private static bool Compare(Sentence sentence, NumberQuery query)
    {
        if (sentence != null)
        {
            long value = 0L;

            if (query.WordCountNumberType == NumberType.Natural)
            {
                return false;
            }
            else if (query.WordCountNumberType == NumberType.None)
            {
                if (query.WordCount > 0)
                {
                    if (!Numbers.Compare(sentence.WordCount, query.WordCount, query.WordCountComparisonOperator, query.WordCountRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(sentence.WordCount, query.WordCountNumberType))
                {
                    return false;
                }
            }

            if (query.LetterCountNumberType == NumberType.Natural)
            {
                return false;
            }
            else if (query.LetterCountNumberType == NumberType.None)
            {
                if (query.LetterCount > 0)
                {
                    if (!Numbers.Compare(sentence.LetterCount, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(sentence.LetterCount, query.LetterCountNumberType))
                {
                    return false;
                }
            }

            if (query.UniqueLetterCountNumberType == NumberType.Natural)
            {
                return false;
            }
            else if (query.UniqueLetterCountNumberType == NumberType.None)
            {
                if (query.UniqueLetterCount > 0)
                {
                    if (!Numbers.Compare(sentence.UniqueLetterCount, query.UniqueLetterCount, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(sentence.UniqueLetterCount, query.UniqueLetterCountNumberType))
                {
                    return false;
                }
            }

            if (query.ValueNumberType == NumberType.Natural)
            {
                return false;
            }
            else if (query.ValueNumberType == NumberType.None)
            {
                if (query.Value > 0)
                {
                    if (value == 0L) { value = CalculateValue(sentence); }
                    if (!Numbers.Compare(value, query.Value, query.ValueComparisonOperator, query.ValueRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (value == 0L) { value = CalculateValue(sentence); }
                if (!Numbers.IsNumberType(value, query.ValueNumberType))
                {
                    return false;
                }
            }
        }

        // passed all tests successfully
        return true;
    }
    private static bool Compare(Verse verse, NumberQuery query)
    {
        if (verse != null)
        {
            int number = 0;
            switch (query.NumberScope)
            {
                case NumberScope.Number:
                    number = verse.Number;
                    break;
                case NumberScope.NumberInChapter:
                    number = verse.NumberInChapter;
                    break;
                default:
                    number = verse.NumberInChapter;
                    break;
            }
            if (query.NumberNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(number, number, query.NumberComparisonOperator, query.NumberRemainder))
                {
                    return false;
                }
            }
            else if (query.NumberNumberType == NumberType.None)
            {
                if (query.Number != 0)
                {
                    if (query.Number < 0)
                    {
                        switch (query.NumberScope)
                        {
                            case NumberScope.Number:
                                query.Number = verse.Book.Verses.Count + query.Number + 1;
                                break;
                            case NumberScope.NumberInChapter:
                                query.Number = verse.Chapter.Verses.Count + query.Number + 1;
                                break;
                            default:
                                query.Number = verse.Chapter.Verses.Count + query.Number + 1;
                                break;
                        }
                    }

                    if (query.Number > 0)
                    {
                        if (!Numbers.Compare(number, query.Number, query.NumberComparisonOperator, query.NumberRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false; // number_out_of_range
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(number, query.NumberNumberType))
                {
                    return false;
                }
            }

            if (query.WordCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(verse.Words.Count, number, query.WordCountComparisonOperator, query.WordCountRemainder))
                {
                    return false;
                }
            }
            else if (query.WordCountNumberType == NumberType.None)
            {
                if (query.WordCount > 0)
                {
                    if (query.WordCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        int word_sum; int letter_sum;
                        CalculateSums(verse, out word_sum, out letter_sum);
                        if (!Numbers.Compare(word_sum, query.WordCount, query.WordCountComparisonOperator, query.WordCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(verse.Words.Count, query.WordCount, query.WordCountComparisonOperator, query.WordCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.WordCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    int word_sum; int letter_sum;
                    CalculateSums(verse, out word_sum, out letter_sum);
                    if (!Numbers.IsNumberType(word_sum, query.WordCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(verse.Words.Count, query.WordCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.LetterCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(verse.LetterCount, number, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                {
                    return false;
                }
            }
            else if (query.LetterCountNumberType == NumberType.None)
            {
                if (query.LetterCount > 0)
                {
                    if (query.LetterCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        int word_sum; int letter_sum;
                        CalculateSums(verse, out word_sum, out letter_sum);
                        if (!Numbers.Compare(letter_sum, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(verse.LetterCount, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.LetterCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    int word_sum; int letter_sum;
                    CalculateSums(verse, out word_sum, out letter_sum);
                    if (!Numbers.IsNumberType(letter_sum, query.LetterCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(verse.LetterCount, query.LetterCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.UniqueLetterCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(verse.UniqueLetters.Count, number, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                {
                    return false;
                }
            }
            else if (query.UniqueLetterCountNumberType == NumberType.None)
            {
                if (query.UniqueLetterCount > 0)
                {
                    if (!Numbers.Compare(verse.UniqueLetters.Count, query.UniqueLetterCount, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(verse.UniqueLetters.Count, query.UniqueLetterCountNumberType))
                {
                    return false;
                }
            }

            long value = CalculateValue(verse);
            if (query.ValueNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(value, number, query.ValueComparisonOperator, query.ValueRemainder))
                {
                    return false;
                }
            }
            else if (query.ValueNumberType == NumberType.None)
            {
                if (query.Value > 0)
                {
                    if (!Numbers.Compare(value, query.Value, query.ValueComparisonOperator, query.ValueRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(value, query.ValueNumberType))
                {
                    return false;
                }
            }

            int frequency = verse.Frequency;
            if (query.FrequencyNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(frequency, number, query.FrequencyComparisonOperator, query.FrequencyRemainder))
                {
                    return false;
                }
            }
            else if (query.FrequencyNumberType == NumberType.None)
            {
                if (query.Frequency > 0)
                {
                    if (!Numbers.Compare(frequency, query.Frequency, query.FrequencyComparisonOperator, query.FrequencyRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(frequency, query.FrequencyNumberType))
                {
                    return false;
                }
            }

            int occurrence = verse.Occurrence;
            if (query.OccurrenceNumberType == NumberType.Natural)
            {
                if (query.Occurrence > 0)
                {
                    if (!Numbers.Compare(occurrence, number, query.OccurrenceComparisonOperator, query.OccurrenceRemainder))
                    {
                        return false;
                    }
                }
            }
            else if (query.OccurrenceNumberType == NumberType.None)
            {
                if (query.Occurrence > 0)
                {
                    if (!Numbers.Compare(occurrence, query.Occurrence, query.OccurrenceComparisonOperator, query.OccurrenceRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(occurrence, query.OccurrenceNumberType))
                {
                    return false;
                }
            }
        }

        // passed all tests successfully
        return true;
    }
    private static bool Compare(Chapter chapter, NumberQuery query)
    {
        if (chapter != null)
        {
            int number = chapter.Number;
            if (query.NumberNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(number, number, query.NumberComparisonOperator, query.NumberRemainder))
                {
                    return false;
                }
            }
            else if (query.NumberNumberType == NumberType.None)
            {
                if (query.Number != 0)
                {
                    if (query.Number < 0)
                    {
                        switch (query.NumberScope)
                        {
                            case NumberScope.Number:
                                query.Number = Book.Chapters.Count + query.Number + 1;
                                break;
                            default:
                                query.Number = Book.Chapters.Count + query.Number + 1;
                                break;
                        }
                    }

                    if (query.Number < 0)
                    {
                        query.Number = number + query.Number + 1;
                    }

                    if (query.Number > 0)
                    {
                        if (!Numbers.Compare(number, query.Number, query.NumberComparisonOperator, query.NumberRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false; // number_out_of_range
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(number, query.NumberNumberType))
                {
                    return false;
                }
            }

            if (query.VerseCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(chapter.Verses.Count, number, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                {
                    return false;
                }
            }
            else if (query.VerseCountNumberType == NumberType.None)
            {
                if (query.VerseCount > 0)
                {
                    if (query.VerseCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        int verse_sum; int word_sum; int letter_sum;
                        CalculateSums(chapter, out verse_sum, out word_sum, out letter_sum);
                        if (!Numbers.Compare(verse_sum, query.VerseCount, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(chapter.Verses.Count, query.VerseCount, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.VerseCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    int verse_sum; int word_sum; int letter_sum;
                    CalculateSums(chapter, out verse_sum, out word_sum, out letter_sum);
                    if (!Numbers.IsNumberType(verse_sum, query.VerseCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(chapter.Verses.Count, query.VerseCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.WordCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(chapter.WordCount, number, query.WordCountComparisonOperator, query.WordCountRemainder))
                {
                    return false;
                }
            }
            else if (query.WordCountNumberType == NumberType.None)
            {
                if (query.WordCount > 0)
                {
                    if (query.WordCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        int verse_sum; int word_sum; int letter_sum;
                        CalculateSums(chapter, out verse_sum, out word_sum, out letter_sum);
                        if (!Numbers.Compare(word_sum, query.WordCount, query.WordCountComparisonOperator, query.WordCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(chapter.WordCount, query.WordCount, query.WordCountComparisonOperator, query.WordCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.WordCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    int verse_sum; int word_sum; int letter_sum;
                    CalculateSums(chapter, out verse_sum, out word_sum, out letter_sum);
                    if (!Numbers.IsNumberType(word_sum, query.WordCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(chapter.WordCount, query.WordCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.LetterCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(chapter.LetterCount, number, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                {
                    return false;
                }
            }
            else if (query.LetterCountNumberType == NumberType.None)
            {
                if (query.LetterCount > 0)
                {
                    if (query.LetterCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        int verse_sum; int word_sum; int letter_sum;
                        CalculateSums(chapter, out verse_sum, out word_sum, out letter_sum);
                        if (!Numbers.Compare(letter_sum, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(chapter.LetterCount, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.LetterCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    int verse_sum; int word_sum; int letter_sum;
                    CalculateSums(chapter, out verse_sum, out word_sum, out letter_sum);
                    if (!Numbers.IsNumberType(letter_sum, query.LetterCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(chapter.LetterCount, query.LetterCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.UniqueLetterCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(chapter.UniqueLetters.Count, number, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                {
                    return false;
                }
            }
            else if (query.UniqueLetterCountNumberType == NumberType.None)
            {
                if (query.UniqueLetterCount > 0)
                {
                    if (!Numbers.Compare(chapter.UniqueLetters.Count, query.UniqueLetterCount, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(chapter.UniqueLetters.Count, query.UniqueLetterCountNumberType))
                {
                    return false;
                }
            }

            long value = CalculateValue(chapter);
            if (query.ValueNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(value, number, query.ValueComparisonOperator, query.ValueRemainder))
                {
                    return false;
                }
            }
            else if (query.ValueNumberType == NumberType.None)
            {
                if (query.Value > 0)
                {
                    if (!Numbers.Compare(value, query.Value, query.ValueComparisonOperator, query.ValueRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(value, query.ValueNumberType))
                {
                    return false;
                }
            }
        }

        // passed all tests successfully
        return true;
    }
    private static bool Compare(Page page, NumberQuery query)
    {
        if (page != null)
        {
            int number = page.Number;
            if (query.NumberNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(number, number, query.NumberComparisonOperator, query.NumberRemainder))
                {
                    return false;
                }
            }
            else if (query.NumberNumberType == NumberType.None)
            {
                if (query.Number != 0)
                {
                    if (query.Number < 0)
                    {
                        switch (query.NumberScope)
                        {
                            case NumberScope.Number:
                                query.Number = Book.Pages.Count + query.Number + 1;
                                break;
                            default:
                                query.Number = Book.Pages.Count + query.Number + 1;
                                break;
                        }
                    }

                    if (query.Number < 0)
                    {
                        query.Number = number + query.Number + 1;
                    }

                    if (query.Number > 0)
                    {
                        if (!Numbers.Compare(number, query.Number, query.NumberComparisonOperator, query.NumberRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false; // number_out_of_range
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(number, query.NumberNumberType))
                {
                    return false;
                }
            }

            if (query.VerseCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(page.Verses.Count, number, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                {
                    return false;
                }
            }
            else if (query.VerseCountNumberType == NumberType.None)
            {
                if (query.VerseCount > 0)
                {
                    if (query.VerseCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        int verse_sum; int word_sum; int letter_sum;
                        CalculateSums(page, out verse_sum, out word_sum, out letter_sum);
                        if (!Numbers.Compare(verse_sum, query.VerseCount, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(page.Verses.Count, query.VerseCount, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.VerseCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    int verse_sum; int word_sum; int letter_sum;
                    CalculateSums(page, out verse_sum, out word_sum, out letter_sum);
                    if (!Numbers.IsNumberType(verse_sum, query.VerseCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(page.Verses.Count, query.VerseCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.WordCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(page.WordCount, number, query.WordCountComparisonOperator, query.WordCountRemainder))
                {
                    return false;
                }
            }
            else if (query.WordCountNumberType == NumberType.None)
            {
                if (query.WordCount > 0)
                {
                    if (query.WordCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        int verse_sum; int word_sum; int letter_sum;
                        CalculateSums(page, out verse_sum, out word_sum, out letter_sum);
                        if (!Numbers.Compare(word_sum, query.WordCount, query.WordCountComparisonOperator, query.WordCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(page.WordCount, query.WordCount, query.WordCountComparisonOperator, query.WordCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.WordCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    int verse_sum; int word_sum; int letter_sum;
                    CalculateSums(page, out verse_sum, out word_sum, out letter_sum);
                    if (!Numbers.IsNumberType(word_sum, query.WordCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(page.WordCount, query.WordCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.LetterCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(page.LetterCount, number, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                {
                    return false;
                }
            }
            else if (query.LetterCountNumberType == NumberType.None)
            {
                if (query.LetterCount > 0)
                {
                    if (query.LetterCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        int verse_sum; int word_sum; int letter_sum;
                        CalculateSums(page, out verse_sum, out word_sum, out letter_sum);
                        if (!Numbers.Compare(letter_sum, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(page.LetterCount, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.LetterCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    int verse_sum; int word_sum; int letter_sum;
                    CalculateSums(page, out verse_sum, out word_sum, out letter_sum);
                    if (!Numbers.IsNumberType(letter_sum, query.LetterCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(page.LetterCount, query.LetterCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.UniqueLetterCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(page.UniqueLetters.Count, number, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                {
                    return false;
                }
            }
            else if (query.UniqueLetterCountNumberType == NumberType.None)
            {
                if (query.UniqueLetterCount > 0)
                {
                    if (!Numbers.Compare(page.UniqueLetters.Count, query.UniqueLetterCount, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(page.UniqueLetters.Count, query.UniqueLetterCountNumberType))
                {
                    return false;
                }
            }

            long value = CalculateValue(page.Verses);
            if (query.ValueNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(value, number, query.ValueComparisonOperator, query.ValueRemainder))
                {
                    return false;
                }
            }
            else if (query.ValueNumberType == NumberType.None)
            {
                if (query.Value > 0)
                {
                    if (!Numbers.Compare(value, query.Value, query.ValueComparisonOperator, query.ValueRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(value, query.ValueNumberType))
                {
                    return false;
                }
            }
        }

        // passed all tests successfully
        return true;
    }
    private static bool Compare(Station station, NumberQuery query)
    {
        if (station != null)
        {
            int number = station.Number;
            if (query.NumberNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(number, number, query.NumberComparisonOperator, query.NumberRemainder))
                {
                    return false;
                }
            }
            else if (query.NumberNumberType == NumberType.None)
            {
                if (query.Number != 0)
                {
                    if (query.Number < 0)
                    {
                        switch (query.NumberScope)
                        {
                            case NumberScope.Number:
                                query.Number = Book.Stations.Count + query.Number + 1;
                                break;
                            default:
                                query.Number = Book.Stations.Count + query.Number + 1;
                                break;
                        }
                    }

                    if (query.Number < 0)
                    {
                        query.Number = number + query.Number + 1;
                    }

                    if (query.Number > 0)
                    {
                        if (!Numbers.Compare(number, query.Number, query.NumberComparisonOperator, query.NumberRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false; // number_out_of_range
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(number, query.NumberNumberType))
                {
                    return false;
                }
            }

            if (query.VerseCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(station.Verses.Count, number, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                {
                    return false;
                }
            }
            else if (query.VerseCountNumberType == NumberType.None)
            {
                if (query.VerseCount > 0)
                {
                    if (query.VerseCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        int verse_sum; int word_sum; int letter_sum;
                        CalculateSums(station, out verse_sum, out word_sum, out letter_sum);
                        if (!Numbers.Compare(verse_sum, query.VerseCount, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(station.Verses.Count, query.VerseCount, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.VerseCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    int verse_sum; int word_sum; int letter_sum;
                    CalculateSums(station, out verse_sum, out word_sum, out letter_sum);
                    if (!Numbers.IsNumberType(verse_sum, query.VerseCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(station.Verses.Count, query.VerseCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.WordCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(station.WordCount, number, query.WordCountComparisonOperator, query.WordCountRemainder))
                {
                    return false;
                }
            }
            else if (query.WordCountNumberType == NumberType.None)
            {
                if (query.WordCount > 0)
                {
                    if (query.WordCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        int verse_sum; int word_sum; int letter_sum;
                        CalculateSums(station, out verse_sum, out word_sum, out letter_sum);
                        if (!Numbers.Compare(word_sum, query.WordCount, query.WordCountComparisonOperator, query.WordCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(station.WordCount, query.WordCount, query.WordCountComparisonOperator, query.WordCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.WordCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    int verse_sum; int word_sum; int letter_sum;
                    CalculateSums(station, out verse_sum, out word_sum, out letter_sum);
                    if (!Numbers.IsNumberType(word_sum, query.WordCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(station.WordCount, query.WordCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.LetterCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(station.LetterCount, number, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                {
                    return false;
                }
            }
            else if (query.LetterCountNumberType == NumberType.None)
            {
                if (query.LetterCount > 0)
                {
                    if (query.LetterCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        int verse_sum; int word_sum; int letter_sum;
                        CalculateSums(station, out verse_sum, out word_sum, out letter_sum);
                        if (!Numbers.Compare(letter_sum, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(station.LetterCount, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.LetterCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    int verse_sum; int word_sum; int letter_sum;
                    CalculateSums(station, out verse_sum, out word_sum, out letter_sum);
                    if (!Numbers.IsNumberType(letter_sum, query.LetterCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(station.LetterCount, query.LetterCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.UniqueLetterCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(station.UniqueLetters.Count, number, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                {
                    return false;
                }
            }
            else if (query.UniqueLetterCountNumberType == NumberType.None)
            {
                if (query.UniqueLetterCount > 0)
                {
                    if (!Numbers.Compare(station.UniqueLetters.Count, query.UniqueLetterCount, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(station.UniqueLetters.Count, query.UniqueLetterCountNumberType))
                {
                    return false;
                }
            }

            long value = CalculateValue(station.Verses);
            if (query.ValueNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(value, number, query.ValueComparisonOperator, query.ValueRemainder))
                {
                    return false;
                }
            }
            else if (query.ValueNumberType == NumberType.None)
            {
                if (query.Value > 0)
                {
                    if (!Numbers.Compare(value, query.Value, query.ValueComparisonOperator, query.ValueRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(value, query.ValueNumberType))
                {
                    return false;
                }
            }
        }

        // passed all tests successfully
        return true;
    }
    private static bool Compare(Part part, NumberQuery query)
    {
        if (part != null)
        {
            int number = part.Number;
            if (query.NumberNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(number, number, query.NumberComparisonOperator, query.NumberRemainder))
                {
                    return false;
                }
            }
            else if (query.NumberNumberType == NumberType.None)
            {
                if (query.Number != 0)
                {
                    if (query.Number < 0)
                    {
                        switch (query.NumberScope)
                        {
                            case NumberScope.Number:
                                query.Number = Book.Parts.Count + query.Number + 1;
                                break;
                            default:
                                query.Number = Book.Parts.Count + query.Number + 1;
                                break;
                        }
                    }

                    if (query.Number < 0)
                    {
                        query.Number = number + query.Number + 1;
                    }

                    if (query.Number > 0)
                    {
                        if (!Numbers.Compare(number, query.Number, query.NumberComparisonOperator, query.NumberRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false; // number_out_of_range
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(number, query.NumberNumberType))
                {
                    return false;
                }
            }

            if (query.VerseCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(part.Verses.Count, number, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                {
                    return false;
                }
            }
            else if (query.VerseCountNumberType == NumberType.None)
            {
                if (query.VerseCount > 0)
                {
                    if (query.VerseCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        int verse_sum; int word_sum; int letter_sum;
                        CalculateSums(part, out verse_sum, out word_sum, out letter_sum);
                        if (!Numbers.Compare(verse_sum, query.VerseCount, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(part.Verses.Count, query.VerseCount, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.VerseCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    int verse_sum; int word_sum; int letter_sum;
                    CalculateSums(part, out verse_sum, out word_sum, out letter_sum);
                    if (!Numbers.IsNumberType(verse_sum, query.VerseCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(part.Verses.Count, query.VerseCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.WordCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(part.WordCount, number, query.WordCountComparisonOperator, query.WordCountRemainder))
                {
                    return false;
                }
            }
            else if (query.WordCountNumberType == NumberType.None)
            {
                if (query.WordCount > 0)
                {
                    if (query.WordCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        int verse_sum; int word_sum; int letter_sum;
                        CalculateSums(part, out verse_sum, out word_sum, out letter_sum);
                        if (!Numbers.Compare(word_sum, query.WordCount, query.WordCountComparisonOperator, query.WordCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(part.WordCount, query.WordCount, query.WordCountComparisonOperator, query.WordCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.WordCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    int verse_sum; int word_sum; int letter_sum;
                    CalculateSums(part, out verse_sum, out word_sum, out letter_sum);
                    if (!Numbers.IsNumberType(word_sum, query.WordCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(part.WordCount, query.WordCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.LetterCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(part.LetterCount, number, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                {
                    return false;
                }
            }
            else if (query.LetterCountNumberType == NumberType.None)
            {
                if (query.LetterCount > 0)
                {
                    if (query.LetterCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        int verse_sum; int word_sum; int letter_sum;
                        CalculateSums(part, out verse_sum, out word_sum, out letter_sum);
                        if (!Numbers.Compare(letter_sum, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(part.LetterCount, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.LetterCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    int verse_sum; int word_sum; int letter_sum;
                    CalculateSums(part, out verse_sum, out word_sum, out letter_sum);
                    if (!Numbers.IsNumberType(letter_sum, query.LetterCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(part.LetterCount, query.LetterCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.UniqueLetterCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(part.UniqueLetters.Count, number, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                {
                    return false;
                }
            }
            else if (query.UniqueLetterCountNumberType == NumberType.None)
            {
                if (query.UniqueLetterCount > 0)
                {
                    if (!Numbers.Compare(part.UniqueLetters.Count, query.UniqueLetterCount, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(part.UniqueLetters.Count, query.UniqueLetterCountNumberType))
                {
                    return false;
                }
            }

            long value = CalculateValue(part.Verses);
            if (query.ValueNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(value, number, query.ValueComparisonOperator, query.ValueRemainder))
                {
                    return false;
                }
            }
            else if (query.ValueNumberType == NumberType.None)
            {
                if (query.Value > 0)
                {
                    if (!Numbers.Compare(value, query.Value, query.ValueComparisonOperator, query.ValueRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(value, query.ValueNumberType))
                {
                    return false;
                }
            }
        }

        // passed all tests successfully
        return true;
    }
    private static bool Compare(Model.Group group, NumberQuery query)
    {
        if (group != null)
        {
            int number = group.Number;
            if (query.NumberNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(number, number, query.NumberComparisonOperator, query.NumberRemainder))
                {
                    return false;
                }
            }
            else if (query.NumberNumberType == NumberType.None)
            {
                if (query.Number != 0)
                {
                    if (query.Number < 0)
                    {
                        switch (query.NumberScope)
                        {
                            case NumberScope.Number:
                                query.Number = Book.Groups.Count + query.Number + 1;
                                break;
                            default:
                                query.Number = Book.Groups.Count + query.Number + 1;
                                break;
                        }
                    }

                    if (query.Number < 0)
                    {
                        query.Number = number + query.Number + 1;
                    }

                    if (query.Number > 0)
                    {
                        if (!Numbers.Compare(number, query.Number, query.NumberComparisonOperator, query.NumberRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false; // number_out_of_range
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(number, query.NumberNumberType))
                {
                    return false;
                }
            }

            if (query.VerseCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(group.Verses.Count, number, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                {
                    return false;
                }
            }
            else if (query.VerseCountNumberType == NumberType.None)
            {
                if (query.VerseCount > 0)
                {
                    if (query.VerseCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        int verse_sum; int word_sum; int letter_sum;
                        CalculateSums(group, out verse_sum, out word_sum, out letter_sum);
                        if (!Numbers.Compare(verse_sum, query.VerseCount, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(group.Verses.Count, query.VerseCount, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.VerseCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    int verse_sum; int word_sum; int letter_sum;
                    CalculateSums(group, out verse_sum, out word_sum, out letter_sum);
                    if (!Numbers.IsNumberType(verse_sum, query.VerseCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(group.Verses.Count, query.VerseCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.WordCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(group.WordCount, number, query.WordCountComparisonOperator, query.WordCountRemainder))
                {
                    return false;
                }
            }
            else if (query.WordCountNumberType == NumberType.None)
            {
                if (query.WordCount > 0)
                {
                    if (query.WordCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        int verse_sum; int word_sum; int letter_sum;
                        CalculateSums(group, out verse_sum, out word_sum, out letter_sum);
                        if (!Numbers.Compare(word_sum, query.WordCount, query.WordCountComparisonOperator, query.WordCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(group.WordCount, query.WordCount, query.WordCountComparisonOperator, query.WordCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.WordCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    int verse_sum; int word_sum; int letter_sum;
                    CalculateSums(group, out verse_sum, out word_sum, out letter_sum);
                    if (!Numbers.IsNumberType(word_sum, query.WordCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(group.WordCount, query.WordCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.LetterCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(group.LetterCount, number, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                {
                    return false;
                }
            }
            else if (query.LetterCountNumberType == NumberType.None)
            {
                if (query.LetterCount > 0)
                {
                    if (query.LetterCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        int verse_sum; int word_sum; int letter_sum;
                        CalculateSums(group, out verse_sum, out word_sum, out letter_sum);
                        if (!Numbers.Compare(letter_sum, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(group.LetterCount, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.LetterCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    int verse_sum; int word_sum; int letter_sum;
                    CalculateSums(group, out verse_sum, out word_sum, out letter_sum);
                    if (!Numbers.IsNumberType(letter_sum, query.LetterCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(group.LetterCount, query.LetterCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.UniqueLetterCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(group.UniqueLetters.Count, number, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                {
                    return false;
                }
            }
            else if (query.UniqueLetterCountNumberType == NumberType.None)
            {
                if (query.UniqueLetterCount > 0)
                {
                    if (!Numbers.Compare(group.UniqueLetters.Count, query.UniqueLetterCount, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(group.UniqueLetters.Count, query.UniqueLetterCountNumberType))
                {
                    return false;
                }
            }

            long value = CalculateValue(group.Verses);
            if (query.ValueNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(value, number, query.ValueComparisonOperator, query.ValueRemainder))
                {
                    return false;
                }
            }
            else if (query.ValueNumberType == NumberType.None)
            {
                if (query.Value > 0)
                {
                    if (!Numbers.Compare(value, query.Value, query.ValueComparisonOperator, query.ValueRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(value, query.ValueNumberType))
                {
                    return false;
                }
            }
        }

        // passed all tests successfully
        return true;
    }
    private static bool Compare(Half half, NumberQuery query)
    {
        if (half != null)
        {
            int number = half.Number;
            if (query.NumberNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(number, number, query.NumberComparisonOperator, query.NumberRemainder))
                {
                    return false;
                }
            }
            else if (query.NumberNumberType == NumberType.None)
            {
                if (query.Number != 0)
                {
                    if (query.Number < 0)
                    {
                        switch (query.NumberScope)
                        {
                            case NumberScope.Number:
                                query.Number = Book.Halfs.Count + query.Number + 1;
                                break;
                            default:
                                query.Number = Book.Halfs.Count + query.Number + 1;
                                break;
                        }
                    }

                    if (query.Number < 0)
                    {
                        query.Number = number + query.Number + 1;
                    }

                    if (query.Number > 0)
                    {
                        if (!Numbers.Compare(number, query.Number, query.NumberComparisonOperator, query.NumberRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false; // number_out_of_range
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(number, query.NumberNumberType))
                {
                    return false;
                }
            }

            if (query.VerseCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(half.Verses.Count, number, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                {
                    return false;
                }
            }
            else if (query.VerseCountNumberType == NumberType.None)
            {
                if (query.VerseCount > 0)
                {
                    if (query.VerseCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        int verse_sum; int word_sum; int letter_sum;
                        CalculateSums(half, out verse_sum, out word_sum, out letter_sum);
                        if (!Numbers.Compare(verse_sum, query.VerseCount, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(half.Verses.Count, query.VerseCount, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.VerseCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    int verse_sum; int word_sum; int letter_sum;
                    CalculateSums(half, out verse_sum, out word_sum, out letter_sum);
                    if (!Numbers.IsNumberType(verse_sum, query.VerseCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(half.Verses.Count, query.VerseCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.WordCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(half.WordCount, number, query.WordCountComparisonOperator, query.WordCountRemainder))
                {
                    return false;
                }
            }
            else if (query.WordCountNumberType == NumberType.None)
            {
                if (query.WordCount > 0)
                {
                    if (query.WordCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        int verse_sum; int word_sum; int letter_sum;
                        CalculateSums(half, out verse_sum, out word_sum, out letter_sum);
                        if (!Numbers.Compare(word_sum, query.WordCount, query.WordCountComparisonOperator, query.WordCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(half.WordCount, query.WordCount, query.WordCountComparisonOperator, query.WordCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.WordCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    int verse_sum; int word_sum; int letter_sum;
                    CalculateSums(half, out verse_sum, out word_sum, out letter_sum);
                    if (!Numbers.IsNumberType(word_sum, query.WordCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(half.WordCount, query.WordCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.LetterCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(half.LetterCount, number, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                {
                    return false;
                }
            }
            else if (query.LetterCountNumberType == NumberType.None)
            {
                if (query.LetterCount > 0)
                {
                    if (query.LetterCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        int verse_sum; int word_sum; int letter_sum;
                        CalculateSums(half, out verse_sum, out word_sum, out letter_sum);
                        if (!Numbers.Compare(letter_sum, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(half.LetterCount, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.LetterCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    int verse_sum; int word_sum; int letter_sum;
                    CalculateSums(half, out verse_sum, out word_sum, out letter_sum);
                    if (!Numbers.IsNumberType(letter_sum, query.LetterCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(half.LetterCount, query.LetterCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.UniqueLetterCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(half.UniqueLetters.Count, number, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                {
                    return false;
                }
            }
            else if (query.UniqueLetterCountNumberType == NumberType.None)
            {
                if (query.UniqueLetterCount > 0)
                {
                    if (!Numbers.Compare(half.UniqueLetters.Count, query.UniqueLetterCount, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(half.UniqueLetters.Count, query.UniqueLetterCountNumberType))
                {
                    return false;
                }
            }

            long value = CalculateValue(half.Verses);
            if (query.ValueNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(value, number, query.ValueComparisonOperator, query.ValueRemainder))
                {
                    return false;
                }
            }
            else if (query.ValueNumberType == NumberType.None)
            {
                if (query.Value > 0)
                {
                    if (!Numbers.Compare(value, query.Value, query.ValueComparisonOperator, query.ValueRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(value, query.ValueNumberType))
                {
                    return false;
                }
            }
        }

        // passed all tests successfully
        return true;
    }
    private static bool Compare(Quarter quarter, NumberQuery query)
    {
        if (quarter != null)
        {
            int number = quarter.Number;
            if (query.NumberNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(number, number, query.NumberComparisonOperator, query.NumberRemainder))
                {
                    return false;
                }
            }
            else if (query.NumberNumberType == NumberType.None)
            {
                if (query.Number != 0)
                {
                    if (query.Number < 0)
                    {
                        switch (query.NumberScope)
                        {
                            case NumberScope.Number:
                                query.Number = Book.Quarters.Count + query.Number + 1;
                                break;
                            default:
                                query.Number = Book.Quarters.Count + query.Number + 1;
                                break;
                        }
                    }

                    if (query.Number < 0)
                    {
                        query.Number = number + query.Number + 1;
                    }

                    if (query.Number > 0)
                    {
                        if (!Numbers.Compare(number, query.Number, query.NumberComparisonOperator, query.NumberRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false; // number_out_of_range
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(number, query.NumberNumberType))
                {
                    return false;
                }
            }

            if (query.VerseCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(quarter.Verses.Count, number, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                {
                    return false;
                }
            }
            else if (query.VerseCountNumberType == NumberType.None)
            {
                if (query.VerseCount > 0)
                {
                    if (query.VerseCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        int verse_sum; int word_sum; int letter_sum;
                        CalculateSums(quarter, out verse_sum, out word_sum, out letter_sum);
                        if (!Numbers.Compare(verse_sum, query.VerseCount, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(quarter.Verses.Count, query.VerseCount, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.VerseCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    int verse_sum; int word_sum; int letter_sum;
                    CalculateSums(quarter, out verse_sum, out word_sum, out letter_sum);
                    if (!Numbers.IsNumberType(verse_sum, query.VerseCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(quarter.Verses.Count, query.VerseCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.WordCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(quarter.WordCount, number, query.WordCountComparisonOperator, query.WordCountRemainder))
                {
                    return false;
                }
            }
            else if (query.WordCountNumberType == NumberType.None)
            {
                if (query.WordCount > 0)
                {
                    if (query.WordCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        int verse_sum; int word_sum; int letter_sum;
                        CalculateSums(quarter, out verse_sum, out word_sum, out letter_sum);
                        if (!Numbers.Compare(word_sum, query.WordCount, query.WordCountComparisonOperator, query.WordCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(quarter.WordCount, query.WordCount, query.WordCountComparisonOperator, query.WordCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.WordCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    int verse_sum; int word_sum; int letter_sum;
                    CalculateSums(quarter, out verse_sum, out word_sum, out letter_sum);
                    if (!Numbers.IsNumberType(word_sum, query.WordCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(quarter.WordCount, query.WordCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.LetterCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(quarter.LetterCount, number, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                {
                    return false;
                }
            }
            else if (query.LetterCountNumberType == NumberType.None)
            {
                if (query.LetterCount > 0)
                {
                    if (query.LetterCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        int verse_sum; int word_sum; int letter_sum;
                        CalculateSums(quarter, out verse_sum, out word_sum, out letter_sum);
                        if (!Numbers.Compare(letter_sum, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(quarter.LetterCount, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.LetterCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    int verse_sum; int word_sum; int letter_sum;
                    CalculateSums(quarter, out verse_sum, out word_sum, out letter_sum);
                    if (!Numbers.IsNumberType(letter_sum, query.LetterCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(quarter.LetterCount, query.LetterCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.UniqueLetterCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(quarter.UniqueLetters.Count, number, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                {
                    return false;
                }
            }
            else if (query.UniqueLetterCountNumberType == NumberType.None)
            {
                if (query.UniqueLetterCount > 0)
                {
                    if (!Numbers.Compare(quarter.UniqueLetters.Count, query.UniqueLetterCount, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(quarter.UniqueLetters.Count, query.UniqueLetterCountNumberType))
                {
                    return false;
                }
            }

            long value = CalculateValue(quarter.Verses);
            if (query.ValueNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(value, number, query.ValueComparisonOperator, query.ValueRemainder))
                {
                    return false;
                }
            }
            else if (query.ValueNumberType == NumberType.None)
            {
                if (query.Value > 0)
                {
                    if (!Numbers.Compare(value, query.Value, query.ValueComparisonOperator, query.ValueRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(value, query.ValueNumberType))
                {
                    return false;
                }
            }
        }

        // passed all tests successfully
        return true;
    }
    private static bool Compare(Bowing bowing, NumberQuery query)
    {
        if (bowing != null)
        {
            int number = bowing.Number;
            if (query.NumberNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(number, number, query.NumberComparisonOperator, query.NumberRemainder))
                {
                    return false;
                }
            }
            else if (query.NumberNumberType == NumberType.None)
            {
                if (query.Number != 0)
                {
                    if (query.Number < 0)
                    {
                        switch (query.NumberScope)
                        {
                            case NumberScope.Number:
                                query.Number = Book.Bowings.Count + query.Number + 1;
                                break;
                            default:
                                query.Number = Book.Bowings.Count + query.Number + 1;
                                break;
                        }
                    }

                    if (query.Number < 0)
                    {
                        query.Number = number + query.Number + 1;
                    }

                    if (query.Number > 0)
                    {
                        if (!Numbers.Compare(number, query.Number, query.NumberComparisonOperator, query.NumberRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false; // number_out_of_range
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(number, query.NumberNumberType))
                {
                    return false;
                }
            }

            if (query.VerseCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(bowing.Verses.Count, number, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                {
                    return false;
                }
            }
            else if (query.VerseCountNumberType == NumberType.None)
            {
                if (query.VerseCount > 0)
                {
                    if (query.VerseCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        int verse_sum; int word_sum; int letter_sum;
                        CalculateSums(bowing, out verse_sum, out word_sum, out letter_sum);
                        if (!Numbers.Compare(verse_sum, query.VerseCount, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(bowing.Verses.Count, query.VerseCount, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.VerseCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    int verse_sum; int word_sum; int letter_sum;
                    CalculateSums(bowing, out verse_sum, out word_sum, out letter_sum);
                    if (!Numbers.IsNumberType(verse_sum, query.VerseCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(bowing.Verses.Count, query.VerseCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.WordCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(bowing.WordCount, number, query.WordCountComparisonOperator, query.WordCountRemainder))
                {
                    return false;
                }
            }
            else if (query.WordCountNumberType == NumberType.None)
            {
                if (query.WordCount > 0)
                {
                    if (query.WordCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        int verse_sum; int word_sum; int letter_sum;
                        CalculateSums(bowing, out verse_sum, out word_sum, out letter_sum);
                        if (!Numbers.Compare(word_sum, query.WordCount, query.WordCountComparisonOperator, query.WordCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(bowing.WordCount, query.WordCount, query.WordCountComparisonOperator, query.WordCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.WordCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    int verse_sum; int word_sum; int letter_sum;
                    CalculateSums(bowing, out verse_sum, out word_sum, out letter_sum);
                    if (!Numbers.IsNumberType(word_sum, query.WordCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(bowing.WordCount, query.WordCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.LetterCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(bowing.LetterCount, number, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                {
                    return false;
                }
            }
            else if (query.LetterCountNumberType == NumberType.None)
            {
                if (query.LetterCount > 0)
                {
                    if (query.LetterCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        int verse_sum; int word_sum; int letter_sum;
                        CalculateSums(bowing, out verse_sum, out word_sum, out letter_sum);
                        if (!Numbers.Compare(letter_sum, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(bowing.LetterCount, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.LetterCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    int verse_sum; int word_sum; int letter_sum;
                    CalculateSums(bowing, out verse_sum, out word_sum, out letter_sum);
                    if (!Numbers.IsNumberType(letter_sum, query.LetterCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(bowing.LetterCount, query.LetterCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.UniqueLetterCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(bowing.UniqueLetters.Count, number, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                {
                    return false;
                }
            }
            else if (query.UniqueLetterCountNumberType == NumberType.None)
            {
                if (query.UniqueLetterCount > 0)
                {
                    if (!Numbers.Compare(bowing.UniqueLetters.Count, query.UniqueLetterCount, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(bowing.UniqueLetters.Count, query.UniqueLetterCountNumberType))
                {
                    return false;
                }
            }

            long value = CalculateValue(bowing.Verses);
            if (query.ValueNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(value, number, query.ValueComparisonOperator, query.ValueRemainder))
                {
                    return false;
                }
            }
            else if (query.ValueNumberType == NumberType.None)
            {
                if (query.Value > 0)
                {
                    if (!Numbers.Compare(value, query.Value, query.ValueComparisonOperator, query.ValueRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(value, query.ValueNumberType))
                {
                    return false;
                }
            }
        }

        // passed all tests successfully
        return true;
    }
    private static bool Compare(List<Letter> letters, NumberQuery query)
    {
        if (letters != null)
        {
            long value = 0L;

            int sum = 0;
            switch (query.NumberScope)
            {
                case NumberScope.Number:
                    foreach (Letter letter in letters)
                    {
                        sum += letter.Number;
                    }
                    break;
                case NumberScope.NumberInChapter:
                    foreach (Letter letter in letters)
                    {
                        sum += letter.NumberInChapter;
                    }
                    break;
                case NumberScope.NumberInVerse:
                    foreach (Letter letter in letters)
                    {
                        sum += letter.NumberInVerse;
                    }
                    break;
                case NumberScope.NumberInWord:
                    foreach (Letter letter in letters)
                    {
                        sum += letter.NumberInWord;
                    }
                    break;
                default:
                    foreach (Letter letter in letters)
                    {
                        sum += letter.NumberInVerse;
                    }
                    break;
            }
            if (query.NumberNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(sum, sum, query.NumberComparisonOperator, query.NumberRemainder))
                {
                    return false;
                }
            }
            else if (query.NumberNumberType == NumberType.None)
            {
                if (query.Number > 0)
                {
                    if (!Numbers.Compare(sum, query.Number, query.NumberComparisonOperator, query.NumberRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(sum, query.NumberNumberType))
                {
                    return false;
                }
            }

            if (value == 0L)
            {
                foreach (Letter letter in letters)
                {
                    value += CalculateValue(letter);
                }
            }
            if (query.ValueNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(value, sum, query.ValueComparisonOperator, query.ValueRemainder))
                {
                    return false;
                }
            }
            else if (query.ValueNumberType == NumberType.None)
            {
                if (query.Value > 0)
                {
                    if (!Numbers.Compare(value, query.Value, query.ValueComparisonOperator, query.ValueRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(value, query.ValueNumberType))
                {
                    return false;
                }
            }
        }

        // passed all tests successfully
        return true;
    }
    private static bool Compare(List<Word> words, NumberQuery query)
    {
        if (words != null)
        {
            long value = 0L;

            int sum = 0;
            switch (query.NumberScope)
            {
                case NumberScope.Number:
                    foreach (Word word in words)
                    {
                        sum += word.Number;
                    }
                    break;
                case NumberScope.NumberInChapter:
                    foreach (Word word in words)
                    {
                        sum += word.NumberInChapter;
                    }
                    break;
                case NumberScope.NumberInVerse:
                    foreach (Word word in words)
                    {
                        sum += word.NumberInVerse;
                    }
                    break;
                default:
                    foreach (Word word in words)
                    {
                        sum += word.NumberInVerse;
                    }
                    break;
            }
            if (query.NumberNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(sum, sum, query.NumberComparisonOperator, query.NumberRemainder))
                {
                    return false;
                }
            }
            else if (query.NumberNumberType == NumberType.None)
            {
                if (query.Number > 0)
                {
                    if (!Numbers.Compare(sum, query.Number, query.NumberComparisonOperator, query.NumberRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(sum, query.NumberNumberType))
                {
                    return false;
                }
            }

            sum = 0;
            foreach (Word word in words)
            {
                sum += word.Letters.Count;
            }
            if (query.LetterCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(sum, sum, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                {
                    return false;
                }
            }
            else if (query.LetterCountNumberType == NumberType.None)
            {
                if (query.LetterCount > 0)
                {
                    if (query.LetterCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        int word_sum; int letter_sum;
                        CalculateSums(words, out word_sum, out letter_sum);
                        if (!Numbers.Compare(letter_sum, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(sum, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.LetterCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    int word_sum; int letter_sum;
                    CalculateSums(words, out word_sum, out letter_sum);
                    if (!Numbers.IsNumberType(letter_sum, query.LetterCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(sum, query.LetterCountNumberType))
                    {
                        return false;
                    }
                }
            }

            List<char> unique_letters = new List<char>();
            foreach (Word word in words)
            {
                foreach (char character in word.UniqueLetters)
                {
                    if (!unique_letters.Contains(character))
                    {
                        unique_letters.Add(character);
                    }
                }
            }
            if (query.UniqueLetterCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(unique_letters.Count, sum, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                {
                    return false;
                }
            }
            else if (query.UniqueLetterCountNumberType == NumberType.None)
            {
                if (query.UniqueLetterCount > 0)
                {
                    if (!Numbers.Compare(unique_letters.Count, query.UniqueLetterCount, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(unique_letters.Count, query.UniqueLetterCountNumberType))
                {
                    return false;
                }
            }

            if (value == 0L)
            {
                foreach (Word word in words)
                {
                    value += CalculateValue(word);
                }
            }
            if (query.ValueNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(value, sum, query.ValueComparisonOperator, query.ValueRemainder))
                {
                    return false;
                }
            }
            else if (query.ValueNumberType == NumberType.None)
            {
                if (query.Value > 0)
                {
                    if (!Numbers.Compare(value, query.Value, query.ValueComparisonOperator, query.ValueRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(value, query.ValueNumberType))
                {
                    return false;
                }
            }
        }

        // passed all tests successfully
        return true;
    }
    private static bool Compare(List<Verse> verses, NumberQuery query)
    {
        if (verses != null)
        {
            long value = 0L;

            int sum = 0;
            switch (query.NumberScope)
            {
                case NumberScope.Number:
                    foreach (Verse verse in verses)
                    {
                        sum += verse.Number;
                    }
                    break;
                case NumberScope.NumberInChapter:
                    foreach (Verse verse in verses)
                    {
                        sum += verse.NumberInChapter;
                    }
                    break;
                default:
                    foreach (Verse verse in verses)
                    {
                        sum += verse.NumberInChapter;
                    }
                    break;
            }
            if (query.NumberNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(sum, sum, query.NumberComparisonOperator, query.NumberRemainder))
                {
                    return false;
                }
            }
            else if (query.NumberNumberType == NumberType.None)
            {
                if (query.Number > 0)
                {
                    if (!Numbers.Compare(sum, query.Number, query.NumberComparisonOperator, query.NumberRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(sum, query.NumberNumberType))
                {
                    return false;
                }
            }

            sum = 0;
            foreach (Verse verse in verses)
            {
                sum += verse.Words.Count;
            }
            if (query.WordCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(sum, sum, query.WordCountComparisonOperator, query.WordCountRemainder))
                {
                    return false;
                }
            }
            else if (query.WordCountNumberType == NumberType.None)
            {
                if (query.WordCount > 0)
                {
                    if (query.WordCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        int chapter_sum; int verse_sum; int word_sum; int letter_sum;
                        CalculateSums(verses, out chapter_sum, out verse_sum, out word_sum, out letter_sum);
                        if (!Numbers.Compare(word_sum, query.WordCount, query.WordCountComparisonOperator, query.WordCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(sum, query.WordCount, query.WordCountComparisonOperator, query.WordCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.WordCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    int chapter_sum; int verse_sum; int word_sum; int letter_sum;
                    CalculateSums(verses, out chapter_sum, out verse_sum, out word_sum, out letter_sum);
                    if (!Numbers.IsNumberType(word_sum, query.WordCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(sum, query.WordCountNumberType))
                    {
                        return false;
                    }
                }
            }

            sum = 0;
            foreach (Verse verse in verses)
            {
                sum += verse.LetterCount;
            }
            if (query.LetterCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(sum, sum, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                {
                    return false;
                }
            }
            else if (query.LetterCountNumberType == NumberType.None)
            {
                if (query.LetterCount > 0)
                {
                    if (query.LetterCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        int chapter_sum; int verse_sum; int word_sum; int letter_sum;
                        CalculateSums(verses, out chapter_sum, out verse_sum, out word_sum, out letter_sum);
                        if (!Numbers.Compare(letter_sum, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(sum, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.LetterCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    int chapter_sum; int verse_sum; int word_sum; int letter_sum;
                    CalculateSums(verses, out chapter_sum, out verse_sum, out word_sum, out letter_sum);
                    if (!Numbers.IsNumberType(letter_sum, query.LetterCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(sum, query.LetterCountNumberType))
                    {
                        return false;
                    }
                }
            }

            List<char> unique_letters = new List<char>();
            foreach (Verse verse in verses)
            {
                foreach (char character in verse.UniqueLetters)
                {
                    if (!unique_letters.Contains(character))
                    {
                        unique_letters.Add(character);
                    }
                }
            }
            if (query.UniqueLetterCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(unique_letters.Count, sum, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                {
                    return false;
                }
            }
            else if (query.UniqueLetterCountNumberType == NumberType.None)
            {
                if (query.UniqueLetterCount > 0)
                {
                    if (!Numbers.Compare(unique_letters.Count, query.UniqueLetterCount, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(unique_letters.Count, query.UniqueLetterCountNumberType))
                {
                    return false;
                }
            }

            if (value == 0L)
            {
                foreach (Verse verse in verses)
                {
                    value += CalculateValue(verse);
                }
            }
            if (query.ValueNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(value, sum, query.ValueComparisonOperator, query.ValueRemainder))
                {
                    return false;
                }
            }
            else if (query.ValueNumberType == NumberType.None)
            {
                if (query.Value > 0)
                {
                    if (!Numbers.Compare(value, query.Value, query.ValueComparisonOperator, query.ValueRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(value, query.ValueNumberType))
                {
                    return false;
                }
            }
        }

        // passed all tests successfully
        return true;
    }
    private static bool Compare(List<Chapter> chapters, NumberQuery query)
    {
        if (chapters != null)
        {
            int chapter_sum; int verse_sum; int word_sum; int letter_sum;
            CalculateSums(chapters, out chapter_sum, out verse_sum, out word_sum, out letter_sum);

            long value = 0L;
            int sum = 0;
            foreach (Chapter chapter in chapters)
            {
                sum += chapter.Number;
            }
            if (query.NumberNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(sum, sum, query.NumberComparisonOperator, query.NumberRemainder))
                {
                    return false;
                }
            }
            else if (query.NumberNumberType == NumberType.None)
            {
                if (query.Number > 0)
                {
                    if (!Numbers.Compare(sum, query.Number, query.NumberComparisonOperator, query.NumberRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(sum, query.NumberNumberType))
                {
                    return false;
                }
            }

            sum = 0;
            foreach (Chapter chapter in chapters)
            {
                sum += chapter.Verses.Count;
            }
            if (query.VerseCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(verse_sum, sum, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                {
                    return false;
                }
            }
            else if (query.VerseCountNumberType == NumberType.None)
            {
                if (query.VerseCount > 0)
                {
                    if (query.VerseCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        if (!Numbers.Compare(verse_sum, query.VerseCount, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(sum, query.VerseCount, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.VerseCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    if (!Numbers.IsNumberType(verse_sum, query.VerseCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(sum, query.VerseCountNumberType))
                    {
                        return false;
                    }
                }
            }

            sum = 0;
            foreach (Chapter chapter in chapters)
            {
                sum += chapter.WordCount;
            }
            if (query.WordCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(word_sum, sum, query.WordCountComparisonOperator, query.WordCountRemainder))
                {
                    return false;
                }
            }
            else if (query.WordCountNumberType == NumberType.None)
            {
                if (query.WordCount > 0)
                {
                    if (query.WordCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        if (!Numbers.Compare(word_sum, query.WordCount, query.WordCountComparisonOperator, query.WordCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(sum, query.WordCount, query.WordCountComparisonOperator, query.WordCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.WordCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    if (!Numbers.IsNumberType(word_sum, query.WordCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(sum, query.WordCountNumberType))
                    {
                        return false;
                    }
                }
            }

            sum = 0;
            foreach (Chapter chapter in chapters)
            {
                sum += chapter.LetterCount;
            }
            if (query.LetterCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(letter_sum, sum, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                {
                    return false;
                }
            }
            else if (query.LetterCountNumberType == NumberType.None)
            {
                if (query.LetterCount > 0)
                {
                    if (query.LetterCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        if (!Numbers.Compare(letter_sum, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(sum, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.LetterCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    if (!Numbers.IsNumberType(letter_sum, query.LetterCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(sum, query.LetterCountNumberType))
                    {
                        return false;
                    }
                }
            }

            List<char> unique_letters = new List<char>();
            foreach (Chapter chapter in chapters)
            {
                foreach (char character in chapter.UniqueLetters)
                {
                    if (!unique_letters.Contains(character))
                    {
                        unique_letters.Add(character);
                    }
                }
            }
            if (query.UniqueLetterCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(unique_letters.Count, sum, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                {
                    return false;
                }
            }
            else if (query.UniqueLetterCountNumberType == NumberType.None)
            {
                if (query.UniqueLetterCount > 0)
                {
                    if (!Numbers.Compare(unique_letters.Count, query.UniqueLetterCount, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(unique_letters.Count, query.UniqueLetterCountNumberType))
                {
                    return false;
                }
            }

            if (value == 0L)
            {
                foreach (Chapter chapter in chapters)
                {
                    value += CalculateValue(chapter);
                }
            }
            if (query.ValueNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(value, sum, query.ValueComparisonOperator, query.ValueRemainder))
                {
                    return false;
                }
            }
            else if (query.ValueNumberType == NumberType.None)
            {
                if (query.Value > 0)
                {
                    if (!Numbers.Compare(value, query.Value, query.ValueComparisonOperator, query.ValueRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(value, query.ValueNumberType))
                {
                    return false;
                }
            }
        }

        // passed all tests successfully
        return true;
    }
    private static bool Compare(List<Page> pages, NumberQuery query)
    {
        if (pages != null)
        {
            int page_sum; int verse_sum; int word_sum; int letter_sum;
            CalculateSums(pages, out page_sum, out verse_sum, out word_sum, out letter_sum);

            long value = 0L;
            int sum = 0;
            foreach (Page page in pages)
            {
                sum += page.Number;
            }
            if (query.NumberNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(sum, sum, query.NumberComparisonOperator, query.NumberRemainder))
                {
                    return false;
                }
            }
            else if (query.NumberNumberType == NumberType.None)
            {
                if (query.Number > 0)
                {
                    if (!Numbers.Compare(sum, query.Number, query.NumberComparisonOperator, query.NumberRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(sum, query.NumberNumberType))
                {
                    return false;
                }
            }

            sum = 0;
            foreach (Page page in pages)
            {
                sum += page.Verses.Count;
            }
            if (query.VerseCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(verse_sum, sum, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                {
                    return false;
                }
            }
            else if (query.VerseCountNumberType == NumberType.None)
            {
                if (query.VerseCount > 0)
                {
                    if (query.VerseCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        if (!Numbers.Compare(verse_sum, query.VerseCount, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(sum, query.VerseCount, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.VerseCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    if (!Numbers.IsNumberType(verse_sum, query.VerseCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(sum, query.VerseCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.WordCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(word_sum, sum, query.WordCountComparisonOperator, query.WordCountRemainder))
                {
                    return false;
                }
            }
            else if (query.WordCountNumberType == NumberType.None)
            {
                if (query.WordCount > 0)
                {
                    if (query.WordCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        if (!Numbers.Compare(word_sum, query.WordCount, query.WordCountComparisonOperator, query.WordCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(sum, query.WordCount, query.WordCountComparisonOperator, query.WordCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.WordCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    if (!Numbers.IsNumberType(word_sum, query.WordCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(sum, query.WordCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.LetterCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(letter_sum, sum, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                {
                    return false;
                }
            }
            else if (query.LetterCountNumberType == NumberType.None)
            {
                if (query.LetterCount > 0)
                {
                    if (query.LetterCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        if (!Numbers.Compare(letter_sum, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(sum, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.LetterCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    if (!Numbers.IsNumberType(letter_sum, query.LetterCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(sum, query.LetterCountNumberType))
                    {
                        return false;
                    }
                }
            }

            List<char> unique_letters = new List<char>();
            foreach (Page page in pages)
            {
                foreach (char character in page.UniqueLetters)
                {
                    if (!unique_letters.Contains(character))
                    {
                        unique_letters.Add(character);
                    }
                }
            }
            if (query.UniqueLetterCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(unique_letters.Count, sum, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                {
                    return false;
                }
            }
            else if (query.UniqueLetterCountNumberType == NumberType.None)
            {
                if (query.UniqueLetterCount > 0)
                {
                    if (!Numbers.Compare(unique_letters.Count, query.UniqueLetterCount, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(unique_letters.Count, query.UniqueLetterCountNumberType))
                {
                    return false;
                }
            }

            if (value == 0L)
            {
                foreach (Page page in pages)
                {
                    value += CalculateValue(page.Verses);
                }
            }
            if (query.ValueNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(value, sum, query.ValueComparisonOperator, query.ValueRemainder))
                {
                    return false;
                }
            }
            else if (query.ValueNumberType == NumberType.None)
            {
                if (query.Value > 0)
                {
                    if (!Numbers.Compare(value, query.Value, query.ValueComparisonOperator, query.ValueRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(value, query.ValueNumberType))
                {
                    return false;
                }
            }
        }

        // passed all tests successfully
        return true;
    }
    private static bool Compare(List<Station> stations, NumberQuery query)
    {
        if (stations != null)
        {
            long value = 0L;
            int sum = 0;
            foreach (Station station in stations)
            {
                sum += station.Number;
            }
            if (query.NumberNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(sum, sum, query.NumberComparisonOperator, query.NumberRemainder))
                {
                    return false;
                }
            }
            else if (query.NumberNumberType == NumberType.None)
            {
                if (query.Number > 0)
                {
                    if (!Numbers.Compare(sum, query.Number, query.NumberComparisonOperator, query.NumberRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(sum, query.NumberNumberType))
                {
                    return false;
                }
            }

            int station_sum; int verse_sum; int word_sum; int letter_sum;
            CalculateSums(stations, out station_sum, out verse_sum, out word_sum, out letter_sum);
            sum = 0;
            foreach (Station station in stations)
            {
                sum += station.Verses.Count;
            }
            if (query.VerseCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(verse_sum, sum, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                {
                    return false;
                }
            }
            else if (query.VerseCountNumberType == NumberType.None)
            {
                if (query.VerseCount > 0)
                {
                    if (query.VerseCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        if (!Numbers.Compare(verse_sum, query.VerseCount, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(sum, query.VerseCount, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.VerseCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    if (!Numbers.IsNumberType(verse_sum, query.VerseCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(sum, query.VerseCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.WordCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(word_sum, sum, query.WordCountComparisonOperator, query.WordCountRemainder))
                {
                    return false;
                }
            }
            else if (query.WordCountNumberType == NumberType.None)
            {
                if (query.WordCount > 0)
                {
                    if (query.WordCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        if (!Numbers.Compare(word_sum, query.WordCount, query.WordCountComparisonOperator, query.WordCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(sum, query.WordCount, query.WordCountComparisonOperator, query.WordCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.WordCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    if (!Numbers.IsNumberType(word_sum, query.WordCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(sum, query.WordCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.LetterCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(letter_sum, sum, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                {
                    return false;
                }
            }
            else if (query.LetterCountNumberType == NumberType.None)
            {
                if (query.LetterCount > 0)
                {
                    if (query.LetterCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        if (!Numbers.Compare(letter_sum, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(sum, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.LetterCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    if (!Numbers.IsNumberType(letter_sum, query.LetterCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(sum, query.LetterCountNumberType))
                    {
                        return false;
                    }
                }
            }

            List<char> unique_letters = new List<char>();
            foreach (Station station in stations)
            {
                foreach (char character in station.UniqueLetters)
                {
                    if (!unique_letters.Contains(character))
                    {
                        unique_letters.Add(character);
                    }
                }
            }
            if (query.UniqueLetterCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(unique_letters.Count, sum, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                {
                    return false;
                }
            }
            else if (query.UniqueLetterCountNumberType == NumberType.None)
            {
                if (query.UniqueLetterCount > 0)
                {
                    if (!Numbers.Compare(unique_letters.Count, query.UniqueLetterCount, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(unique_letters.Count, query.UniqueLetterCountNumberType))
                {
                    return false;
                }
            }

            if (value == 0L)
            {
                foreach (Station station in stations)
                {
                    value += CalculateValue(station.Verses);
                }
            }
            if (query.ValueNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(value, sum, query.ValueComparisonOperator, query.ValueRemainder))
                {
                    return false;
                }
            }
            else if (query.ValueNumberType == NumberType.None)
            {
                if (query.Value > 0)
                {
                    if (!Numbers.Compare(value, query.Value, query.ValueComparisonOperator, query.ValueRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(value, query.ValueNumberType))
                {
                    return false;
                }
            }
        }

        // passed all tests successfully
        return true;
    }
    private static bool Compare(List<Part> parts, NumberQuery query)
    {
        if (parts != null)
        {
            long value = 0L;
            int sum = 0;
            foreach (Part part in parts)
            {
                sum += part.Number;
            }
            if (query.NumberNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(sum, sum, query.NumberComparisonOperator, query.NumberRemainder))
                {
                    return false;
                }
            }
            else if (query.NumberNumberType == NumberType.None)
            {
                if (query.Number > 0)
                {
                    if (!Numbers.Compare(sum, query.Number, query.NumberComparisonOperator, query.NumberRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(sum, query.NumberNumberType))
                {
                    return false;
                }
            }

            int part_sum; int verse_sum; int word_sum; int letter_sum;
            CalculateSums(parts, out part_sum, out verse_sum, out word_sum, out letter_sum);
            sum = 0;
            foreach (Part part in parts)
            {
                sum += part.Verses.Count;
            }
            if (query.VerseCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(verse_sum, sum, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                {
                    return false;
                }
            }
            else if (query.VerseCountNumberType == NumberType.None)
            {
                if (query.VerseCount > 0)
                {
                    if (query.VerseCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        if (!Numbers.Compare(verse_sum, query.VerseCount, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(sum, query.VerseCount, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.VerseCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    if (!Numbers.IsNumberType(verse_sum, query.VerseCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(sum, query.VerseCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.WordCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(word_sum, sum, query.WordCountComparisonOperator, query.WordCountRemainder))
                {
                    return false;
                }
            }
            else if (query.WordCountNumberType == NumberType.None)
            {
                if (query.WordCount > 0)
                {
                    if (query.WordCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        if (!Numbers.Compare(word_sum, query.WordCount, query.WordCountComparisonOperator, query.WordCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(sum, query.WordCount, query.WordCountComparisonOperator, query.WordCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.WordCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    if (!Numbers.IsNumberType(word_sum, query.WordCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(sum, query.WordCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.LetterCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(letter_sum, sum, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                {
                    return false;
                }
            }
            else if (query.LetterCountNumberType == NumberType.None)
            {
                if (query.LetterCount > 0)
                {
                    if (query.LetterCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        if (!Numbers.Compare(letter_sum, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(sum, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.LetterCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    if (!Numbers.IsNumberType(letter_sum, query.LetterCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(sum, query.LetterCountNumberType))
                    {
                        return false;
                    }
                }
            }

            List<char> unique_letters = new List<char>();
            foreach (Part part in parts)
            {
                foreach (char character in part.UniqueLetters)
                {
                    if (!unique_letters.Contains(character))
                    {
                        unique_letters.Add(character);
                    }
                }
            }
            if (query.UniqueLetterCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(unique_letters.Count, sum, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                {
                    return false;
                }
            }
            else if (query.UniqueLetterCountNumberType == NumberType.None)
            {
                if (query.UniqueLetterCount > 0)
                {
                    if (!Numbers.Compare(unique_letters.Count, query.UniqueLetterCount, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(unique_letters.Count, query.UniqueLetterCountNumberType))
                {
                    return false;
                }
            }

            if (value == 0L)
            {
                foreach (Part part in parts)
                {
                    value += CalculateValue(part.Verses);
                }
            }
            if (query.ValueNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(value, sum, query.ValueComparisonOperator, query.ValueRemainder))
                {
                    return false;
                }
            }
            else if (query.ValueNumberType == NumberType.None)
            {
                if (query.Value > 0)
                {
                    if (!Numbers.Compare(value, query.Value, query.ValueComparisonOperator, query.ValueRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(value, query.ValueNumberType))
                {
                    return false;
                }
            }
        }

        // passed all tests successfully
        return true;
    }
    private static bool Compare(List<Model.Group> groups, NumberQuery query)
    {
        if (groups != null)
        {
            long value = 0L;
            int sum = 0;
            foreach (Model.Group group in groups)
            {
                sum += group.Number;
            }
            if (query.NumberNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(sum, sum, query.NumberComparisonOperator, query.NumberRemainder))
                {
                    return false;
                }
            }
            else if (query.NumberNumberType == NumberType.None)
            {
                if (query.Number > 0)
                {
                    if (!Numbers.Compare(sum, query.Number, query.NumberComparisonOperator, query.NumberRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(sum, query.NumberNumberType))
                {
                    return false;
                }
            }

            int group_sum; int verse_sum; int word_sum; int letter_sum;
            CalculateSums(groups, out group_sum, out verse_sum, out word_sum, out letter_sum);
            sum = 0;
            foreach (Model.Group group in groups)
            {
                sum += group.Verses.Count;
            }
            if (query.VerseCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(verse_sum, sum, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                {
                    return false;
                }
            }
            else if (query.VerseCountNumberType == NumberType.None)
            {
                if (query.VerseCount > 0)
                {
                    if (query.VerseCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        if (!Numbers.Compare(verse_sum, query.VerseCount, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(sum, query.VerseCount, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.VerseCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    if (!Numbers.IsNumberType(verse_sum, query.VerseCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(sum, query.VerseCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.WordCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(word_sum, sum, query.WordCountComparisonOperator, query.WordCountRemainder))
                {
                    return false;
                }
            }
            else if (query.WordCountNumberType == NumberType.None)
            {
                if (query.WordCount > 0)
                {
                    if (query.WordCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        if (!Numbers.Compare(word_sum, query.WordCount, query.WordCountComparisonOperator, query.WordCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(sum, query.WordCount, query.WordCountComparisonOperator, query.WordCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.WordCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    if (!Numbers.IsNumberType(word_sum, query.WordCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(sum, query.WordCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.LetterCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(letter_sum, sum, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                {
                    return false;
                }
            }
            else if (query.LetterCountNumberType == NumberType.None)
            {
                if (query.LetterCount > 0)
                {
                    if (query.LetterCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        if (!Numbers.Compare(letter_sum, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(sum, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.LetterCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    if (!Numbers.IsNumberType(letter_sum, query.LetterCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(sum, query.LetterCountNumberType))
                    {
                        return false;
                    }
                }
            }

            List<char> unique_letters = new List<char>();
            foreach (Model.Group group in groups)
            {
                foreach (char character in group.UniqueLetters)
                {
                    if (!unique_letters.Contains(character))
                    {
                        unique_letters.Add(character);
                    }
                }
            }
            if (query.UniqueLetterCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(unique_letters.Count, sum, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                {
                    return false;
                }
            }
            else if (query.UniqueLetterCountNumberType == NumberType.None)
            {
                if (query.UniqueLetterCount > 0)
                {
                    if (!Numbers.Compare(unique_letters.Count, query.UniqueLetterCount, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(unique_letters.Count, query.UniqueLetterCountNumberType))
                {
                    return false;
                }
            }

            if (value == 0L)
            {
                foreach (Model.Group group in groups)
                {
                    value += CalculateValue(group.Verses);
                }
            }
            if (query.ValueNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(value, sum, query.ValueComparisonOperator, query.ValueRemainder))
                {
                    return false;
                }
            }
            else if (query.ValueNumberType == NumberType.None)
            {
                if (query.Value > 0)
                {
                    if (!Numbers.Compare(value, query.Value, query.ValueComparisonOperator, query.ValueRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(value, query.ValueNumberType))
                {
                    return false;
                }
            }
        }

        // passed all tests successfully
        return true;
    }
    private static bool Compare(List<Half> halfs, NumberQuery query)
    {
        if (halfs != null)
        {
            long value = 0L;
            int sum = 0;
            foreach (Half half in halfs)
            {
                sum += half.Number;
            }
            if (query.NumberNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(sum, sum, query.NumberComparisonOperator, query.NumberRemainder))
                {
                    return false;
                }
            }
            else if (query.NumberNumberType == NumberType.None)
            {
                if (query.Number > 0)
                {
                    if (!Numbers.Compare(sum, query.Number, query.NumberComparisonOperator, query.NumberRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(sum, query.NumberNumberType))
                {
                    return false;
                }
            }

            int half_sum; int verse_sum; int word_sum; int letter_sum;
            CalculateSums(halfs, out half_sum, out verse_sum, out word_sum, out letter_sum);
            sum = 0;
            foreach (Half half in halfs)
            {
                sum += half.Verses.Count;
            }
            if (query.VerseCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(verse_sum, sum, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                {
                    return false;
                }
            }
            else if (query.VerseCountNumberType == NumberType.None)
            {
                if (query.VerseCount > 0)
                {
                    if (query.VerseCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        if (!Numbers.Compare(verse_sum, query.VerseCount, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(sum, query.VerseCount, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.VerseCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    if (!Numbers.IsNumberType(verse_sum, query.VerseCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(sum, query.VerseCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.WordCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(word_sum, sum, query.WordCountComparisonOperator, query.WordCountRemainder))
                {
                    return false;
                }
            }
            else if (query.WordCountNumberType == NumberType.None)
            {
                if (query.WordCount > 0)
                {
                    if (query.WordCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        if (!Numbers.Compare(word_sum, query.WordCount, query.WordCountComparisonOperator, query.WordCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(sum, query.WordCount, query.WordCountComparisonOperator, query.WordCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.WordCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    if (!Numbers.IsNumberType(word_sum, query.WordCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(sum, query.WordCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.LetterCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(letter_sum, sum, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                {
                    return false;
                }
            }
            else if (query.LetterCountNumberType == NumberType.None)
            {
                if (query.LetterCount > 0)
                {
                    if (query.LetterCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        if (!Numbers.Compare(letter_sum, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(sum, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.LetterCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    if (!Numbers.IsNumberType(letter_sum, query.LetterCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(sum, query.LetterCountNumberType))
                    {
                        return false;
                    }
                }
            }

            List<char> unique_letters = new List<char>();
            foreach (Half half in halfs)
            {
                foreach (char character in half.UniqueLetters)
                {
                    if (!unique_letters.Contains(character))
                    {
                        unique_letters.Add(character);
                    }
                }
            }
            if (query.UniqueLetterCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(unique_letters.Count, sum, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                {
                    return false;
                }
            }
            else if (query.UniqueLetterCountNumberType == NumberType.None)
            {
                if (query.UniqueLetterCount > 0)
                {
                    if (!Numbers.Compare(unique_letters.Count, query.UniqueLetterCount, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(unique_letters.Count, query.UniqueLetterCountNumberType))
                {
                    return false;
                }
            }

            if (value == 0L)
            {
                foreach (Half half in halfs)
                {
                    value += CalculateValue(half.Verses);
                }
            }
            if (query.ValueNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(value, sum, query.ValueComparisonOperator, query.ValueRemainder))
                {
                    return false;
                }
            }
            else if (query.ValueNumberType == NumberType.None)
            {
                if (query.Value > 0)
                {
                    if (!Numbers.Compare(value, query.Value, query.ValueComparisonOperator, query.ValueRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(value, query.ValueNumberType))
                {
                    return false;
                }
            }
        }

        // passed all tests successfully
        return true;
    }
    private static bool Compare(List<Quarter> quarters, NumberQuery query)
    {
        if (quarters != null)
        {
            long value = 0L;
            int sum = 0;
            foreach (Quarter quarter in quarters)
            {
                sum += quarter.Number;
            }
            if (query.NumberNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(sum, sum, query.NumberComparisonOperator, query.NumberRemainder))
                {
                    return false;
                }
            }
            else if (query.NumberNumberType == NumberType.None)
            {
                if (query.Number > 0)
                {
                    if (!Numbers.Compare(sum, query.Number, query.NumberComparisonOperator, query.NumberRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(sum, query.NumberNumberType))
                {
                    return false;
                }
            }

            int quarter_sum; int verse_sum; int word_sum; int letter_sum;
            CalculateSums(quarters, out quarter_sum, out verse_sum, out word_sum, out letter_sum);
            sum = 0;
            foreach (Quarter quarter in quarters)
            {
                sum += quarter.Verses.Count;
            }
            if (query.VerseCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(verse_sum, sum, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                {
                    return false;
                }
            }
            else if (query.VerseCountNumberType == NumberType.None)
            {
                if (query.VerseCount > 0)
                {
                    if (query.VerseCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        if (!Numbers.Compare(verse_sum, query.VerseCount, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(sum, query.VerseCount, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.VerseCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    if (!Numbers.IsNumberType(verse_sum, query.VerseCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(sum, query.VerseCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.WordCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(word_sum, sum, query.WordCountComparisonOperator, query.WordCountRemainder))
                {
                    return false;
                }
            }
            else if (query.WordCountNumberType == NumberType.None)
            {
                if (query.WordCount > 0)
                {
                    if (query.WordCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        if (!Numbers.Compare(word_sum, query.WordCount, query.WordCountComparisonOperator, query.WordCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(sum, query.WordCount, query.WordCountComparisonOperator, query.WordCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.WordCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    if (!Numbers.IsNumberType(word_sum, query.WordCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(sum, query.WordCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.LetterCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(letter_sum, sum, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                {
                    return false;
                }
            }
            else if (query.LetterCountNumberType == NumberType.None)
            {
                if (query.LetterCount > 0)
                {
                    if (query.LetterCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        if (!Numbers.Compare(letter_sum, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(sum, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.LetterCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    if (!Numbers.IsNumberType(letter_sum, query.LetterCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(sum, query.LetterCountNumberType))
                    {
                        return false;
                    }
                }
            }

            List<char> unique_letters = new List<char>();
            foreach (Quarter quarter in quarters)
            {
                foreach (char character in quarter.UniqueLetters)
                {
                    if (!unique_letters.Contains(character))
                    {
                        unique_letters.Add(character);
                    }
                }
            }
            if (query.UniqueLetterCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(unique_letters.Count, sum, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                {
                    return false;
                }
            }
            else if (query.UniqueLetterCountNumberType == NumberType.None)
            {
                if (query.UniqueLetterCount > 0)
                {
                    if (!Numbers.Compare(unique_letters.Count, query.UniqueLetterCount, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(unique_letters.Count, query.UniqueLetterCountNumberType))
                {
                    return false;
                }
            }

            if (value == 0L)
            {
                foreach (Quarter quarter in quarters)
                {
                    value += CalculateValue(quarter.Verses);
                }
            }
            if (query.ValueNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(value, sum, query.ValueComparisonOperator, query.ValueRemainder))
                {
                    return false;
                }
            }
            else if (query.ValueNumberType == NumberType.None)
            {
                if (query.Value > 0)
                {
                    if (!Numbers.Compare(value, query.Value, query.ValueComparisonOperator, query.ValueRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(value, query.ValueNumberType))
                {
                    return false;
                }
            }
        }

        // passed all tests successfully
        return true;
    }
    private static bool Compare(List<Bowing> bowings, NumberQuery query)
    {
        if (bowings != null)
        {
            long value = 0L;
            int sum = 0;
            foreach (Bowing bowing in bowings)
            {
                sum += bowing.Number;
            }
            if (query.NumberNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(sum, sum, query.NumberComparisonOperator, query.NumberRemainder))
                {
                    return false;
                }
            }
            else if (query.NumberNumberType == NumberType.None)
            {
                if (query.Number > 0)
                {
                    if (!Numbers.Compare(sum, query.Number, query.NumberComparisonOperator, query.NumberRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(sum, query.NumberNumberType))
                {
                    return false;
                }
            }

            int bowing_sum; int verse_sum; int word_sum; int letter_sum;
            CalculateSums(bowings, out bowing_sum, out verse_sum, out word_sum, out letter_sum);
            sum = 0;
            foreach (Bowing bowing in bowings)
            {
                sum += bowing.Verses.Count;
            }
            if (query.VerseCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(verse_sum, sum, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                {
                    return false;
                }
            }
            else if (query.VerseCountNumberType == NumberType.None)
            {
                if (query.VerseCount > 0)
                {
                    if (query.VerseCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        if (!Numbers.Compare(verse_sum, query.VerseCount, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(sum, query.VerseCount, query.VerseCountComparisonOperator, query.VerseCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.VerseCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    if (!Numbers.IsNumberType(verse_sum, query.VerseCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(sum, query.VerseCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.WordCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(word_sum, sum, query.WordCountComparisonOperator, query.WordCountRemainder))
                {
                    return false;
                }
            }
            else if (query.WordCountNumberType == NumberType.None)
            {
                if (query.WordCount > 0)
                {
                    if (query.WordCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        if (!Numbers.Compare(word_sum, query.WordCount, query.WordCountComparisonOperator, query.WordCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(sum, query.WordCount, query.WordCountComparisonOperator, query.WordCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.WordCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    if (!Numbers.IsNumberType(word_sum, query.WordCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(sum, query.WordCountNumberType))
                    {
                        return false;
                    }
                }
            }

            if (query.LetterCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(letter_sum, sum, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                {
                    return false;
                }
            }
            else if (query.LetterCountNumberType == NumberType.None)
            {
                if (query.LetterCount > 0)
                {
                    if (query.LetterCountComparisonOperator == ComparisonOperator.EqualSum)
                    {
                        if (!Numbers.Compare(letter_sum, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!Numbers.Compare(sum, query.LetterCount, query.LetterCountComparisonOperator, query.LetterCountRemainder))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (query.LetterCountComparisonOperator == ComparisonOperator.EqualSum)
                {
                    if (!Numbers.IsNumberType(letter_sum, query.LetterCountNumberType))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!Numbers.IsNumberType(sum, query.LetterCountNumberType))
                    {
                        return false;
                    }
                }
            }

            List<char> unique_letters = new List<char>();
            foreach (Bowing bowing in bowings)
            {
                foreach (char character in bowing.UniqueLetters)
                {
                    if (!unique_letters.Contains(character))
                    {
                        unique_letters.Add(character);
                    }
                }
            }
            if (query.UniqueLetterCountNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(unique_letters.Count, sum, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                {
                    return false;
                }
            }
            else if (query.UniqueLetterCountNumberType == NumberType.None)
            {
                if (query.UniqueLetterCount > 0)
                {
                    if (!Numbers.Compare(unique_letters.Count, query.UniqueLetterCount, query.UniqueLetterCountComparisonOperator, query.UniqueLetterCountRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(unique_letters.Count, query.UniqueLetterCountNumberType))
                {
                    return false;
                }
            }

            if (value == 0L)
            {
                foreach (Bowing bowing in bowings)
                {
                    value += CalculateValue(bowing.Verses);
                }
            }
            if (query.ValueNumberType == NumberType.Natural)
            {
                if (!Numbers.Compare(value, sum, query.ValueComparisonOperator, query.ValueRemainder))
                {
                    return false;
                }
            }
            else if (query.ValueNumberType == NumberType.None)
            {
                if (query.Value > 0)
                {
                    if (!Numbers.Compare(value, query.Value, query.ValueComparisonOperator, query.ValueRemainder))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (!Numbers.IsNumberType(value, query.ValueNumberType))
                {
                    return false;
                }
            }
        }

        // passed all tests successfully
        return true;
    }
    private static bool Compare(int number1, int number2, NumberType number_type, ComparisonOperator comparison_operator, int remainder)
    {
        if ((number_type == NumberType.None) || (number_type == NumberType.Natural))
        {
            if (Numbers.Compare(number1, number2, comparison_operator, remainder))
            {
                return true;
            }
        }
        else
        {
            if (Numbers.IsNumberType(number1, number_type))
            {
                return true;
            }
        }

        return false;
    }

    // find by numbers - Letters
    public static List<Letter> FindLetters(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        return DoFindLetters(search_scope, current_selection, previous_verses, query);
    }
    private static List<Letter> DoFindLetters(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_verses, TextLocationInChapter.Any);
        return DoFindLetters(source, query);
    }
    private static List<Letter> DoFindLetters(List<Verse> source, NumberQuery query)
    {
        List<Letter> result = new List<Letter>();

        if (source != null)
        {
            foreach (Verse verse in source)
            {
                foreach (Word word in verse.Words)
                {
                    foreach (Letter letter in word.Letters)
                    {
                        if (Compare(letter, query))
                        {
                            result.Add(letter);
                        }
                    }
                }
            }
        }

        return result;
    }
    // find by numbers - LetterRanges
    public static List<List<Letter>> FindLetterRanges(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        return DoFindLetterRanges(search_scope, current_selection, previous_verses, query);
    }
    private static List<List<Letter>> DoFindLetterRanges(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_verses, TextLocationInChapter.Any);
        return DoFindLetterRanges(source, query);
    }
    private static List<List<Letter>> DoFindLetterRanges(List<Verse> source, NumberQuery query)
    {
        List<List<Letter>> result = new List<List<Letter>>();

        if (source != null)
        {
            List<Letter> letters = new List<Letter>();
            foreach (Verse verse in source)
            {
                foreach (Word word in verse.Words)
                {
                    letters.AddRange(word.Letters);
                }
            }

            int range_length = query.LetterCount;
            if (range_length == 1)
            {
                result.Add(DoFindLetters(source, query));
            }
            else if (range_length == 0) // non-specified range length
            {
                for (int r = 1; r <= 29; r++) // try all possible range lengths
                {
                    for (int i = 0; i <= letters.Count - r; i++)
                    {
                        // build required range
                        List<Letter> range = new List<Letter>();
                        for (int j = i; j < i + r; j++)
                        {
                            range.Add(letters[j]);
                        }

                        // check range
                        if (Compare(range, query))
                        {
                            result.Add(range);
                        }
                    }
                }
            }
            else // specified range length
            {
                int r = range_length;
                for (int i = 0; i <= letters.Count - r; i++)
                {
                    // build required range
                    List<Letter> range = new List<Letter>();
                    for (int j = i; j < i + r; j++)
                    {
                        range.Add(letters[j]);
                    }

                    // check range
                    if (Compare(range, query))
                    {
                        result.Add(range);
                    }
                }
            }
        }

        return result;
    }

    // find by numbers - Words
    public static List<Word> FindWords(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        return DoFindWords(search_scope, current_selection, previous_verses, query);
    }
    private static List<Word> DoFindWords(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_verses, TextLocationInChapter.Any);
        return DoFindWords(source, query);
    }
    private static List<Word> DoFindWords(List<Verse> source, NumberQuery query)
    {
        List<Word> result = new List<Word>();

        if (source != null)
        {
            foreach (Verse verse in source)
            {
                foreach (Word word in verse.Words)
                {
                    if (Compare(word, query))
                    {
                        result.Add(word);
                    }
                }
            }
        }

        return result;
    }
    // find by numbers - WordRanges
    public static List<List<Word>> FindWordRanges(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        return DoFindWordRanges(search_scope, current_selection, previous_verses, query);
    }
    private static List<List<Word>> DoFindWordRanges(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_verses, TextLocationInChapter.Any);
        return DoFindWordRanges(source, query);
    }
    private static List<List<Word>> DoFindWordRanges(List<Verse> source, NumberQuery query)
    {
        List<List<Word>> result = new List<List<Word>>();

        if (source != null)
        {
            List<Word> words = new List<Word>();
            foreach (Verse verse in source)
            {
                words.AddRange(verse.Words);
            }

            int range_length = query.WordCount;
            if (range_length == 1)
            {
                result.Add(DoFindWords(source, query));
            }
            else if (range_length == 0) // non-specified range length
            {
                for (int r = 1; r <= 29; r++) // try all possible range lengths
                {
                    for (int i = 0; i <= words.Count - r; i++)
                    {
                        // build required range
                        List<Word> range = new List<Word>();
                        for (int j = i; j < i + r; j++)
                        {
                            range.Add(words[j]);
                        }

                        // check range
                        if (Compare(range, query))
                        {
                            result.Add(range);
                        }
                    }
                }
            }
            else // specified range length
            {
                int r = range_length;
                for (int i = 0; i <= words.Count - r; i++)
                {
                    // build required range
                    List<Word> range = new List<Word>();
                    for (int j = i; j < i + r; j++)
                    {
                        range.Add(words[j]);
                    }

                    // check range
                    if (Compare(range, query))
                    {
                        result.Add(range);
                    }
                }
            }
        }

        return result;
    }

    // find by numbers - Sentences
    public static List<Sentence> FindSentences(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        return DoFindSentences(search_scope, current_selection, previous_verses, query);
    }
    private static List<Sentence> DoFindSentences(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_verses, TextLocationInChapter.Any);
        return DoFindSentences(source, query);
    }
    private static List<Sentence> DoFindSentences(List<Verse> source, NumberQuery query)
    {
        List<Sentence> result = new List<Sentence>();

        if (source != null)
        {
            if (source.Count > 0)
            {
                List<Word> words = new List<Word>();
                foreach (Verse verse in source)
                {
                    words.AddRange(verse.Words);
                }

                if (s_numerology_system != null)
                {
                    // scan linearly for sequence of words with total Text matching query
                    bool done_MustContinue = false;
                    for (int i = 0; i < words.Count - 1; i++)
                    {
                        StringBuilder str = new StringBuilder();

                        // start building word sequence
                        str.Append(words[i].Text);

                        string stopmark_text = StopmarkHelper.GetStopmarkText(words[i].Stopmark);

                        // 1-word sentence
                        if (
                             (words[i].Stopmark != Stopmark.None) &&
                             (words[i].Stopmark != Stopmark.CanStopAtEither) &&
                             (words[i].Stopmark != Stopmark.MustPause) //&&
                            //(words[i].Stopmark != Stopmark.MustContinue)
                           )
                        {
                            Sentence sentence = new Sentence(words[i].Verse, words[i].Position, words[i].Verse, words[i].Position + words[i].Text.Length, str.ToString());
                            if (sentence != null)
                            {
                                if (Compare(sentence, query))
                                {
                                    result.Add(sentence);
                                }
                            }
                        }
                        else // multi-word sentence
                        {
                            // mark the start of 1-to-m MustContinue stopmarks
                            int backup_i = i;

                            // continue building with next words until a stopmark
                            bool done_CanStopAtEither = false;
                            for (int j = i + 1; j < words.Count; j++)
                            {
                                str.Append(" " + words[j].Text);

                                if (words[j].Stopmark == Stopmark.None)
                                {
                                    continue; // continue building longer senetence
                                }
                                else // there is a real stopmark
                                {
                                    if (s_numerology_system.TextMode == "Original")
                                    {
                                        str.Append(" " + stopmark_text);
                                    }

                                    if (words[j].Stopmark == Stopmark.MustContinue)
                                    {
                                        // TEST Stopmark.MustContinue
                                        //----1 2 3 4 sentences
                                        //1268
                                        //4153
                                        //1799
                                        //2973
                                        //----1 12 123 1234 sentences
                                        //1268
                                        //5421
                                        //7220
                                        //10193
                                        //-------------
                                        //ERRORS
                                        //# duplicate 1
                                        //# short str
                                        //  in 123 1234
                                        //-------------
                                        //// not needed yet
                                        //// multi-mid sentences
                                        //5952
                                        //4772

                                        Sentence sentence = new Sentence(words[i].Verse, words[i].Position, words[j].Verse, words[j].Position + words[j].Text.Length, str.ToString());
                                        if (sentence != null)
                                        {
                                            if (Compare(sentence, query))
                                            {
                                                result.Add(sentence);
                                            }
                                        }

                                        if (done_MustContinue)
                                        {
                                            done_MustContinue = false;
                                            continue; // get all overlapping long sentence
                                        }

                                        StringBuilder k_str = new StringBuilder();
                                        for (int k = j + 1; k < words.Count; k++)
                                        {
                                            k_str.Append(words[k].Text + " ");

                                            if (words[k].Stopmark == Stopmark.None)
                                            {
                                                continue; // next k
                                            }
                                            else // there is a stopmark
                                            {
                                                if (s_numerology_system.TextMode == "Original")
                                                {
                                                    stopmark_text = StopmarkHelper.GetStopmarkText(words[k].Stopmark);
                                                    k_str.Append(stopmark_text + " ");
                                                }
                                                if (k_str.Length > 0)
                                                {
                                                    k_str.Remove(k_str.Length - 1, 1);
                                                }

                                                sentence = new Sentence(words[j + 1].Verse, words[j + 1].Position, words[k].Verse, words[k].Position + words[k].Text.Length, k_str.ToString());
                                                if (sentence != null)
                                                {
                                                    if (Compare(sentence, query))
                                                    {
                                                        result.Add(sentence);
                                                    }
                                                }

                                                if (
                                                     (words[k].Stopmark == Stopmark.ShouldContinue) ||
                                                     (words[k].Stopmark == Stopmark.CanStop) ||
                                                     (words[k].Stopmark == Stopmark.ShouldStop)
                                                   )
                                                {
                                                    done_MustContinue = true;   // restart from beginning skipping any MustContinue
                                                }
                                                else
                                                {
                                                    done_MustContinue = false;   // keep building ever-longer multi-MustContinue sentence
                                                }

                                                j = k;
                                                break; // next j
                                            }
                                        }

                                        if (done_MustContinue)
                                        {
                                            i = backup_i - 1;  // start new sentence from beginning
                                            break; // next i
                                        }
                                        else
                                        {
                                            continue; // next j
                                        }
                                    }
                                    else if (
                                         (words[j].Stopmark == Stopmark.ShouldContinue) ||
                                         (words[j].Stopmark == Stopmark.CanStop) ||
                                         (words[j].Stopmark == Stopmark.ShouldStop)
                                       )
                                    {
                                        Sentence sentence = new Sentence(words[i].Verse, words[i].Position, words[j].Verse, words[j].Position + words[j].Text.Length, str.ToString());
                                        if (sentence != null)
                                        {
                                            if (Compare(sentence, query))
                                            {
                                                result.Add(sentence);
                                            }
                                        }

                                        i = j; // start new sentence after j
                                        break; // next i
                                    }
                                    else if (words[j].Stopmark == Stopmark.MustPause)
                                    {
                                        if (
                                             (words[j].Text.Simplify(s_numerology_system.TextMode) == "مَنْ".Simplify(s_numerology_system.TextMode)) ||
                                             (words[j].Text.Simplify(s_numerology_system.TextMode) == "بَلْ".Simplify(s_numerology_system.TextMode))
                                           )
                                        {
                                            continue; // continue building longer senetence
                                        }
                                        else if (
                                                  (words[j].Text.Simplify(s_numerology_system.TextMode) == "عِوَجَا".Simplify(s_numerology_system.TextMode)) ||
                                                  (words[j].Text.Simplify(s_numerology_system.TextMode) == "مَّرْقَدِنَا".Simplify(s_numerology_system.TextMode)) ||
                                                  (words[j].Text.Simplify(s_numerology_system.TextMode) == "مَالِيَهْ".Simplify(s_numerology_system.TextMode))
                                                )
                                        {
                                            Sentence sentence = new Sentence(words[i].Verse, words[i].Position, words[j].Verse, words[j].Position + words[j].Text.Length, str.ToString());
                                            if (sentence != null)
                                            {
                                                if (Compare(sentence, query))
                                                {
                                                    result.Add(sentence);
                                                }
                                            }

                                            i = j; // start new sentence after j
                                            break; // next i
                                        }
                                        else // unknown case
                                        {
                                            throw new Exception("Unknown paused Quran word.");
                                        }
                                    }
                                    // first CanStopAtEither found at j
                                    else if ((!done_CanStopAtEither) && (words[j].Stopmark == Stopmark.CanStopAtEither))
                                    {
                                        // ^ ذَٰلِكَ ٱلْكِتَٰبُ لَا رَيْبَ
                                        Sentence sentence = new Sentence(words[i].Verse, words[i].Position, words[j].Verse, words[j].Position + words[j].Text.Length, str.ToString());
                                        if (sentence != null)
                                        {
                                            if (Compare(sentence, query))
                                            {
                                                result.Add(sentence);
                                            }
                                        }

                                        int kk = -1; // start after ^ (e.g. هُدًۭى)
                                        StringBuilder kk_str = new StringBuilder();
                                        StringBuilder kkk_str = new StringBuilder();
                                        for (int k = j + 1; k < words.Count; k++)
                                        {
                                            str.Append(" " + words[k].Text);
                                            if (kkk_str.Length > 0) // skip first k loop
                                            {
                                                kk_str.Append(" " + words[k].Text);
                                            }
                                            kkk_str.Append(" " + words[k].Text);

                                            if (words[k].Stopmark == Stopmark.None)
                                            {
                                                continue; // next k
                                            }
                                            else // there is a stopmark
                                            {
                                                if (s_numerology_system.TextMode == "Original")
                                                {
                                                    str.Append(" " + stopmark_text);
                                                    if (kk_str.Length > 0)
                                                    {
                                                        kk_str.Append(" " + stopmark_text);
                                                    }
                                                    kkk_str.Append(" " + stopmark_text);
                                                }

                                                // second CanStopAtEither found at k
                                                if (words[k].Stopmark == Stopmark.CanStopAtEither)
                                                {
                                                    // ^ ذَٰلِكَ ٱلْكِتَٰبُ لَا رَيْبَ ۛ^ فِيهِ
                                                    sentence = new Sentence(words[i].Verse, words[i].Position, words[k].Verse, words[k].Position + words[k].Text.Length, str.ToString());
                                                    if (sentence != null)
                                                    {
                                                        if (Compare(sentence, query))
                                                        {
                                                            result.Add(sentence);
                                                        }
                                                    }

                                                    kk = k + 1; // backup k after second ^
                                                    continue; // next k
                                                }
                                                else // non-CanStopAtEither stopmark
                                                {
                                                    // kkk_str   فِيهِ ۛ^ هُدًۭى لِّلْمُتَّقِينَ
                                                    sentence = new Sentence(words[j + 1].Verse, words[j + 1].Position, words[k].Verse, words[k].Position + words[k].Text.Length, kkk_str.ToString());
                                                    if (sentence != null)
                                                    {
                                                        if (Compare(sentence, query))
                                                        {
                                                            result.Add(sentence);
                                                        }
                                                    }

                                                    // kk_str   هُدًۭى لِّلْمُتَّقِينَ
                                                    sentence = new Sentence(words[kk].Verse, words[kk].Position, words[k].Verse, words[k].Position + words[k].Text.Length, kk_str.ToString());
                                                    if (sentence != null)
                                                    {
                                                        if (Compare(sentence, query))
                                                        {
                                                            result.Add(sentence);
                                                        }
                                                    }

                                                    // skip the whole surrounding non-CanStopAtEither sentence
                                                    j = k;
                                                    break; // next j
                                                }
                                            }
                                        }

                                        // restart from last
                                        str.Length = 0;
                                        j = i - 1; // will be j++ by reloop
                                        done_CanStopAtEither = true;
                                    }
                                    else if (words[j].Stopmark == Stopmark.MustStop)
                                    {
                                        Sentence sentence = new Sentence(words[i].Verse, words[i].Position, words[j].Verse, words[j].Position + words[j].Text.Length, str.ToString());
                                        if (sentence != null)
                                        {
                                            if (Compare(sentence, query))
                                            {
                                                result.Add(sentence);
                                            }
                                        }

                                        i = j; // start new sentence after j
                                        break; // next i
                                    }
                                    else // unknown case
                                    {
                                        continue;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return result;
    }

    // find by numbers - Verses
    public static List<Verse> FindVerses(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        return DoFindVerses(search_scope, current_selection, previous_verses, query);
    }
    private static List<Verse> DoFindVerses(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_verses, TextLocationInChapter.Any);
        return DoFindVerses(source, query);
    }
    private static List<Verse> DoFindVerses(List<Verse> source, NumberQuery query)
    {
        List<Verse> result = new List<Verse>();

        if (source != null)
        {
            foreach (Verse verse in source)
            {
                if (Compare(verse, query))
                {
                    result.Add(verse);
                }
            }
        }

        return result;
    }
    // find by numbers - VerseRanges
    public static List<List<Verse>> FindVerseRanges(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        return DoFindVerseRanges(search_scope, current_selection, previous_verses, query);
    }
    private static List<List<Verse>> DoFindVerseRanges(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_verses, TextLocationInChapter.Any);
        return DoFindVerseRanges(source, query);
    }
    private static List<List<Verse>> DoFindVerseRanges(List<Verse> source, NumberQuery query)
    {
        List<List<Verse>> result = new List<List<Verse>>();

        if (source != null)
        {
            int range_length = query.VerseCount;
            if (range_length == 1)
            {
                result.Add(DoFindVerses(source, query));
            }
            else if (range_length == 0) // non-specified range length
            {
                for (int r = 1; r <= 29; r++) // try all possible range lengths
                {
                    for (int i = 0; i < source.Count - r + 1; i++)
                    {
                        // build required range
                        List<Verse> range = new List<Verse>();
                        for (int j = i; j < i + r; j++)
                        {
                            range.Add(source[j]);
                        }

                        // check range
                        if (Compare(range, query))
                        {
                            result.Add(range);
                        }
                    }
                }
            }
            else // specified range length
            {
                int r = range_length;
                for (int i = 0; i < source.Count - r + 1; i++)
                {
                    // build required range
                    List<Verse> range = new List<Verse>();
                    for (int j = i; j < i + r; j++)
                    {
                        range.Add(source[j]);
                    }

                    // check range
                    if (Compare(range, query))
                    {
                        result.Add(range);
                    }
                }
            }
        }

        return result;
    }

    // find by numbers - Chapters
    public static List<Chapter> FindChapters(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        return DoFindChapters(search_scope, current_selection, previous_verses, query);
    }
    private static List<Chapter> DoFindChapters(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_verses, TextLocationInChapter.Any);
        return DoFindChapters(source, query);
    }
    private static List<Chapter> DoFindChapters(List<Verse> source, NumberQuery query)
    {
        List<Chapter> result = new List<Chapter>();

        if (source != null)
        {
            if (source.Count > 0)
            {
                if (s_book != null)
                {
                    List<Chapter> chapters = s_book.GetChapters(source);
                    if (chapters != null)
                    {
                        foreach (Chapter chapter in chapters)
                        {
                            if (Compare(chapter, query))
                            {
                                result.Add(chapter);
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
    // find by numbers - ChapterRanges
    public static List<List<Chapter>> FindChapterRanges(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        return DoFindChapterRanges(search_scope, current_selection, previous_verses, query);
    }
    private static List<List<Chapter>> DoFindChapterRanges(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_verses, TextLocationInChapter.Any);
        return DoFindChapterRanges(source, query);
    }
    private static List<List<Chapter>> DoFindChapterRanges(List<Verse> source, NumberQuery query)
    {
        List<List<Chapter>> result = new List<List<Chapter>>();

        if (source != null)
        {
            if (source.Count > 0)
            {
                if (s_book != null)
                {
                    List<Chapter> chapters = s_book.GetChapters(source);
                    if (chapters != null)
                    {
                        int range_length = query.PartitionCount;
                        if (range_length == 1)
                        {
                            result.Add(DoFindChapters(source, query));
                        }
                        else if (range_length == 0) // non-specified range length
                        {
                            // limit range length to minimum
                            int limit = chapters.Count - 1;

                            for (int r = 1; r <= limit; r++) // try all possible range lengths
                            {
                                for (int i = 0; i < chapters.Count - r + 1; i++)
                                {
                                    // build required range
                                    List<Chapter> range = new List<Chapter>();
                                    for (int j = i; j < i + r; j++)
                                    {
                                        range.Add(chapters[j]);
                                    }

                                    // check range
                                    if (Compare(range, query))
                                    {
                                        result.Add(range);
                                    }
                                }
                            }
                        }
                        else // specified range length
                        {
                            int r = range_length;
                            for (int i = 0; i < chapters.Count - r + 1; i++)
                            {
                                // build required range
                                List<Chapter> range = new List<Chapter>();
                                for (int j = i; j < i + r; j++)
                                {
                                    range.Add(chapters[j]);
                                }

                                // check range
                                if (Compare(range, query))
                                {
                                    result.Add(range);
                                }
                            }
                        }
                    }
                }
            }
        }

        return result;
    }

    // find by numbers - Pages
    public static List<Page> FindPages(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        return DoFindPages(search_scope, current_selection, previous_verses, query);
    }
    private static List<Page> DoFindPages(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_verses, TextLocationInChapter.Any);
        return DoFindPages(source, query);
    }
    private static List<Page> DoFindPages(List<Verse> source, NumberQuery query)
    {
        List<Page> result = new List<Page>();

        if (source != null)
        {
            if (source.Count > 0)
            {
                if (s_book != null)
                {
                    List<Page> pages = s_book.GetPages(source);
                    if (pages != null)
                    {
                        foreach (Page page in pages)
                        {
                            if (Compare(page, query))
                            {
                                result.Add(page);
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
    // find by numbers - PageRanges
    public static List<List<Page>> FindPageRanges(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        return DoFindPageRanges(search_scope, current_selection, previous_verses, query);
    }
    private static List<List<Page>> DoFindPageRanges(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_verses, TextLocationInChapter.Any);
        return DoFindPageRanges(source, query);
    }
    private static List<List<Page>> DoFindPageRanges(List<Verse> source, NumberQuery query)
    {
        List<List<Page>> result = new List<List<Page>>();

        if (source != null)
        {
            if (source.Count > 0)
            {
                if (s_book != null)
                {
                    List<Page> pages = s_book.GetPages(source);
                    if (pages != null)
                    {
                        int range_length = query.PartitionCount;
                        if (range_length == 1)
                        {
                            result.Add(DoFindPages(source, query));
                        }
                        else if (range_length == 0) // non-specified range length
                        {
                            // limit range length to minimum
                            int limit = pages.Count - 1;

                            for (int r = 1; r <= limit; r++) // try all possible range lengths
                            {
                                for (int i = 0; i < pages.Count - r + 1; i++)
                                {
                                    // build required range
                                    List<Page> range = new List<Page>();
                                    for (int j = i; j < i + r; j++)
                                    {
                                        range.Add(pages[j]);
                                    }

                                    // check range
                                    if (Compare(range, query))
                                    {
                                        result.Add(range);
                                    }
                                }
                            }
                        }
                        else // specified range length
                        {
                            int r = range_length;
                            for (int i = 0; i < pages.Count - r + 1; i++)
                            {
                                // build required range
                                List<Page> range = new List<Page>();
                                for (int j = i; j < i + r; j++)
                                {
                                    range.Add(pages[j]);
                                }

                                // check range
                                if (Compare(range, query))
                                {
                                    result.Add(range);
                                }
                            }
                        }
                    }
                }
            }
        }

        return result;
    }

    // find by numbers - Stations
    public static List<Station> FindStations(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        return DoFindStations(search_scope, current_selection, previous_verses, query);
    }
    private static List<Station> DoFindStations(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_verses, TextLocationInChapter.Any);
        return DoFindStations(source, query);
    }
    private static List<Station> DoFindStations(List<Verse> source, NumberQuery query)
    {
        List<Station> result = new List<Station>();

        if (source != null)
        {
            if (source.Count > 0)
            {
                if (s_book != null)
                {
                    List<Station> stations = s_book.GetStations(source);
                    if (stations != null)
                    {
                        foreach (Station station in stations)
                        {
                            if (Compare(station, query))
                            {
                                result.Add(station);
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
    // find by numbers - StationRanges
    public static List<List<Station>> FindStationRanges(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        return DoFindStationRanges(search_scope, current_selection, previous_verses, query);
    }
    private static List<List<Station>> DoFindStationRanges(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_verses, TextLocationInChapter.Any);
        return DoFindStationRanges(source, query);
    }
    private static List<List<Station>> DoFindStationRanges(List<Verse> source, NumberQuery query)
    {
        List<List<Station>> result = new List<List<Station>>();

        if (source != null)
        {
            if (source.Count > 0)
            {
                if (s_book != null)
                {
                    List<Station> stations = s_book.GetStations(source);
                    if (stations != null)
                    {
                        int range_length = query.PartitionCount;
                        if (range_length == 1)
                        {
                            result.Add(DoFindStations(source, query));
                        }
                        else if (range_length == 0) // non-specified range length
                        {
                            // limit range length to minimum
                            int limit = stations.Count - 1;

                            for (int r = 1; r <= limit; r++) // try all possible range lengths
                            {
                                for (int i = 0; i < stations.Count - r + 1; i++)
                                {
                                    // build required range
                                    List<Station> range = new List<Station>();
                                    for (int j = i; j < i + r; j++)
                                    {
                                        range.Add(stations[j]);
                                    }

                                    // check range
                                    if (Compare(range, query))
                                    {
                                        result.Add(range);
                                    }
                                }
                            }
                        }
                        else // specified range length
                        {
                            int r = range_length;
                            for (int i = 0; i < stations.Count - r + 1; i++)
                            {
                                // build required range
                                List<Station> range = new List<Station>();
                                for (int j = i; j < i + r; j++)
                                {
                                    range.Add(stations[j]);
                                }

                                // check range
                                if (Compare(range, query))
                                {
                                    result.Add(range);
                                }
                            }
                        }
                    }
                }
            }
        }

        return result;
    }

    // find by numbers - Parts
    public static List<Part> FindParts(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        return DoFindParts(search_scope, current_selection, previous_verses, query);
    }
    private static List<Part> DoFindParts(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_verses, TextLocationInChapter.Any);
        return DoFindParts(source, query);
    }
    private static List<Part> DoFindParts(List<Verse> source, NumberQuery query)
    {
        List<Part> result = new List<Part>();

        if (source != null)
        {
            if (source.Count > 0)
            {
                if (s_book != null)
                {
                    List<Part> parts = s_book.GetParts(source);
                    if (parts != null)
                    {
                        foreach (Part part in parts)
                        {
                            if (Compare(part, query))
                            {
                                result.Add(part);
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
    // find by numbers - PartRanges
    public static List<List<Part>> FindPartRanges(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        return DoFindPartRanges(search_scope, current_selection, previous_verses, query);
    }
    private static List<List<Part>> DoFindPartRanges(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_verses, TextLocationInChapter.Any);
        return DoFindPartRanges(source, query);
    }
    private static List<List<Part>> DoFindPartRanges(List<Verse> source, NumberQuery query)
    {
        List<List<Part>> result = new List<List<Part>>();

        if (source != null)
        {
            if (source.Count > 0)
            {
                if (s_book != null)
                {
                    List<Part> parts = s_book.GetParts(source);
                    if (parts != null)
                    {
                        int range_length = query.PartitionCount;
                        if (range_length == 1)
                        {
                            result.Add(DoFindParts(source, query));
                        }
                        else if (range_length == 0) // non-specified range length
                        {
                            // limit range length to minimum
                            int limit = parts.Count - 1;

                            for (int r = 1; r <= limit; r++) // try all possible range lengths
                            {
                                for (int i = 0; i < parts.Count - r + 1; i++)
                                {
                                    // build required range
                                    List<Part> range = new List<Part>();
                                    for (int j = i; j < i + r; j++)
                                    {
                                        range.Add(parts[j]);
                                    }

                                    // check range
                                    if (Compare(range, query))
                                    {
                                        result.Add(range);
                                    }
                                }
                            }
                        }
                        else // specified range length
                        {
                            int r = range_length;
                            for (int i = 0; i < parts.Count - r + 1; i++)
                            {
                                // build required range
                                List<Part> range = new List<Part>();
                                for (int j = i; j < i + r; j++)
                                {
                                    range.Add(parts[j]);
                                }

                                // check range
                                if (Compare(range, query))
                                {
                                    result.Add(range);
                                }
                            }
                        }
                    }
                }
            }
        }

        return result;
    }

    // find by numbers - Groups
    public static List<Model.Group> FindGroups(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        return DoFindGroups(search_scope, current_selection, previous_verses, query);
    }
    private static List<Model.Group> DoFindGroups(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_verses, TextLocationInChapter.Any);
        return DoFindGroups(source, query);
    }
    private static List<Model.Group> DoFindGroups(List<Verse> source, NumberQuery query)
    {
        List<Model.Group> result = new List<Model.Group>();

        if (source != null)
        {
            if (source.Count > 0)
            {
                if (s_book != null)
                {
                    List<Model.Group> chapters = s_book.GetGroups(source);
                    if (chapters != null)
                    {
                        foreach (Model.Group chapter in chapters)
                        {
                            if (Compare(chapter, query))
                            {
                                result.Add(chapter);
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
    // find by numbers - GroupRanges
    public static List<List<Model.Group>> FindGroupRanges(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        return DoFindGroupRanges(search_scope, current_selection, previous_verses, query);
    }
    private static List<List<Model.Group>> DoFindGroupRanges(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_verses, TextLocationInChapter.Any);
        return DoFindGroupRanges(source, query);
    }
    private static List<List<Model.Group>> DoFindGroupRanges(List<Verse> source, NumberQuery query)
    {
        List<List<Model.Group>> result = new List<List<Model.Group>>();

        if (source != null)
        {
            if (source.Count > 0)
            {
                if (s_book != null)
                {
                    List<Model.Group> groups = s_book.GetGroups(source);
                    if (groups != null)
                    {
                        int range_length = query.PartitionCount;
                        if (range_length == 1)
                        {
                            result.Add(DoFindGroups(source, query));
                        }
                        else if (range_length == 0) // non-specified range length
                        {
                            // limit range length to minimum
                            int limit = groups.Count - 1;

                            for (int r = 1; r <= limit; r++) // try all possible range lengths
                            {
                                for (int i = 0; i < groups.Count - r + 1; i++)
                                {
                                    // build required range
                                    List<Model.Group> range = new List<Model.Group>();
                                    for (int j = i; j < i + r; j++)
                                    {
                                        range.Add(groups[j]);
                                    }

                                    // check range
                                    if (Compare(range, query))
                                    {
                                        result.Add(range);
                                    }
                                }
                            }
                        }
                        else // specified range length
                        {
                            int r = range_length;
                            for (int i = 0; i < groups.Count - r + 1; i++)
                            {
                                // build required range
                                List<Model.Group> range = new List<Model.Group>();
                                for (int j = i; j < i + r; j++)
                                {
                                    range.Add(groups[j]);
                                }

                                // check range
                                if (Compare(range, query))
                                {
                                    result.Add(range);
                                }
                            }
                        }
                    }
                }
            }
        }

        return result;
    }

    // find by numbers - Halfs
    public static List<Half> FindHalfs(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        return DoFindHalfs(search_scope, current_selection, previous_verses, query);
    }
    private static List<Half> DoFindHalfs(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_verses, TextLocationInChapter.Any);
        return DoFindHalfs(source, query);
    }
    private static List<Half> DoFindHalfs(List<Verse> source, NumberQuery query)
    {
        List<Half> result = new List<Half>();

        if (source != null)
        {
            if (source.Count > 0)
            {
                if (s_book != null)
                {
                    List<Half> chapters = s_book.GetHalfs(source);
                    if (chapters != null)
                    {
                        foreach (Half chapter in chapters)
                        {
                            if (Compare(chapter, query))
                            {
                                result.Add(chapter);
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
    // find by numbers - HalfRanges
    public static List<List<Half>> FindHalfRanges(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        return DoFindHalfRanges(search_scope, current_selection, previous_verses, query);
    }
    private static List<List<Half>> DoFindHalfRanges(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_verses, TextLocationInChapter.Any);
        return DoFindHalfRanges(source, query);
    }
    private static List<List<Half>> DoFindHalfRanges(List<Verse> source, NumberQuery query)
    {
        List<List<Half>> result = new List<List<Half>>();

        if (source != null)
        {
            if (source.Count > 0)
            {
                if (s_book != null)
                {
                    List<Half> halfs = s_book.GetHalfs(source);
                    if (halfs != null)
                    {
                        int range_length = query.PartitionCount;
                        if (range_length == 1)
                        {
                            result.Add(DoFindHalfs(source, query));
                        }
                        else if (range_length == 0) // non-specified range length
                        {
                            // limit range length to minimum
                            int limit = halfs.Count - 1;

                            for (int r = 1; r <= limit; r++) // try all possible range lengths
                            {
                                for (int i = 0; i < halfs.Count - r + 1; i++)
                                {
                                    // build required range
                                    List<Half> range = new List<Half>();
                                    for (int j = i; j < i + r; j++)
                                    {
                                        range.Add(halfs[j]);
                                    }

                                    // check range
                                    if (Compare(range, query))
                                    {
                                        result.Add(range);
                                    }
                                }
                            }
                        }
                        else // specified range length
                        {
                            int r = range_length;
                            for (int i = 0; i < halfs.Count - r + 1; i++)
                            {
                                // build required range
                                List<Half> range = new List<Half>();
                                for (int j = i; j < i + r; j++)
                                {
                                    range.Add(halfs[j]);
                                }

                                // check range
                                if (Compare(range, query))
                                {
                                    result.Add(range);
                                }
                            }
                        }
                    }
                }
            }
        }

        return result;
    }

    // find by numbers - Quarters
    public static List<Quarter> FindQuarters(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        return DoFindQuarters(search_scope, current_selection, previous_verses, query);
    }
    private static List<Quarter> DoFindQuarters(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_verses, TextLocationInChapter.Any);
        return DoFindQuarters(source, query);
    }
    private static List<Quarter> DoFindQuarters(List<Verse> source, NumberQuery query)
    {
        List<Quarter> result = new List<Quarter>();

        if (source != null)
        {
            if (source.Count > 0)
            {
                if (s_book != null)
                {
                    List<Quarter> chapters = s_book.GetQuarters(source);
                    if (chapters != null)
                    {
                        foreach (Quarter chapter in chapters)
                        {
                            if (Compare(chapter, query))
                            {
                                result.Add(chapter);
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
    // find by numbers - QuarterRanges
    public static List<List<Quarter>> FindQuarterRanges(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        return DoFindQuarterRanges(search_scope, current_selection, previous_verses, query);
    }
    private static List<List<Quarter>> DoFindQuarterRanges(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_verses, TextLocationInChapter.Any);
        return DoFindQuarterRanges(source, query);
    }
    private static List<List<Quarter>> DoFindQuarterRanges(List<Verse> source, NumberQuery query)
    {
        List<List<Quarter>> result = new List<List<Quarter>>();

        if (source != null)
        {
            if (source.Count > 0)
            {
                if (s_book != null)
                {
                    List<Quarter> quarters = s_book.GetQuarters(source);
                    if (quarters != null)
                    {
                        int range_length = query.PartitionCount;
                        if (range_length == 1)
                        {
                            result.Add(DoFindQuarters(source, query));
                        }
                        else if (range_length == 0) // non-specified range length
                        {
                            // limit range length to minimum
                            int limit = quarters.Count - 1;

                            for (int r = 1; r <= limit; r++) // try all possible range lengths
                            {
                                for (int i = 0; i < quarters.Count - r + 1; i++)
                                {
                                    // build required range
                                    List<Quarter> range = new List<Quarter>();
                                    for (int j = i; j < i + r; j++)
                                    {
                                        range.Add(quarters[j]);
                                    }

                                    // check range
                                    if (Compare(range, query))
                                    {
                                        result.Add(range);
                                    }
                                }
                            }
                        }
                        else // specified range length
                        {
                            int r = range_length;
                            for (int i = 0; i < quarters.Count - r + 1; i++)
                            {
                                // build required range
                                List<Quarter> range = new List<Quarter>();
                                for (int j = i; j < i + r; j++)
                                {
                                    range.Add(quarters[j]);
                                }

                                // check range
                                if (Compare(range, query))
                                {
                                    result.Add(range);
                                }
                            }
                        }
                    }
                }
            }
        }

        return result;
    }

    // find by numbers - Bowings
    public static List<Bowing> FindBowings(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        return DoFindBowings(search_scope, current_selection, previous_verses, query);
    }
    private static List<Bowing> DoFindBowings(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_verses, TextLocationInChapter.Any);
        return DoFindBowings(source, query);
    }
    private static List<Bowing> DoFindBowings(List<Verse> source, NumberQuery query)
    {
        List<Bowing> result = new List<Bowing>();

        if (source != null)
        {
            if (source.Count > 0)
            {
                if (s_book != null)
                {
                    List<Bowing> chapters = s_book.GetBowings(source);
                    if (chapters != null)
                    {
                        foreach (Bowing chapter in chapters)
                        {
                            if (Compare(chapter, query))
                            {
                                result.Add(chapter);
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
    // find by numbers - BowingRanges
    public static List<List<Bowing>> FindBowingRanges(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        return DoFindBowingRanges(search_scope, current_selection, previous_verses, query);
    }
    private static List<List<Bowing>> DoFindBowingRanges(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, NumberQuery query)
    {
        List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_verses, TextLocationInChapter.Any);
        return DoFindBowingRanges(source, query);
    }
    private static List<List<Bowing>> DoFindBowingRanges(List<Verse> source, NumberQuery query)
    {
        List<List<Bowing>> result = new List<List<Bowing>>();

        if (source != null)
        {
            if (source.Count > 0)
            {
                if (s_book != null)
                {
                    List<Bowing> bowings = s_book.GetBowings(source);
                    if (bowings != null)
                    {
                        int range_length = query.PartitionCount;
                        if (range_length == 1)
                        {
                            result.Add(DoFindBowings(source, query));
                        }
                        else if (range_length == 0) // non-specified range length
                        {
                            // limit range length to minimum
                            int limit = bowings.Count - 1;

                            for (int r = 1; r <= limit; r++) // try all possible range lengths
                            {
                                for (int i = 0; i < bowings.Count - r + 1; i++)
                                {
                                    // build required range
                                    List<Bowing> range = new List<Bowing>();
                                    for (int j = i; j < i + r; j++)
                                    {
                                        range.Add(bowings[j]);
                                    }

                                    // check range
                                    if (Compare(range, query))
                                    {
                                        result.Add(range);
                                    }
                                }
                            }
                        }
                        else // specified range length
                        {
                            int r = range_length;
                            for (int i = 0; i < bowings.Count - r + 1; i++)
                            {
                                // build required range
                                List<Bowing> range = new List<Bowing>();
                                for (int j = i; j < i + r; j++)
                                {
                                    range.Add(bowings[j]);
                                }

                                // check range
                                if (Compare(range, query))
                                {
                                    result.Add(range);
                                }
                            }
                        }
                    }
                }
            }
        }

        return result;
    }


    // find by similarity - phrases similar to given text
    public static List<Phrase> FindPhrases(SearchScope search_scope, Selection current_selection, List<Verse> previous_result, string text, double similarity_percentage)
    {
        List<Phrase> result = new List<Phrase>();

        List<Verse> found_verses = previous_result;
        while (text.Contains("  "))
        {
            text = text.Replace("  ", " ");
        }
        while (text.Contains("+"))
        {
            text = text.Replace("+", "");
        }
        while (text.Contains("-"))
        {
            text = text.Replace("-", "");
        }

        string[] word_texts = text.Split();
        if (word_texts.Length == 0)
        {
            return result;
        }
        if (word_texts.Length == 1)
        {
            return DoFindPhrases(search_scope, current_selection, previous_result, text, similarity_percentage);
        }
        if (word_texts.Length > 1) // enable nested searches
        {
            if (text.Length > 1) // enable nested searches
            {
                List<Phrase> phrases = null;
                List<Verse> verses = null;

                foreach (string word_text in word_texts)
                {
                    phrases = DoFindPhrases(search_scope, current_selection, found_verses, word_text, similarity_percentage);
                    verses = new List<Verse>(GetVerses(phrases));

                    // if first result
                    if (found_verses == null)
                    {
                        // fill it up with a copy of the first similar word search result
                        result = new List<Phrase>(phrases);
                        found_verses = new List<Verse>(verses);

                        // prepare for nested search by search
                        search_scope = SearchScope.Result;
                    }
                    else // subsequent search result
                    {
                        found_verses = new List<Verse>(verses);

                        List<Phrase> union_phrases = new List<Phrase>(phrases);
                        foreach (Phrase phrase in result)
                        {
                            if (phrase != null)
                            {
                                if (verses.Contains(phrase.Verse))
                                {
                                    union_phrases.Add(phrase);
                                }
                            }
                        }
                        result = union_phrases;
                    }
                }
            }
        }

        return result;
    }
    private static List<Phrase> DoFindPhrases(SearchScope search_scope, Selection current_selection, List<Verse> previous_result, string text, double similarity_percentage)
    {
        List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_result, TextLocationInChapter.Any);
        return DoFindPhrases(source, current_selection, previous_result, text, similarity_percentage);
    }
    private static List<Phrase> DoFindPhrases(List<Verse> source, Selection current_selection, List<Verse> previous_result, string text, double similarity_percentage)
    {
        List<Phrase> result = new List<Phrase>();

        string simplified_text = text;
        List<Verse> simplified_source = new List<Verse>();
        if (s_numerology_system.TextMode == "Original")
        {
            simplified_text = text.Simplify29();

            // simplify verse texts
            List<string> verse_texts = new List<string>();
            foreach (Verse verse in source)
            {
                string verse_text = verse.Text.Simplify29().Trim();
                verse_texts.Add(verse_text);
            }

            // build verses
            for (int i = 0; i < verse_texts.Count; i++)
            {
                Verse v = new Verse(i + 1, verse_texts[i], Stopmark.None);
                if (v != null)
                {
                    simplified_source.Add(v);
                }
            }
        }
        else
        {
            simplified_source = source;
        }

        if (simplified_source != null)
        {
            if (simplified_source.Count > 0)
            {
                if (!String.IsNullOrEmpty(text))
                {
                    for (int i = 0; i < simplified_source.Count; i++)
                    {
                        for (int j = 0; j < simplified_source[i].Words.Count; j++)
                        {
                            if (simplified_source[i].Words[j].Text.IsSimilarTo(simplified_text, similarity_percentage))
                            {
                                result.Add(new Phrase(source[i], source[i].Words[j].Position, source[i].Words[j].Text));
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
    // find by similarity - verses similar to given verse
    public static List<Verse> FindVerses(SearchScope search_scope, Selection current_selection, List<Verse> previous_result, Verse verse, SimilarityMethod similarity_method, double similarity_percentage)
    {
        return DoFindVerses(search_scope, current_selection, previous_result, verse, similarity_method, similarity_percentage);
    }
    private static List<Verse> DoFindVerses(SearchScope search_scope, Selection current_selection, List<Verse> previous_result, Verse verse, SimilarityMethod similarity_method, double similarity_percentage)
    {
        List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_result, TextLocationInChapter.Any);
        return DoFindVerses(source, current_selection, previous_result, verse, similarity_method, similarity_percentage);
    }
    private static List<Verse> DoFindVerses(List<Verse> source, Selection current_selection, List<Verse> previous_result, Verse verse, SimilarityMethod find_similarity_method, double similarity_percentage)
    {
        List<Verse> result = new List<Verse>();

        Verse simplified_verse = verse;
        List<Verse> simplified_source = new List<Verse>();
        if (s_numerology_system.TextMode == "Original")
        {
            simplified_verse = new Verse(verse.Number, verse.Text.Simplify29().Trim(), verse.Stopmark);

            // simplify verse texts
            List<string> verse_texts = new List<string>();
            foreach (Verse v in source)
            {
                string verse_text = v.Text.Simplify29().Trim();
                verse_texts.Add(verse_text);
            }

            // build verses
            for (int i = 0; i < verse_texts.Count; i++)
            {
                Verse v = new Verse(i + 1, verse_texts[i], Stopmark.None);
                if (v != null)
                {
                    simplified_source.Add(v);
                }
            }
        }
        else
        {
            simplified_source = source;
        }

        if (simplified_source != null)
        {
            if (simplified_source.Count > 0)
            {
                if (simplified_verse != null)
                {
                    switch (find_similarity_method)
                    {
                        case SimilarityMethod.SimilarText:
                            {
                                for (int j = 0; j < simplified_source.Count; j++)
                                {
                                    if (simplified_verse.Text.IsSimilarTo(simplified_source[j].Text, similarity_percentage))
                                    {
                                        result.Add(source[j]);
                                    }
                                }
                            }
                            break;
                        case SimilarityMethod.SimilarWords:
                            {
                                for (int j = 0; j < simplified_source.Count; j++)
                                {
                                    if (simplified_verse.Text.HasSimilarWordsTo(simplified_source[j].Text, (int)Math.Round((Math.Min(simplified_verse.Words.Count, simplified_source[j].Words.Count) * similarity_percentage)), similarity_percentage))
                                    {
                                        result.Add(source[j]);
                                    }
                                }
                            }
                            break;
                        case SimilarityMethod.SimilarFirstHalf:
                            {
                                for (int j = 0; j < simplified_source.Count; j++)
                                {
                                    if (simplified_verse.Text.HasSimilarFirstHalfTo(simplified_source[j].Text, similarity_percentage))
                                    {
                                        result.Add(source[j]);
                                    }
                                }
                            }
                            break;
                        case SimilarityMethod.SimilarLastHalf:
                            {
                                for (int j = 0; j < simplified_source.Count; j++)
                                {
                                    if (simplified_verse.Text.HasSimilarLastHalfTo(simplified_source[j].Text, similarity_percentage))
                                    {
                                        result.Add(source[j]);
                                    }
                                }
                            }
                            break;
                        case SimilarityMethod.SimilarFirstWord:
                            {
                                for (int j = 0; j < simplified_source.Count; j++)
                                {
                                    if (simplified_verse.Text.HasSimilarFirstWordTo(simplified_source[j].Text, similarity_percentage))
                                    {
                                        result.Add(source[j]);
                                    }
                                }
                            }
                            break;
                        case SimilarityMethod.SimilarLastWord:
                            {
                                for (int j = 0; j < simplified_source.Count; j++)
                                {
                                    if (simplified_verse.Text.HasSimilarLastWordTo(simplified_source[j].Text, similarity_percentage))
                                    {
                                        result.Add(source[j]);
                                    }
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        return result;
    }
    // find by similarity - all similar verses to each other throughout the book
    public static List<List<Verse>> FindVersess(SearchScope search_scope, Selection current_selection, List<Verse> previous_result, SimilarityMethod similarity_method, double similarity_percentage)
    {
        return DoFindVersess(search_scope, current_selection, previous_result, similarity_method, similarity_percentage);
    }
    private static List<List<Verse>> DoFindVersess(SearchScope search_scope, Selection current_selection, List<Verse> previous_result, SimilarityMethod similarity_method, double similarity_percentage)
    {
        List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_result, TextLocationInChapter.Any);
        return DoFindVersess(source, current_selection, previous_result, similarity_method, similarity_percentage);
    }
    private static List<List<Verse>> DoFindVersess(List<Verse> source, Selection current_selection, List<Verse> previous_result, SimilarityMethod find_similarity_method, double similarity_percentage)
    {
        List<List<Verse>> result = new List<List<Verse>>();

        List<Verse> simplified_source = new List<Verse>();
        if (s_numerology_system.TextMode == "Original")
        {
            // simplify verse texts
            List<string> verse_texts = new List<string>();
            foreach (Verse verse in source)
            {
                string verse_text = verse.Text.Simplify29().Trim();
                verse_texts.Add(verse_text);
            }

            // build verses
            for (int i = 0; i < verse_texts.Count; i++)
            {
                Verse verse = new Verse(i + 1, verse_texts[i], Stopmark.None);
                if (verse != null)
                {
                    simplified_source.Add(verse);
                }
            }
        }
        else
        {
            simplified_source = source;
        }

        Dictionary<Verse, List<Verse>> verse_ranges = new Dictionary<Verse, List<Verse>>(); // need dictionary to check if key exist
        bool[] already_compared = new bool[simplified_source.Count];
        if (simplified_source != null)
        {
            if (simplified_source.Count > 0)
            {
                switch (find_similarity_method)
                {
                    case SimilarityMethod.SimilarText:
                        {
                            for (int i = 0; i < simplified_source.Count - 1; i++)
                            {
                                for (int j = i + 1; j < simplified_source.Count; j++)
                                {
                                    if (!already_compared[j])
                                    {
                                        if (simplified_source[i].Text.IsSimilarTo(simplified_source[j].Text, similarity_percentage))
                                        {
                                            if (!verse_ranges.ContainsKey(source[i])) // first time matching verses found
                                            {
                                                List<Verse> similar_verses = new List<Verse>();
                                                verse_ranges.Add(source[i], similar_verses);
                                                similar_verses.Add(source[i]);
                                                similar_verses.Add(source[j]);
                                                already_compared[i] = true;
                                                already_compared[j] = true;
                                            }
                                            else // matching verses already exists
                                            {
                                                List<Verse> similar_verses = verse_ranges[source[i]];
                                                similar_verses.Add(source[j]);
                                                already_compared[j] = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case SimilarityMethod.SimilarWords:
                        {
                            for (int i = 0; i < simplified_source.Count - 1; i++)
                            {
                                for (int j = i + 1; j < simplified_source.Count; j++)
                                {
                                    if (!already_compared[j])
                                    {
                                        if (simplified_source[i].Text.HasSimilarWordsTo(simplified_source[j].Text, (int)Math.Round((Math.Min(simplified_source[i].Words.Count, simplified_source[j].Words.Count) * similarity_percentage)), similarity_percentage))
                                        {
                                            if (!verse_ranges.ContainsKey(source[i])) // first time matching verses found
                                            {
                                                List<Verse> similar_verses = new List<Verse>();
                                                verse_ranges.Add(source[i], similar_verses);
                                                similar_verses.Add(source[i]);
                                                similar_verses.Add(source[j]);
                                                already_compared[i] = true;
                                                already_compared[j] = true;
                                            }
                                            else // matching verses already exists
                                            {
                                                List<Verse> similar_verses = verse_ranges[source[i]];
                                                similar_verses.Add(source[j]);
                                                already_compared[j] = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case SimilarityMethod.SimilarFirstWord:
                        {
                            for (int i = 0; i < simplified_source.Count - 1; i++)
                            {
                                for (int j = i + 1; j < simplified_source.Count; j++)
                                {
                                    if (!already_compared[j])
                                    {
                                        if (simplified_source[j].Text.HasSimilarFirstWordTo(simplified_source[j].Text, similarity_percentage))
                                        {
                                            if (!verse_ranges.ContainsKey(source[i])) // first time matching verses found
                                            {
                                                List<Verse> similar_verses = new List<Verse>();
                                                verse_ranges.Add(source[i], similar_verses);
                                                similar_verses.Add(source[i]);
                                                similar_verses.Add(source[j]);
                                                already_compared[i] = true;
                                                already_compared[j] = true;
                                            }
                                            else // matching verses already exists
                                            {
                                                List<Verse> similar_verses = verse_ranges[source[i]];
                                                similar_verses.Add(source[j]);
                                                already_compared[j] = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case SimilarityMethod.SimilarLastWord:
                        {
                            for (int i = 0; i < simplified_source.Count - 1; i++)
                            {
                                for (int j = i + 1; j < simplified_source.Count; j++)
                                {
                                    if (!already_compared[j])
                                    {
                                        if (simplified_source[i].Text.HasSimilarLastWordTo(simplified_source[j].Text, similarity_percentage))
                                        {
                                            if (!verse_ranges.ContainsKey(source[i])) // first time matching verses found
                                            {
                                                List<Verse> similar_verses = new List<Verse>();
                                                verse_ranges.Add(source[i], similar_verses);
                                                similar_verses.Add(source[i]);
                                                similar_verses.Add(source[j]);
                                                already_compared[i] = true;
                                                already_compared[j] = true;
                                            }
                                            else // matching verses already exists
                                            {
                                                List<Verse> similar_verses = verse_ranges[source[i]];
                                                similar_verses.Add(source[j]);
                                                already_compared[j] = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        // copy dictionary to list of list
        if (verse_ranges.Count > 0)
        {
            foreach (List<Verse> verse_range in verse_ranges.Values)
            {
                result.Add(verse_range);
            }
        }

        return result;
    }


    // find by prostration type
    public static List<Verse> FindVerses(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, ProstrationType prostration_type)
    {
        return DoFindVerses(search_scope, current_selection, previous_verses, prostration_type);
    }
    private static List<Verse> DoFindVerses(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, ProstrationType prostration_type)
    {
        List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_verses, TextLocationInChapter.Any);
        return DoFindVerses(source, prostration_type);
    }
    private static List<Verse> DoFindVerses(List<Verse> source, ProstrationType prostration_type)
    {
        List<Verse> result = new List<Verse>();

        if (source != null)
        {
            if (source.Count > 0)
            {
                foreach (Verse verse in source)
                {
                    if ((verse.ProstrationType & prostration_type) > 0)
                    {
                        result.Add(verse);
                    }
                }
            }
        }

        return result;
    }


    // find by revelation place
    public static List<Chapter> FindChapters(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, RevelationPlace revelation_place)
    {
        return DoFindChapters(search_scope, current_selection, previous_verses, revelation_place);
    }
    private static List<Chapter> DoFindChapters(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, RevelationPlace revelation_place)
    {
        List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_verses, TextLocationInChapter.Any);
        return DoFindChapters(source, revelation_place);
    }
    private static List<Chapter> DoFindChapters(List<Verse> source, RevelationPlace revelation_place)
    {
        List<Chapter> result = new List<Chapter>();

        List<Verse> result_verses = new List<Verse>();
        if (source != null)
        {
            if (source.Count > 0)
            {
                foreach (Verse verse in source)
                {
                    if (verse.Chapter != null)
                    {
                        if (verse.Chapter.RevelationPlace == revelation_place)
                        {
                            result_verses.Add(verse);
                        }
                    }
                }
            }
        }

        int current_chapter_number = -1;
        foreach (Verse verse in result_verses)
        {
            if (verse.Chapter != null)
            {
                if (current_chapter_number != verse.Chapter.Number)
                {
                    current_chapter_number = verse.Chapter.Number;
                    result.Add(verse.Chapter);
                }
            }
        }

        return result;
    }


    // find by initialization type
    public static List<Verse> FindVerses(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, InitializationType initialization_type)
    {
        return DoFindVerses(search_scope, current_selection, previous_verses, initialization_type);
    }
    private static List<Verse> DoFindVerses(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, InitializationType initialization_type)
    {
        List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_verses, TextLocationInChapter.Any);
        return DoFindVerses(source, initialization_type);
    }
    private static List<Verse> DoFindVerses(List<Verse> source, InitializationType initialization_type)
    {
        List<Verse> result = new List<Verse>();

        if (source != null)
        {
            if (source.Count > 0)
            {
                foreach (Verse verse in source)
                {
                    if ((verse.InitializationType & initialization_type) > 0)
                    {
                        result.Add(verse);
                    }
                }
            }
        }

        return result;
    }


    // find by frequency - helper methods   
    public static int CalculateLetterFrequencySum(string text, string phrase, FrequencySearchType frequency_search_type, bool? include_diacritics)
    {
        int result = 0;

        if (s_numerology_system != null)
        {
            text = text.Replace("\r", "");
            text = text.Replace("\n", "");
            text = text.Replace("\t", "");
            text = text.Replace("_", "");
            text = text.Replace(" ", "");
            text = text.Replace(Constants.ORNATE_RIGHT_PARENTHESIS, "");
            text = text.Replace(Constants.ORNATE_LEFT_PARENTHESIS, "");
            foreach (char character in Constants.INDIAN_DIGITS)
            {
                text = text.Replace(character.ToString(), "");
            }
            foreach (char character in Constants.ARABIC_DIGITS)
            {
                text = text.Replace(character.ToString(), "");
            }
            foreach (char character in Constants.SYMBOLS)
            {
                text = text.Replace(character.ToString(), "");
            }
            text = text.Simplify(s_numerology_system.TextMode).Trim();

            if (!String.IsNullOrEmpty(phrase))
            {
                phrase = phrase.Replace("\r", "");
                phrase = phrase.Replace("\n", "");
                phrase = phrase.Replace("\t", "");
                phrase = phrase.Replace("_", "");
                phrase = phrase.Replace(" ", "");
                phrase = phrase.Replace(Constants.ORNATE_RIGHT_PARENTHESIS, "");
                phrase = phrase.Replace(Constants.ORNATE_LEFT_PARENTHESIS, "");
                foreach (char character in Constants.INDIAN_DIGITS)
                {
                    phrase = phrase.Replace(character.ToString(), "");
                }
                foreach (char character in Constants.ARABIC_DIGITS)
                {
                    phrase = phrase.Replace(character.ToString(), "");
                }
                foreach (char character in Constants.SYMBOLS)
                {
                    phrase = phrase.Replace(character.ToString(), "");
                }
                phrase = phrase.Simplify(s_numerology_system.TextMode).Trim();

                if (frequency_search_type == FrequencySearchType.UniqueLetters)
                {
                    phrase = phrase.RemoveDuplicates();
                    phrase = phrase.Replace(" ", "");
                }

                if (!String.IsNullOrEmpty(phrase))
                {
                    for (int i = 0; i < phrase.Length; i++)
                    {
                        int frequency = 0;
                        for (int j = 0; j < text.Length; j++)
                        {
                            if (phrase[i] == text[j])
                            {
                                frequency++;
                            }
                        }

                        if (frequency > 0)
                        {
                            result += frequency;
                        }
                    }
                }
            }
        }

        return result;
    }
    // find by frequency - Words
    public static List<Word> FindWords(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, string phrase, int sum, NumberType number_type, ComparisonOperator comparison_operator, int sum_remainder, FrequencySearchType frequency_search_type, bool? include_diacritics)
    {
        return DoFindWords(search_scope, current_selection, previous_verses, phrase, sum, number_type, comparison_operator, sum_remainder, frequency_search_type, include_diacritics);
    }
    private static List<Word> DoFindWords(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, string phrase, int sum, NumberType number_type, ComparisonOperator comparison_operator, int sum_remainder, FrequencySearchType frequency_search_type, bool? include_diacritics)
    {
        List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_verses, TextLocationInChapter.Any);
        return DoFindWords(source, phrase, sum, number_type, comparison_operator, sum_remainder, frequency_search_type, include_diacritics);
    }
    private static List<Word> DoFindWords(List<Verse> source, string phrase, int sum, NumberType number_type, ComparisonOperator comparison_operator, int sum_remainder, FrequencySearchType frequency_search_type, bool? include_diacritics)
    {
        List<Word> result = new List<Word>();

        if (!string.IsNullOrEmpty(phrase))
        {
            if (source != null)
            {
                if (source.Count > 0)
                {
                    if (!String.IsNullOrEmpty(phrase))
                    {
                        foreach (Verse verse in source)
                        {
                            if (verse != null)
                            {
                                foreach (Word word in verse.Words)
                                {
                                    string text = word.Text;
                                    int letter_frequency_sum = CalculateLetterFrequencySum(text, phrase, frequency_search_type, include_diacritics);
                                    if (Compare(letter_frequency_sum, sum, number_type, comparison_operator, sum_remainder))
                                    {
                                        result.Add(word);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
    // find by frequency - Sentences
    public static List<Sentence> FindSentences(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, string phrase, int sum, NumberType number_type, ComparisonOperator comparison_operator, int sum_remainder, FrequencySearchType frequency_search_type, bool? include_diacritics)
    {
        return DoFindSentences(search_scope, current_selection, previous_verses, phrase, sum, number_type, comparison_operator, sum_remainder, frequency_search_type, include_diacritics);
    }
    private static List<Sentence> DoFindSentences(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, string phrase, int sum, NumberType number_type, ComparisonOperator comparison_operator, int sum_remainder, FrequencySearchType frequency_search_type, bool? include_diacritics)
    {
        List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_verses, TextLocationInChapter.Any);
        return DoFindSentences(source, phrase, sum, number_type, comparison_operator, sum_remainder, frequency_search_type, include_diacritics);
    }
    private static List<Sentence> DoFindSentences(List<Verse> source, string phrase, int sum, NumberType number_type, ComparisonOperator comparison_operator, int sum_remainder, FrequencySearchType frequency_search_type, bool? include_diacritics)
    {
        List<Sentence> result = new List<Sentence>();

        if (source != null)
        {
            if (source.Count > 0)
            {
                List<Word> words = new List<Word>();
                foreach (Verse verse in source)
                {
                    words.AddRange(verse.Words);
                }

                if (s_numerology_system != null)
                {
                    // scan linearly for sequence of words with total Text matching query
                    bool done_MustContinue = false;
                    for (int i = 0; i < words.Count - 1; i++)
                    {
                        StringBuilder str = new StringBuilder();

                        // start building word sequence
                        str.Append(words[i].Text);

                        string stopmark_text = StopmarkHelper.GetStopmarkText(words[i].Stopmark);

                        // 1-word sentence
                        if (
                             (words[i].Stopmark != Stopmark.None) &&
                             (words[i].Stopmark != Stopmark.CanStopAtEither) &&
                             (words[i].Stopmark != Stopmark.MustPause) //&&
                            //(words[i].Stopmark != Stopmark.MustContinue)
                           )
                        {
                            Sentence sentence = new Sentence(words[i].Verse, words[i].Position, words[i].Verse, words[i].Position + words[i].Text.Length, str.ToString());
                            if (sentence != null)
                            {
                                string text = sentence.ToString();
                                int letter_frequency_sum = CalculateLetterFrequencySum(text, phrase, frequency_search_type, include_diacritics);
                                if (Compare(letter_frequency_sum, sum, number_type, comparison_operator, sum_remainder))
                                {
                                    result.Add(sentence);
                                }
                            }
                        }
                        else // multi-word sentence
                        {
                            // mark the start of 1-to-m MustContinue stopmarks
                            int backup_i = i;

                            // continue building with next words until a stopmark
                            bool done_CanStopAtEither = false;
                            for (int j = i + 1; j < words.Count; j++)
                            {
                                str.Append(" " + words[j].Text);

                                if (words[j].Stopmark == Stopmark.None)
                                {
                                    continue; // continue building longer senetence
                                }
                                else // there is a real stopmark
                                {
                                    if (s_numerology_system.TextMode == "Original")
                                    {
                                        str.Append(" " + stopmark_text);
                                    }

                                    if (words[j].Stopmark == Stopmark.MustContinue)
                                    {
                                        // TEST Stopmark.MustContinue
                                        //----1 2 3 4 sentences
                                        //1268
                                        //4153
                                        //1799
                                        //2973
                                        //----1 12 123 1234 sentences
                                        //1268
                                        //5421
                                        //7220
                                        //10193
                                        //-------------
                                        //ERRORS
                                        //# duplicate 1
                                        //# short str
                                        //  in 123 1234
                                        //-------------
                                        //// not needed yet
                                        //// multi-mid sentences
                                        //5952
                                        //4772

                                        Sentence sentence = new Sentence(words[i].Verse, words[i].Position, words[j].Verse, words[j].Position + words[j].Text.Length, str.ToString());
                                        if (sentence != null)
                                        {
                                            string text = sentence.ToString();
                                            int letter_frequency_sum = CalculateLetterFrequencySum(text, phrase, frequency_search_type, include_diacritics);
                                            if (Compare(letter_frequency_sum, sum, number_type, comparison_operator, sum_remainder))
                                            {
                                                result.Add(sentence);
                                            }
                                        }

                                        if (done_MustContinue)
                                        {
                                            done_MustContinue = false;
                                            continue; // get all overlapping long sentence
                                        }

                                        StringBuilder k_str = new StringBuilder();
                                        for (int k = j + 1; k < words.Count; k++)
                                        {
                                            k_str.Append(words[k].Text + " ");

                                            if (words[k].Stopmark == Stopmark.None)
                                            {
                                                continue; // next k
                                            }
                                            else // there is a stopmark
                                            {
                                                if (s_numerology_system.TextMode == "Original")
                                                {
                                                    stopmark_text = StopmarkHelper.GetStopmarkText(words[k].Stopmark);
                                                    k_str.Append(stopmark_text + " ");
                                                }
                                                if (k_str.Length > 0)
                                                {
                                                    k_str.Remove(k_str.Length - 1, 1);
                                                }

                                                sentence = new Sentence(words[j + 1].Verse, words[j + 1].Position, words[k].Verse, words[k].Position + words[k].Text.Length, k_str.ToString());
                                                if (sentence != null)
                                                {
                                                    string text = sentence.ToString();
                                                    int letter_frequency_sum = CalculateLetterFrequencySum(text, phrase, frequency_search_type, include_diacritics);
                                                    if (Compare(letter_frequency_sum, sum, number_type, comparison_operator, sum_remainder))
                                                    {
                                                        result.Add(sentence);
                                                    }
                                                }

                                                if (
                                                     (words[k].Stopmark == Stopmark.ShouldContinue) ||
                                                     (words[k].Stopmark == Stopmark.CanStop) ||
                                                     (words[k].Stopmark == Stopmark.ShouldStop)
                                                   )
                                                {
                                                    done_MustContinue = true;   // restart from beginning skipping any MustContinue
                                                }
                                                else
                                                {
                                                    done_MustContinue = false;   // keep building ever-longer multi-MustContinue sentence
                                                }

                                                j = k;
                                                break; // next j
                                            }
                                        }

                                        if (done_MustContinue)
                                        {
                                            i = backup_i - 1;  // start new sentence from beginning
                                            break; // next i
                                        }
                                        else
                                        {
                                            continue; // next j
                                        }
                                    }
                                    else if (
                                         (words[j].Stopmark == Stopmark.ShouldContinue) ||
                                         (words[j].Stopmark == Stopmark.CanStop) ||
                                         (words[j].Stopmark == Stopmark.ShouldStop)
                                       )
                                    {
                                        Sentence sentence = new Sentence(words[i].Verse, words[i].Position, words[j].Verse, words[j].Position + words[j].Text.Length, str.ToString());
                                        if (sentence != null)
                                        {
                                            string text = sentence.ToString();
                                            int letter_frequency_sum = CalculateLetterFrequencySum(text, phrase, frequency_search_type, include_diacritics);
                                            if (Compare(letter_frequency_sum, sum, number_type, comparison_operator, sum_remainder))
                                            {
                                                result.Add(sentence);
                                            }
                                        }

                                        i = j; // start new sentence after j
                                        break; // next i
                                    }
                                    else if (words[j].Stopmark == Stopmark.MustPause)
                                    {
                                        if (
                                             (words[j].Text.Simplify(s_numerology_system.TextMode) == "مَنْ".Simplify(s_numerology_system.TextMode)) ||
                                             (words[j].Text.Simplify(s_numerology_system.TextMode) == "بَلْ".Simplify(s_numerology_system.TextMode))
                                           )
                                        {
                                            continue; // continue building longer senetence
                                        }
                                        else if (
                                                  (words[j].Text.Simplify(s_numerology_system.TextMode) == "عِوَجَا".Simplify(s_numerology_system.TextMode)) ||
                                                  (words[j].Text.Simplify(s_numerology_system.TextMode) == "مَّرْقَدِنَا".Simplify(s_numerology_system.TextMode)) ||
                                                  (words[j].Text.Simplify(s_numerology_system.TextMode) == "مَالِيَهْ".Simplify(s_numerology_system.TextMode))
                                                )
                                        {
                                            Sentence sentence = new Sentence(words[i].Verse, words[i].Position, words[j].Verse, words[j].Position + words[j].Text.Length, str.ToString());
                                            if (sentence != null)
                                            {
                                                string text = sentence.ToString();
                                                int letter_frequency_sum = CalculateLetterFrequencySum(text, phrase, frequency_search_type, include_diacritics);
                                                if (Compare(letter_frequency_sum, sum, number_type, comparison_operator, sum_remainder))
                                                {
                                                    result.Add(sentence);
                                                }
                                            }

                                            i = j; // start new sentence after j
                                            break; // next i
                                        }
                                        else // unknown case
                                        {
                                            throw new Exception("Unknown paused Quran word.");
                                        }
                                    }
                                    // first CanStopAtEither found at j
                                    else if ((!done_CanStopAtEither) && (words[j].Stopmark == Stopmark.CanStopAtEither))
                                    {
                                        // ^ ذَٰلِكَ ٱلْكِتَٰبُ لَا رَيْبَ
                                        Sentence sentence = new Sentence(words[i].Verse, words[i].Position, words[j].Verse, words[j].Position + words[j].Text.Length, str.ToString());
                                        if (sentence != null)
                                        {
                                            string text = sentence.ToString();
                                            int letter_frequency_sum = CalculateLetterFrequencySum(text, phrase, frequency_search_type, include_diacritics);
                                            if (Compare(letter_frequency_sum, sum, number_type, comparison_operator, sum_remainder))
                                            {
                                                result.Add(sentence);
                                            }
                                        }

                                        int kk = -1; // start after ^ (e.g. هُدًۭى)
                                        StringBuilder kk_str = new StringBuilder();
                                        StringBuilder kkk_str = new StringBuilder();
                                        for (int k = j + 1; k < words.Count; k++)
                                        {
                                            str.Append(" " + words[k].Text);
                                            if (kkk_str.Length > 0) // skip first k loop
                                            {
                                                kk_str.Append(" " + words[k].Text);
                                            }
                                            kkk_str.Append(" " + words[k].Text);

                                            if (words[k].Stopmark == Stopmark.None)
                                            {
                                                continue; // next k
                                            }
                                            else // there is a stopmark
                                            {
                                                if (s_numerology_system.TextMode == "Original")
                                                {
                                                    str.Append(" " + stopmark_text);
                                                    if (kk_str.Length > 0)
                                                    {
                                                        kk_str.Append(" " + stopmark_text);
                                                    }
                                                    kkk_str.Append(" " + stopmark_text);
                                                }

                                                // second CanStopAtEither found at k
                                                if (words[k].Stopmark == Stopmark.CanStopAtEither)
                                                {
                                                    // ^ ذَٰلِكَ ٱلْكِتَٰبُ لَا رَيْبَ ۛ^ فِيهِ
                                                    sentence = new Sentence(words[i].Verse, words[i].Position, words[k].Verse, words[k].Position + words[k].Text.Length, str.ToString());
                                                    if (sentence != null)
                                                    {
                                                        string text = sentence.ToString();
                                                        int letter_frequency_sum = CalculateLetterFrequencySum(text, phrase, frequency_search_type, include_diacritics);
                                                        if (Compare(letter_frequency_sum, sum, number_type, comparison_operator, sum_remainder))
                                                        {
                                                            result.Add(sentence);
                                                        }
                                                    }

                                                    kk = k + 1; // backup k after second ^
                                                    continue; // next k
                                                }
                                                else // non-CanStopAtEither stopmark
                                                {
                                                    // kkk_str   فِيهِ ۛ^ هُدًۭى لِّلْمُتَّقِينَ
                                                    sentence = new Sentence(words[j + 1].Verse, words[j + 1].Position, words[k].Verse, words[k].Position + words[k].Text.Length, kkk_str.ToString());
                                                    if (sentence != null)
                                                    {
                                                        string text = sentence.ToString();
                                                        int letter_frequency_sum = CalculateLetterFrequencySum(text, phrase, frequency_search_type, include_diacritics);
                                                        if (Compare(letter_frequency_sum, sum, number_type, comparison_operator, sum_remainder))
                                                        {
                                                            result.Add(sentence);
                                                        }
                                                    }

                                                    // kk_str   هُدًۭى لِّلْمُتَّقِينَ
                                                    sentence = new Sentence(words[kk].Verse, words[kk].Position, words[k].Verse, words[k].Position + words[k].Text.Length, kk_str.ToString());
                                                    if (sentence != null)
                                                    {
                                                        string text = sentence.ToString();
                                                        int letter_frequency_sum = CalculateLetterFrequencySum(text, phrase, frequency_search_type, include_diacritics);
                                                        if (Compare(letter_frequency_sum, sum, number_type, comparison_operator, sum_remainder))
                                                        {
                                                            result.Add(sentence);
                                                        }
                                                    }

                                                    // skip the whole surrounding non-CanStopAtEither sentence
                                                    j = k;
                                                    break; // next j
                                                }
                                            }
                                        }

                                        // restart from last
                                        str.Length = 0;
                                        j = i - 1; // will be j++ by reloop
                                        done_CanStopAtEither = true;
                                    }
                                    else if (words[j].Stopmark == Stopmark.MustStop)
                                    {
                                        Sentence sentence = new Sentence(words[i].Verse, words[i].Position, words[j].Verse, words[j].Position + words[j].Text.Length, str.ToString());
                                        if (sentence != null)
                                        {
                                            string text = sentence.ToString();
                                            int letter_frequency_sum = CalculateLetterFrequencySum(text, phrase, frequency_search_type, include_diacritics);
                                            if (Compare(letter_frequency_sum, sum, number_type, comparison_operator, sum_remainder))
                                            {
                                                result.Add(sentence);
                                            }
                                        }

                                        i = j; // start new sentence after j
                                        break; // next i
                                    }
                                    else // unknown case
                                    {
                                        continue;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
    // find by frequency - Verses
    public static List<Verse> FindVerses(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, string phrase, int sum, NumberType number_type, ComparisonOperator comparison_operator, int sum_remainder, FrequencySearchType frequency_search_type, bool? include_diacritics)
    {
        return DoFindVerses(search_scope, current_selection, previous_verses, phrase, sum, number_type, comparison_operator, sum_remainder, frequency_search_type, include_diacritics);
    }
    private static List<Verse> DoFindVerses(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, string phrase, int sum, NumberType number_type, ComparisonOperator comparison_operator, int sum_remainder, FrequencySearchType frequency_search_type, bool? include_diacritics)
    {
        List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_verses, TextLocationInChapter.Any);
        return DoFindVerses(source, phrase, sum, number_type, comparison_operator, sum_remainder, frequency_search_type, include_diacritics);
    }
    private static List<Verse> DoFindVerses(List<Verse> source, string phrase, int sum, NumberType number_type, ComparisonOperator comparison_operator, int sum_remainder, FrequencySearchType frequency_search_type, bool? include_diacritics)
    {
        List<Verse> result = new List<Verse>();

        if (!string.IsNullOrEmpty(phrase))
        {
            if (source != null)
            {
                if (source.Count > 0)
                {
                    if (!String.IsNullOrEmpty(phrase))
                    {
                        foreach (Verse verse in source)
                        {
                            if (verse != null)
                            {
                                string text = verse.Text;
                                int letter_frequency_sum = CalculateLetterFrequencySum(text, phrase, frequency_search_type, include_diacritics);
                                if (Compare(letter_frequency_sum, sum, number_type, comparison_operator, sum_remainder))
                                {
                                    result.Add(verse);
                                }
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
    // find by frequency - Chapters
    public static List<Chapter> FindChapters(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, string phrase, int sum, NumberType number_type, ComparisonOperator comparison_operator, int sum_remainder, FrequencySearchType frequency_search_type, bool? include_diacritics)
    {
        return DoFindChapters(search_scope, current_selection, previous_verses, phrase, sum, number_type, comparison_operator, sum_remainder, frequency_search_type, include_diacritics);
    }
    private static List<Chapter> DoFindChapters(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, string phrase, int sum, NumberType number_type, ComparisonOperator comparison_operator, int sum_remainder, FrequencySearchType frequency_search_type, bool? include_diacritics)
    {
        List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_verses, TextLocationInChapter.Any);
        return DoFindChapters(source, phrase, sum, number_type, comparison_operator, sum_remainder, frequency_search_type, include_diacritics);
    }
    private static List<Chapter> DoFindChapters(List<Verse> source, string phrase, int sum, NumberType number_type, ComparisonOperator comparison_operator, int sum_remainder, FrequencySearchType frequency_search_type, bool? include_diacritics)
    {
        List<Chapter> result = new List<Chapter>();

        if (!string.IsNullOrEmpty(phrase))
        {
            if (source != null)
            {
                if (source.Count > 0)
                {
                    if (s_book != null)
                    {
                        List<Chapter> source_chapters = s_book.GetChapters(source);
                        if (!String.IsNullOrEmpty(phrase))
                        {
                            foreach (Chapter chapter in source_chapters)
                            {
                                if (chapter != null)
                                {
                                    string text = chapter.Text;
                                    int letter_frequency_sum = CalculateLetterFrequencySum(text, phrase, frequency_search_type, include_diacritics);
                                    if (Compare(letter_frequency_sum, sum, number_type, comparison_operator, sum_remainder))
                                    {
                                        result.Add(chapter);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return result;
    }

    // find by frequency matching letters - Words
    public static List<Word> FindWords(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, string phrase, FrequencyMatchingType frequency_matching_type, FrequencySearchType frequency_search_type, bool? include_diacritics)
    {
        return DoFindWords(search_scope, current_selection, previous_verses, phrase, frequency_matching_type, frequency_search_type, include_diacritics);
    }
    private static List<Word> DoFindWords(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, string phrase, FrequencyMatchingType frequency_matching_type, FrequencySearchType frequency_search_type, bool? include_diacritics)
    {
        List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_verses, TextLocationInChapter.Any);
        return DoFindWords(source, phrase, frequency_matching_type, frequency_search_type, include_diacritics);
    }
    private static List<Word> DoFindWords(List<Verse> source, string phrase, FrequencyMatchingType frequency_matching_type, FrequencySearchType frequency_search_type, bool? include_diacritics)
    {
        List<Word> result = new List<Word>();

        if (!string.IsNullOrEmpty(phrase))
        {
            if (source != null)
            {
                if (source.Count > 0)
                {
                    if (!String.IsNullOrEmpty(phrase))
                    {
                        if (s_numerology_system != null)
                        {
                            foreach (Verse verse in source)
                            {
                                if (verse != null)
                                {
                                    foreach (Word word in verse.Words)
                                    {
                                        string text = word.Text;
                                        if (IsMatchingLetters(s_numerology_system.TextMode, text, phrase, frequency_matching_type, frequency_search_type, include_diacritics))
                                        {
                                            result.Add(word);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
    // find by frequency matching letters - Sentences
    public static List<Sentence> FindSentences(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, string phrase, FrequencyMatchingType frequency_matching_type, FrequencySearchType frequency_search_type, bool? include_diacritics)
    {
        return DoFindSentences(search_scope, current_selection, previous_verses, phrase, frequency_matching_type, frequency_search_type, include_diacritics);
    }
    private static List<Sentence> DoFindSentences(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, string phrase, FrequencyMatchingType frequency_matching_type, FrequencySearchType frequency_search_type, bool? include_diacritics)
    {
        List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_verses, TextLocationInChapter.Any);
        return DoFindSentences(source, phrase, frequency_matching_type, frequency_search_type, include_diacritics);
    }
    private static List<Sentence> DoFindSentences(List<Verse> source, string phrase, FrequencyMatchingType frequency_matching_type, FrequencySearchType frequency_search_type, bool? include_diacritics)
    {
        List<Sentence> result = new List<Sentence>();

        if (source != null)
        {
            if (source.Count > 0)
            {
                if (s_numerology_system != null)
                {
                    List<Word> words = new List<Word>();
                    foreach (Verse verse in source)
                    {
                        words.AddRange(verse.Words);
                    }

                    // scan linearly for sequence of words with total Text matching query
                    bool done_MustContinue = false;
                    for (int i = 0; i < words.Count - 1; i++)
                    {
                        StringBuilder str = new StringBuilder();

                        // start building word sequence
                        str.Append(words[i].Text);

                        string stopmark_text = StopmarkHelper.GetStopmarkText(words[i].Stopmark);

                        // 1-word sentence
                        if (
                             (words[i].Stopmark != Stopmark.None) &&
                             (words[i].Stopmark != Stopmark.CanStopAtEither) &&
                             (words[i].Stopmark != Stopmark.MustPause) //&&
                            //(words[i].Stopmark != Stopmark.MustContinue)
                           )
                        {
                            Sentence sentence = new Sentence(words[i].Verse, words[i].Position, words[i].Verse, words[i].Position + words[i].Text.Length, str.ToString());
                            if (sentence != null)
                            {
                                string text = sentence.ToString();
                                if (IsMatchingLetters(s_numerology_system.TextMode, text, phrase, frequency_matching_type, frequency_search_type, include_diacritics))
                                {
                                    result.Add(sentence);
                                }
                            }
                        }
                        else // multi-word sentence
                        {
                            // mark the start of 1-to-m MustContinue stopmarks
                            int backup_i = i;

                            // continue building with next words until a stopmark
                            bool done_CanStopAtEither = false;
                            for (int j = i + 1; j < words.Count; j++)
                            {
                                str.Append(" " + words[j].Text);

                                if (words[j].Stopmark == Stopmark.None)
                                {
                                    continue; // continue building longer senetence
                                }
                                else // there is a real stopmark
                                {
                                    if (s_numerology_system.TextMode == "Original")
                                    {
                                        str.Append(" " + stopmark_text);
                                    }

                                    if (words[j].Stopmark == Stopmark.MustContinue)
                                    {
                                        // TEST Stopmark.MustContinue
                                        //----1 2 3 4 sentences
                                        //1268
                                        //4153
                                        //1799
                                        //2973
                                        //----1 12 123 1234 sentences
                                        //1268
                                        //5421
                                        //7220
                                        //10193
                                        //-------------
                                        //ERRORS
                                        //# duplicate 1
                                        //# short str
                                        //  in 123 1234
                                        //-------------
                                        //// not needed yet
                                        //// multi-mid sentences
                                        //5952
                                        //4772

                                        Sentence sentence = new Sentence(words[i].Verse, words[i].Position, words[j].Verse, words[j].Position + words[j].Text.Length, str.ToString());
                                        if (sentence != null)
                                        {
                                            string text = sentence.ToString();
                                            if (IsMatchingLetters(s_numerology_system.TextMode, text, phrase, frequency_matching_type, frequency_search_type, include_diacritics))
                                            {
                                                result.Add(sentence);
                                            }
                                        }

                                        if (done_MustContinue)
                                        {
                                            done_MustContinue = false;
                                            continue; // get all overlapping long sentence
                                        }

                                        StringBuilder k_str = new StringBuilder();
                                        for (int k = j + 1; k < words.Count; k++)
                                        {
                                            k_str.Append(words[k].Text + " ");

                                            if (words[k].Stopmark == Stopmark.None)
                                            {
                                                continue; // next k
                                            }
                                            else // there is a stopmark
                                            {
                                                if (s_numerology_system.TextMode == "Original")
                                                {
                                                    stopmark_text = StopmarkHelper.GetStopmarkText(words[k].Stopmark);
                                                    k_str.Append(stopmark_text + " ");
                                                }
                                                if (k_str.Length > 0)
                                                {
                                                    k_str.Remove(k_str.Length - 1, 1);
                                                }

                                                sentence = new Sentence(words[j + 1].Verse, words[j + 1].Position, words[k].Verse, words[k].Position + words[k].Text.Length, k_str.ToString());
                                                if (sentence != null)
                                                {
                                                    string text = sentence.ToString();
                                                    if (IsMatchingLetters(s_numerology_system.TextMode, text, phrase, frequency_matching_type, frequency_search_type, include_diacritics))
                                                    {
                                                        result.Add(sentence);
                                                    }
                                                }

                                                if (
                                                     (words[k].Stopmark == Stopmark.ShouldContinue) ||
                                                     (words[k].Stopmark == Stopmark.CanStop) ||
                                                     (words[k].Stopmark == Stopmark.ShouldStop)
                                                   )
                                                {
                                                    done_MustContinue = true;   // restart from beginning skipping any MustContinue
                                                }
                                                else
                                                {
                                                    done_MustContinue = false;   // keep building ever-longer multi-MustContinue sentence
                                                }

                                                j = k;
                                                break; // next j
                                            }
                                        }

                                        if (done_MustContinue)
                                        {
                                            i = backup_i - 1;  // start new sentence from beginning
                                            break; // next i
                                        }
                                        else
                                        {
                                            continue; // next j
                                        }
                                    }
                                    else if (
                                         (words[j].Stopmark == Stopmark.ShouldContinue) ||
                                         (words[j].Stopmark == Stopmark.CanStop) ||
                                         (words[j].Stopmark == Stopmark.ShouldStop)
                                       )
                                    {
                                        Sentence sentence = new Sentence(words[i].Verse, words[i].Position, words[j].Verse, words[j].Position + words[j].Text.Length, str.ToString());
                                        if (sentence != null)
                                        {
                                            string text = sentence.ToString();
                                            if (IsMatchingLetters(s_numerology_system.TextMode, text, phrase, frequency_matching_type, frequency_search_type, include_diacritics))
                                            {
                                                result.Add(sentence);
                                            }
                                        }

                                        i = j; // start new sentence after j
                                        break; // next i
                                    }
                                    else if (words[j].Stopmark == Stopmark.MustPause)
                                    {
                                        if (
                                             (words[j].Text.Simplify(s_numerology_system.TextMode) == "مَنْ".Simplify(s_numerology_system.TextMode)) ||
                                             (words[j].Text.Simplify(s_numerology_system.TextMode) == "بَلْ".Simplify(s_numerology_system.TextMode))
                                           )
                                        {
                                            continue; // continue building longer senetence
                                        }
                                        else if (
                                                  (words[j].Text.Simplify(s_numerology_system.TextMode) == "عِوَجَا".Simplify(s_numerology_system.TextMode)) ||
                                                  (words[j].Text.Simplify(s_numerology_system.TextMode) == "مَّرْقَدِنَا".Simplify(s_numerology_system.TextMode)) ||
                                                  (words[j].Text.Simplify(s_numerology_system.TextMode) == "مَالِيَهْ".Simplify(s_numerology_system.TextMode))
                                                )
                                        {
                                            Sentence sentence = new Sentence(words[i].Verse, words[i].Position, words[j].Verse, words[j].Position + words[j].Text.Length, str.ToString());
                                            if (sentence != null)
                                            {
                                                string text = sentence.ToString();
                                                if (IsMatchingLetters(s_numerology_system.TextMode, text, phrase, frequency_matching_type, frequency_search_type, include_diacritics))
                                                {
                                                    result.Add(sentence);
                                                }
                                            }

                                            i = j; // start new sentence after j
                                            break; // next i
                                        }
                                        else // unknown case
                                        {
                                            throw new Exception("Unknown paused Quran word.");
                                        }
                                    }
                                    // first CanStopAtEither found at j
                                    else if ((!done_CanStopAtEither) && (words[j].Stopmark == Stopmark.CanStopAtEither))
                                    {
                                        // ^ ذَٰلِكَ ٱلْكِتَٰبُ لَا رَيْبَ
                                        Sentence sentence = new Sentence(words[i].Verse, words[i].Position, words[j].Verse, words[j].Position + words[j].Text.Length, str.ToString());
                                        if (sentence != null)
                                        {
                                            string text = sentence.ToString();
                                            if (IsMatchingLetters(s_numerology_system.TextMode, text, phrase, frequency_matching_type, frequency_search_type, include_diacritics))
                                            {
                                                result.Add(sentence);
                                            }
                                        }

                                        int kk = -1; // start after ^ (e.g. هُدًۭى)
                                        StringBuilder kk_str = new StringBuilder();
                                        StringBuilder kkk_str = new StringBuilder();
                                        for (int k = j + 1; k < words.Count; k++)
                                        {
                                            str.Append(" " + words[k].Text);
                                            if (kkk_str.Length > 0) // skip first k loop
                                            {
                                                kk_str.Append(" " + words[k].Text);
                                            }
                                            kkk_str.Append(" " + words[k].Text);

                                            if (words[k].Stopmark == Stopmark.None)
                                            {
                                                continue; // next k
                                            }
                                            else // there is a stopmark
                                            {
                                                if (s_numerology_system.TextMode == "Original")
                                                {
                                                    str.Append(" " + stopmark_text);
                                                    if (kk_str.Length > 0)
                                                    {
                                                        kk_str.Append(" " + stopmark_text);
                                                    }
                                                    kkk_str.Append(" " + stopmark_text);
                                                }

                                                // second CanStopAtEither found at k
                                                if (words[k].Stopmark == Stopmark.CanStopAtEither)
                                                {
                                                    // ^ ذَٰلِكَ ٱلْكِتَٰبُ لَا رَيْبَ ۛ^ فِيهِ
                                                    sentence = new Sentence(words[i].Verse, words[i].Position, words[k].Verse, words[k].Position + words[k].Text.Length, str.ToString());
                                                    if (sentence != null)
                                                    {
                                                        string text = sentence.ToString();
                                                        if (IsMatchingLetters(s_numerology_system.TextMode, text, phrase, frequency_matching_type, frequency_search_type, include_diacritics))
                                                        {
                                                            result.Add(sentence);
                                                        }
                                                    }

                                                    kk = k + 1; // backup k after second ^
                                                    continue; // next k
                                                }
                                                else // non-CanStopAtEither stopmark
                                                {
                                                    // kkk_str   فِيهِ ۛ^ هُدًۭى لِّلْمُتَّقِينَ
                                                    sentence = new Sentence(words[j + 1].Verse, words[j + 1].Position, words[k].Verse, words[k].Position + words[k].Text.Length, kkk_str.ToString());
                                                    if (sentence != null)
                                                    {
                                                        string text = sentence.ToString();
                                                        if (IsMatchingLetters(s_numerology_system.TextMode, text, phrase, frequency_matching_type, frequency_search_type, include_diacritics))
                                                        {
                                                            result.Add(sentence);
                                                        }
                                                    }

                                                    // kk_str   هُدًۭى لِّلْمُتَّقِينَ
                                                    sentence = new Sentence(words[kk].Verse, words[kk].Position, words[k].Verse, words[k].Position + words[k].Text.Length, kk_str.ToString());
                                                    if (sentence != null)
                                                    {
                                                        string text = sentence.ToString();
                                                        if (IsMatchingLetters(s_numerology_system.TextMode, text, phrase, frequency_matching_type, frequency_search_type, include_diacritics))
                                                        {
                                                            result.Add(sentence);
                                                        }
                                                    }

                                                    // skip the whole surrounding non-CanStopAtEither sentence
                                                    j = k;
                                                    break; // next j
                                                }
                                            }
                                        }

                                        // restart from last
                                        str.Length = 0;
                                        j = i - 1; // will be j++ by reloop
                                        done_CanStopAtEither = true;
                                    }
                                    else if (words[j].Stopmark == Stopmark.MustStop)
                                    {
                                        Sentence sentence = new Sentence(words[i].Verse, words[i].Position, words[j].Verse, words[j].Position + words[j].Text.Length, str.ToString());
                                        if (sentence != null)
                                        {
                                            string text = sentence.ToString();
                                            if (IsMatchingLetters(s_numerology_system.TextMode, text, phrase, frequency_matching_type, frequency_search_type, include_diacritics))
                                            {
                                                result.Add(sentence);
                                            }
                                        }

                                        i = j; // start new sentence after j
                                        break; // next i
                                    }
                                    else // unknown case
                                    {
                                        continue;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
    // find by frequency matching letters - Verses
    public static List<Verse> FindVerses(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, string phrase, FrequencyMatchingType frequency_matching_type, FrequencySearchType frequency_search_type, bool? include_diacritics)
    {
        return DoFindVerses(search_scope, current_selection, previous_verses, phrase, frequency_matching_type, frequency_search_type, include_diacritics);
    }
    private static List<Verse> DoFindVerses(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, string phrase, FrequencyMatchingType frequency_matching_type, FrequencySearchType frequency_search_type, bool? include_diacritics)
    {
        List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_verses, TextLocationInChapter.Any);
        return DoFindVerses(source, phrase, frequency_matching_type, frequency_search_type, include_diacritics);
    }
    private static List<Verse> DoFindVerses(List<Verse> source, string phrase, FrequencyMatchingType frequency_matching_type, FrequencySearchType frequency_search_type, bool? include_diacritics)
    {
        List<Verse> result = new List<Verse>();

        if (!string.IsNullOrEmpty(phrase))
        {
            if (source != null)
            {
                if (source.Count > 0)
                {
                    if (!String.IsNullOrEmpty(phrase))
                    {
                        if (s_numerology_system != null)
                        {
                            foreach (Verse verse in source)
                            {
                                if (verse != null)
                                {
                                    string text = verse.Text;
                                    if (IsMatchingLetters(s_numerology_system.TextMode, text, phrase, frequency_matching_type, frequency_search_type, include_diacritics))
                                    {
                                        result.Add(verse);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
    // find by frequency matching letters - Chapters
    public static List<Chapter> FindChapters(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, string phrase, FrequencyMatchingType frequency_matching_type, FrequencySearchType frequency_search_type, bool? include_diacritics)
    {
        return DoFindChapters(search_scope, current_selection, previous_verses, phrase, frequency_matching_type, frequency_search_type, include_diacritics);
    }
    private static List<Chapter> DoFindChapters(SearchScope search_scope, Selection current_selection, List<Verse> previous_verses, string phrase, FrequencyMatchingType frequency_matching_type, FrequencySearchType frequency_search_type, bool? include_diacritics)
    {
        List<Verse> source = GetSourceVerses(search_scope, current_selection, previous_verses, TextLocationInChapter.Any);
        return DoFindChapters(source, phrase, frequency_matching_type, frequency_search_type, include_diacritics);
    }
    private static List<Chapter> DoFindChapters(List<Verse> source, string phrase, FrequencyMatchingType frequency_matching_type, FrequencySearchType frequency_search_type, bool? include_diacritics)
    {
        List<Chapter> result = new List<Chapter>();

        if (!string.IsNullOrEmpty(phrase))
        {
            if (source != null)
            {
                if (source.Count > 0)
                {
                    if (s_book != null)
                    {
                        List<Chapter> source_chapters = s_book.GetChapters(source);
                        if (!String.IsNullOrEmpty(phrase))
                        {
                            if (s_numerology_system != null)
                            {
                                foreach (Chapter chapter in source_chapters)
                                {
                                    if (chapter != null)
                                    {
                                        string text = chapter.Text;
                                        if (IsMatchingLetters(s_numerology_system.TextMode, text, phrase, frequency_matching_type, frequency_search_type, include_diacritics))
                                        {
                                            result.Add(chapter);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return result;
    }
    public static bool IsMatchingLetters(string text_mode, string text, string phrase, FrequencyMatchingType frequency_matching_type, FrequencySearchType frequency_search_type, bool? include_diacritics)
    {
        if (String.IsNullOrEmpty(text)) return false;
        if (String.IsNullOrEmpty(phrase)) return false;

        text = text.Replace("\r", "");
        text = text.Replace("\n", "");
        text = text.Replace("\t", "");
        text = text.Replace("_", "");
        text = text.Replace(" ", "");
        text = text.Replace(Constants.ORNATE_RIGHT_PARENTHESIS, "");
        text = text.Replace(Constants.ORNATE_LEFT_PARENTHESIS, "");
        foreach (char character in Constants.INDIAN_DIGITS)
        {
            text = text.Replace(character.ToString(), "");
        }
        foreach (char character in Constants.ARABIC_DIGITS)
        {
            text = text.Replace(character.ToString(), "");
        }
        foreach (char character in Constants.SYMBOLS)
        {
            text = text.Replace(character.ToString(), "");
        }
        foreach (char character in Constants.STOPMARKS)
        {
            text = text.Replace(character.ToString(), "");
        }
        foreach (char character in Constants.QURANMARKS)
        {
            text = text.Replace(character.ToString(), "");
        }
        text = text.Simplify(s_numerology_system.TextMode).Trim();

        phrase = phrase.Simplify(NumerologySystem.TextMode);
        phrase = phrase.Replace("\r", "");
        phrase = phrase.Replace("\n", "");
        phrase = phrase.Replace("\t", "");
        phrase = phrase.Replace("_", "");
        phrase = phrase.Replace(" ", "");
        phrase = phrase.Replace(Constants.ORNATE_RIGHT_PARENTHESIS, "");
        phrase = phrase.Replace(Constants.ORNATE_LEFT_PARENTHESIS, "");
        foreach (char character in Constants.INDIAN_DIGITS)
        {
            phrase = phrase.Replace(character.ToString(), "");
        }
        foreach (char character in Constants.ARABIC_DIGITS)
        {
            phrase = phrase.Replace(character.ToString(), "");
        }
        foreach (char character in Constants.SYMBOLS)
        {
            phrase = phrase.Replace(character.ToString(), "");
        }
        foreach (char character in Constants.STOPMARKS)
        {
            phrase = phrase.Replace(character.ToString(), "");
        }
        foreach (char character in Constants.QURANMARKS)
        {
            phrase = phrase.Replace(character.ToString(), "");
        }
        phrase = phrase.Simplify(s_numerology_system.TextMode).Trim();

        if (frequency_search_type == FrequencySearchType.UniqueLetters)
        {
            phrase = phrase.RemoveDuplicates();
        }

        Dictionary<char, int> text_letter_statistics = new Dictionary<char, int>();
        if (text_letter_statistics != null)
        {
            for (int i = 0; i < text.Length; i++)
            {
                if (text_letter_statistics.ContainsKey(text[i]))
                {
                    text_letter_statistics[text[i]]++;
                }
                else
                {
                    text_letter_statistics.Add(text[i], 1);
                }
            }
        }

        Dictionary<char, int> phrase_letter_statistics = new Dictionary<char, int>();
        if (phrase_letter_statistics != null)
        {
            for (int i = 0; i < phrase.Length; i++)
            {
                if (phrase_letter_statistics.ContainsKey(phrase[i]))
                {
                    phrase_letter_statistics[phrase[i]]++;
                }
                else
                {
                    phrase_letter_statistics.Add(phrase[i], 1);
                }
            }
        }

        if ((text_letter_statistics != null) && (phrase_letter_statistics != null))
        {
            switch (frequency_matching_type)
            {
                case FrequencyMatchingType.AllLettersOf:
                    {
                        for (int i = 0; i < phrase.Length; i++)
                        {
                            if (!text_letter_statistics.ContainsKey(phrase[i]))
                            {
                                return false;
                            }

                            if (frequency_search_type == FrequencySearchType.DuplicateLetters)
                            {
                                if (text_letter_statistics[phrase[i]] != phrase_letter_statistics[phrase[i]])
                                {
                                    return false;
                                }
                            }
                        }
                    }
                    break;
                case FrequencyMatchingType.AnyLetterOf:
                    {
                        int count = 0;
                        for (int i = 0; i < phrase.Length; i++)
                        {
                            if (text_letter_statistics.ContainsKey(phrase[i]))
                            {
                                if (frequency_search_type == FrequencySearchType.DuplicateLetters)
                                {
                                    if (text_letter_statistics[phrase[i]] == phrase_letter_statistics[phrase[i]])
                                    {
                                        count++;
                                    }
                                }
                                else
                                {
                                    count++;
                                }
                            }
                        }
                        if (count == 0) return false;
                    }
                    break;
                case FrequencyMatchingType.OnlyLettersOf:
                    {
                        int count = 0;
                        for (int i = 0; i < text.Length; i++)
                        {
                            if (phrase_letter_statistics.ContainsKey(text[i]))
                            {
                                if (frequency_search_type == FrequencySearchType.DuplicateLetters)
                                {
                                    if (text_letter_statistics[text[i]] == phrase_letter_statistics[text[i]])
                                    {
                                        count++;
                                    }
                                }
                                else
                                {
                                    count++;
                                }
                            }
                            else
                            {
                                return false;
                            }
                        }
                        if (count < text.Length) return false;
                    }
                    break;
                case FrequencyMatchingType.NoLetterOf:
                    {
                        int count = 0;
                        for (int i = 0; i < text.Length; i++)
                        {
                            if (phrase_letter_statistics.ContainsKey(text[i]))
                            {
                                if (frequency_search_type == FrequencySearchType.DuplicateLetters)
                                {
                                    //if (text_letter_statistics[text[i]] == phrase_letter_statistics[text[i]])
                                    {
                                        count++;
                                    }
                                }
                                else
                                {
                                    count++;
                                }
                            }
                        }
                        if (count > 0) return false;
                    }
                    break;
                default:
                    {
                        return false;
                    }
            }
        }
        return true;
    }


    public static string GetTranslationKey(string translation)
    {
        string result = null;

        if (s_book != null)
        {
            if (s_book.TranslationInfos != null)
            {
                foreach (string key in s_book.TranslationInfos.Keys)
                {
                    if (s_book.TranslationInfos[key].Name == translation)
                    {
                        result = key;
                    }
                }
            }
        }

        return result;
    }
    public static void LoadTranslation(string translation)
    {
        DataAccess.LoadTranslation(s_book, translation);
    }
    public static void UnloadTranslation(string translation)
    {
        DataAccess.UnloadTranslation(s_book, translation);
    }
    public static void SaveTranslation(string translation)
    {
        DataAccess.SaveTranslation(s_book, translation);
    }


    // help messages
    private static List<string> s_help_messages = new List<string>();
    public static List<string> HelpMessages
    {
        get { return s_help_messages; }
    }
    private static void LoadHelpMessages()
    {
        string filename = Globals.HELP_FOLDER + Path.DirectorySeparatorChar + "Messages.txt";
        if (File.Exists(filename))
        {
            s_help_messages = FileHelper.LoadLines(filename);
        }
    }
}
