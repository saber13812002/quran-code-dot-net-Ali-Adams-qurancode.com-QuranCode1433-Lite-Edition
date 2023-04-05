using System;
using System.Text;

namespace Model
{
    public class Phrase
    {
        private Verse verse = null;
        public Verse Verse
        {
            get { return verse; }
        }

        private int position = -1;
        public int Position
        {
            get { return position; }
        }

        private string text = null;
        public string Text
        {
            get { return text; }
        }
        public override string ToString()
        {
            return this.Text;
        }

        public Phrase(Verse verse, int position, string text)
        {
            this.verse = verse;
            this.text = text;
            this.position = position;
        }
    }
}
