using System;
using System.Collections.Generic;
using System.Windows;

namespace ChronosEditor {
    /// <summary>
    /// Okno pro výběr atributu k přidání
    /// </summary>
    public partial class AttrList : Window {
        private readonly Dictionary<string, Tuple<int, string, string>> _attrs;

        //Konstruktor
        public AttrList(Dictionary<string, Tuple<int, string, string>> attrs) {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();
            _attrs = attrs;
            Success = false;
        }

        //Vlastnost pro uchovávání informace o úspěchu
        public bool Success {
            get;
            private set;
        }

        //Obsluha tlačítka pro přidání
        private void btnChoose_Click(object sender, RoutedEventArgs e) {
            if (cbName.SelectedIndex == -1) return;
            var selected = _attrs[cbName.Text];
            //Získání cesty a zjištění, zda lze atribut přidat
            var fullPath = selected.Item3 + "/" + selected.Item2;
            if (fullPath.CanAdd()) {
                //Vyvolání okna pro přidání atributu
                var newAttr = new NewAttr(fullPath, selected.Item2, selected.Item1);
                newAttr.ShowDialog();
                this.Success = newAttr.Success;
                this.Close();
            } else {
                MessageBox.Show("Atribut nelze být přidán", "Přidání neúspěšné", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        //Obsluha načtení okna - předání zdroje dat ComboBoxu
        private void Window_Loaded(object sender, RoutedEventArgs e) {
            cbName.ItemsSource = _attrs.Keys;
        }
    }
}
