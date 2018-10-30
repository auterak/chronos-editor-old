using Microsoft.Win32;
using System;
using System.Windows;

namespace ChronosEditor {
    /// <summary>
    /// Okno pro vytvoření nového dokumentu
    /// </summary>
    public partial class NewDoc : Window {
        private readonly ChronosLib _db;
        private readonly string _login;
        private readonly string _pwd;
        private bool _closing;

        //Konstruktor
        public NewDoc() {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();
            _db = (ChronosLib)Application.Current.Properties["dbHandler"];
            _login = Application.Current.Properties["login"].ToString();
            _pwd = Application.Current.Properties["pwd"].ToString().DecryptPassword();
            _closing = true;
        }

        //Obsluha tlačítka pro nalezení schématu
        private void btnBrowse_Click(object sender, RoutedEventArgs e) {
            var openFileDialog = new OpenFileDialog {Filter = "JSON files (*.json)|*.json"};
            if (openFileDialog.ShowDialog() == true) {
                tbScheme.Text = openFileDialog.FileName;
            }
        }

        //Obsluha tlačítka pro vytvoření nového dokumentu
        private void btnNew_Click(object sender, RoutedEventArgs e) {
            if (tbName.Text == "") {
                MessageBox.Show("Název musí být vyplněný.", "Nevyplněný název", MessageBoxButton.OK, MessageBoxImage.Warning);
            } else if (tbScheme.Text == "") {
                MessageBox.Show("Musí být vybrané schéma.", "Nevybrané schéma", MessageBoxButton.OK, MessageBoxImage.Warning);
            } else {
                try {
                    //Vložení do databáze, uložení identifikátoru
                    var docId = _db.InsertDoc(_login, _pwd);
                    Application.Current.Properties["docId"] = docId;
                    _db.SetAttribute(docId, "_id", tbName.Text, false, _login, _pwd);
                    //Vyvolání načítacího okna pro schéma
                    var loading = new Loading(tbScheme.Text);
                    this.Hide();
                    loading.Show();
                    _closing = false;
                    this.Close();
                } catch (Exception ex) {
                    MessageBox.Show("Při zpracování došlo k chybě: "+ex.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        //Obsluha zavření okna
        private void Window_Closed(object sender, EventArgs e) {
            if (!_closing) return;
            var docList = new DocList();
            this.Hide();
            docList.Show();
            this.Close();
        }
    }
}
