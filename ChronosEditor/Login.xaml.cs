using System;
using System.Windows;
using System.Windows.Input;

namespace ChronosEditor {
    /// <summary>
    /// Přihlašovací okno
    /// </summary>
    public partial class Login : Window {
        private readonly ChronosLib _db;

        //Konstruktor
        public Login() {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();
            _db = (ChronosLib)Application.Current.Properties["dbHandler"];
        }

        //Obsluha stisknutí tlačítka pro přihlášení
        private void btnLogin_Click(object sender, RoutedEventArgs e) {
            DoLogin();
        }

        //Obsluha stisknutí tlačítka Enter
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e) {
            if (!e.Key.Equals(Key.Return)) return;
            e.Handled = true;
            DoLogin();
        }

        //Metoda pro přihlášení
        private void DoLogin() {
            if (tbLogin.Text == "") {
                MessageBox.Show("Login musí být vyplněný.", "Nevyplněný login", MessageBoxButton.OK, MessageBoxImage.Warning);
            } else if (pbPwd.Password == "") {
                MessageBox.Show("Heslo musí být vyplněné.", "Nevyplněné heslo", MessageBoxButton.OK, MessageBoxImage.Warning);
            } else {
                try {
                    //Otestování údajů a případné uložení a vyvolání okna pro výběr dokumentu
                    _db.Credentials(tbLogin.Text, pbPwd.Password);
                    Application.Current.Properties["login"] = tbLogin.Text;
                    Application.Current.Properties["pwd"] = pbPwd.Password.EncryptPassword();
                    var docList = new DocList();
                    this.Hide();
                    docList.Show();
                    this.Close();
                } catch (Exception ex) {
                    MessageBox.Show("Přihlášení se nezdařilo: "+ex.Message, "Došlo k chybě", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        //Obsluha načtení okna
        private void Window_Loaded(object sender, RoutedEventArgs e) {
            Keyboard.Focus(tbLogin);
        }
    }
}
