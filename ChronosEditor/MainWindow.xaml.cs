using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ChronosEditor {
    /// <summary>
    /// Hlavní okno aplikace
    /// </summary>
    public partial class MainWindow : Window {
        private readonly int _docId;
        private readonly Document _scheme;
        private readonly ChronosLib _db;
        private readonly string _login;
        private readonly string _pwd;
        private readonly string _mode;
        private double _oldWidth;

        //Konstruktor
        public MainWindow(string mode = "normal") {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();
            _mode = mode;
            _oldWidth = 1205;
            if (mode != "normal") return;
            _db = (ChronosLib)Application.Current.Properties["dbHandler"];
            _docId = (int)Application.Current.Properties["docId"];
            _login = Application.Current.Properties["login"].ToString();
            _pwd = Application.Current.Properties["pwd"].ToString().DecryptPassword();
            Application.Current.Properties["links"] = new Dictionary<string, string>();
            try {
                var schemeId = _db.GetSchemeId(_docId, _login, _pwd);
                if (schemeId == -1) return;
                _scheme = schemeId.LoadScheme();
                Application.Current.Properties["scheme"] = _scheme;
            } catch (Exception ex) {
                MessageBox.Show("Nepodařilo se načíst schéma: " + ex.Message, "Schéma nenačteno", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        //Obsluha načtení okna
        private void Window_Loaded(object sender, RoutedEventArgs e) {
            //Podle módu se zpřístupní určité prvky
            switch (_mode) {
                case "normal":
                    if (_scheme != null) {
                        subGrid.GenerateElements(_db.GetName(_docId, _login, _pwd));
                    }
                    miAddUser.IsEnabled = _db.IsAdmin(_login);
                    miAddLease.IsEnabled = _db.IsCreator(_docId, _login);
                    miNewDoc.IsEnabled = !_db.HasShadow(_docId, _login, _pwd);
                    break;
                case "admin":
                    miGenerateJson.IsEnabled = false;
                    miGenerateXml.IsEnabled = false;
                    miAddLease.IsEnabled = false;
                    miSwitchDoc.IsEnabled = false;
                    miNewDoc.IsEnabled = false;
                    break;
            }
            cHistory.Visibility = Visibility.Hidden;
        }

        //Obsluha tlačítka pro generování XML souboru
        private void miGenerateXml_Click(object sender, RoutedEventArgs e) {
            bool correct;

            //Získání časové značky pro generování
            var date = chbMode.IsChecked != null && chbMode.IsChecked.Value ? GetDate() : DateTime.Now;
            if (date == DateTime.MinValue) date = DateTime.Now;

            //Otestování správnosti dokumentu
            try {
                correct = _docId.IsCorrect(_scheme, date);
            } catch (Exception ex) {
                MessageBox.Show("Při zpracování došlo k chybě: " + ex.Message, "Chyba", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            //Zavolání dialogového okna pro vytvoření souboru
            if (correct) {
                GuiHandler.OpenSaveDialog("xml", date);
            } else {
                var result = MessageBox.Show("Vybraný dokument neodpovídá schématu! Chcete i přesto pokračovat?",
                    "Neodpovídá schématu", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes) {
                    GuiHandler.OpenSaveDialog("xml", date);
                }
            }
        }

        //Obsluha tlačítka pro generování JSON souboru (viz XML výše)
        private void miGenerateJson_Click(object sender, RoutedEventArgs e) {
            bool correct;

            var date = chbMode.IsChecked != null && chbMode.IsChecked.Value ? GetDate() : DateTime.Now;
            if (date == DateTime.MinValue) date = DateTime.Now;

            try {
                correct = _docId.IsCorrect(_scheme, date);
            } catch (Exception ex) {
                MessageBox.Show("Při zpracování došlo k chybě: " + ex.Message, "Chyba", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            if (correct) {
                GuiHandler.OpenSaveDialog("json", date);
            } else {
                var result = MessageBox.Show("Vybraný dokument neodpovídá schématu! Chcete i přesto pokračovat?", "Neodpovídá schématu", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes) {
                    GuiHandler.OpenSaveDialog("json", date);
                }
            }
        }

        //Obsluha tlačítka pro přidání uživatele
        private void miAddUser_Click(object sender, RoutedEventArgs e) {
            var addUser = new AddUser();
            addUser.ShowDialog();
        }

        //Obsluha tlačítka pro propůjčení dokumentu
        private void miAddLease_Click(object sender, RoutedEventArgs e) {
            var addLease = new AddLease(_docId);
            addLease.ShowDialog();
        }

        //Obsluha tlačítka pro odhlášení
        private void miLogout_Click(object sender, RoutedEventArgs e) {
            Application.Current.Properties["docId"] = null;
            Application.Current.Properties["login"] = null;
            Application.Current.Properties["pwd"] = null;
            Application.Current.Properties["links"] = null;
            Application.Current.Properties["scheme"] = null;
            var loginW = new Login();
            this.Hide();
            loginW.Show();
            this.Close();
        }

        //Obsluha tlačítka pro přepnutí dokumentu
        private void miSwitchDoc_Click(object sender, RoutedEventArgs e) {
            Application.Current.Properties["docId"] = null;
            Application.Current.Properties["links"] = null;
            Application.Current.Properties["scheme"] = null;
            var docList = new DocList();
            this.Hide();
            docList.Show();
            this.Close();
        }

        //Obsluha aktivování okna (pro obnovení stromu)
        private void Main_Activated(object sender, EventArgs e) {
            if (chbMode.IsChecked != null && chbMode.IsChecked.Value || _mode == "admin") return;
            tvDocument.Items.Clear();
            tvDocument.Items.Add(_docId.GenerateTree(_scheme, DateTime.Now));
        }

        //Obsluha stisknutí pravého tlačítka myši
        private void tvDocument_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            //V režimu procházení historie se nic nestane
            if (chbMode.IsChecked != null && chbMode.IsChecked.Value) return;
            //Nalezení správného prvku stromu
            var treeViewItem = GuiHandler.VisualUpwardSearch(e.OriginalSource as DependencyObject);
            //Pokud není nalezen, nic se nestane
            if (treeViewItem == null) return;
            //Jinak získá prvek "focus" a je vyvoláno kontextové menu
            treeViewItem.Focus();
            e.Handled = true;

            if (!(tvDocument.FindResource("TreeViewContext") is ContextMenu contextMenu)) return;
            contextMenu.PlacementTarget = treeViewItem;
            contextMenu.IsOpen = true;
        }

        //Obsluha tlačítka pro přidání atributu z kontextového menu
        private void cmAdd_Click(object sender, RoutedEventArgs e) {
            var selectedItem = (TreeViewItem)tvDocument.SelectedItem;
            var path = selectedItem.Tag.ToString();
            var choppedPath = path.Split('/').ToList();

            var success = selectedItem.AddToDoc(_docId, _scheme, choppedPath);
            if (!success) return;
            tvDocument.Items.Clear();
            tvDocument.Items.Add(_docId.GenerateTree(_scheme, DateTime.Now));
        }

        //Obsluha tlačítka pro úpravu atributu z kontextového menu
        private void cmEdit_Click(object sender, RoutedEventArgs e) {
            var selectedItem = (TreeViewItem)tvDocument.SelectedItem;
            var path = selectedItem.Tag.ToString();
            var choppedPath = path.Split('/').ToList();
            if (choppedPath.First() == "shadow") {
                MessageBox.Show("Nelze upravovat atributy stínového dokumentu", "Atribut nelze upravit", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (choppedPath.Last().IndexOf("@", StringComparison.Ordinal) != 0) {
                MessageBox.Show("Upravovat lze pouze poddokumenty", "Atribut nelze upravit", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var docId = int.Parse(choppedPath.Last().Substring(1));
            var pathForScheme = new List<string> { _db.GetName(_docId, _login, _pwd) };
            foreach (var chunk in choppedPath) {
                if (chunk.First() != '@') pathForScheme.Add(chunk.SplitJoinExcLast());
            }

            var subAttribute = new SubAttribute(string.Join("/", pathForScheme), docId) {
                Title = pathForScheme.Last() + "-Úprava",
                Tag = string.Join("/", pathForScheme)
            };
            subAttribute.ShowDialog();
        }

        //Obsluha tlačítka pro odstranění atributu z kontextového menu
        private void cmRemove_Click(object sender, RoutedEventArgs e) {
            var selectedItem = (TreeViewItem)tvDocument.SelectedItem;
            var path = selectedItem.Tag.ToString();
            var choppedPath = path.Split('/').ToList();

            selectedItem.DeleteFromTree(_docId, _scheme, choppedPath);
            tvDocument.Items.Clear();
            tvDocument.Items.Add(_docId.GenerateTree(_scheme, DateTime.Now));
        }

        //Obsluha tlačítka pro propůjčení poddokumentu z kontextového menu
        private void cmLease_Click(object sender, RoutedEventArgs e) {
            var selectedItem = (TreeViewItem)tvDocument.SelectedItem;
            var path = selectedItem.Tag.ToString();
            var choppedPath = path.Split('/').ToList();

            if (choppedPath.First() == "shadow") {
                MessageBox.Show("Atribut stínového dokumentu nelze propůjčit", "Atribut nelze propůjčit", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var tempId = choppedPath.Last();
            int docId;

            if (tempId == _db.GetName(_docId, _login, _pwd)) {
                docId = _docId;
            } else if (tempId.IndexOf("@", StringComparison.Ordinal) == 0) {
                docId = int.Parse(tempId.Substring(1));
            } else {
                MessageBox.Show("Tento atribut nelze propůjčit.", "Nelze propůjčit", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var addLease = new AddLease(docId);
            addLease.ShowDialog();
        }

        //Obsluha změny zaškrtnutí režimu historie - zneplatnění/zplatnění tlačítek, zobrazení kalendáře
        private void ModeChanged(object sender, RoutedEventArgs e) {
            cHistory.Visibility = chbMode.IsChecked != null && chbMode.IsChecked.Value ? Visibility.Visible : Visibility.Hidden;
            if (chbMode.IsChecked != null && chbMode.IsChecked.Value) {
                subGrid.DisableButtons();
            } else {
                subGrid.EnableButtons();
                tvDocument.Items.Clear();
                tvDocument.Items.Add(_docId.GenerateTree(_scheme, DateTime.Now));
            }
        }

        //Obsluha výběru data v kalendáři
        private void cHistory_SelectedDatesChanged(object sender, SelectionChangedEventArgs e) {
            var date = GetDate();
            if (date == DateTime.MinValue) return;
            tvDocument.Items.Clear();
            tvDocument.Items.Add(_docId.GenerateTree(_scheme, date));
        }

        //Obsluha tlačítka pro odvození nového dokumentu
        private void miNewDoc_Click(object sender, RoutedEventArgs e) {
            var date = GetDate();
            if (date == DateTime.MinValue) date = DateTime.Now;
            try {
                var newId = _db.InsertDoc(_login, _pwd);
                var newSchemeId = _db.GetSchemeId(_docId, _login, _pwd);
                var newName = _db.GetName(_docId, _login, _pwd);
                _db.SetAttribute(newId, "_id", $"{newName}_{DateTime.Now:yyyyMMddHHmmss}", false, _login, _pwd);
                _db.SetAttribute(newId, "_scheme", "@" + newSchemeId, true, _login, _pwd);
                _db.SetAttribute(newId, "_shadow", "@" + _docId + "_" + date, true, _login, _pwd);
                MessageBox.Show("Nový dokument úspěšně odvozen.", "Dokument vytvořen", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            } catch (Exception ex) {
                MessageBox.Show("Vytvoření dokumentu se nezdařilo: " + ex.Message, "Chyba", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        //Metoda pro získání správné časové značky
        private DateTime GetDate() {
            return cHistory.SelectedDate?.AddHours(23).AddMinutes(59).AddSeconds(59) ?? DateTime.MinValue;
        }

        //Obslha změny velikosti okna - změna velikosti plochy stromu dokumentu
        private void Main_SizeChanged(object sender, SizeChangedEventArgs e) {
            var dif = _oldWidth - ActualWidth;
            _oldWidth = ActualWidth;
            tvDocument.Width -= dif;
        }
    }
}
