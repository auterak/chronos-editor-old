using System.Windows;

namespace ChronosEditor {
    /// <summary>
    /// Okno pro formulář pro přidání poddokumentu
    /// </summary>
    public partial class SubAttribute : Window {
        private readonly string _path;
        private readonly int _docId;
        private readonly Document _scheme;

        //Konstruktor
        public SubAttribute(string path, int docId) {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();
            this._path = path;
            this._docId = docId;
            _scheme = (Document)Application.Current.Properties["scheme"];
        }

        //Obsluha načtení okna - generování formuláře
        private void Window_Loaded(object sender, RoutedEventArgs e) {
            if (_scheme != null) {
                grid.GenerateElements(_path, false, _docId, true);
            }
        }
    }
}
