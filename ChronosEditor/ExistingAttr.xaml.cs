using System;
using System.Data;
using System.Linq;
using System.Windows;

namespace ChronosEditor {
    /// <summary>
    /// Okno pro přidání existujícího dokumentu
    /// </summary>
    public partial class ExistingAttr : Window
    {
        private readonly ChronosLib _db;
        private readonly string _login;
        private readonly string _pwd;
        private readonly int _docId;
        private readonly int _rootId;
        private readonly string _path;
        private DataTable _attrs;
        private Document _scheme;

        //Konstruktor
        public ExistingAttr(string path, int docId = -1) {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();
            _db = (ChronosLib)Application.Current.Properties["dbHandler"];
            _login = Application.Current.Properties["login"].ToString();
            _pwd = Application.Current.Properties["pwd"].ToString().DecryptPassword();
            _docId = docId == -1 ? (int)Application.Current.Properties["docId"] : docId;
            _rootId = (int)Application.Current.Properties["docId"];
            _scheme = (Document)Application.Current.Properties["scheme"];
            _path = path;
            Success = false;
        }

        //Vlastnost pro udržení vlastnosti o úspěchu
        public bool Success {
            get;
            private set;
        }

        //Obsluha načtení okna - získání zdroje dat
        private void Window_Loaded(object sender, RoutedEventArgs e) {
            try {
                _attrs = _db.ListAllDocs(_login, _pwd);
                var attrList = (from d in _attrs.AsEnumerable() where d.Field<int>("id") != _rootId select d.Field<string>("name")).ToList();
                if (attrList.Count == 0) {
                    MessageBox.Show("Žádný dokument pro přidání nebyl nalezen.", "Nenalezen dokument", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Success = false;
                    this.Close();
                }
                cbName.ItemsSource = attrList;
            } catch (Exception ex) {
                MessageBox.Show("Nebylo možné načíst dokumenty: " + ex.Message, "Nastala chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //Obsluha tlačítka pro přidání
        private void btnAdd_Click(object sender, RoutedEventArgs e) {
            try {
                var choppedPath = _path.Split('/').ToList();
                var id = (from d in _attrs.AsEnumerable() where d.Field<string>("name").Equals(cbName.Text) select d.Field<int>("id")).Take(1).ToList()[0];
                //Získání správného schématu, přeskakuje se kořenový dokument a neodkazové atributy v cestě
                for (var i = 0; i < choppedPath.Count - 1; i++) {
                    if (choppedPath[i].IndexOf("@", StringComparison.Ordinal) != -1) continue;
                    if (choppedPath[i] == _db.GetName(_rootId, _login, _pwd)) continue;
                    _scheme = _scheme.Attributes[choppedPath[i]].Child;
                }
                //Otestování a přidání atributu
                var schemeAttr = (from a in _scheme.Attributes where a.Key.SplitJoinExcLast().Equals(choppedPath.Last()) select a.Value).Take(1).ToList()[0];
                if (id.IsCorrect(schemeAttr.Child, DateTime.Now)) {
                    AddAttribute(schemeAttr, choppedPath.Last(), id);
                } else {
                    var result = MessageBox.Show("Vybraný dokument neodpovídá schématu! Chcete i přesto přidat?", "Neodpovídá schématu", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes) {
                        AddAttribute(schemeAttr, choppedPath.Last(), id);
                    }
                }
            } catch (Exception ex) {
                MessageBox.Show("Přidání atributu se nezdařilo: "+ex.Message, "Přidání neúspěšné", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //Metoda pro přidání atributu do dokumentu
        private void AddAttribute(Attribute schemeAttr, string name, int id) {
            if (schemeAttr.Container) {
                _db.InsertAttribute(_docId, name, "@" + id, true, _login, _pwd);
            } else {
                _db.SetAttribute(_docId, name, "@" + id, true, _login, _pwd);
            }
            MessageBox.Show("Přidání atributu proběhlo úspěšně", "Přidání se zdařilo");
            Success = true;
            this.Close();
        }
    }
}
