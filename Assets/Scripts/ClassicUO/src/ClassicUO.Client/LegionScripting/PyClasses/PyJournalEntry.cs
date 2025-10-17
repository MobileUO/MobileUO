using System;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;

namespace ClassicUO.LegionScripting.PyClasses
{
    // MobileUO: primary constructors not available in Unity
    public class PyJournalEntry//(JournalEntry entry)
    {
        //public ushort Hue = entry.Hue;
        //public string Name = entry.Name;
        //public string Text = entry.Text;

        //public TextType TextType = entry.TextType;
        //public DateTime Time = entry.Time;
        //public MessageType MessageType = entry.MessageType;

        public ushort Hue;
        public string Name;
        public string Text;

        public TextType TextType;
        public DateTime Time;
        public MessageType MessageType;

        public PyJournalEntry(JournalEntry entry)
        {
            Hue = entry.Hue;
            Name = entry.Name;
            Text = entry.Text;

            TextType = entry.TextType;
            Time = entry.Time;
            MessageType = entry.MessageType;
        }

        public bool Disposed;
    }
}