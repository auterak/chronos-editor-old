using System;
using System.Data;
using System.Windows;
using System.Xml;

namespace ChronosEditor {
    internal static class XmlHandler {
        /// <summary>
        /// Metoda pro generování XML souboru z dokumentu
        /// </summary>
        /// <param name="scheme">Schéma dokumentu</param>
        /// <param name="docId">Identifikátor dokumentu</param>
        /// <param name="filename">Soubor</param>
        /// <param name="date">Časová značka</param>
        public static void CreateXmlFile(Document scheme, int docId, string filename, DateTime date) {
            var login = Application.Current.Properties["login"].ToString();
            var pwd = Application.Current.Properties["pwd"].ToString().DecryptPassword();
            var db = (ChronosLib)Application.Current.Properties["dbHandler"];

            using (var xmlw = XmlWriter.Create(filename)) {
                var name = db.GetName(docId, login, pwd);
                xmlw.WriteStartDocument();
                xmlw.WriteStartElement(name);
                HandleElements(scheme, docId, xmlw, date);
                xmlw.WriteEndElement();
                xmlw.WriteEndDocument();
            }
        }

        /// <summary>
        /// Podpůrná metoda pro generování XML souboru z dokumentu
        /// </summary>
        /// <param name="scheme">Schéma dokumentu</param>
        /// <param name="docId">Identifikátor dokumentu</param>
        /// <param name="xmlw">Handler pro XmlWriter</param>
        /// <param name="date">Časová značka</param>
        private static void HandleElements(Document scheme, int docId, XmlWriter xmlw, DateTime date) {
            var db = (ChronosLib)Application.Current.Properties["dbHandler"];
            DataTable docAttrs;
            DataTable shadowAttrs;
            //Získání atributů
            try {
                docAttrs = db.ScanDocs(docId, date);
                shadowAttrs = docAttrs.GetShadowAttrs();
            } catch (Exception ex) {
                MessageBox.Show("Nepodařilo se načíst dokument kvůli následující chybě: " + ex.Message,
                    "Chyba při zpracování", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            //Procházení podle schématu
            foreach (var schemeAttr in scheme.Attributes) {
                var attrName = schemeAttr.Value.Name.SplitJoinExcLast();
                //Atribut není polem
                if (!schemeAttr.Value.Container) {
                    var values = docAttrs.GetValues(attrName);
                    if (values.Count == 0 && shadowAttrs != null) {
                        values = shadowAttrs.GetValues(attrName);
                    }
                    foreach (var v in values) {
                        //Jednoduchý dokument se rovnou zapisuje, poddokument se řeší rekurzí
                        if (v.IndexOf('@') != 0) {
                            xmlw.WriteElementString(attrName, v);
                        } else {
                            xmlw.WriteStartElement(attrName);
                            HandleElements(schemeAttr.Value.Child, int.Parse(v.Substring(1)), xmlw, date);
                            xmlw.WriteEndElement();
                        }
                    }
                //Atribut je polem skalárů
                } else if (schemeAttr.Value.Container && !schemeAttr.Value.Link) {
                    var values = docAttrs.GetValues(attrName);
                    if (values.Count == 0 && shadowAttrs != null) {
                        values = shadowAttrs.GetValues(attrName);
                    }
                    foreach (var v in values) {
                        xmlw.WriteElementString(attrName, v);
                    }
                //Atribut je polem poddokumentů, řešeno rekurzí
                } else if (schemeAttr.Value.Container && schemeAttr.Value.Link) {
                    var values = docAttrs.GetValues(attrName);
                    if (values.Count == 0 && shadowAttrs != null) {
                        values = shadowAttrs.GetValues(attrName);
                    }
                    foreach (var v in values) {
                        if (v.IndexOf('@') != 0) continue;
                        xmlw.WriteStartElement(attrName);
                        HandleElements(schemeAttr.Value.Child, int.Parse(v.Substring(1)), xmlw, date);
                        xmlw.WriteEndElement();
                    }
                }
            }
        }
    }
}
