using System;
using System.Data;
using System.Linq;
using System.Windows;

namespace ChronosEditor {
    /// <summary>
    /// Okno pro propůjčování dokumentů
    /// </summary>
    public partial class AddLease : Window {
        private readonly ChronosLib _db;
        private readonly string _login;
        private readonly string _pwd;
        private readonly int _docId;

        //Konstruktor
        public AddLease(int docId) {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();
            _db = (ChronosLib)Application.Current.Properties["dbHandler"];
            _login = Application.Current.Properties["login"].ToString();
            _pwd = Application.Current.Properties["pwd"].ToString().DecryptPassword();
            _docId = docId;
        }

        //Obsluha kliknutí tlačítka pro propůjčení dokumentu
        private void btnLease_Click(object sender, RoutedEventArgs e) {
            var name = cbName.Text.Trim();
            if (name != "") {
                try {
                    _db.CreateLease(_docId, _login, _pwd, name);
                    cbName.SelectedIndex = -1;
                    MessageBox.Show("Dokument byl propůjčen uživateli " + name, "Dokument propůjčen");
                } catch (Exception ex) {
                    MessageBox.Show("Nepodařilo se propůjčit dokument: " + ex.Message, "Nastala chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            } else {
                MessageBox.Show("Nejsou vyplněné všechny údaje", "Nevyplněné údaje", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        //Obsluha načtení okna
        private void Window_Loaded(object sender, RoutedEventArgs e) {
            try {
                //Získání uživatelů 
                var users = _db.ListUsers(_login, _pwd);
                var usersList = (from d in users.AsEnumerable() select d.Field<string>("name")).ToList();
                cbName.ItemsSource = usersList;
            } catch (Exception ex) {
                MessageBox.Show("Nebylo možné načíst uživatele: "+ex.Message, "Nastala chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
