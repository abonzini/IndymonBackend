using System.Text;

namespace GameDataContainer
{
    public class MessageLogger
    {
        public string DirectoryPath = "";
        const string BASIC_TITLE = "EVENT TITLE HERE";
        const string BASIC_SIGNATURE = "<@&1430941258041266358>"; // The role of the Indymon Revival group signature
        public string EventTitle = $"# {BASIC_TITLE}";
        public StringBuilder PreEventText = new StringBuilder();
        public StringBuilder EventText = new StringBuilder();
        public StringBuilder PostEventText = new StringBuilder();
        public string Signature = BASIC_SIGNATURE;
        /// <summary>
        /// Clears the current message building
        /// </summary>
        public void Clear()
        {
            EventTitle = BASIC_TITLE;
            Signature = BASIC_SIGNATURE;
            PreEventText.Clear();
            EventText.Clear();
            PostEventText.Clear();
        }
        /// <summary>
        /// Saves built message into file
        /// </summary>
        /// <param name="filename">File to save to</param>
        public void SaveToFile(string filename)
        {
            if (DirectoryPath == "") throw new Exception("No directory has been defined yet!");
            string path = Path.Combine(DirectoryPath, $"{filename}.txt");
            // Path done, now print the string
            List<string> finalStringArray = [EventTitle];
            if (PreEventText.Length > 0) { finalStringArray.Add(PreEventText.ToString()); }
            if (EventText.Length > 0) { finalStringArray.Add(EventText.ToString()); }
            if (PostEventText.Length > 0) { finalStringArray.Add(PostEventText.ToString()); }
            finalStringArray.Add(Signature);
            File.WriteAllText(filename, string.Join("\n\n", finalStringArray));
        }
    }
}
