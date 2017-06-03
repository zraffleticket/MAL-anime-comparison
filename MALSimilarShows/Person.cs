namespace MALSimilarShows {
    internal class Person {
        private string url;
        private string name;
        private string position;

        public Person(string url, string name, string position) {
            this.url = url;
            this.name = name;
            this.position = position;
        }

        public string Url { get => url; set => url = value; }
        public string Name { get => name; set => name = value; }
        public string Position { get => position; set => position = value; }
    }
}