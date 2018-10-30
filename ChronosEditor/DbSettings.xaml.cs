using System;
using System.IO;
using System.Windows;

namespace ChronosEditor {
    /// <summary>
    /// Okno pro nastavení databáze
    /// </summary>
    public partial class DbSettings : Window {
        //Konstruktor
        public DbSettings() {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();
        }

        //Obsluha stisknutí tlačítka pro zpracování nastavení
        private void btnOk_Click(object sender, RoutedEventArgs e) {
            if(tbHost.Text == "" || tbUsername.Text == "" || pbPwd.Password == "" || tbDatabase.Text == "") {
                MessageBox.Show("Všechny položky musejí být vyplněné.", "Nevyplněné položky", MessageBoxButton.OK, MessageBoxImage.Warning);
            } else {
                //Sestavení řetězce pro připojení k databázi
                var connString =
                    $"Host={tbHost.Text};Username={tbUsername.Text};Password={pbPwd.Password};Database={tbDatabase.Text}";
                string provider;
                //Výběr poskytovatele
                switch (tbDatabase.Text) {
                    case "postgres":
                        provider = "Npgsql";
                        break;
                    default:
                        provider = "";
                        break;
                }
                //Vytvoření knihovny a testování připojení - pokud se připojí, uloží se konfigurace
                var db = new ChronosLib(provider, connString);
                try {
                    db.TestConnection();
                    using (TextWriter wr = new StreamWriter("chronos.conf")) {
                        wr.WriteLine("Host={0}\nUsername={1}\nPassword={2}\nDatabase={3}", tbHost.Text, tbUsername.Text, pbPwd.Password.EncryptPassword(), tbDatabase.Text);
                    }
                    this.Close();
                } catch (Exception ex) {
                    MessageBox.Show("Spojení s databází se nepodařilo navázat.\nChyba: "+ex.Message, "Spojení se nezdařilo", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
    }
}
