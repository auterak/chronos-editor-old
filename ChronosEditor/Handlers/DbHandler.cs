using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;

namespace ChronosEditor {
    internal static class DbHandler {
        /// <summary>
        /// Metoda pro načtení schématu z databáze
        /// </summary>
        /// <param name="schemeId">Identifikátor schématu</param>
        /// <returns>Schéma</returns>
        public static Document LoadScheme(this int schemeId) {
            var scheme = new Document();
            var db = (ChronosLib)Application.Current.Properties["dbHandler"];
            var dbScheme = db.ScanDocs(schemeId, DateTime.Now);
            //Procházení atributů (řádků) z databáze
            foreach (var a in dbScheme.AsEnumerable()) {
                scheme = scheme.Populate(a);
            }
            return scheme;
        }

        /// <summary>
        /// Metoda pro testování správnosti dokumentu proti schématu
        /// </summary>
        /// <param name="docId">Identifikátor dokumentu</param>
        /// <param name="scheme">Schéma</param>
        /// <param name="date">Čas pro správnou verzi</param>
        /// <returns>True=dokument podle schématu, false=dokument není podle schématu</returns>
        public static bool IsCorrect(this int docId, Document scheme, DateTime date) {
            var db = (ChronosLib)Application.Current.Properties["dbHandler"];
            var children = new List<Tuple<int, Document>>();
            var attrs = db.ScanDocs(docId, date);
            var shadowAttrs = attrs.GetShadowAttrs();

            //Otestování, zda v dokumentu není atribut, který není ve schématu (id, schéma a stínový dokument se přeskakuje)
            foreach (var r in attrs.AsEnumerable()) {
                var name = r.Field<string>("name");
                if (name == "_id" || name == "_scheme" || name == "_shadow") continue;
                if (!scheme.Attributes.ContainsKey(name)) {
                    return false;
                }
            }

            //Procházení atributů podle schématu
            foreach (var sa in scheme.Attributes) {
                //Získání atributů z dokumentu, případně ze stínového dokumentu
                var a = (from d in attrs.AsEnumerable() where d.Field<string>("name").Equals(sa.Value.Name.SplitJoinExcLast()) select d.Field<string>("value")).ToList();
                if (a.Count == 0 && shadowAttrs != null) a = (from d in shadowAttrs.AsEnumerable() where d.Field<string>("name").Equals(sa.Value.Name.SplitJoinExcLast()) select d.Field<string>("value")).ToList();
                var count = a.Count;
                //Pokud atribut není pole a počet přesáhl 1, vrací se rovnou false
                if (!sa.Value.Container && count > 1) return false;
                //Zjištění povinnosti atributu
                bool mandatory;
                try {
                    mandatory = bool.Parse(sa.Value.Value);
                } catch {
                    mandatory = true;
                }
                //Atribut je povinný, ale nevyskytuje se v dokumentu = false
                if (mandatory && count == 0) return false;
                if (sa.Value.Child == null) continue;
                //Uložení poddokumentů pro další zpracování
                foreach (var aa in a) {
                    children.Add(new Tuple<int, Document>(int.Parse(aa.Substring(1)), sa.Value.Child));
                }
            }
            //Zpracování poddokumentů
            foreach (var ch in children) {
                var a = ch.Item1;
                var s = ch.Item2;
                var rValue = a.IsCorrect(s, date);
                if (!rValue) return false;
            }
            return true;
        }

        /// <summary>
        /// Metoda pro získání atributů stínového dokumentu
        /// </summary>
        /// <param name="docAttrs">Atributy dokumentu</param>
        /// <returns>Atributy stínového dokumentu</returns>
        public static DataTable GetShadowAttrs(this DataTable docAttrs) {
            var db = (ChronosLib)Application.Current.Properties["dbHandler"];
            //Získání odkazu na stínový dokument
            var shadowTemp = docAttrs.GetValues("_shadow");
            //Pokud neexistuje, vrací se null
            if (shadowTemp.Count <= 0) return null;
            //Parsování identifikátoru a časové značky a následné získání atributů ze stínového dokumentu
            var shadowDoc = shadowTemp.First();
            var shadowId = int.Parse(shadowDoc.Substring(1, shadowDoc.IndexOf("_", StringComparison.Ordinal) - 1));
            var shadowTime = DateTime.Parse(shadowDoc.Split('_').Last());
            return db.ScanDocs(shadowId, shadowTime);
        }

        /// <summary>
        /// Pomocná metoda pro načítání schématu z databáze
        /// </summary>
        /// <param name="doc">Schéma</param>
        /// <param name="a">Řádek z databáze</param>
        /// <returns></returns>
        private static Document Populate(this Document doc, DataRow a) {
            var links = (Dictionary<string, string>)Application.Current.Properties["links"];
            Document tempDoc = null;
            //Získání hodnot z řádku
            var name = a.Field<string>("name");
            var value = a.Field<string>("value");
            var container = a.Field<bool>("container");
            var link = a.Field<bool>("link");
            var db = (ChronosLib)Application.Current.Properties["dbHandler"];

            //Vyřešení odkazu na atribut
            if(link && value.IndexOf("@", StringComparison.Ordinal) != 0) {
                links.Add(name, value);
                Application.Current.Properties["links"] = links;
            }

            //Vyřešení odkazu na poddokument - rekurzivní volání na poddokument
            if (link && value.IndexOf("@", StringComparison.Ordinal) != -1) {
                var linkId = int.Parse(value.Substring(1));
                var dbScheme = db.ScanDocs(linkId, DateTime.Now);
                tempDoc = new Document();
                foreach (var aa in dbScheme.AsEnumerable()) {
                    tempDoc = tempDoc.Populate(aa);
                }
            }

            //Vložení atributu do schématu
            doc.AddAttribute(new Attribute(
                name,
                value,
                tempDoc,
                container,
                link
                ));

            return doc;
        }
    }
}
