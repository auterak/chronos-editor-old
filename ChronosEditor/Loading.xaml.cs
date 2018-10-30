using System.Windows;

namespace ChronosEditor
{
    /// <summary>
    /// Okno s progress barem pro načtení schématu z JSON dokumentu
    /// </summary>
    public partial class Loading : Window {
        private readonly string _schemePath;
        private readonly ChronosLib _db;
        private readonly string _login;
        private readonly string _pwd;
        private readonly int _docId;

        //Konstruktor
        public Loading(string schemePath) {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();
            _schemePath = schemePath;
            _db = (ChronosLib)Application.Current.Properties["dbHandler"];
            _login = Application.Current.Properties["login"].ToString();
            _pwd = Application.Current.Properties["pwd"].ToString().DecryptPassword();
            _docId = (int)Application.Current.Properties["docId"];
        }

        //Obsluha načtení okna
        private async void Grid_Loaded(object sender, RoutedEventArgs e) {
            //Asynchronně zpracované schéma
            var schemeId = await JsonHandler.LoadJson(_schemePath);
            if (schemeId == -1) {
                MessageBox.Show("Během zpracovávání došlo k chybě.",
                    "Chyba při zpracování", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
            _db.SetAttribute(_docId, "_scheme", $"@{schemeId}", false, _login, _pwd);
            //Vyvolání hlavního okna
            var mainWindow = new MainWindow();
            this.Hide();
            mainWindow.Show();
            this.Close();
        }
    }
}
