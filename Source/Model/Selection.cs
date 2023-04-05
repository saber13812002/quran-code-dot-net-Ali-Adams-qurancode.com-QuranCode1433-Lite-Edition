using System;
using System.Text;
using System.Collections.Generic;

namespace Model
{
    public class Selection
    {
        private Book book = null;
        public Book Book
        {
            get { return book; }
        }

        private SelectionScope scope = SelectionScope.Book;
        public SelectionScope Scope
        {
            get { return scope; }
        }

        private List<int> indexes = null;
        public List<int> Indexes
        {
            get { return indexes; }
        }

        private List<Verse> verses = null;
        public List<Verse> Verses
        {
            get
            {
                if (verses != null) return verses;

                if (book != null)
                {
                    if (indexes != null)
                    {
                        verses = new List<Verse>();
                        switch (scope)
                        {
                            case SelectionScope.Book:
                                {
                                    if (book.Verses != null)
                                    {
                                        verses.AddRange(book.Verses);
                                    }
                                }
                                break;
                            case SelectionScope.Station:
                                {
                                    if (book.Stations != null)
                                    {
                                        foreach (int index in indexes)
                                        {
                                            if ((index >= 0) && (index < book.Stations.Count))
                                            {
                                                verses.AddRange(book.Stations[index].Verses);
                                            }
                                        }
                                    }
                                }
                                break;
                            case SelectionScope.Part:
                                {
                                    if (book.Parts != null)
                                    {
                                        foreach (int index in indexes)
                                        {
                                            if ((index >= 0) && (index < book.Parts.Count))
                                            {
                                                verses.AddRange(book.Parts[index].Verses);
                                            }
                                        }
                                    }
                                }
                                break;
                            case SelectionScope.Group:
                                {
                                    if (book.Groups != null)
                                    {
                                        foreach (int index in indexes)
                                        {
                                            if ((index >= 0) && (index < book.Groups.Count))
                                            {
                                                verses.AddRange(book.Groups[index].Verses);
                                            }
                                        }
                                    }
                                }
                                break;
                            case SelectionScope.Half:
                                {
                                    if (book.Halfs != null)
                                    {
                                        foreach (int index in indexes)
                                        {
                                            if ((index >= 0) && (index < book.Halfs.Count))
                                            {
                                                verses.AddRange(book.Halfs[index].Verses);
                                            }
                                        }
                                    }
                                }
                                break;
                            case SelectionScope.Quarter:
                                {
                                    if (book.Quarters != null)
                                    {
                                        foreach (int index in indexes)
                                        {
                                            if ((index >= 0) && (index < book.Quarters.Count))
                                            {
                                                verses.AddRange(book.Quarters[index].Verses);
                                            }
                                        }
                                    }
                                }
                                break;
                            case SelectionScope.Bowing:
                                {
                                    if (book.Bowings != null)
                                    {
                                        foreach (int index in indexes)
                                        {
                                            if ((index >= 0) && (index < book.Bowings.Count))
                                            {
                                                verses.AddRange(book.Bowings[index].Verses);
                                            }
                                        }
                                    }
                                }
                                break;
                            case SelectionScope.Chapter:
                                {
                                    if (book.Chapters != null)
                                    {
                                        foreach (int index in indexes)
                                        {
                                            foreach (Chapter chapter in book.Chapters)
                                            {
                                                if ((chapter.Number - 1) == index)
                                                {
                                                    verses.AddRange(chapter.Verses);
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            case SelectionScope.Verse:
                                {
                                    if (book.Verses != null)
                                    {
                                        foreach (int index in indexes)
                                        {
                                            if ((index >= 0) && (index < book.Verses.Count))
                                            {
                                                verses.Add(book.Verses[index]);
                                            }
                                        }
                                    }
                                }
                                break;
                            case SelectionScope.Page:
                                {
                                    if (book.Pages != null)
                                    {
                                        foreach (int index in indexes)
                                        {
                                            if ((index >= 0) && (index < book.Pages.Count))
                                            {
                                                verses.AddRange(book.Pages[index].Verses);
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
                return verses;
            }
        }

        private List<Chapter> chapters = null;
        public List<Chapter> Chapters
        {
            get
            {
                if (chapters != null) return chapters;

                if (book != null)
                {
                    if (indexes != null)
                    {
                        chapters = new List<Chapter>();
                        switch (scope)
                        {
                            case SelectionScope.Book:
                                {
                                    if (book.Chapters != null)
                                    {
                                        chapters.AddRange(book.Chapters);
                                    }
                                }
                                break;
                            case SelectionScope.Station:
                                {
                                    if (book.Stations != null)
                                    {
                                        foreach (int index in indexes)
                                        {
                                            if ((index >= 0) && (index < book.Stations.Count))
                                            {
                                                foreach (Verse verse in book.Stations[index].Verses)
                                                {
                                                    if (!chapters.Contains(verse.Chapter))
                                                    {
                                                        chapters.Add(verse.Chapter);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            case SelectionScope.Part:
                                {
                                    if (book.Parts != null)
                                    {
                                        foreach (int index in indexes)
                                        {
                                            if ((index >= 0) && (index < book.Parts.Count))
                                            {
                                                foreach (Verse verse in book.Parts[index].Verses)
                                                {
                                                    if (!chapters.Contains(verse.Chapter))
                                                    {
                                                        chapters.Add(verse.Chapter);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            case SelectionScope.Group:
                                {
                                    if (book.Groups != null)
                                    {
                                        foreach (int index in indexes)
                                        {
                                            if ((index >= 0) && (index < book.Groups.Count))
                                            {
                                                foreach (Verse verse in book.Groups[index].Verses)
                                                {
                                                    if (!chapters.Contains(verse.Chapter))
                                                    {
                                                        chapters.Add(verse.Chapter);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            case SelectionScope.Half:
                                {
                                    if (book.Halfs != null)
                                    {
                                        foreach (int index in indexes)
                                        {
                                            if ((index >= 0) && (index < book.Halfs.Count))
                                            {
                                                foreach (Verse verse in book.Halfs[index].Verses)
                                                {
                                                    if (!chapters.Contains(verse.Chapter))
                                                    {
                                                        chapters.Add(verse.Chapter);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            case SelectionScope.Quarter:
                                {
                                    if (book.Quarters != null)
                                    {
                                        foreach (int index in indexes)
                                        {
                                            if ((index >= 0) && (index < book.Quarters.Count))
                                            {
                                                foreach (Verse verse in book.Quarters[index].Verses)
                                                {
                                                    if (!chapters.Contains(verse.Chapter))
                                                    {
                                                        chapters.Add(verse.Chapter);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            case SelectionScope.Bowing:
                                {
                                    if (book.Bowings != null)
                                    {
                                        foreach (int index in indexes)
                                        {
                                            if ((index >= 0) && (index < book.Bowings.Count))
                                            {
                                                foreach (Verse verse in book.Bowings[index].Verses)
                                                {
                                                    if (!chapters.Contains(verse.Chapter))
                                                    {
                                                        chapters.Add(verse.Chapter);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            case SelectionScope.Chapter:
                                {
                                    if (book.Chapters != null)
                                    {
                                        foreach (int index in indexes)
                                        {
                                            foreach (Chapter chapter in book.Chapters)
                                            {
                                                if ((chapter.Number - 1) == index)
                                                {
                                                    chapters.Add(chapter);
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            case SelectionScope.Verse:
                                {
                                    if (book.Verses != null)
                                    {
                                        foreach (int index in indexes)
                                        {
                                            if ((index >= 0) && (index < book.Verses.Count))
                                            {
                                                if (!chapters.Contains(book.Verses[index].Chapter))
                                                {
                                                    chapters.Add(book.Verses[index].Chapter);
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            case SelectionScope.Page:
                                {
                                    if (book.Pages != null)
                                    {
                                        foreach (int index in indexes)
                                        {
                                            if ((index >= 0) && (index < book.Pages.Count))
                                            {
                                                foreach (Verse verse in book.Pages[index].Verses)
                                                {
                                                    if (!chapters.Contains(verse.Chapter))
                                                    {
                                                        chapters.Add(verse.Chapter);
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
                return chapters;
            }
        }

        public Selection(Book book, SelectionScope scope, List<int> indexes)
        {
            this.book = book;
            this.scope = scope;
            if (indexes != null)
            {
                this.indexes = new List<int>(indexes);
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
