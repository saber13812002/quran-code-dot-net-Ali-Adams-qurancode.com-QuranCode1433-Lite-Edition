using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Model;

public class Client
{
    public const string DEFAULT_RECITATION = Server.DEFAULT_RECITATION;
    public const string DEFAULT_TRANSLITERATION = Server.DEFAULT_TRANSLITERATION;

    public Client(string numerology_system_name)
    {
        if (!Directory.Exists(Globals.BOOKMARKS_FOLDER))
        {
            Directory.CreateDirectory(Globals.BOOKMARKS_FOLDER);
        }

        if (!Directory.Exists(Globals.HISTORY_FOLDER))
        {
            Directory.CreateDirectory(Globals.HISTORY_FOLDER);
        }

        // load and set initial NumerologySystem
        LoadNumerologySystem(numerology_system_name);
    }

    // current book
    public Book Book
    {
        get { return Server.Book; }
    }

    // current simplification system
    public SimplificationSystem SimplificationSystem
    {
        get { return Server.SimplificationSystem; }
    }
    // all loaded simplification systems
    public static Dictionary<string, SimplificationSystem> LoadedSimplificationSystems
    {
        get { return Server.LoadedSimplificationSystems; }
    }
    public void BuildSimplifiedBook(string text_mode, bool with_diacritics, bool with_bism_Allah, bool waw_as_word, bool shadda_as_letter, bool hamza_above_horizontal_line_as_letter, bool elf_above_horizontal_line_as_letter, bool yaa_above_horizontal_line_as_letter, bool noon_above_horizontal_line_as_letter)
    {
        Server.BuildSimplifiedBook(text_mode, with_diacritics, with_bism_Allah, waw_as_word, shadda_as_letter, hamza_above_horizontal_line_as_letter, elf_above_horizontal_line_as_letter, yaa_above_horizontal_line_as_letter, noon_above_horizontal_line_as_letter);
        UpdatePhrasePositionsAndLengths(text_mode);
    }

