using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;

namespace ChronosEditor {
    internal static class MiscHandler {
        /// <summary>
        /// Metoda pro získání schématu poddokumentu podle cesty
        /// </summary>
        /// <param name="path">Cesta</param>
        /// <returns>Schéma poddokumentu</returns>
        public static Document GetScheme(this string path) {
            var scheme = (Document) Application.Current.Properties["scheme"];
            var docId = (int) Application.Current.Properties["docId"];
            var login = Application.Current.Properties["login"].ToString();
            var pwd = Application.Current.Properties["pwd"].ToString().DecryptPassword();
            var db = (ChronosLib) Application.Current.Properties["dbHandler"];

            string name;
            try {
                name = db.GetName(docId, login, pwd);
            } catch (Exception ex) {
                MessageBox.Show("Nepodařilo se načíst dokument kvůli následující chybě: " + ex.Message,
                    "Chyba při zpracování", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            var choppedPath = path.Split('/');

            foreach (var chunk in choppedPath) {
                //Přeskočení názvu dokumentu
                if (chunk == name) continue;
                //Pokud je atribut v cestě odkazem na poddokument, uloži se schéma poddokumentu, jinak se vrátí null
                if (scheme.Attributes.ContainsKey(chunk)) {
                    scheme = scheme.Attributes[chunk].Child;
                } else {
                    return null;
                }
            }

            return scheme;
        }

        /// <summary>
        /// Metoda pro velké první písmeno řetězce
        /// </summary>
        /// <param name="input">Vstupní řetězec</param>
        /// <returns>Upravený řetězec</returns>
        public static string FirstCharToUpper(this string input) {
            return input.First().ToString().ToUpper() + input.Substring(1);
        }

        /// <summary>
        /// Metoda pro zjištění, zda lze vložit atribut do dokumentu podle cesty
        /// </summary>
        /// <param name="path">Cesta</param>
        /// <returns>True=lze vložit, false=nelze vložit</returns>
        public static bool CanAdd(this string path) {
            var db = (ChronosLib) Application.Current.Properties["dbHandler"];
            var login = Application.Current.Properties["login"].ToString();
            var pwd = Application.Current.Properties["pwd"].ToString().DecryptPassword();
            var docId = (int) Application.Current.Properties["docId"];
            var scheme = (Document) Application.Current.Properties["scheme"];
            var choppedPath = path.Split('/').ToList();
            var noLinkPath = new List<string>();
            var docIds = new Dictionary<string, int>();
            var firstFlag = true;

            //Vyparsování cesty bez odkazů na poddokumenty, odkazy jsou ukládány
            for (var i = 0; i < choppedPath.Count; i++) {
                if (choppedPath[i].IndexOf("@", StringComparison.Ordinal) == -1) {
                    noLinkPath.Add(choppedPath[i]);
                } else {
                    docIds.Add(choppedPath[i - 1], int.Parse(choppedPath[i].Substring(1)));
                }
            }

            var attr = new List<string>();
            Attribute schemeAttr = null;

            foreach (var chunk in noLinkPath) {
                DataTable docAttrs;
                string name;
                try {
                    docAttrs = db.ScanDocs(docId, DateTime.Now);
                    name = db.GetName(docId, login, pwd);
                } catch (Exception ex) {
                    MessageBox.Show("Nepodařilo se načíst dokument kvůli následující chybě: " + ex.Message,
                        "Chyba při zpracování", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                //Přeskočení názvu dokumentu
                if (firstFlag && chunk == name) {
                    firstFlag = false;
                    continue;
                }

                //Postupné procházení cesty
                schemeAttr = (from a in scheme.Attributes where a.Key.Equals(chunk) select a.Value).Take(1).ToList()[0];
                attr = (from d in docAttrs.AsEnumerable()
                    where d.Field<string>("name").Equals(chunk)
                    select d.Field<string>("value")).ToList();
                if (chunk == noLinkPath.Last() || schemeAttr.Value.IndexOf('@') == -1) {
                    schemeAttr = chunk == noLinkPath.Last() ? schemeAttr : null;
                    continue;
                }

                scheme = schemeAttr.Child;
                docId = docIds[chunk];
            }

            //Pokud se nepodařilo získat schéma, vrací se false
            if (schemeAttr == null) return false;
            //Pokud je počet hodnot atributu 0, rovnou se vrací true
            if (attr.Count == 0) return true;
            //Vyřešení pole
            return attr.Count < 1 || schemeAttr.Container;
        }

        /// <summary>
        /// Metoda pro získání hodnoty z odkazu
        /// </summary>
        /// <param name="link">Odkaz</param>
        /// <param name="path">Cesta</param>
        /// <param name="nesting">Vnoření</param>
        /// <returns>Pole odkazovaných hodnot</returns>
        public static List<string> GetContent(this string link, List<string> path, int nesting) {
            var scheme = (Document) Application.Current.Properties["scheme"];
            var content = new List<string>();
            //Pokud se nejedná o odkaz na atribut, vrací se prázdný seznam
            if (link.IndexOf("->", StringComparison.Ordinal) == -1 &&
                link.IndexOf("=>", StringComparison.Ordinal) == -1) return content;

            //Získání cesty v rámci dokumentu podle odkazu a vnoření
            var addedPath = new List<string>();
            link = link.Substring(2);
            var choppedLink = link.Split('/');
            var lookup = nesting;
            foreach (var l in choppedLink) {
                if (l.Equals("..")) {
                    lookup -= 1;
                } else {
                    addedPath.Add(l);
                }
            }

            var fullPath = new List<string>();
            if (lookup > 0) fullPath = path.GetRange(1, lookup);
            fullPath.AddRange(addedPath);

            //Pokud se odkazuje na neplatné místo, vrací se prázdný seznam
            if (lookup < 0) return content;
            var docId = (int) Application.Current.Properties["docId"];

            //Získání obsahu podle celé cesty a schématu
            content = GetContentRecursive(fullPath, scheme, docId);
            return content;
        }

        /// <summary>
        /// Podpůrná metoda pro získávání hodnoty z odkazu
        /// </summary>
        /// <param name="path">Cesta k odkazovanému atributu</param>
        /// <param name="scheme">Schéma dokumentu</param>
        /// <param name="docId">Identifikátor dokumentu</param>
        /// <returns>Seznam odkazovaných hodnot</returns>
        private static List<string> GetContentRecursive(List<string> path, Document scheme, int docId) {
            var db = (ChronosLib) Application.Current.Properties["dbHandler"];
            DataTable docAttrs;
            DataTable shadowAttrs;
            //Získání atributů
            var content = new List<string>();
            try {
                docAttrs = db.ScanDocs(docId, DateTime.Now);
                shadowAttrs = docAttrs.GetShadowAttrs();
            } catch (Exception ex) {
                MessageBox.Show("Nepodařilo se načíst dokument kvůli následující chybě: " + ex.Message,
                    "Chyba při zpracování", MessageBoxButton.OK, MessageBoxImage.Error);
                return content;
            }

            var chunk = path.First();

            var schemeAttr = (from a in scheme.Attributes where a.Key.Equals(chunk) select a.Value).Take(1).ToList()[0];
            var attr = docAttrs.GetValues(chunk);
            if (attr.Count == 0 && shadowAttrs != null) {
                attr = shadowAttrs.GetValues(chunk);
            }

            //Pokud atribut není v databázi, vrací se prázdný seznam
            if (attr.Count == 0) return content;
            //Procházení atributů
            foreach (var a in attr) {
                //Je-li atribut odkazem na poddokument, řeší se rekurzí
                if (schemeAttr.Link) {
                    if (schemeAttr.Value.IndexOf('@') != -1) {
                        scheme = schemeAttr.Child;
                        docId = int.Parse(a.Substring(1));
                        content.AddRange(GetContentRecursive(path.GetRange(1, path.Count - 1), scheme, docId));
                    } else return content;
                //Pokud se nachází na konci cesty, přidá se hodnota do seznamu
                } else if (chunk == path.Last()) {
                    content.Add(a);
                }
            }

            return content;
        }

        /// <summary>
        /// Metoda pro získání hodnot atributu
        /// </summary>
        /// <param name="attrs">Všechny atributy</param>
        /// <param name="name">Název hledaného atributu</param>
        /// <returns>Seznam hodnot atributu</returns>
        public static List<string> GetValues(this DataTable attrs, string name) {
            return (from a in attrs.AsEnumerable()
                where a.Field<string>("name").Equals(name)
                select a.Field<string>("value")).ToList();
        }

        /// <summary>
        /// Metoda pro získání názvu atributu v případě explicitního uvedení počtu řádků
        /// </summary>
        /// <param name="input">Vstupní řetězec</param>
        /// <returns>Upravený řetězec</returns>
        public static string SplitJoinExcLast(this string input) {
            //Rozdělení podle závorky
            var stringArr = input.Split('(');
            //Pokud po rozdělení existuje jen jedna část řetězce, vrací se celý řetězec
            if (stringArr.Length == 1) return input;
            //Pokud existuje více části, testuje se hodnota v poslední závorce na číslo
            var integerTest = string.Join("", stringArr.Last().Take(stringArr.Last().Length - 1));
            return !int.TryParse(integerTest, out var temp) ? input : string.Join("", stringArr.Take(stringArr.Length - 1));
        }
    }
}
