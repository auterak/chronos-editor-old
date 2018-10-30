using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace ChronosEditor {
    /// <summary>
    /// Okno pro výběr dokumentu
    /// </summary>
    public partial class DocList : Window {
        private readonly ChronosLib _db;
        private readonly string _login;
        private readonly string _pwd;

        //Konstruktor
        public DocList() {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();
            _db = (ChronosLib)Application.Current.Properties["dbHandler"];
            _login = Application.Current.Properties["login"].ToString();
            _pwd = Application.Current.Properties["pwd"].ToString().DecryptPassword();
        }

        //Obsluha tlačítka pro vytvoření nového dokumentu
        private void btnNewDoc_Click(object sender, RoutedEventArgs e) {
            var newDoc = new NewDoc();
            this.Hide();
            newDoc.Show();
            this.Close();
        }

        //Obsluha výběru dokumentu ze seznamu
        private void dgDocList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var selectedRow = dgDocList.SelectedItems[0] as DataRowView;
            //Ošetření výběru
            if (selectedRow?[0] == null) return;
            //Uložení identifikátoru dokumentu
            var docId = int.Parse(selectedRow[0].ToString());
            Application.Current.Properties["docId"] = docId;
            //Vyvolání hlavního okna
            var mainWindow = new MainWindow();
            this.Hide();
            mainWindow.Show();
            this.Close();
        }

        //Obsluha tlačítka pro adminovský režim
        private void btnAdminMode_Click(object sender, RoutedEventArgs e) {
            var mainWindow = new MainWindow("admin");
            this.Hide();
            mainWindow.Show();
            this.Close();
        }

        //Obsluha načtení okna - nastavení zdroje dat
        private void Window_Loaded(object sender, RoutedEventArgs e) {           
            try {
                var docList = _db.ListDocs(_login, _pwd);
                dgDocList.DataContext = docList.DefaultView;

                btnAdminMode.Visibility = _db.IsAdmin(_login) ? Visibility.Visible : Visibility.Hidden;
            } catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
