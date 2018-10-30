namespace ChronosEditor {
    internal class Attribute {
        public string Name { get; set; }
        public string Value { get; set; }
        internal Document Child { get; set; }
        public bool Container { get; set; }
        public bool Link { get; set; }

        public Attribute(string name, string value, Document child = null, bool container = false, bool link = false) {
            Name = name;
            Value = value;
            Child = child;
            Container = container;
            Link = link;
        }
    }
}