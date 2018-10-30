using System.Windows;

namespace ChronosEditor {
    /// <summary>
    /// Okno pro přidání nového atributu
    /// </summary>
    public partial class NewAttr : Window {
        private readonly string _path;
        private readonly string _title;
        private readonly int _docId;

        //Konstruktor
        public NewAttr(string path, string title, int docId = -1) {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();
            _path = path;
            _title = title;
            _docId = docId;
            Success = false;
        }

        //Vlastnost pro uchování informace o úspěchu
        public bool Success {
            get;
            private set;
        }

        //Obsluha tlačítka pro vytvoření nového atributu - vyvolání formuláře
        private void btnNew_Click(object sender, RoutedEventArgs e) {
            var success = false;
            var subAttribute = new SubAttribute(_path, _docId) {
                Title = _title,
                Tag = _path
            };
            this.Hide();
            subAttribute.ShowDialog();
            this.Show();
            try {
                success = bool.Parse(subAttribute.Tag.ToString());
            } catch {
                success = false;
            }

            if (!success) return;
            Success = true;
            this.Close();
        }

        //Obsluha tlačítka pro vložení existujícího dokumentu
        private void btnExisting_Click(object sender, RoutedEventArgs e) {
            var existingAttr = new ExistingAttr(_path, _docId);
            this.Hide();
            existingAttr.ShowDialog();
            this.Show();
            if (!existingAttr.Success) return;
            Success = true;
            this.Close();
        }
    }
}
