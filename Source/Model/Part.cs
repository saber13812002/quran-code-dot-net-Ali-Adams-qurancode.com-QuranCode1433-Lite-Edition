using System;
using System.Text;
using System.Collections.Generic;

namespace Model
{
    public class Part
    {
        private Book book = null;
        public Book Book
        {
            get { return book; }
            set { book = value; }
        }

        private List<Verse> verses = null;
        public List<Verse> Verses
        {
            get { return verses; }
        }

        private int number = 0;
        public int Number
        {
            get { return number; }
        }

        private int word_count = 0;
        public int WordCount
        {
            get
            {
                if (word_count <= 0)
                {
                    if (this.verses != null)
                    {
                        foreach (Verse verse in this.verses)
                        {
                            if (verse.Words != null)
                            {
                                word_count += verse.Words.Count;
                            }
                        }
                    }
                }
                return word_count;
            }
        }

        private int letter_count = 0;
        public int LetterCount
        {
            get
            {
                if (letter_count <= 0)
                {
                    if (this.verses != null)
                    {
                        foreach (Verse verse in this.verses)
                        {
                            if (verse.Words != null)
                            {
                                foreach (Word word in verse.Words)
                                {
                                    if ((word.Letters != null) && (word.Letters.Count > 0))
                                    {
                                        letter_count += word.Letters.Count;
                                    }
                                }
                            }
                        }
                    }
                }
                return letter_count;
            }
        }

        private List<char> unique_letters = null;
        public List<char> UniqueLetters
        {
            get
            {
                if (unique_letters == null)
                {
                    unique_letters = new List<char>();
                    if (this.verses != null)
                    {
                        foreach (Verse verse in this.verses)
                        {
                            if (verse.UniqueLetters != null)
                            {
                                foreach (char character in verse.UniqueLetters)
                                {
                                    if (!unique_letters.Contains(character))
                                    {
                                        unique_letters.Add(character);
                                    }
                                }
                            }
                        }
                    }
                }
                return unique_letters;
            }
        }
        public int GetLetterFrequency(char character)
        {
            int result = 0;
            if (this.verses != null)
            {
                foreach (Verse verse in this.verses)
                {
                    foreach (Word word in verse.Words)
                    {
                        foreach (Letter letter in word.Letters)
                        {
                            if (letter.Character == character)
                            {
                                result++;
                            }
                        }
                    }
                }
            }
            return result;
        }

        public Part(Book book, int number, List<Verse> verses)
        {
            this.book = book;
            this.number = number;
            this.verses = verses;
            if (this.verses != null)
            {
                int verse_number = 1;
                foreach (Verse verse in this.verses)
                {
                    verse.Part = this;
                    verse.NumberInPart = verse_number++;
                }
            }
        }

        public string Text
        {
            get
            {
                StringBuilder str = new StringBuilder();
                if (this.verses != null)
                {
                    if (this.verses.Count > 0)
                    {
                        foreach (Verse verse in this.verses)
                        {
                            str.AppendLine(verse.Text);
                        }
                        if (str.Length > 2)
                        {
                            str.Remove(str.Length - 2, 2);
                        }
                    }
                }
                return str.ToString();
            }
        }
        public override string ToString()
        {
            return this.Text;
        }
    }
}