    // current numerology system
    public NumerologySystem NumerologySystem
    {
        get { return Server.NumerologySystem; }
        set { Server.NumerologySystem = value; }
    }
    // all loaded numerology systems
    public Dictionary<string, NumerologySystem> LoadedNumerologySystems
    {
        get { return Server.LoadedNumerologySystems; }
    }
    // update current numerology system
    public void UpdateNumerologySystem(string text)
    {
        Server.UpdateNumerologySystem(text);
    }
    private void UpdatePhrasePositionsAndLengths(string text_mode)
    {
        if (Book != null)
        {
            // update Selection to point at new book object
            if (m_selection != null)
            {
                m_selection = new Selection(Book, m_selection.Scope, m_selection.Indexes);
            }

            if (m_selection != null)
            {
                if (NumerologySystem != null)
                {
                    // update FoundVerses to point at new book object
                    if (Book.Verses != null)
                    {
                        if (m_found_verses != null)
                        {
                            List<Verse> verses = new List<Verse>();
                            foreach (Verse verse in m_found_verses)
                            {
                                int index = verse.Number - 1;
                                if ((index >= 0) && (index < Book.Verses.Count))
                                {
                                    verses.Add(Book.Verses[index]);
                                }
                            }
                            m_found_verses = verses;
                        }
                    }

                    // update FoundPhrases to point at new book object
                    if (Book.Verses != null)
                    {
                        if (m_found_phrases != null)
                        {
                            for (int i = 0; i < m_found_phrases.Count; i++)
                            {
                                Phrase phrase = m_found_phrases[i];
                                if (phrase != null)
                                {
                                    int index = phrase.Verse.Number - 1;
                                    if ((index >= 0) && (index < Book.Verses.Count))
                                    {
                                        phrase = new Phrase(Book.Verses[index], phrase.Position, phrase.Text);
                                        m_found_phrases[i] = Server.SwitchTextMode(phrase, text_mode);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // ALSO should update these less used collections as they are already held by FoundVerses
            // update FoundVerseRanges to point at new book object
            // update FoundVerseSets to point at new book object
            // update FoundChapters to point at new book object
            // update FoundChapterRanges to point at new book object
            // update FoundChapterSets to point at new book object
        }
    }
    // load and replace current numerology system
    public void LoadNumerologySystem(string numerology_system_name)
    {
        Server.LoadNumerologySystem(numerology_system_name);
    }

    // translations
    public string GetTranslationKey(string translation)
    {
        return Server.GetTranslationKey(translation);
    }
    public void LoadTranslation(string translation)
    {
        Server.LoadTranslation(translation);
    }
    public void UnloadTranslation(string translation)
    {
        Server.UnloadTranslation(translation);
    }
    public void SaveTranslation(string translation)
    {
        Server.SaveTranslation(translation);
    }

    // for user text or Quran highlighted text
    public long CalculateValue(char character)
    {
        return Server.CalculateValue(character);
    }
    public long CalculateValue(string text)
    {
        if (NumerologySystem != null)
        {
            text = text.Replace("\t", "");
            text = text.Replace("_", "");
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
        }

        return Server.CalculateValue(text);
    }
    // for Quran text
    public long CalculateValue(Letter letter)
    {
        return Server.CalculateValue(letter);
    }
    public long CalculateValue(List<Letter> letters)
    {
        return Server.CalculateValue(letters);
    }
    public long CalculateValue(Word word)
    {
        return Server.CalculateValue(word);
    }
    public long CalculateValue(List<Word> words)
    {
        return Server.CalculateValue(words);
    }
    public long CalculateValue(Verse verse)
    {
        return Server.CalculateValue(verse);
    }
    public long CalculateValue(List<Verse> verses)
    {
        return Server.CalculateValue(verses);
    }
    public long CalculateValue(Chapter chapter)
    {
        return Server.CalculateValue(chapter);
    }
    public long CalculateValue(List<Chapter> chapters)
    {
        return Server.CalculateValue(chapters);
    }
    public long CalculateValue(Book book)
    {
        return Server.CalculateValue(book);
    }
    public long CalculateValue(List<Verse> verses, Letter start_letter, Letter end_letter)
    {
        return Server.CalculateValue(verses, start_letter, end_letter);
    }
    public List<long> CalculateVerseValues(List<Verse> verses)
    {
        List<long> result = new List<long>();
        foreach (Verse verse in verses)
        {
            long value = Server.CalculateValue(verse);
            result.Add(value);
        }
        return result;
    }
    public List<long> CalculateWordValues(List<Verse> verses)
    {
        List<long> result = new List<long>();
        foreach (Verse verse in verses)
        {
            foreach (Word word in verse.Words)
            {
                long value = 0L;
                value = Server.CalculateValue(word);
                result.Add(value);
            }
        }
        return result;
    }
    public List<long> CalculateLetterValues(List<Verse> verses)
    {
        List<long> result = new List<long>();
        foreach (Verse verse in verses)
        {
            foreach (Word word in verse.Words)
            {
                foreach (Letter letter in word.Letters)
                {
                    long value = 0L;
                    value = Server.CalculateValue(letter);
                    result.Add(value);
                }
            }
        }
        return result;
    }
    public long MaximumVerseValue
    {
        get
        {
            long result = 0L;
            foreach (Verse verse in Book.Verses)
            {
                long value = Server.CalculateValue(verse);
                if (result < value)
                {
                    result = value;
                }
            }
            return result;
        }
    }
    public long MaximumWordValue
    {
        get
        {
            long result = 0L;
            foreach (Verse verse in Book.Verses)
            {
                foreach (Word word in verse.Words)
                {
                    long value = Server.CalculateValue(word);
                    if (result < value)
                    {
                        result = value;
                    }
                }
            }
            return result;
        }
    }
    public long MaximumLetterValue
    {
        get
        {
            long result = 0L;
            if (NumerologySystem != null)
            {
                foreach (long value in NumerologySystem.Values)
                {
                    if (result < value)
                    {
                        result = value;
                    }
                }
            }
            return result;
        }
    }
    public void SaveValueCalculations(string filename, string text, bool is_value)
    {
        if (Directory.Exists(Globals.STATISTICS_FOLDER))
        {
            filename = Globals.STATISTICS_FOLDER + Path.DirectorySeparatorChar + filename;
            try
            {
                if (NumerologySystem != null)
                {
                    using (StreamWriter writer = new StreamWriter(filename, false, Encoding.Unicode))
                    {
                        StringBuilder numbers = new StringBuilder();
                        foreach (int index in Selection.Indexes)
                        {
                            numbers.Append((index + 1).ToString() + ", ");
                        }
                        if (numbers.Length > 0)
                        {
                            numbers.Remove(numbers.Length - 2, 2);
                        }

                        if (is_value)
                        {
                            writer.WriteLine("---------------------------------------------------------------------------------------------------------------------------");
                            writer.WriteLine(NumerologySystem.Name);
                            writer.WriteLine("Selection = " + Selection.Scope.ToString() + " " + numbers.ToString());
                            writer.WriteLine("---------------------------------------------------------------------------------------------------------------------------");
                            writer.WriteLine(NumerologySystem.ToOverview());
                            writer.WriteLine();
                        }
                        writer.WriteLine(is_value ? "Text" : "Number Analysis");
                        writer.WriteLine("---------------------------------------------------------------------------------------------------------------------------");
                        writer.WriteLine(text);
                    }
                }
            }
            catch
            {
                // silence IO error in case running from read-only media (CD/DVD)
            }

            // show file content after save
            FileHelper.DisplayFile(filename);
        }
    }
    public void SaveNumberIndexChain(string filename, long number, int chain_length, string text)
    {
        if (Directory.Exists(Globals.STATISTICS_FOLDER))
        {
            filename = Globals.STATISTICS_FOLDER + Path.DirectorySeparatorChar + filename;
            try
            {
                using (StreamWriter writer = new StreamWriter(filename, false, Encoding.Unicode))
                {
                    writer.WriteLine("-----------------------------------------------------");
                    writer.WriteLine(number.ToString() + " with IndexChainLength = " + chain_length.ToString());
                    writer.WriteLine("-----------------------------------------------------");
                    writer.WriteLine("Number\tTotal\tPC_L2R\tPC_R2L\tCP_L2R\tCP_R2L\tChain");
                    writer.WriteLine("-----------------------------------------------------");
                    writer.Write(text);
                    writer.WriteLine("-----------------------------------------------------");
                }
            }
            catch
            {
                // silence IO error in case running from read-only media (CD/DVD)
            }

            // show file content after save
            FileHelper.DisplayFile(filename);
        }
    }
    public void SaveIndexChainLength(string filename, NumberType number_type, int chain_length, string text)
    {
        if (Directory.Exists(Globals.STATISTICS_FOLDER))
        {
            filename = Globals.STATISTICS_FOLDER + Path.DirectorySeparatorChar + filename;
            try
            {
                using (StreamWriter writer = new StreamWriter(filename, false, Encoding.Unicode))
                {
                    writer.WriteLine("-----------------------------------------------------");
                    writer.WriteLine(number_type.ToString() + " numbers with IndexChainLength = " + chain_length.ToString());
                    writer.WriteLine("-----------------------------------------------------");
                    writer.WriteLine("Number\tTotal\tPC_L2R\tPC_R2L\tCP_L2R\tCP_R2L\tChain");
                    writer.WriteLine("-----------------------------------------------------");
                    writer.Write(text);
                    writer.WriteLine("-----------------------------------------------------");
                }
            }
            catch
            {
                // silence IO error in case running from read-only media (CD/DVD)
            }

            // show file content after save
            FileHelper.DisplayFile(filename);
        }
    }


    private List<Chapter> m_filter_chapters = null;
    public List<Chapter> FilterChapters
    {
        set { m_filter_chapters = value; }
        get { return m_filter_chapters; }
    }
    private Selection m_selection = null;
    public Selection Selection
    {
        get
        {
            if (Book != null)
            {
                return m_selection;
            }
            return null;
        }
        set
        {
            if (Book != null)
            {
                m_selection = value;
            }
        }
    }
    private void ClearSelection()
    {
        if (Book != null)
        {
            if (m_selection != null)
            {
                m_selection = new Selection(Book, SelectionScope.Chapter, new List<int>() { 0 });
            }
        }
    }
    private SearchScope m_search_scope = SearchScope.Book;
    public SearchScope SearchScope
    {
        set { m_search_scope = value; }
        get { return m_search_scope; }
    }

    private List<Phrase> m_found_phrases = null;
    public List<Phrase> FoundPhrases
    {
        set { m_found_phrases = value; }
        get
        {
            if (m_found_phrases == null) return null;
            if (m_filter_chapters == null) return m_found_phrases;

            List<Phrase> filtered_found_phrases = new List<Phrase>();
            foreach (Phrase phrase in m_found_phrases)
            {
                if (phrase != null)
                {
                    if (phrase.Verse != null)
                    {
                        if (phrase.Verse.Chapter != null)
                        {
                            if (m_filter_chapters.Contains(phrase.Verse.Chapter))
                            {
                                filtered_found_phrases.Add(phrase);
                            }
                        }
                    }
                }
            }
            return filtered_found_phrases;
        }
    }

    private List<Letter> m_found_letters = null;
    public List<Letter> FoundLetters
    {
        set { m_found_letters = value; }
        get
        {
            if (m_found_letters == null) return null;
            if (m_filter_chapters == null) return m_found_letters;

            List<Letter> filtered_found_letters = new List<Letter>();
            foreach (Letter letter in m_found_letters)
            {
                if (letter != null)
                {
                    if (letter.Word != null)
                    {
                        if (letter.Word.Verse != null)
                        {
                            if (letter.Word.Verse.Chapter != null)
                            {
                                if (m_filter_chapters.Contains(letter.Word.Verse.Chapter))
                                {
                                    filtered_found_letters.Add(letter);
                                }
                            }
                        }
                    }
                }
            }
            return filtered_found_letters;
        }
    }
    private List<List<Letter>> m_found_letter_ranges = null;
    public List<List<Letter>> FoundLetterRanges
    {
        set { m_found_letter_ranges = value; }
        get
        {
            if (m_found_letter_ranges == null) return null;
            if (m_filter_chapters == null) return m_found_letter_ranges;

            List<List<Letter>> filtered_found_letter_ranges = new List<List<Letter>>();
            foreach (List<Letter> range in m_found_letter_ranges)
            {
                bool valid_range = true;
                foreach (Letter letter in range)
                {
                    if (letter != null)
                    {
                        if (letter.Word != null)
                        {
                            if (letter.Word.Verse != null)
                            {
                                if (letter.Word.Verse.Chapter != null)
                                {
                                    if (!m_filter_chapters.Contains(letter.Word.Verse.Chapter))
                                    {
                                        valid_range = false;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                if (valid_range)
                {
                    filtered_found_letter_ranges.Add(range);
                }
            }
            return filtered_found_letter_ranges;
        }
    }
    private List<List<Letter>> m_found_letter_sets = null;
    public List<List<Letter>> FoundLetterSets
    {
        set { m_found_letter_sets = value; }
        get
        {
            if (m_found_letter_sets == null) return null;
            if (m_filter_chapters == null) return m_found_letter_sets;

            List<List<Letter>> filtered_found_letter_sets = new List<List<Letter>>();
            foreach (List<Letter> set in m_found_letter_sets)
            {
                bool valid_set = true;
                foreach (Letter letter in set)
                {
                    if (letter != null)
                    {
                        if (letter.Word != null)
                        {
                            if (letter.Word.Verse != null)
                            {
                                if (letter.Word.Verse.Chapter != null)
                                {
                                    if (!m_filter_chapters.Contains(letter.Word.Verse.Chapter))
                                    {
                                        valid_set = false;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                if (valid_set)
                {
                    filtered_found_letter_sets.Add(set);
                }
            }
            return filtered_found_letter_sets;
        }
    }

    private List<Word> m_found_words = null;
    public List<Word> FoundWords
    {
        set { m_found_words = value; }
        get
        {
            if (m_found_words == null) return null;
            if (m_filter_chapters == null) return m_found_words;

            List<Word> filtered_found_words = new List<Word>();
            foreach (Word word in m_found_words)
            {
                if (word != null)
                {
                    if (word.Verse != null)
                    {
                        if (word.Verse.Chapter != null)
                        {
                            if (m_filter_chapters.Contains(word.Verse.Chapter))
                            {
                                filtered_found_words.Add(word);
                            }
                        }
                    }
                }
            }
            return filtered_found_words;
        }
    }
    private List<List<Word>> m_found_word_ranges = null;
    public List<List<Word>> FoundWordRanges
    {
        set { m_found_word_ranges = value; }
        get
        {
            if (m_found_word_ranges == null) return null;
            if (m_filter_chapters == null) return m_found_word_ranges;

            List<List<Word>> filtered_found_word_ranges = new List<List<Word>>();
            foreach (List<Word> range in m_found_word_ranges)
            {
                bool valid_range = true;
                foreach (Word word in range)
                {
                    if (word != null)
                    {
                        if (word.Verse != null)
                        {
                            if (word.Verse.Chapter != null)
                            {
                                if (!m_filter_chapters.Contains(word.Verse.Chapter))
                                {
                                    valid_range = false;
                                    break;
                                }
                            }
                        }
                    }
                }
                if (valid_range)
                {
                    filtered_found_word_ranges.Add(range);
                }
            }
            return filtered_found_word_ranges;
        }
    }
    private List<List<Word>> m_found_word_sets = null;
    public List<List<Word>> FoundWordSets
    {
        set { m_found_word_sets = value; }
        get
        {
            if (m_found_word_sets == null) return null;
            if (m_filter_chapters == null) return m_found_word_sets;

            List<List<Word>> filtered_found_word_sets = new List<List<Word>>();
            foreach (List<Word> set in m_found_word_sets)
            {
                bool valid_set = true;
                foreach (Word word in set)
                {
                    if (word != null)
                    {
                        if (word.Verse != null)
                        {
                            if (word.Verse.Chapter != null)
                            {
                                if (!m_filter_chapters.Contains(word.Verse.Chapter))
                                {
                                    valid_set = false;
                                    break;
                                }
                            }
                        }
                    }
                }
                if (valid_set)
                {
                    filtered_found_word_sets.Add(set);
                }
            }
            return filtered_found_word_sets;
        }
    }

    private List<Sentence> m_found_sentences = null;
    public List<Sentence> FoundSentences
    {
        set { m_found_sentences = value; }
        get
        {
            if (m_found_sentences == null) return null;
            if (m_filter_chapters == null) return m_found_sentences;

            List<Sentence> filtered_found_sentences = new List<Sentence>();
            foreach (Sentence sentence in m_found_sentences)
            {
                if (sentence != null)
                {
                    if (sentence.FirstVerse != null)
                    {
                        if (sentence.FirstVerse.Chapter != null)
                        {
                            if (m_filter_chapters.Contains(sentence.FirstVerse.Chapter))
                            {
                                filtered_found_sentences.Add(sentence);
                            }
                        }
                    }
                }
            }
            return filtered_found_sentences;
        }
    }

    private List<Verse> m_found_verses = null;
    public List<Verse> FoundVerses
    {
        set { m_found_verses = value; }
        get
        {
            if (m_found_verses == null) return null;
            if (m_filter_chapters == null) return m_found_verses;

            List<Verse> filtered_found_verses = new List<Verse>();
            foreach (Verse verse in m_found_verses)
            {
                if (verse != null)
                {
                    if (verse.Chapter != null)
                    {
                        if (m_filter_chapters.Contains(verse.Chapter))
                        {
                            filtered_found_verses.Add(verse);
                        }
                    }
                }
            }
            return filtered_found_verses;
        }
    }
    private List<List<Verse>> m_found_verse_ranges = null;
    public List<List<Verse>> FoundVerseRanges
    {
        set { m_found_verse_ranges = value; }
        get
        {
            if (m_found_verse_ranges == null) return null;
            if (m_filter_chapters == null) return m_found_verse_ranges;

            List<List<Verse>> filtered_found_verse_ranges = new List<List<Verse>>();
            foreach (List<Verse> range in m_found_verse_ranges)
            {
                bool valid_range = true;
                foreach (Verse verse in range)
                {
                    if (verse != null)
                    {
                        if (verse.Chapter != null)
                        {
                            if (!m_filter_chapters.Contains(verse.Chapter))
                            {
                                valid_range = false;
                                break;
                            }
                        }
                    }
                }
                if (valid_range)
                {
                    filtered_found_verse_ranges.Add(range);
                }
            }
            return filtered_found_verse_ranges;
        }
    }
    private List<List<Verse>> m_found_verse_sets = null;
    public List<List<Verse>> FoundVerseSets
    {
        set { m_found_verse_sets = value; }
        get
        {
            if (m_found_verse_sets == null) return null;
            if (m_filter_chapters == null) return m_found_verse_sets;

            List<List<Verse>> filtered_found_verse_sets = new List<List<Verse>>();
            foreach (List<Verse> set in m_found_verse_sets)
            {
                bool valid_set = true;
                foreach (Verse verse in set)
                {
                    if (verse != null)
                    {
                        if (verse.Chapter != null)
                        {
                            if (!m_filter_chapters.Contains(verse.Chapter))
                            {
                                valid_set = false;
                                break;
                            }
                        }
                    }
                }
                if (valid_set)
                {
                    filtered_found_verse_sets.Add(set);
                }
            }
            return filtered_found_verse_sets;
        }
    }

    private List<Chapter> m_found_chapters = null;
    public List<Chapter> FoundChapters
    {
        set { m_found_chapters = value; }
        get
        {
            if (m_found_chapters == null) return null;
            if (m_filter_chapters == null) return m_found_chapters;

            List<Chapter> filtered_found_chapters = new List<Chapter>();
            foreach (Chapter chapter in m_found_chapters)
            {
                if (chapter != null)
                {
                    if (m_filter_chapters.Contains(chapter))
                    {
                        filtered_found_chapters.Add(chapter);
                    }
                }
            }
            return filtered_found_chapters;
        }
    }
    private List<List<Chapter>> m_found_chapter_ranges = null;
    public List<List<Chapter>> FoundChapterRanges
    {
        set { m_found_chapter_ranges = value; }
        get
        {
            if (m_found_chapter_ranges == null) return null;
            if (m_filter_chapters == null) return m_found_chapter_ranges;

            List<List<Chapter>> filtered_found_chapter_ranges = new List<List<Chapter>>();
            foreach (List<Chapter> range in m_found_chapter_ranges)
            {
                bool valid_range = true;
                foreach (Chapter chapter in range)
                {
                    if (chapter != null)
                    {
                        if (!m_filter_chapters.Contains(chapter))
                        {
                            valid_range = false;
                            break;
                        }
                    }
                }
                if (valid_range)
                {
                    filtered_found_chapter_ranges.Add(range);
                }
            }
            return filtered_found_chapter_ranges;
        }
    }
    private List<List<Chapter>> m_found_chapter_sets = null;
    public List<List<Chapter>> FoundChapterSets
    {
        set { m_found_chapter_sets = value; }
        get
        {
            if (m_found_chapter_sets == null) return null;
            if (m_filter_chapters == null) return m_found_chapter_sets;

            List<List<Chapter>> filtered_found_chapter_sets = new List<List<Chapter>>();
            foreach (List<Chapter> set in m_found_chapter_sets)
            {
                bool valid_set = true;
                foreach (Chapter chapter in set)
                {
                    if (chapter != null)
                    {
                        if (!m_filter_chapters.Contains(chapter))
                        {
                            valid_set = false;
                            break;
                        }
                    }
                }
                if (valid_set)
                {
                    filtered_found_chapter_sets.Add(set);
                }
            }
            return filtered_found_chapter_sets;
        }
    }

    private List<Page> m_found_pages = null;
    public List<Page> FoundPages
    {
        set { m_found_pages = value; }
        get
        {
            if (m_found_pages == null) return null;
            if (m_filter_chapters == null) return m_found_pages;

            List<Page> filtered_found_pages = new List<Page>();
            foreach (Page page in m_found_pages)
            {
                if (page != null)
                {
                    if (page.Verses[0].Chapter != null)
                    {
                        if (m_filter_chapters.Contains(page.Verses[0].Chapter))
                        {
                            filtered_found_pages.Add(page);
                        }
                    }
                }
            }
            return filtered_found_pages;
        }
    }
    private List<List<Page>> m_found_page_ranges = null;
    public List<List<Page>> FoundPageRanges
    {
        set { m_found_page_ranges = value; }
        get
        {
            if (m_found_page_ranges == null) return null;
            if (m_filter_chapters == null) return m_found_page_ranges;

            List<List<Page>> filtered_found_page_ranges = new List<List<Page>>();
            foreach (List<Page> range in m_found_page_ranges)
            {
                bool valid_range = true;
                foreach (Page page in range)
                {
                    if (page != null)
                    {
                        if (page.Verses[0].Chapter != null)
                        {
                            if (!m_filter_chapters.Contains(page.Verses[0].Chapter))
                            {
                                valid_range = false;
                                break;
                            }
                        }
                    }
                }
                if (valid_range)
                {
                    filtered_found_page_ranges.Add(range);
                }
            }
            return filtered_found_page_ranges;
        }
    }
    private List<List<Page>> m_found_page_sets = null;
    public List<List<Page>> FoundPageSets
    {
        set { m_found_page_sets = value; }
        get
        {
            if (m_found_page_sets == null) return null;
            if (m_filter_chapters == null) return m_found_page_sets;

            List<List<Page>> filtered_found_page_sets = new List<List<Page>>();
            foreach (List<Page> set in m_found_page_sets)
            {
                bool valid_set = true;
                foreach (Page page in set)
                {
                    if (page != null)
                    {
                        if (page.Verses[0].Chapter != null)
                        {
                            if (!m_filter_chapters.Contains(page.Verses[0].Chapter))
                            {
                                valid_set = false;
                                break;
                            }
                        }
                    }
                }
                if (valid_set)
                {
                    filtered_found_page_sets.Add(set);
                }
            }
            return filtered_found_page_sets;
        }
    }

    private List<Station> m_found_stations = null;
    public List<Station> FoundStations
    {
        set { m_found_stations = value; }
        get
        {
            if (m_found_stations == null) return null;
            if (m_filter_chapters == null) return m_found_stations;

            List<Station> filtered_found_stations = new List<Station>();
            foreach (Station station in m_found_stations)
            {
                if (station != null)
                {
                    if (station.Verses[0].Chapter != null)
                    {
                        if (m_filter_chapters.Contains(station.Verses[0].Chapter))
                        {
                            filtered_found_stations.Add(station);
                        }
                    }
                }
            }
            return filtered_found_stations;
        }
    }
    private List<List<Station>> m_found_station_ranges = null;
    public List<List<Station>> FoundStationRanges
    {
        set { m_found_station_ranges = value; }
        get
        {
            if (m_found_station_ranges == null) return null;
            if (m_filter_chapters == null) return m_found_station_ranges;

            List<List<Station>> filtered_found_station_ranges = new List<List<Station>>();
            foreach (List<Station> range in m_found_station_ranges)
            {
                bool valid_range = true;
                foreach (Station station in range)
                {
                    if (station != null)
                    {
                        if (station.Verses[0].Chapter != null)
                        {
                            if (!m_filter_chapters.Contains(station.Verses[0].Chapter))
                            {
                                valid_range = false;
                                break;
                            }
                        }
                    }
                }
                if (valid_range)
                {
                    filtered_found_station_ranges.Add(range);
                }
            }
            return filtered_found_station_ranges;
        }
    }
    private List<List<Station>> m_found_station_sets = null;
    public List<List<Station>> FoundStationSets
    {
        set { m_found_station_sets = value; }
        get
        {
            if (m_found_station_sets == null) return null;
            if (m_filter_chapters == null) return m_found_station_sets;

            List<List<Station>> filtered_found_station_sets = new List<List<Station>>();
            foreach (List<Station> set in m_found_station_sets)
            {
                bool valid_set = true;
                foreach (Station station in set)
                {
                    if (station != null)
                    {
                        if (station.Verses[0].Chapter != null)
                        {
                            if (!m_filter_chapters.Contains(station.Verses[0].Chapter))
                            {
                                valid_set = false;
                                break;
                            }
                        }
                    }
                }
                if (valid_set)
                {
                    filtered_found_station_sets.Add(set);
                }
            }
            return filtered_found_station_sets;
        }
    }

    private List<Part> m_found_parts = null;
    public List<Part> FoundParts
    {
        set { m_found_parts = value; }
        get
        {
            if (m_found_parts == null) return null;
            if (m_filter_chapters == null) return m_found_parts;

            List<Part> filtered_found_parts = new List<Part>();
            foreach (Part part in m_found_parts)
            {
                if (part != null)
                {
                    if (part.Verses[0].Chapter != null)
                    {
                        if (m_filter_chapters.Contains(part.Verses[0].Chapter))
                        {
                            filtered_found_parts.Add(part);
                        }
                    }
                }
            }
            return filtered_found_parts;
        }
    }
    private List<List<Part>> m_found_part_ranges = null;
    public List<List<Part>> FoundPartRanges
    {
        set { m_found_part_ranges = value; }
        get
        {
            if (m_found_part_ranges == null) return null;
            if (m_filter_chapters == null) return m_found_part_ranges;

            List<List<Part>> filtered_found_part_ranges = new List<List<Part>>();
            foreach (List<Part> range in m_found_part_ranges)
            {
                bool valid_range = true;
                foreach (Part part in range)
                {
                    if (part != null)
                    {
                        if (part.Verses[0].Chapter != null)
                        {
                            if (!m_filter_chapters.Contains(part.Verses[0].Chapter))
                            {
                                valid_range = false;
                                break;
                            }
                        }
                    }
                }
                if (valid_range)
                {
                    filtered_found_part_ranges.Add(range);
                }
            }
            return filtered_found_part_ranges;
        }
    }
    private List<List<Part>> m_found_part_sets = null;
    public List<List<Part>> FoundPartSets
    {
        set { m_found_part_sets = value; }
        get
        {
            if (m_found_part_sets == null) return null;
            if (m_filter_chapters == null) return m_found_part_sets;

            List<List<Part>> filtered_found_part_sets = new List<List<Part>>();
            foreach (List<Part> set in m_found_part_sets)
            {
                bool valid_set = true;
                foreach (Part part in set)
                {
                    if (part != null)
                    {
                        if (part.Verses[0].Chapter != null)
                        {
                            if (!m_filter_chapters.Contains(part.Verses[0].Chapter))
                            {
                                valid_set = false;
                                break;
                            }
                        }
                    }
                }
                if (valid_set)
                {
                    filtered_found_part_sets.Add(set);
                }
            }
            return filtered_found_part_sets;
        }
    }

    private List<Group> m_found_groups = null;
    public List<Group> FoundGroups
    {
        set { m_found_groups = value; }
        get
        {
            if (m_found_groups == null) return null;
            if (m_filter_chapters == null) return m_found_groups;

            List<Group> filtered_found_groups = new List<Group>();
            foreach (Group group in m_found_groups)
            {
                if (group != null)
                {
                    if (group.Verses[0].Chapter != null)
                    {
                        if (m_filter_chapters.Contains(group.Verses[0].Chapter))
                        {
                            filtered_found_groups.Add(group);
                        }
                    }
                }
            }
            return filtered_found_groups;
        }
    }
    private List<List<Group>> m_found_group_ranges = null;
    public List<List<Group>> FoundGroupRanges
    {
        set { m_found_group_ranges = value; }
        get
        {
            if (m_found_group_ranges == null) return null;
            if (m_filter_chapters == null) return m_found_group_ranges;

            List<List<Group>> filtered_found_group_ranges = new List<List<Group>>();
            foreach (List<Group> range in m_found_group_ranges)
            {
                bool valid_range = true;
                foreach (Group group in range)
                {
                    if (group != null)
                    {
                        if (group.Verses[0].Chapter != null)
                        {
                            if (!m_filter_chapters.Contains(group.Verses[0].Chapter))
                            {
                                valid_range = false;
                                break;
                            }
                        }
                    }
                }
                if (valid_range)
                {
                    filtered_found_group_ranges.Add(range);
                }
            }
            return filtered_found_group_ranges;
        }
    }
    private List<List<Group>> m_found_group_sets = null;
    public List<List<Group>> FoundGroupSets
    {
        set { m_found_group_sets = value; }
        get
        {
            if (m_found_group_sets == null) return null;
            if (m_filter_chapters == null) return m_found_group_sets;

            List<List<Group>> filtered_found_group_sets = new List<List<Group>>();
            foreach (List<Group> set in m_found_group_sets)
            {
                bool valid_set = true;
                foreach (Group group in set)
                {
                    if (group != null)
                    {
                        if (group.Verses[0].Chapter != null)
                        {
                            if (!m_filter_chapters.Contains(group.Verses[0].Chapter))
                            {
                                valid_set = false;
                                break;
                            }
                        }
                    }
                }
                if (valid_set)
                {
                    filtered_found_group_sets.Add(set);
                }
            }
            return filtered_found_group_sets;
        }
    }

    private List<Half> m_found_halfs = null;
    public List<Half> FoundHalfs
    {
        set { m_found_halfs = value; }
        get
        {
            if (m_found_halfs == null) return null;
            if (m_filter_chapters == null) return m_found_halfs;

            List<Half> filtered_found_halfs = new List<Half>();
            foreach (Half half in m_found_halfs)
            {
                if (half != null)
                {
                    if (half.Verses[0].Chapter != null)
                    {
                        if (m_filter_chapters.Contains(half.Verses[0].Chapter))
                        {
                            filtered_found_halfs.Add(half);
                        }
                    }
                }
            }
            return filtered_found_halfs;
        }
    }
    private List<List<Half>> m_found_half_ranges = null;
    public List<List<Half>> FoundHalfRanges
    {
        set { m_found_half_ranges = value; }
        get
        {
            if (m_found_half_ranges == null) return null;
            if (m_filter_chapters == null) return m_found_half_ranges;

            List<List<Half>> filtered_found_half_ranges = new List<List<Half>>();
            foreach (List<Half> range in m_found_half_ranges)
            {
                bool valid_range = true;
                foreach (Half half in range)
                {
                    if (half != null)
                    {
                        if (half.Verses[0].Chapter != null)
                        {
                            if (!m_filter_chapters.Contains(half.Verses[0].Chapter))
                            {
                                valid_range = false;
                                break;
                            }
                        }
                    }
                }
                if (valid_range)
                {
                    filtered_found_half_ranges.Add(range);
                }
            }
            return filtered_found_half_ranges;
        }
    }
    private List<List<Half>> m_found_half_sets = null;
    public List<List<Half>> FoundHalfSets
    {
        set { m_found_half_sets = value; }
        get
        {
            if (m_found_half_sets == null) return null;
            if (m_filter_chapters == null) return m_found_half_sets;

            List<List<Half>> filtered_found_half_sets = new List<List<Half>>();
            foreach (List<Half> set in m_found_half_sets)
            {
                bool valid_set = true;
                foreach (Half half in set)
                {
                    if (half != null)
                    {
                        if (half.Verses[0].Chapter != null)
                        {
                            if (!m_filter_chapters.Contains(half.Verses[0].Chapter))
                            {
                                valid_set = false;
                                break;
                            }
                        }
                    }
                }
                if (valid_set)
                {
                    filtered_found_half_sets.Add(set);
                }
            }
            return filtered_found_half_sets;
        }
    }

    private List<Quarter> m_found_quarters = null;
    public List<Quarter> FoundQuarters
    {
        set { m_found_quarters = value; }
        get
        {
            if (m_found_quarters == null) return null;
            if (m_filter_chapters == null) return m_found_quarters;

            List<Quarter> filtered_found_quarters = new List<Quarter>();
            foreach (Quarter quarter in m_found_quarters)
            {
                if (quarter != null)
                {
                    if (quarter.Verses[0].Chapter != null)
                    {
                        if (m_filter_chapters.Contains(quarter.Verses[0].Chapter))
                        {
                            filtered_found_quarters.Add(quarter);
                        }
                    }
                }
            }
            return filtered_found_quarters;
        }
    }
    private List<List<Quarter>> m_found_quarter_ranges = null;
    public List<List<Quarter>> FoundQuarterRanges
    {
        set { m_found_quarter_ranges = value; }
        get
        {
            if (m_found_quarter_ranges == null) return null;
            if (m_filter_chapters == null) return m_found_quarter_ranges;

            List<List<Quarter>> filtered_found_quarter_ranges = new List<List<Quarter>>();
            foreach (List<Quarter> range in m_found_quarter_ranges)
            {
                bool valid_range = true;
                foreach (Quarter quarter in range)
                {
                    if (quarter != null)
                    {
                        if (quarter.Verses[0].Chapter != null)
                        {
                            if (!m_filter_chapters.Contains(quarter.Verses[0].Chapter))
                            {
                                valid_range = false;
                                break;
                            }
                        }
                    }
                }
                if (valid_range)
                {
                    filtered_found_quarter_ranges.Add(range);
                }
            }
            return filtered_found_quarter_ranges;
        }
    }
    private List<List<Quarter>> m_found_quarter_sets = null;
    public List<List<Quarter>> FoundQuarterSets
    {
        set { m_found_quarter_sets = value; }
        get
        {
            if (m_found_quarter_sets == null) return null;
            if (m_filter_chapters == null) return m_found_quarter_sets;

            List<List<Quarter>> filtered_found_quarter_sets = new List<List<Quarter>>();
            foreach (List<Quarter> set in m_found_quarter_sets)
            {
                bool valid_set = true;
                foreach (Quarter quarter in set)
                {
                    if (quarter != null)
                    {
                        if (quarter.Verses[0].Chapter != null)
                        {
                            if (!m_filter_chapters.Contains(quarter.Verses[0].Chapter))
                            {
                                valid_set = false;
                                break;
                            }
                        }
                    }
                }
                if (valid_set)
                {
                    filtered_found_quarter_sets.Add(set);
                }
            }
            return filtered_found_quarter_sets;
        }
    }

    private List<Bowing> m_found_bowings = null;
    public List<Bowing> FoundBowings
    {
        set { m_found_bowings = value; }
        get
        {
            if (m_found_bowings == null) return null;
            if (m_filter_chapters == null) return m_found_bowings;

            List<Bowing> filtered_found_bowings = new List<Bowing>();
            foreach (Bowing bowing in m_found_bowings)
            {
                if (bowing != null)
                {
                    if (bowing.Verses[0].Chapter != null)
                    {
                        if (m_filter_chapters.Contains(bowing.Verses[0].Chapter))
                        {
                            filtered_found_bowings.Add(bowing);
                        }
                    }
                }
            }
            return filtered_found_bowings;
        }
    }
    private List<List<Bowing>> m_found_bowing_ranges = null;
    public List<List<Bowing>> FoundBowingRanges
    {
        set { m_found_bowing_ranges = value; }
        get
        {
            if (m_found_bowing_ranges == null) return null;
            if (m_filter_chapters == null) return m_found_bowing_ranges;

            List<List<Bowing>> filtered_found_bowing_ranges = new List<List<Bowing>>();
            foreach (List<Bowing> range in m_found_bowing_ranges)
            {
                bool valid_range = true;
                foreach (Bowing bowing in range)
                {
                    if (bowing != null)
                    {
                        if (bowing.Verses[0].Chapter != null)
                        {
                            if (!m_filter_chapters.Contains(bowing.Verses[0].Chapter))
                            {
                                valid_range = false;
                                break;
                            }
                        }
                    }
                }
                if (valid_range)
                {
                    filtered_found_bowing_ranges.Add(range);
                }
            }
            return filtered_found_bowing_ranges;
        }
    }
    private List<List<Bowing>> m_found_bowing_sets = null;
    public List<List<Bowing>> FoundBowingSets
    {
        set { m_found_bowing_sets = value; }
        get
        {
            if (m_found_bowing_sets == null) return null;
            if (m_filter_chapters == null) return m_found_bowing_sets;

            List<List<Bowing>> filtered_found_bowing_sets = new List<List<Bowing>>();
            foreach (List<Bowing> set in m_found_bowing_sets)
            {
                bool valid_set = true;
                foreach (Bowing bowing in set)
                {
                    if (bowing != null)
                    {
                        if (bowing.Verses[0].Chapter != null)
                        {
                            if (!m_filter_chapters.Contains(bowing.Verses[0].Chapter))
                            {
                                valid_set = false;
                                break;
                            }
                        }
                    }
                }
                if (valid_set)
                {
                    filtered_found_bowing_sets.Add(set);
                }
            }
            return filtered_found_bowing_sets;
        }
    }

    public void ClearSearchResults()
    {
        m_filter_chapters = null;

        m_found_phrases = null;

        m_found_letters = null;
        m_found_letter_ranges = null;
        m_found_letter_sets = null;

        m_found_words = null;
        m_found_word_ranges = null;
        m_found_word_sets = null;

        m_found_sentences = null;

        // m_found_verses are needed in nested searches
        if (m_search_scope != SearchScope.Result)
        {
            m_found_verses = null;
        }
        m_found_verse_ranges = null;
        m_found_verse_sets = null;

        m_found_chapters = null;
        m_found_chapter_ranges = null;
        m_found_chapter_sets = null;

        m_found_pages = null;
        m_found_page_ranges = null;
        m_found_page_sets = null;

        m_found_stations = null;
        m_found_station_ranges = null;
        m_found_station_sets = null;

        m_found_parts = null;
        m_found_part_ranges = null;
        m_found_part_sets = null;

        m_found_groups = null;
        m_found_group_ranges = null;
        m_found_group_sets = null;

        m_found_halfs = null;
        m_found_half_ranges = null;
        m_found_half_sets = null;

        m_found_quarters = null;
        m_found_quarter_ranges = null;
        m_found_quarter_sets = null;

        m_found_bowings = null;
        m_found_bowing_ranges = null;
        m_found_bowing_sets = null;
    }

    // helper methods with GetSourceVerses (not entire book verses)
    public Dictionary<string, int> GetCurrentWords(string text, TextLocationInChapter text_location_in_chapter, TextLocationInVerse text_location_in_verse, TextLocationInWord text_location_in_word, TextWordness text_wordness, bool case_sensitive, bool with_diacritics)
    {
        Dictionary<string, int> result = new Dictionary<string, int>();
        List<Verse> source = Server.GetSourceVerses(m_search_scope, m_selection, m_found_verses, text_location_in_chapter);
        if (Book != null)
        {
            result = Book.GetCurrentWords(source, text, text_location_in_verse, text_location_in_word, text_wordness, with_diacritics);
        }
        return result;
    }
    public Dictionary<string, int> GetNextWords(string text, TextLocationInChapter text_location_in_chapter, TextLocationInVerse text_location_in_verse, TextLocationInWord text_location_in_word, TextWordness text_wordness, bool case_sensitive, bool with_diacritics)
    {
        Dictionary<string, int> result = new Dictionary<string, int>();
        List<Verse> source = Server.GetSourceVerses(m_search_scope, m_selection, m_found_verses, text_location_in_chapter);
        if (Book != null)
        {
            result = Book.GetNextWords(source, text, text_location_in_verse, text_location_in_word, text_wordness, with_diacritics);
        }
        return result;
    }

    // helper method with GetSourceVerses (not entire book verses)
    public Dictionary<string, int> GetWordRoots(string text, TextLocationInWord text_location_in_word)
    {
        Dictionary<string, int> result = new Dictionary<string, int>();
        List<Verse> source = Server.GetSourceVerses(m_search_scope, m_selection, m_found_verses, TextLocationInChapter.Any);
        if (Book != null)
        {
            result = Book.GetRoots(source, text, text_location_in_word);
        }
        return result;
    }

    // helper methods
    public List<string> GetSimplifiedWords()
    {
        List<string> result = null;

        if (this.NumerologySystem != null)
        {
            if (this.Book != null)
            {
                if (this.Book.Verses != null)
                {
                    SortedDictionary<string, int> word_frequencies = new SortedDictionary<string, int>();
                    if (word_frequencies != null)
                    {
                        List<Word> quran_words = new List<Word>();
                        if (quran_words != null)
                        {
                            foreach (Verse verse in this.Book.Verses)
                            {
                                quran_words.AddRange(verse.Words);
                            }
                        }

                        foreach (Word quran_word in quran_words)
                        {
                            string simplified_quran_word_text = quran_word.Text.Simplify(NumerologySystem.TextMode);
                            if (word_frequencies.ContainsKey(simplified_quran_word_text))
                            {
                                word_frequencies[simplified_quran_word_text]++;
                            }
                            else
                            {
                                word_frequencies.Add(simplified_quran_word_text, 1);
                            }
                        }

                        result = new List<string>(word_frequencies.Keys);
                    }
                }
            }
        }

        return result;
    }
    public SortedDictionary<string, int> GetSimplifiedWordFrequencies()
    {
        SortedDictionary<string, int> result = new SortedDictionary<string, int>();

        if (this.NumerologySystem != null)
        {
            if (this.Book != null)
            {
                if (this.Book.Verses != null)
                {
                    if (result != null)
                    {
                        List<Word> quran_words = new List<Word>();
                        if (quran_words != null)
                        {
                            foreach (Verse verse in this.Book.Verses)
                            {
                                quran_words.AddRange(verse.Words);
                            }
                        }

                        foreach (Word quran_word in quran_words)
                        {
                            if (result.ContainsKey(quran_word.Text))
                            {
                                result[quran_word.Text]++;
                            }
                            else
                            {
                                result.Add(quran_word.Text, 1);
                            }
                        }
                    }
                }
            }
        }

        return result;
    }

    // find by text - Exact
    /// <summary>
    /// Find phrases for given exact text that meet all parameters.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="language_type"></param>
    /// <param name="translation"></param>
    /// <param name="text_location_in_verse"></param>
    /// <param name="text_location_in_word"></param>
    /// <param name="case_sensitive"></param>
    /// <param name="text_wordness"></param>
    /// <param name="multiplicity"></param>
    /// <param name="at_word_start"></param>
    /// <returns>Number of found phrases. Result is stored in FoundPhrases.</returns>
    public int FindPhrases(TextSearchBlockSize text_search_block_size, string text, LanguageType language_type, TextLocationInChapter text_location_in_chapter, TextLocationInVerse text_location_in_verse, TextLocationInWord text_location_in_word, TextWordness text_wordness, bool case_sensitive, bool with_diacritics, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        ClearSearchResults();
        m_found_phrases = Server.FindPhrases(m_search_scope, m_selection, m_found_verses, text_search_block_size, text, language_type, text_location_in_chapter, text_location_in_verse, text_location_in_word, text_wordness, case_sensitive, with_diacritics, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
        if (m_found_phrases != null)
        {
            m_found_verses = new List<Verse>();
            foreach (Phrase phrase in m_found_phrases)
            {
                if (phrase != null)
                {
                    if (!m_found_verses.Contains(phrase.Verse))
                    {
                        m_found_verses.Add(phrase.Verse);
                    }
                }
            }
            return m_found_phrases.Count;
        }
        return 0;
    }
    // find by text - Proximity
    /// <summary>
    /// Find phrases for given text by proximity that meet all parameters.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="language_type"></param>
    /// <param name="translation"></param>
    /// <param name="text_location_in_verse"></param>
    /// <param name="text_location_in_word"></param>
    /// <param name="case_sensitive"></param>
    /// <param name="text_wordness"></param>
    /// <param name="multiplicity"></param>
    /// <param name="at_word_start"></param>
    /// <returns>Number of found phrases. Result is stored in FoundPhrases.</returns>
    public int FindWords(TextSearchBlockSize text_search_block_size, string text, LanguageType language_type, TextWordGrouping text_word_grouping, TextWordness text_wordness, bool case_sensitive, bool with_diacritics)
    {
        ClearSearchResults();
        m_found_words = Server.FindWords(m_search_scope, m_selection, m_found_verses, text_search_block_size, text, language_type, text_word_grouping, text_wordness, case_sensitive, with_diacritics);
        if (m_found_words != null)
        {
            m_found_verses = new List<Verse>();
            foreach (Word word in m_found_words)
            {
                if (word != null)
                {
                    if (!m_found_verses.Contains(word.Verse))
                    {
                        m_found_verses.Add(word.Verse);
                    }
                }
            }
            return m_found_words.Count;
        }
        return 0;
    }
    // find by text - Root
    /// <summary>
    /// Find phrases for given text( or space separate roots) that meet all parameters.
    /// </summary>
    /// <param name="roots"></param>
    /// <param name="multiplicity"></param>
    /// <returns>Number of found phrases. Result is stored in FoundPhrases.</returns>
    public int FindWords(TextSearchBlockSize text_search_block_size, string roots, TextWordGrouping text_word_grouping, int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        ClearSearchResults();
        m_found_words = Server.FindWords(m_search_scope, m_selection, m_found_verses, text_search_block_size, roots, text_word_grouping, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
        if (m_found_words != null)
        {
            m_found_verses = new List<Verse>();
            foreach (Word word in m_found_words)
            {
                if (word != null)
                {
                    if (!m_found_verses.Contains(word.Verse))
                    {
                        m_found_verses.Add(word.Verse);
                    }
                }
            }
            return m_found_words.Count;
        }
        return 0;
    }
    // find by text - Related words
    /// <summary>
    /// Find verses with related words from the same text
    /// </summary>
    /// <param name="verse"></param>
    /// <returns>Number of found verses. Result is stored in FoundVerses.</returns>
    public int FindRelatedVerses(Verse verse)
    {
        ClearSearchResults();
        m_found_verses = Server.FindRelatedVerses(m_search_scope, m_selection, m_found_verses, verse);
        if (m_found_verses != null)
        {
            return m_found_verses.Count;
        }
        return 0;
    }
    // find by text - Repetition
    public int FindConsecutivelyRepeatedWords(int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder, bool with_diacritics)
    {
        ClearSearchResults();
        m_found_words = Server.FindConsecutivelyRepeatedWords(m_search_scope, m_selection, m_found_verses, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder, with_diacritics);
        if (m_found_words != null)
        {
            m_found_verses = new List<Verse>();
            foreach (Word word in m_found_words)
            {
                if (word != null)
                {
                    if (!m_found_verses.Contains(word.Verse))
                    {
                        m_found_verses.Add(word.Verse);
                    }
                }
            }
            return m_found_words.Count;
        }
        return 0;
    }
    public int FindConsecutivelyRepeatedRoots(int multiplicity, NumberType multiplicity_number_type, ComparisonOperator multiplicity_comparison_operator, int multiplicity_remainder)
    {
        ClearSearchResults();
        m_found_words = Server.FindConsecutivelyRepeatedRoots(m_search_scope, m_selection, m_found_verses, multiplicity, multiplicity_number_type, multiplicity_comparison_operator, multiplicity_remainder);
        if (m_found_words != null)
        {
            m_found_verses = new List<Verse>();
            foreach (Word word in m_found_words)
            {
                if (word != null)
                {
                    if (!m_found_verses.Contains(word.Verse))
                    {
                        m_found_verses.Add(word.Verse);
                    }
                }
            }
            return m_found_words.Count;
        }
        return 0;
    }


    // find by numbers - Letters
    /// <summary>
    /// Find letters that meet query criteria.
    /// </summary>
    /// <param name="query"></param>
    /// <returns>Number of found letters. Result is stored in FoundLetters.</returns>
    public int FindLetters(NumberQuery query)
    {
        ClearSearchResults();
        m_found_letters = Server.FindLetters(m_search_scope, m_selection, m_found_verses, query);
        if (m_found_letters != null)
        {
            m_found_words = new List<Word>();
            m_found_verses = new List<Verse>();
            m_found_phrases = new List<Phrase>();
            foreach (Letter letter in m_found_letters)
            {
                if (letter != null)
                {
                    if (letter.Word != null)
                    {
                        if (!m_found_words.Contains(letter.Word))
                        {
                            m_found_words.Add(letter.Word);
                        }

                        Verse verse = letter.Word.Verse;
                        if (!m_found_verses.Contains(verse))
                        {
                            m_found_verses.Add(verse);
                        }
                    }
                }

                Phrase phrase = new Phrase(letter.Word.Verse, letter.Word.Position + letter.NumberInWord - 1, letter.Text);
                if (NumerologySystem.TextMode == "Original")
                {
                    Server.SwitchTextMode(phrase, NumerologySystem.TextMode);
                }
                m_found_phrases.Add(phrase);
            }
            return m_found_letters.Count;
        }
        return 0;
    }
    // find by numbers - LetterRanges
    /// <summary>
    /// Find letter ranges that meet query criteria.
    /// </summary>
    /// <param name="query"></param>
    /// <returns>Number of found letter ranges. Result is stored in FoundLetterRanges.</returns>
    public int FindLetterRanges(NumberQuery query)
    {
        ClearSearchResults();
        m_found_letter_ranges = Server.FindLetterRanges(m_search_scope, m_selection, m_found_verses, query);
        if (m_found_letter_ranges != null)
        {
            m_found_verses = new List<Verse>();
            m_found_phrases = new List<Phrase>();
            foreach (List<Letter> range in m_found_letter_ranges)
            {
                if (range != null)
                {
                    if (range.Count > 0)
                    {
                        string range_text = null;
                        Verse previous_verse = null;
                        foreach (Letter letter in range)
                        {
                            if (letter != null)
                            {
                                if (letter.Word != null)
                                {
                                    // prepare found phrase verse
                                    Verse verse = letter.Word.Verse;

                                    // build found verses // prevent duplicate verses in case more than 1 range is found in same verse
                                    if (!m_found_verses.Contains(verse))
                                    {
                                        m_found_verses.Add(verse);
                                    }

                                    // prepare found phrase text
                                    if (previous_verse != null)
                                    {
                                        //    first letter in word     &&  word is not first in verse
                                        if ((letter.NumberInWord == 1) && (letter.Word.NumberInVerse > 1))
                                        {
                                            range_text += " " + letter.Text;
                                        }
                                        // first letter in new verse
                                        else if (letter.NumberInVerse == 1)
                                        {
                                            range_text += previous_verse.Endmark + verse.Address + "\t" + letter.Text;
                                        }
                                        else
                                        {
                                            range_text += letter.Text;
                                        }
                                    }
                                    else
                                    {
                                        range_text += letter.Text;
                                    }

                                    if (NumerologySystem.TextMode == "Original")
                                    {
                                        if (letter.Word.Stopmark != Stopmark.None)
                                        {
                                            range_text += StopmarkHelper.GetStopmarkText(letter.Word.Stopmark) + " ";
                                        }
                                    }

                                    previous_verse = verse;
                                }
                            }
                        }

                        // build found phrases // allow multiple phrases even if overlapping inside same verse
                        Phrase phrase = new Phrase(range[0].Word.Verse, range[0].Word.Position + range[0].NumberInWord - 1, range_text);
                        if (phrase != null)
                        {
                            m_found_phrases.Add(phrase);
                        }
                    }
                }
            }
            return m_found_letter_ranges.Count;
        }
        return 0;
    }

    // find by numbers - Words
    /// <summary>
    /// Find words that meet query criteria.
    /// </summary>
    /// <param name="query"></param>
    /// <returns>Number of found words. Result is stored in FoundWords.</returns>
    public int FindWords(NumberQuery query)
    {
        ClearSearchResults();
        m_found_words = Server.FindWords(m_search_scope, m_selection, m_found_verses, query);
        if (m_found_words != null)
        {
            m_found_verses = new List<Verse>();
            foreach (Word word in m_found_words)
            {
                if (word != null)
                {
                    if (!m_found_verses.Contains(word.Verse))
                    {
                        m_found_verses.Add(word.Verse);
                    }
                }
            }
            return m_found_words.Count;
        }
        return 0;
    }
    // find by numbers - WordRanges
    /// <summary>
    /// Find word ranges that meet query criteria.
    /// </summary>
    /// <param name="query"></param>
    /// <returns>Number of found word ranges. Result is stored in FoundWordRanges.</returns>
    public int FindWordRanges(NumberQuery query)
    {
        ClearSearchResults();
        m_found_word_ranges = Server.FindWordRanges(m_search_scope, m_selection, m_found_verses, query);
        if (m_found_word_ranges != null)
        {
            m_found_verses = new List<Verse>();
            m_found_phrases = new List<Phrase>();
            foreach (List<Word> range in m_found_word_ranges)
            {
                if (range != null)
                {
                    if (range.Count > 0)
                    {
                        string range_text = null;
                        foreach (Word word in range)
                        {
                            if (word != null)
                            {
                                // prepare found phrase verse
                                Verse verse = word.Verse;

                                // build found verses // prevent duplicate verses in case more than 1 range is found in same verse
                                if (!m_found_verses.Contains(verse))
                                {
                                    m_found_verses.Add(verse);
                                }

                                // prepare found phrase text
                                range_text += word.Text + " ";
                                if (NumerologySystem.TextMode == "Original")
                                {
                                    if (word.Stopmark != Stopmark.None)
                                    {
                                        range_text += StopmarkHelper.GetStopmarkText(word.Stopmark) + " ";
                                    }
                                }
                            }
                        }
                        range_text = range_text.Remove(range_text.Length - 1, 1);

                        // build found phrases // allow multiple phrases even if overlapping inside same verse
                        Phrase phrase = new Phrase(range[0].Verse, range[0].Position, range_text);
                        if (phrase != null)
                        {
                            m_found_phrases.Add(phrase);
                        }
                    }
                }
            }
            return m_found_word_ranges.Count;
        }
        return 0;
    }

    // find by numbers - Sentences
    /// <summary>
    /// Find sentences across verses that meet query criteria.
    /// </summary>
    /// <param name="query"></param>
    /// <returns>Number of found sentences. Result is stored in FoundSentences.</returns>
    public int FindSentences(NumberQuery query)
    {
        ClearSearchResults();
        m_found_sentences = Server.FindSentences(m_search_scope, m_selection, m_found_verses, query);
        if (m_found_sentences != null)
        {
            BuildSentencePhrases();

            return m_found_sentences.Count;
        }
        return 0;
    }

    // find by numbers - Verses
    /// <summary>
    /// Find verses that meet query criteria.
    /// </summary>
    /// <param name="query"></param>
    /// <returns>Number of found verses. Result is stored in FoundVerses.</returns>
    public int FindVerses(NumberQuery query)
    {
        ClearSearchResults();
        m_found_verses = Server.FindVerses(m_search_scope, m_selection, m_found_verses, query);
        if (m_found_verses != null)
        {
            return m_found_verses.Count;
        }
        return 0;
    }
    // find by numbers - VerseRanges
    /// <summary>
    /// Find verse ranges that meet query criteria.
    /// </summary>
    /// <param name="query"></param>
    /// <returns>Number of found verse ranges. Result is stored in FoundVerseRanges.</returns>
    public int FindVerseRanges(NumberQuery query)
    {
        ClearSearchResults();
        m_found_verse_ranges = Server.FindVerseRanges(m_search_scope, m_selection, m_found_verses, query);
        m_found_verses = new List<Verse>();
        if (m_found_verse_ranges != null)
        {
            foreach (List<Verse> range in m_found_verse_ranges)
            {
                m_found_verses.AddRange(range);
            }
            return m_found_verse_ranges.Count;
        }
        return 0;
    }

    // find by numbers - Chapters
    /// <summary>
    /// Find chapters that meet query criteria.
    /// </summary>
    /// <param name="query"></param>
    /// <returns>Number of found chapters. Result is stored in FoundChapters.</returns>
    public int FindChapters(NumberQuery query)
    {
        ClearSearchResults();
        m_found_chapters = Server.FindChapters(m_search_scope, m_selection, m_found_verses, query);
        if (m_found_chapters != null)
        {
            m_found_verses = new List<Verse>();
            m_found_verse_ranges = new List<List<Verse>>();
            foreach (Chapter chapter in m_found_chapters)
            {
                if (chapter != null)
                {
                    m_found_verses.AddRange(chapter.Verses);
                    m_found_verse_ranges.Add(chapter.Verses);
                }
            }
            return m_found_chapters.Count;
        }
        return 0;
    }
    // find by numbers - ChapterRanges
    /// <summary>
    /// Find chapter ranges that meet query criteria.
    /// </summary>
    /// <param name="query"></param>
    /// <returns>Number of found chapter ranges. Result is stored in FoundChapterRanges.</returns>
    public int FindChapterRanges(NumberQuery query)
    {
        ClearSearchResults();
        m_found_chapter_ranges = Server.FindChapterRanges(m_search_scope, m_selection, m_found_verses, query);
        if (m_found_chapter_ranges != null)
        {
            m_found_verses = new List<Verse>();
            m_found_verse_ranges = new List<List<Verse>>();
            foreach (List<Chapter> range in m_found_chapter_ranges)
            {
                List<Verse> verses = new List<Verse>();
                foreach (Chapter chapter in range)
                {
                    if (chapter != null)
                    {
                        m_found_verses.AddRange(chapter.Verses);
                        verses.AddRange(chapter.Verses);
                    }
                }
                m_found_verse_ranges.Add(verses);
            }
            return m_found_chapter_ranges.Count;
        }
        return 0;
    }

    // find by numbers - Pages
    /// <summary>
    /// Find pages that meet query criteria.
    /// </summary>
    /// <param name="query"></param>
    /// <returns>Number of found pages. Result is stored in FoundPages.</returns>
    public int FindPages(NumberQuery query)
    {
        ClearSearchResults();
        m_found_pages = Server.FindPages(m_search_scope, m_selection, m_found_verses, query);
        if (m_found_pages != null)
        {
            m_found_verses = new List<Verse>();
            m_found_verse_ranges = new List<List<Verse>>();
            foreach (Page page in m_found_pages)
            {
                if (page != null)
                {
                    m_found_verses.AddRange(page.Verses);
                    m_found_verse_ranges.Add(page.Verses);
                }
            }
            return m_found_pages.Count;
        }
        return 0;
    }
    // find by numbers - PageRanges
    /// <summary>
    /// Find page ranges that meet query criteria.
    /// </summary>
    /// <param name="query"></param>
    /// <returns>Number of found page ranges. Result is stored in FoundPageRanges.</returns>
    public int FindPageRanges(NumberQuery query)
    {
        ClearSearchResults();
        m_found_page_ranges = Server.FindPageRanges(m_search_scope, m_selection, m_found_verses, query);
        if (m_found_page_ranges != null)
        {
            m_found_verses = new List<Verse>();
            m_found_verse_ranges = new List<List<Verse>>();
            foreach (List<Page> range in m_found_page_ranges)
            {
                List<Verse> verses = new List<Verse>();
                foreach (Page page in range)
                {
                    if (page != null)
                    {
                        m_found_verses.AddRange(page.Verses);
                        verses.AddRange(page.Verses);
                    }
                }
                m_found_verse_ranges.Add(verses);
            }
            return m_found_page_ranges.Count;
        }
        return 0;
    }

    // find by numbers - Stations
    /// <summary>
    /// Find stations that meet query criteria.
    /// </summary>
    /// <param name="query"></param>
    /// <returns>Number of found stations. Result is stored in FoundStations.</returns>
    public int FindStations(NumberQuery query)
    {
        ClearSearchResults();
        m_found_stations = Server.FindStations(m_search_scope, m_selection, m_found_verses, query);
        if (m_found_stations != null)
        {
            m_found_verses = new List<Verse>();
            m_found_verse_ranges = new List<List<Verse>>();
            foreach (Station station in m_found_stations)
            {
                if (station != null)
                {
                    m_found_verses.AddRange(station.Verses);
                    m_found_verse_ranges.Add(station.Verses);
                }
            }
            return m_found_stations.Count;
        }
        return 0;
    }
    // find by numbers - StationRanges
    /// <summary>
    /// Find station ranges that meet query criteria.
    /// </summary>
    /// <param name="query"></param>
    /// <returns>Number of found station ranges. Result is stored in FoundStationRanges.</returns>
    public int FindStationRanges(NumberQuery query)
    {
        ClearSearchResults();
        m_found_station_ranges = Server.FindStationRanges(m_search_scope, m_selection, m_found_verses, query);
        if (m_found_station_ranges != null)
        {
            m_found_verses = new List<Verse>();
            m_found_verse_ranges = new List<List<Verse>>();
            foreach (List<Station> range in m_found_station_ranges)
            {
                List<Verse> verses = new List<Verse>();
                foreach (Station station in range)
                {
                    if (station != null)
                    {
                        m_found_verses.AddRange(station.Verses);
                        verses.AddRange(station.Verses);
                    }
                }
                m_found_verse_ranges.Add(verses);
            }
            return m_found_station_ranges.Count;
        }
        return 0;
    }

    // find by numbers - Parts
    /// <summary>
    /// Find parts that meet query criteria.
    /// </summary>
    /// <param name="query"></param>
    /// <returns>Number of found parts. Result is stored in FoundParts.</returns>
    public int FindParts(NumberQuery query)
    {
        ClearSearchResults();
        m_found_parts = Server.FindParts(m_search_scope, m_selection, m_found_verses, query);
        if (m_found_parts != null)
        {
            m_found_verses = new List<Verse>();
            m_found_verse_ranges = new List<List<Verse>>();
            foreach (Part part in m_found_parts)
            {
                if (part != null)
                {
                    m_found_verses.AddRange(part.Verses);
                    m_found_verse_ranges.Add(part.Verses);
                }
            }
            return m_found_parts.Count;
        }
        return 0;
    }
    // find by numbers - PartRanges
    /// <summary>
    /// Find part ranges that meet query criteria.
    /// </summary>
    /// <param name="query"></param>
    /// <returns>Number of found part ranges. Result is stored in FoundPartRanges.</returns>
    public int FindPartRanges(NumberQuery query)
    {
        ClearSearchResults();
        m_found_part_ranges = Server.FindPartRanges(m_search_scope, m_selection, m_found_verses, query);
        if (m_found_part_ranges != null)
        {
            m_found_verses = new List<Verse>();
            m_found_verse_ranges = new List<List<Verse>>();
            foreach (List<Part> range in m_found_part_ranges)
            {
                List<Verse> verses = new List<Verse>();
                foreach (Part part in range)
                {
                    if (part != null)
                    {
                        m_found_verses.AddRange(part.Verses);
                        verses.AddRange(part.Verses);
                    }
                }
                m_found_verse_ranges.Add(verses);
            }
            return m_found_part_ranges.Count;
        }
        return 0;
    }

    // find by numbers - Groups
    /// <summary>
    /// Find groups that meet query criteria.
    /// </summary>
    /// <param name="query"></param>
    /// <returns>Number of found groups. Result is stored in FoundGroups.</returns>
    public int FindGroups(NumberQuery query)
    {
        ClearSearchResults();
        m_found_groups = Server.FindGroups(m_search_scope, m_selection, m_found_verses, query);
        if (m_found_groups != null)
        {
            m_found_verses = new List<Verse>();
            m_found_verse_ranges = new List<List<Verse>>();
            foreach (Group group in m_found_groups)
            {
                if (group != null)
                {
                    m_found_verses.AddRange(group.Verses);
                    m_found_verse_ranges.Add(group.Verses);
                }
            }
            return m_found_groups.Count;
        }
        return 0;
    }
    // find by numbers - GroupRanges
    /// <summary>
    /// Find group ranges that meet query criteria.
    /// </summary>
    /// <param name="query"></param>
    /// <returns>Number of found group ranges. Result is stored in FoundGroupRanges.</returns>
    public int FindGroupRanges(NumberQuery query)
    {
        ClearSearchResults();
        m_found_group_ranges = Server.FindGroupRanges(m_search_scope, m_selection, m_found_verses, query);
        if (m_found_group_ranges != null)
        {
            m_found_verses = new List<Verse>();
            m_found_verse_ranges = new List<List<Verse>>();
            foreach (List<Group> range in m_found_group_ranges)
            {
                List<Verse> verses = new List<Verse>();
                foreach (Group group in range)
                {
                    if (group != null)
                    {
                        m_found_verses.AddRange(group.Verses);
                        verses.AddRange(group.Verses);
                    }
                }
                m_found_verse_ranges.Add(verses);
            }
            return m_found_group_ranges.Count;
        }
        return 0;
    }

    // find by numbers - Halfs
    /// <summary>
    /// Find halfs that meet query criteria.
    /// </summary>
    /// <param name="query"></param>
    /// <returns>Number of found halfs. Result is stored in FoundHalfs.</returns>
    public int FindHalfs(NumberQuery query)
    {
        ClearSearchResults();
        m_found_halfs = Server.FindHalfs(m_search_scope, m_selection, m_found_verses, query);
        if (m_found_halfs != null)
        {
            m_found_verses = new List<Verse>();
            m_found_verse_ranges = new List<List<Verse>>();
            foreach (Half half in m_found_halfs)
            {
                if (half != null)
                {
                    m_found_verses.AddRange(half.Verses);
                    m_found_verse_ranges.Add(half.Verses);
                }
            }
            return m_found_halfs.Count;
        }
        return 0;
    }
    // find by numbers - HalfRanges
    /// <summary>
    /// Find half ranges that meet query criteria.
    /// </summary>
    /// <param name="query"></param>
    /// <returns>Number of found half ranges. Result is stored in FoundHalfRanges.</returns>
    public int FindHalfRanges(NumberQuery query)
    {
        ClearSearchResults();
        m_found_half_ranges = Server.FindHalfRanges(m_search_scope, m_selection, m_found_verses, query);
        if (m_found_half_ranges != null)
        {
            m_found_verses = new List<Verse>();
            m_found_verse_ranges = new List<List<Verse>>();
            foreach (List<Half> range in m_found_half_ranges)
            {
                List<Verse> verses = new List<Verse>();
                foreach (Half half in range)
                {
                    if (half != null)
                    {
                        m_found_verses.AddRange(half.Verses);
                        verses.AddRange(half.Verses);
                    }
                }
                m_found_verse_ranges.Add(verses);
            }
            return m_found_half_ranges.Count;
        }
        return 0;
    }

    // find by numbers - Quarters
    /// <summary>
    /// Find quarters that meet query criteria.
    /// </summary>
    /// <param name="query"></param>
    /// <returns>Number of found quarters. Result is stored in FoundQuarters.</returns>
    public int FindQuarters(NumberQuery query)
    {
        ClearSearchResults();
        m_found_quarters = Server.FindQuarters(m_search_scope, m_selection, m_found_verses, query);
        if (m_found_quarters != null)
        {
            m_found_verses = new List<Verse>();
            m_found_verse_ranges = new List<List<Verse>>();
            foreach (Quarter quarter in m_found_quarters)
            {
                if (quarter != null)
                {
                    m_found_verses.AddRange(quarter.Verses);
                    m_found_verse_ranges.Add(quarter.Verses);
                }
            }
            return m_found_quarters.Count;
        }
        return 0;
    }
    // find by numbers - QuarterRanges
    /// <summary>
    /// Find quarter ranges that meet query criteria.
    /// </summary>
    /// <param name="query"></param>
    /// <returns>Number of found quarter ranges. Result is stored in FoundQuarterRanges.</returns>
    public int FindQuarterRanges(NumberQuery query)
    {
        ClearSearchResults();
        m_found_quarter_ranges = Server.FindQuarterRanges(m_search_scope, m_selection, m_found_verses, query);
        if (m_found_quarter_ranges != null)
        {
            m_found_verses = new List<Verse>();
            m_found_verse_ranges = new List<List<Verse>>();
            foreach (List<Quarter> range in m_found_quarter_ranges)
            {
                List<Verse> verses = new List<Verse>();
                foreach (Quarter quarter in range)
                {
                    if (quarter != null)
                    {
                        m_found_verses.AddRange(quarter.Verses);
                        verses.AddRange(quarter.Verses);
                    }
                }
                m_found_verse_ranges.Add(verses);
            }
            return m_found_quarter_ranges.Count;
        }
        return 0;
    }

    // find by numbers - Bowings
    /// <summary>
    /// Find bowings that meet query criteria.
    /// </summary>
    /// <param name="query"></param>
    /// <returns>Number of found bowings. Result is stored in FoundBowings.</returns>
    public int FindBowings(NumberQuery query)
    {
        ClearSearchResults();
        m_found_bowings = Server.FindBowings(m_search_scope, m_selection, m_found_verses, query);
        if (m_found_bowings != null)
        {
            m_found_verses = new List<Verse>();
            m_found_verse_ranges = new List<List<Verse>>();
            foreach (Bowing bowing in m_found_bowings)
            {
                if (bowing != null)
                {
                    m_found_verses.AddRange(bowing.Verses);
                    m_found_verse_ranges.Add(bowing.Verses);
                }
            }
            return m_found_bowings.Count;
        }
        return 0;
    }
    // find by numbers - BowingRanges
    /// <summary>
    /// Find bowing ranges that meet query criteria.
    /// </summary>
    /// <param name="query"></param>
    /// <returns>Number of found bowing ranges. Result is stored in FoundBowingRanges.</returns>
    public int FindBowingRanges(NumberQuery query)
    {
        ClearSearchResults();
        m_found_bowing_ranges = Server.FindBowingRanges(m_search_scope, m_selection, m_found_verses, query);
        if (m_found_bowing_ranges != null)
        {
            m_found_verses = new List<Verse>();
            m_found_verse_ranges = new List<List<Verse>>();
            foreach (List<Bowing> range in m_found_bowing_ranges)
            {
                List<Verse> verses = new List<Verse>();
                foreach (Bowing bowing in range)
                {
                    if (bowing != null)
                    {
                        m_found_verses.AddRange(bowing.Verses);
                        verses.AddRange(bowing.Verses);
                    }
                }
                m_found_verse_ranges.Add(verses);
            }
            return m_found_bowing_ranges.Count;
        }
        return 0;
    }


    // find by similarity - phrases similar to given text
    /// <summary>
    /// Find phrases with similar text to given text with given similarity percentage or more.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="similarity_percentage"></param>
    /// <returns>Number of found phrases. Result is stored in FoundPhrases.</returns>
    public int FindPhrases(string text, double similarity_percentage)
    {
        ClearSearchResults();
        m_found_phrases = Server.FindPhrases(m_search_scope, m_selection, m_found_verses, text, similarity_percentage);
        if (m_found_phrases != null)
        {
            m_found_verses = new List<Verse>();
            foreach (Phrase phrase in m_found_phrases)
            {
                if (phrase != null)
                {
                    if (!m_found_verses.Contains(phrase.Verse))
                    {
                        m_found_verses.Add(phrase.Verse);
                    }
                }
            }
            return m_found_phrases.Count;
        }
        return 0;
    }
    // find by similarity - verses similar to given verse
    /// <summary>
    /// Find verses with similar text to verse text with given similarity percentage or more using given similarity method
    /// </summary>
    /// <param name="verse"></param>
    /// <param name="similarity_method"></param>
    /// <param name="similarity_percentage"></param>
    /// <returns>Number of found verses. Result is stored in FoundVerses.</returns>
    public int FindVerses(Verse verse, SimilarityMethod similarity_method, double similarity_percentage)
    {
        ClearSearchResults();
        m_found_verses = Server.FindVerses(m_search_scope, m_selection, m_found_verses, verse, similarity_method, similarity_percentage);
        if (m_found_verses != null)
        {
            return m_found_verses.Count;
        }
        return 0;
    }
    // find by similarity - all similar verses to each other throughout the book
    /// <summary>
    /// Find verse ranges with similar text to each other with given similarity percentage or more.
    /// </summary>
    /// <param name="similarity_method"></param>
    /// <param name="similarity_percentage"></param>
    /// <returns>Number of found verse ranges. Result is stored in FoundVerseRanges.</returns>
    public int FindVerses(SimilarityMethod similarity_method, double similarity_percentage)
    {
        ClearSearchResults();
        m_found_verse_ranges = Server.FindVersess(m_search_scope, m_selection, m_found_verses, similarity_method, similarity_percentage);
        if (m_found_verse_ranges != null)
        {
            m_found_verses = new List<Verse>();
            foreach (List<Verse> verse_range in m_found_verse_ranges)
            {
                m_found_verses.AddRange(verse_range);
            }
            return m_found_verse_ranges.Count;
        }
        return 0;
    }


    // find by prostration type
    /// <summary>
    /// Find verses with given prostration type.
    /// </summary>
    /// <param name="prostration_type"></param>
    /// <returns>Number of found verses. Result is stored in FoundVerses.</returns>
    public int FindVerses(ProstrationType prostration_type)
    {
        ClearSearchResults();
        m_found_verses = Server.FindVerses(m_search_scope, m_selection, m_found_verses, prostration_type);
        if (m_found_verses != null)
        {
            return m_found_verses.Count;
        }
        return 0;
    }


    // find by revelation place
    /// <summary>
    /// Find chapters that were revealed at given revelation place.
    /// </summary>
    /// <param name="revelation_place"></param>
    /// <returns>Number of found chapters. Result is stored in FoundChapters.</returns>
    public int FindChapters(RevelationPlace revelation_place)
    {
        ClearSearchResults();
        m_found_chapters = Server.FindChapters(m_search_scope, m_selection, m_found_verses, revelation_place);
        if (m_found_chapters != null)
        {
            if (m_found_chapters != null)
            {
                m_found_verses = new List<Verse>();
                foreach (Chapter chapter in m_found_chapters)
                {
                    if (chapter != null)
                    {
                        m_found_verses.AddRange(chapter.Verses);
                    }
                }
                return m_found_chapters.Count;
            }
        }
        return 0;
    }


    // find by initialization type
    /// <summary>
    /// Find verses with given initialization type.
    /// </summary>
    /// <param name="initialization_type"></param>
    /// <returns>Number of found verses. Result is stored in FoundVerses.</returns>
    public int FindVerses(InitializationType initialization_type)
    {
        ClearSearchResults();
        m_found_verses = Server.FindVerses(m_search_scope, m_selection, m_found_verses, initialization_type);
        if (m_found_verses != null)
        {
            return m_found_verses.Count;
        }
        return 0;
    }


    // find by letter frequency sum
    /// <summary>
    /// Find words with required letter frequency sum in their text of the given phrase.
    /// </summary>
    /// <param name="phrase"></param>
    /// <param name="sum"></param>
    /// <param name="frequency_search_type"></param>
    /// <returns>Number of found words. Result is stored in FoundWords.</returns>
    public int FindWords(string phrase, int sum, NumberType number_type, ComparisonOperator comparison_operator, int sum_remainder, FrequencySearchType frequency_search_type, bool? include_diacritics)
    {
        ClearSearchResults();
        m_found_words = Server.FindWords(m_search_scope, m_selection, m_found_verses, phrase, sum, number_type, comparison_operator, sum_remainder, frequency_search_type, include_diacritics);
        if (m_found_words != null)
        {
            m_found_verses = new List<Verse>();
            foreach (Word word in m_found_words)
            {
                if (word != null)
                {
                    if (!m_found_verses.Contains(word.Verse))
                    {
                        m_found_verses.Add(word.Verse);
                    }
                }
            }
            return m_found_words.Count;
        }
        return 0;
    }
    /// <summary>
    /// Find sentences across verses with required letter frequency sum in their text of the given phrase.
    /// </summary>
    /// <param name="phrase"></param>
    /// <param name="sum"></param>
    /// <param name="frequency_search_type"></param>
    /// <returns>Number of found sentences. Result is stored in FoundSentences.</returns>
    public int FindSentences(string phrase, int sum, NumberType number_type, ComparisonOperator comparison_operator, int sum_remainder, FrequencySearchType frequency_search_type, bool? include_diacritics)
    {
        ClearSearchResults();
        m_found_sentences = Server.FindSentences(m_search_scope, m_selection, m_found_verses, phrase, sum, number_type, comparison_operator, sum_remainder, frequency_search_type, include_diacritics);
        if (m_found_sentences != null)
        {
            BuildSentencePhrases();

            return m_found_sentences.Count;
        }
        return 0;
    }
    private void BuildSentencePhrases()
    {
        if (m_found_sentences != null)
        {
            m_found_verses = new List<Verse>();
            m_found_phrases = new List<Phrase>();
            foreach (Sentence sentence in m_found_sentences)
            {
                if (sentence != null)
                {
                    Verse first_verse = sentence.FirstVerse;
                    Verse last_verse = sentence.LastVerse;
                    if ((first_verse != null) && (last_verse != null))
                    {
                        int start = first_verse.Number - 1;
                        int end = last_verse.Number - 1;
                        if (end >= start)
                        {
                            // add unique verses
                            for (int i = start; i <= end; i++)
                            {
                                if (!m_found_verses.Contains(Book.Verses[i]))
                                {
                                    m_found_verses.Add(Book.Verses[i]);
                                }
                            }

                            // build phrases for colorization
                            if (start == end) // sentence within verse
                            {
                                Phrase sentence_phrase = new Phrase(first_verse, sentence.StartPosition, sentence.Text);
                                m_found_phrases.Add(sentence_phrase);
                            }
                            else // sentence across verses
                            {
                                // first verse
                                string start_text = first_verse.Text.Substring(sentence.StartPosition);
                                Phrase start_phrase = new Phrase(sentence.FirstVerse, sentence.StartPosition, start_text);
                                m_found_phrases.Add(start_phrase);

                                // middle verses
                                for (int i = start + 1; i < end; i++)
                                {
                                    Verse verse = Book.Verses[i];
                                    if (verse != null)
                                    {
                                        Phrase middle_phrase = new Phrase(verse, 0, verse.Text);
                                        m_found_phrases.Add(middle_phrase);
                                    }
                                }

                                // last verse
                                string end_text = last_verse.Text.Substring(0, sentence.EndPosition);
                                Phrase end_phrase = new Phrase(last_verse, 0, end_text);
                                m_found_phrases.Add(end_phrase);
                            }
                        }
                    }
                }
            }
        }
    }
    /// <summary>
    /// Find verses with required letter frequency sum in their text of the given phrase.
    /// </summary>
    /// <param name="phrase"></param>
    /// <param name="sum"></param>
    /// <param name="frequency_search_type"></param>
    /// <returns>Number of found verses. Result is stored in FoundVerses.</returns>
    public int FindVerses(string phrase, int sum, NumberType number_type, ComparisonOperator comparison_operator, int sum_remainder, FrequencySearchType frequency_search_type, bool? include_diacritics)
    {
        ClearSearchResults();
        m_found_verses = Server.FindVerses(m_search_scope, m_selection, m_found_verses, phrase, sum, number_type, comparison_operator, sum_remainder, frequency_search_type, include_diacritics);
        if (m_found_verses != null)
        {
            return m_found_verses.Count;
        }
        return 0;
    }
    /// <summary>
    /// Find chapters with required letter frequency sum in their text of the given phrase.
    /// </summary>
    /// <param name="phrase"></param>
    /// <param name="sum"></param>
    /// <param name="frequency_search_type"></param>
    /// <returns>Number of found chapters. Result is stored in FoundChapters.</returns>
    public int FindChapters(string phrase, int sum, NumberType number_type, ComparisonOperator comparison_operator, int sum_remainder, FrequencySearchType frequency_search_type, bool? include_diacritics)
    {
        ClearSearchResults();
        m_found_chapters = Server.FindChapters(m_search_scope, m_selection, m_found_verses, phrase, sum, number_type, comparison_operator, sum_remainder, frequency_search_type, include_diacritics);
        if (m_found_chapters != null)
        {
            if (m_found_chapters != null)
            {
                m_found_verses = new List<Verse>();
                foreach (Chapter chapter in m_found_chapters)
                {
                    if (chapter != null)
                    {
                        m_found_verses.AddRange(chapter.Verses);
                    }
                }
                return m_found_chapters.Count;
            }
        }
        return 0;
    }
    public int CalculateLetterFrequencySum(string text, string phrase, FrequencySearchType frequency_search_type, bool? include_diacritics)
    {
        return Server.CalculateLetterFrequencySum(text, phrase, frequency_search_type, include_diacritics);
    }

    /// <summary>
    /// Find words with required letter matching with optional frequency sum in phrase.
    /// </summary>
    /// <param name="phrase"></param>
    /// <param name="frequency_matching_type"></param>
    /// <param name="frequency_search_type"></param>
    /// <param name="include_diacritics"></param>
    /// <returns>Number of found words. Result is stored in FoundWords.</returns>
    public int FindWords(string phrase, FrequencyMatchingType frequency_matching_type, FrequencySearchType frequency_search_type, bool? include_diacritics)
    {
        ClearSearchResults();
        m_found_words = Server.FindWords(m_search_scope, m_selection, m_found_verses, phrase, frequency_matching_type, frequency_search_type, include_diacritics);
        if (m_found_words != null)
        {
            m_found_verses = new List<Verse>();
            foreach (Word word in m_found_words)
            {
                if (word != null)
                {
                    if (!m_found_verses.Contains(word.Verse))
                    {
                        m_found_verses.Add(word.Verse);
                    }
                }
            }
            return m_found_words.Count;
        }
        return 0;
    }
    /// <summary>
    /// Find sentences across verses with required letter matching with optional frequency sum in phrase.
    /// </summary>
    /// <param name="phrase"></param>
    /// <param name="frequency_matching_type"></param>
    /// <param name="frequency_search_type"></param>
    /// <param name="include_diacritics"></param>
    /// <returns>Number of found sentences. Result is stored in FoundSentences.</returns>
    public int FindSentences(string phrase, FrequencyMatchingType frequency_matching_type, FrequencySearchType frequency_search_type, bool? include_diacritics)
    {
        ClearSearchResults();
        m_found_sentences = Server.FindSentences(m_search_scope, m_selection, m_found_verses, phrase, frequency_matching_type, frequency_search_type, include_diacritics);
        if (m_found_sentences != null)
        {
            BuildSentencePhrases();

            return m_found_sentences.Count;
        }
        return 0;
    }
    /// <summary>
    /// Find verses verses with required letter matching with optional frequency sum in phrase.
    /// </summary>
    /// <param name="phrase"></param>
    /// <param name="frequency_matching_type"></param>
    /// <param name="frequency_search_type"></param>
    /// <param name="include_diacritics"></param>
    /// <returns>Number of found verses. Result is stored in FoundVerses.</returns>
    public int FindVerses(string phrase, FrequencyMatchingType frequency_matching_type, FrequencySearchType frequency_search_type, bool? include_diacritics)
    {
        ClearSearchResults();
        m_found_verses = Server.FindVerses(m_search_scope, m_selection, m_found_verses, phrase, frequency_matching_type, frequency_search_type, include_diacritics);
        if (m_found_verses != null)
        {
            return m_found_verses.Count;
        }
        return 0;
    }
    /// <summary>
    /// Find chapters verses with required letter matching with optional frequency sum in phrase.
    /// </summary>
    /// <param name="phrase"></param>
    /// <param name="frequency_matching_type"></param>
    /// <param name="frequency_search_type"></param>
    /// <param name="include_diacritics"></param>
    /// <returns>Number of found chapters. Result is stored in FoundChapters.</returns>
    public int FindChapters(string phrase, FrequencyMatchingType frequency_matching_type, FrequencySearchType frequency_search_type, bool? include_diacritics)
    {
        ClearSearchResults();
        m_found_chapters = Server.FindChapters(m_search_scope, m_selection, m_found_verses, phrase, frequency_matching_type, frequency_search_type, include_diacritics);
        if (m_found_chapters != null)
        {
            if (m_found_chapters != null)
            {
                m_found_verses = new List<Verse>();
                foreach (Chapter chapter in m_found_chapters)
                {
                    if (chapter != null)
                    {
                        m_found_verses.AddRange(chapter.Verses);
                    }
                }
                return m_found_chapters.Count;
            }
        }
        return 0;
    }


    private List<LetterStatistic> m_letter_statistics = new List<LetterStatistic>();
    public List<LetterStatistic> LetterStatistics
    {
        get { return m_letter_statistics; }
    }
    public void SortLetterStatistics(StatisticCompareBy compare_by)
    {
        LetterStatistic.CompareBy = compare_by;

        if (LetterStatistic.CompareOrder == StatisticCompareOrder.Ascending)
        {
            LetterStatistic.CompareOrder = StatisticCompareOrder.Descending;
        }
        else
        {
            LetterStatistic.CompareOrder = StatisticCompareOrder.Ascending;
        }

        m_letter_statistics.Sort();
    }
    /// <summary>
    /// Calculate letter statistics for the given text.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="phrase"></param>
    /// <param name="frequency_search_type"></param>
    /// <returns>Result is stored in LetterStatistics.</returns>
    public void BuildLetterStatistics(string text, bool? include_diacritics)
    {
        if (String.IsNullOrEmpty(text)) return;

        if (NumerologySystem != null)
        {
            if (m_letter_statistics != null)
            {
                if (include_diacritics == true) { /* do nothing */ }
                else if (include_diacritics == null) { text = text.GetDiacritics(); }
                else if (include_diacritics == false) { text = text.Simplify(NumerologySystem.TextMode); }

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

                m_letter_statistics.Clear();
                if (!String.IsNullOrEmpty(text))
                {
                    for (int i = 0; i < text.Length; i++)
                    {
                        bool is_found = false;
                        for (int j = 0; j < m_letter_statistics.Count; j++)
                        {
                            if (text[i] == m_letter_statistics[j].Letter)
                            {
                                is_found = true;
                                m_letter_statistics[j].Frequency++;
                                int position = i + 1;
                                long last_position = m_letter_statistics[j].Positions[m_letter_statistics[j].Positions.Count - 1];
                                m_letter_statistics[j].Positions.Add(position);
                                m_letter_statistics[j].PositionSum += position;
                                long distance = position - last_position;
                                m_letter_statistics[j].Distances.Add(distance);
                                m_letter_statistics[j].DistanceSum += distance;
                            }
                        }
                        if (!is_found)
                        {
                            LetterStatistic letter_statistic = new LetterStatistic();

                            letter_statistic.Order = m_letter_statistics.Count + 1;
                            letter_statistic.Letter = text[i];
                            letter_statistic.Frequency = 1;
                            int position = i + 1;
                            letter_statistic.Positions.Add(position);
                            letter_statistic.PositionSum += position;

                            m_letter_statistics.Add(letter_statistic);
                        }
                    }

                    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    // Reverse Positions and Disctances
                    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    for (int i = text.Length - 1; i >= 0; i--)
                    {
                        for (int j = 0; j < m_letter_statistics.Count; j++)
                        {
                            if (text[i] == m_letter_statistics[j].Letter)
                            {
                                int reverse_position = text.Length - i;
                                if (m_letter_statistics[j].ReversePositions.Count == 0)
                                {
                                    m_letter_statistics[j].ReversePositions.Add(reverse_position);
                                    m_letter_statistics[j].ReversePositionSum += reverse_position;
                                }
                                else
                                {
                                    long last_reverse_position = m_letter_statistics[j].ReversePositions[m_letter_statistics[j].ReversePositions.Count - 1];
                                    m_letter_statistics[j].ReversePositions.Add(reverse_position);
                                    m_letter_statistics[j].ReversePositionSum += reverse_position;
                                    long reverse_distance = reverse_position - last_reverse_position;
                                    m_letter_statistics[j].ReverseDistances.Add(reverse_distance);
                                    m_letter_statistics[j].ReverseDistanceSum += reverse_distance;
                                }
                            }
                        }
                    }
                    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                }
            }
        }
    }
    public void SaveLetterStatistics(string filename, string text, bool verbose)
    {
        if (String.IsNullOrEmpty(filename)) return;
        if (String.IsNullOrEmpty(text)) return;

        if (Directory.Exists(Globals.STATISTICS_FOLDER))
        {
            filename = Globals.STATISTICS_FOLDER + Path.DirectorySeparatorChar + filename;
            try
            {
                if (NumerologySystem != null)
                {
                    using (StreamWriter writer = new StreamWriter(filename, false, Encoding.Unicode))
                    {
                        writer.WriteLine("-------------------------------------------------------------------");
                        StringBuilder numbers = new StringBuilder();
                        foreach (int index in Selection.Indexes)
                        {
                            numbers.Append((index + 1).ToString() + ", ");
                        }
                        if (numbers.Length > 0)
                        {
                            numbers.Remove(numbers.Length - 2, 2);
                        }
                        writer.WriteLine(NumerologySystem.Name);
                        writer.WriteLine("Selection = " + Selection.Scope.ToString() + " " + numbers.ToString());
                        writer.WriteLine("-------------------------------------------------------------------");
                        writer.WriteLine();
                        writer.WriteLine("-------------------------------------------------------------------");
                        writer.WriteLine("Text");
                        writer.WriteLine("-------------------------------------------------------------------");
                        writer.WriteLine(text);
                        writer.WriteLine("-------------------------------------------------------------------");
                        writer.WriteLine();
                        writer.WriteLine("-------------------------------------------------------------------");
                        if (verbose) writer.WriteLine("Order" + "\t" + "Letter" + "\t" + "Freq" + "\t" + "ΣPos" + "\t" + "\t" + "Σ∆" + "\t" + "\t" + "ΣrPos" + "\t" + "\t" + "Σr∆" + "\t");
                        else writer.WriteLine("Order" + "\t" + "Letter" + "\t" + "Freq" + "\t" + "ΣPos" + "\t" + "Σ∆" + "\t" + "ΣrPos" + "\t" + "Σr∆");
                        writer.WriteLine("-------------------------------------------------------------------");
                        int count = 0;
                        int running_sum = 0;
                        int frequence_sum = 0;
                        long positionsum_sum = 0;
                        long distancesum_sum = 0;
                        long reverse_positionsum_sum = 0;
                        long reverse_distancesum_sum = 0;
                        foreach (LetterStatistic letter_statistic in m_letter_statistics)
                        {
                            if (verbose)
                            {
                                StringBuilder positions = new StringBuilder();
                                foreach (int value in letter_statistic.Positions)
                                {
                                    positions.Append(value.ToString() + "+");
                                }
                                if (positions.Length > 0)
                                {
                                    positions.Remove(positions.Length - 1, 1);
                                }

                                StringBuilder distances = new StringBuilder();
                                foreach (int value in letter_statistic.Distances)
                                {
                                    distances.Append(value.ToString() + "+");
                                }
                                if (distances.Length > 0)
                                {
                                    distances.Remove(distances.Length - 1, 1);
                                }

                                StringBuilder reverse_positions = new StringBuilder();
                                foreach (int value in letter_statistic.ReversePositions)
                                {
                                    reverse_positions.Append(value.ToString() + "+");
                                }
                                if (reverse_positions.Length > 0)
                                {
                                    reverse_positions.Remove(reverse_positions.Length - 1, 1);
                                }

                                StringBuilder reverse_distances = new StringBuilder();
                                foreach (int value in letter_statistic.ReverseDistances)
                                {
                                    reverse_distances.Append(value.ToString() + "+");
                                }
                                if (reverse_distances.Length > 0)
                                {
                                    reverse_distances.Remove(reverse_distances.Length - 1, 1);
                                }

                                writer.WriteLine(letter_statistic.Order.ToString() + "\t" +
                                                 letter_statistic.Letter.ToString() + '\t' +
                                                 letter_statistic.Frequency.ToString() + "\t" +
                                                 letter_statistic.PositionSum.ToString() + "\t" + positions.ToString() + "\t" +
                                                 letter_statistic.DistanceSum.ToString() + "\t" + distances.ToString() + "\t" +
                                                 letter_statistic.ReversePositionSum.ToString() + "\t" + reverse_positions.ToString() + "\t" +
                                                 letter_statistic.ReverseDistanceSum.ToString() + "\t" + reverse_distances.ToString()
                                                 );
                            }
                            else
                            {
                                writer.WriteLine(letter_statistic.Order.ToString() + "\t" +
                                                letter_statistic.Letter.ToString() + '\t' +
                                                letter_statistic.Frequency.ToString() + "\t" +
                                                letter_statistic.PositionSum.ToString() + "\t" +
                                                letter_statistic.DistanceSum.ToString() + "\t" +
                                                letter_statistic.ReversePositionSum.ToString() + "\t" +
                                                letter_statistic.ReverseDistanceSum.ToString() + "\t"
                                                );
                            }
                            count++;
                            running_sum += count;
                            frequence_sum += letter_statistic.Frequency;
                            positionsum_sum += letter_statistic.PositionSum;
                            distancesum_sum += letter_statistic.DistanceSum;
                            reverse_positionsum_sum += letter_statistic.ReversePositionSum;
                            reverse_distancesum_sum += letter_statistic.ReverseDistanceSum;
                        }
                        writer.WriteLine("-------------------------------------------------------------------");
                        if (verbose) writer.WriteLine(running_sum + "\t" + "Sum" + "\t" + frequence_sum.ToString() + "\t" + positionsum_sum.ToString() + "\t" + "\t" + distancesum_sum.ToString() + "\t" + "\t" + reverse_positionsum_sum.ToString() + "\t" + "\t" + reverse_distancesum_sum.ToString() + "\t");
                        else writer.WriteLine(running_sum + "\t" + "Sum" + "\t" + frequence_sum.ToString() + "\t" + positionsum_sum.ToString() + "\t" + distancesum_sum.ToString() + "\t" + reverse_positionsum_sum.ToString() + "\t" + reverse_distancesum_sum.ToString());
                        writer.WriteLine("-------------------------------------------------------------------");
                    }
                }
            }
            catch
            {
                // silence IO error in case running from read-only media (CD/DVD)
            }

            // show file content after save
            FileHelper.DisplayFile(filename);
        }
    }
    /// <summary>
    /// Calculate letter statistics for the given phrase in text.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="phrase"></param>
    /// <param name="frequency_search_type"></param>
    /// <returns>Letter frequencies are stored in LetterStatistics.</returns>
    public void BuildLetterStatistics(string text, string phrase, FrequencySearchType frequency_search_type, bool? include_diacritics)
    {
        if (String.IsNullOrEmpty(text)) return;
        if (String.IsNullOrEmpty(phrase)) return;

        if (NumerologySystem != null)
        {
            if (m_letter_statistics != null)
            {
                if (include_diacritics == true) { /* do nothing */ }
                else if (include_diacritics == null) { text = text.GetDiacritics(); }
                else if (include_diacritics == false) { text = text.Simplify(NumerologySystem.TextMode); }

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

                if (include_diacritics == true) { /* do nothing */ }
                else if (include_diacritics == null) { phrase = phrase.GetDiacritics(); }
                else if (include_diacritics == false) { phrase = phrase.Simplify(NumerologySystem.TextMode); }

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

                if (frequency_search_type == FrequencySearchType.UniqueLetters)
                {
                    phrase = phrase.RemoveDuplicates();
                }

                m_letter_statistics.Clear();
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
                            LetterStatistic phrase_letter_statistic = new LetterStatistic();
                            phrase_letter_statistic.Order = m_letter_statistics.Count + 1;
                            phrase_letter_statistic.Letter = phrase[i];
                            phrase_letter_statistic.Frequency = frequency;
                            m_letter_statistics.Add(phrase_letter_statistic);
                        }
                    }
                }
            }
        }
    }
    public void SaveLetterStatistics(string filename, string text, string phrase)
    {
        if (String.IsNullOrEmpty(filename)) return;
        if (String.IsNullOrEmpty(text)) return;

        if (Directory.Exists(Globals.STATISTICS_FOLDER))
        {
            filename = Globals.STATISTICS_FOLDER + Path.DirectorySeparatorChar + "Phrase_" + filename;
            try
            {
                if (NumerologySystem != null)
                {
                    if (m_letter_statistics != null)
                    {
                        using (StreamWriter writer = new StreamWriter(filename, false, Encoding.Unicode))
                        {
                            writer.WriteLine("-------------------------------------------------------------------");
                            StringBuilder numbers = new StringBuilder();
                            foreach (int index in Selection.Indexes)
                            {
                                numbers.Append((index + 1).ToString() + ", ");
                            }
                            if (numbers.Length > 0)
                            {
                                numbers.Remove(numbers.Length - 2, 2);
                            }
                            writer.WriteLine(NumerologySystem.Name);
                            writer.WriteLine("Selection = " + Selection.Scope.ToString() + " " + numbers.ToString());
                            writer.WriteLine("-------------------------------------------------------------------");
                            writer.WriteLine();
                            writer.WriteLine("-------------------------------------------------------------------");
                            writer.WriteLine("Text");
                            writer.WriteLine("-------------------------------------------------------------------");
                            writer.WriteLine(text);
                            writer.WriteLine("-------------------------------------------------------------------");
                            writer.WriteLine();
                            writer.WriteLine("-------------------------------------------------------------------");
                            writer.WriteLine("Phrase");
                            writer.WriteLine("-------------------------------------------------------------------");
                            writer.WriteLine(phrase);
                            writer.WriteLine("-------------------------------------------------------------------");
                            writer.WriteLine();
                            writer.WriteLine("-------------------------------------------------------------------");
                            writer.WriteLine("Order" + "\t" + "Letter" + "\t" + "Frequency");
                            writer.WriteLine("-------------------------------------------------------------------");
                            int count = m_letter_statistics.Count;
                            int sum = 0;
                            for (int i = 0; i < count; i++)
                            {
                                writer.WriteLine(m_letter_statistics[i].Order.ToString() + "\t" + m_letter_statistics[i].Letter.ToString() + '\t' + m_letter_statistics[i].Frequency.ToString());
                                sum += m_letter_statistics[i].Frequency;
                            }
                            writer.WriteLine("-------------------------------------------------------------------");
                            writer.WriteLine("Total" + "\t" + count.ToString() + "\t" + sum.ToString());
                            writer.WriteLine("-------------------------------------------------------------------");
                        }
                    }
                }
            }
            catch
            {
                // silence IO error in case running from read-only media (CD/DVD)
            }

            // show file content after save
            FileHelper.DisplayFile(filename);
        }
    }


    public List<string> HelpMessages
    {
        get { return Server.HelpMessages; }
    }
}
