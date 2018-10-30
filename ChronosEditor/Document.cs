using System.Collections.Generic;

namespace ChronosEditor {
    internal class Document {
        public Dictionary<string, Attribute> Attributes { get; }

        public Document() {
            Attributes = new Dictionary<string, Attribute>();
        }

        public void AddAttribute(Attribute attribute) {
            Attributes.Add(attribute.Name.SplitJoinExcLast(), attribute);
        }
    }
}
