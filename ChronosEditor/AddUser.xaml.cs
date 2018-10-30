using System;
using System.Windows;
using System.Windows.Input;

namespace ChronosEditor {
    /// <summary>
    /// Okno pro přidávání uživatelů
    /// </summary>
    public partial class AddUser : Window {
        private readonly ChronosLib _db;
        private readonly string _login;
        private readonly string _pwd;

        //Konstruktor
        public AddUser() {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();
            _db = (ChronosLib)Application.Current.Properties["dbHandler"];
            _login = Application.Current.Properties["login"].ToString();
            _pwd = Application.Current.Properties["pwd"].ToString().DecryptPassword();
        }

        //Obsluha kliknutí na tlačítko pro přidání uživatele
        private void btnNew_Click(object sender, RoutedEventArgs e) {
            DoAddUser();
        }

        //Metoda pro přidání uživatele
        private void DoAddUser() {
            var name = tbName.Text.Trim();
            var pass = pbPwd.Password.Trim();
            if (name != "" && _pwd != "") {
                try {
                    _db.CreateUser(name, pass, chbAdmin.IsChecked != null && chbAdmin.IsChecked.Value, _login, _pwd);
                    tbName.Clear();
                    pbPwd.Clear();
                    chbAdmin.IsChecked = false;
                    MessageBox.Show("Byl vytvořen uživatel " + name, "Uživatel přidán");
                } catch (Exception ex) {
                    MessageBox.Show("Nepodařilo se vytvořit uživatele: " + ex.Message, "Uživatel nevytvořen", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            } else {
                MessageBox.Show("Nejsou vyplněné všechny údaje", "Nevyplněné údaje", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        //Obsluha stisknutí tlačítka Enter
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e) {
            if (!e.Key.Equals(Key.Return)) return;
            e.Handled = true;
            DoAddUser();
        }

        //Obsluha načtení okna
        private void Window_Loaded(object sender, RoutedEventArgs e) {
            Keyboard.Focus(tbName);
        }
    }
}
