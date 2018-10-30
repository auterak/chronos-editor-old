using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ChronosEditor {
    /// <summary>
    /// Úvodní okno aplikace
    /// </summary>
    public partial class Splash : Window {
        //Konstruktor
        public Splash() {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();
            var logo = GuiHandler.GetLogoFile();
            if (logo != null) {
                ibLogo.ImageSource = new BitmapImage(new Uri(logo));
            }
        }

        //Obsluha načtení okna - pro responzivitu je asynchronní
        private async void wSplash_Loaded(object sender, RoutedEventArgs e) {
            lbInfo.Content = "Kontroluji nastavení databáze";
            await ShowSettings();
            lbInfo.Content = "Načítám nastavení databáze";
            var connString = await LoadSettings();
            lbInfo.Content = "Testuji spojení s databází";
            var database = connString.Substring(connString.IndexOf("Database=", StringComparison.Ordinal) + 9);
            database = database.Substring(0, database.Length - 1);
            //Nastavení poskytovatele
            var provider = "";
            switch (database) {
                case "postgres":
                    provider = "Npgsql";
                    break;
            }
            //Vytvoření handleru pro klientskou knihovnu a testování spojení
            var db = new ChronosLib(provider, connString);
            var testedDb = await TestConnection(db);
            if (testedDb) {
                //Při úspěchu dojde k vyvolání přihlašovacího okna
                lbInfo.Content = "Spojení s databází úspěšně navázáno";
                Application.Current.Properties["dbHandler"] = db;
                var login = new Login();
                this.Hide();
                login.Show();
                this.Close();
            } else {
                //Při neúspěchu dojde k zavření aplikace
                lbInfo.Content = "Spojení s databází nebylo navázáno, aplikace bude ukončena.";
                Thread.Sleep(1000);
                this.Close();
            }
        }

        //Metoda pro kontrolu konfigurace a zobrazení okna s nastavením databáze
        private static async Task ShowSettings() {
            var task = Task.Run(() => {
                if (!File.Exists("chronos.conf")) {
                    //Kvůli GUI nutné volat okno přes dispečera
                    Application.Current.Dispatcher.Invoke(delegate {
                        var dbSettings = new DbSettings();
                        dbSettings.ShowDialog();
                    });
                }
                Thread.Sleep(200); //Strategická pauza
            });
            await task;
        }

        //Metoda pro načtení konfigurace
        private static async Task<string> LoadSettings() {
            var task = Task.Run(() => {
                var connString = "";
                using (var sr = new StreamReader("chronos.conf")) {
                    string confLine;
                    //Sestavení řetězce pro připojení k databázi
                    while ((confLine = sr.ReadLine()) != null) {
                        if (confLine.IndexOf("Password", StringComparison.Ordinal) != -1) {
                            var splitLine = confLine.Split(new[] { '=' }, 2);
                            var dPwd = splitLine[1].DecryptPassword();
                            confLine = $"{splitLine[0]}={dPwd}";
                        }
                        connString += confLine + ";";
                    }
                    Thread.Sleep(200); //Strategická pauza
                    return connString;
                }
            });
            return await task;
        }

        //Metoda pro testování spojení
        private static async Task<bool> TestConnection(ChronosLib db) {
            var task = Task.Run(() => {
                try {
                    db.TestConnection();
                    Thread.Sleep(200); //Strategická pauza
                    return true;
                } catch {
                    Thread.Sleep(200); //Strategická pauza
                    return false;
                }
            });
            return await task;
        }
    }
}
