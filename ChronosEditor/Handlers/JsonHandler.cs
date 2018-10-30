using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace ChronosEditor {
    internal static class JsonHandler {
        /// <summary>
        /// Metoda pro načtení schématu z JSON souboru
        /// </summary>
        /// <param name="file">Cesta k souboru</param>
        /// <returns>Identifikátor schématu</returns>
        public static async Task<int> LoadJson(string file) {
            //Řešeno asynchronně pro responzivní GUI
            var task = Task.Run(() => {
                var db = (ChronosLib) Application.Current.Properties["dbHandler"];
                var login = Application.Current.Properties["login"].ToString();
                var pwd = Application.Current.Properties["pwd"].ToString().DecryptPassword();
                dynamic parsedJson = null;

                //Parsování dokumentu
                try {
                    parsedJson = JObject.Parse(File.ReadAllText(file));
                } catch {
                    return -1;
                }

                var docId = db.InsertDoc(login, pwd);

                //Procházení vyparsovaných dokumentů
                foreach (var attr in parsedJson) {
                    if (!PopulateFromJson(attr, docId, login, pwd)) {
                        return -1;
                    }
                }

                return docId;
            });
            return await task;
        }

        /// <summary>
        /// Podpůrná metoda pro načítání schématu z JSON souboru
        /// </summary>
        /// <param name="attr">Atribut</param>
        /// <param name="docId">Identifikátor schématu</param>
        /// <param name="login">Přihlašovací jméno</param>
        /// <param name="pwd">Přihlašovací heslo</param>
        private static bool PopulateFromJson(dynamic attr, int docId, string login, string pwd) {
            var db = (ChronosLib)Application.Current.Properties["dbHandler"];

            try {
                var name = ((string)attr.Name).Trim().Replace(' ', '_');
                string value = null;
                //Atribut má hodnotu Boolean = jednoduchý atribut
                if (attr.Value.Type == JTokenType.Boolean) {
                    value = attr.Value.ToString();
                    db.SetAttribute(docId, name, value, false, login, pwd);
                //Atribut je polem
                } else if (attr.Value.Type == JTokenType.Array) {
                    //Pokud má potomky, řeší se poddokument rekurzivně
                    if (attr.Value.Count != 0) {
                        var tempDocId = db.InsertDoc(login, pwd);
                        value = "@" + tempDocId;
                        db.InsertAttribute(docId, name, value, true, login, pwd);
                        foreach (var temp in attr) {
                            foreach (var t in temp) {
                                foreach (var tt in t) {
                                    if (!PopulateFromJson(tt, tempDocId, login, pwd)) { 
                                        return false;
                                    }
                                }
                            }
                        }
                        //Pokud nemá potomky, jedná se o pole skalárů a je vložen do databáze
                    } else {
                        value = false.ToString();
                        db.InsertAttribute(docId, name, value, false, login, pwd);
                    }
                    //Atribut je odkazem na atribut
                } else if (attr.Value.Type == JTokenType.String) {
                    value = attr.Value;
                    db.SetAttribute(docId, name, value, true, login, pwd);
                    //Atribut je poddokumentem - řešeno rekurzí
                } else if (attr.Type == JTokenType.Property) {
                    var tempDocId = db.InsertDoc(login, pwd);
                    value = "@" + tempDocId;
                    db.SetAttribute(docId, name, value, true, login, pwd);
                    foreach (var temp in attr) {
                        foreach (var t in temp) {
                            if (!PopulateFromJson(t, tempDocId, login, pwd)) { 
                                return false;
                            }
                        }
                    }
                }

                return true;
            } catch {
                return false;
            }
        }

        /// <summary>
        /// Metoda pro generování JSON souboru z dokumentu
        /// </summary>
        /// <param name="scheme">Schéma dokumentu</param>
        /// <param name="docId">Identifikátor dokumentu</param>
        /// <param name="filename">Soubor</param>
        /// <param name="date">Časová značka</param>
        public static void CreateJsonFile(Document scheme, int docId, string filename, DateTime date) {
            TextWriter tw = new StreamWriter(filename);
            using (var jsonw = new JsonTextWriter(tw)) {
                jsonw.Formatting = Formatting.Indented;
                jsonw.WriteStartObject();
                HandleProperties(scheme, docId, jsonw, date);
                jsonw.WriteEndObject();
            }
        }

        /// <summary>
        /// Podpůrná metoda pro generování JSON souboru z dokumentu
        /// </summary>
        /// <param name="scheme">Schéma dokumentu</param>
        /// <param name="docId">Identifikátor dokumentu</param>
        /// <param name="jsonw">Handler pro JsonTextWriter</param>
        /// <param name="date">Časová značka</param>
        private static void HandleProperties(Document scheme, int docId, JsonTextWriter jsonw, DateTime date) {
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
                            jsonw.WritePropertyName(attrName);
                            jsonw.WriteValue(v);
                        } else {
                            jsonw.WritePropertyName(attrName);
                            jsonw.WriteStartObject();
                            HandleProperties(schemeAttr.Value.Child, int.Parse(v.Substring(1)), jsonw, date);
                            jsonw.WriteEndObject();
                        }
                    }
                //Atribut je polem skalárů
                } else if (schemeAttr.Value.Container && !schemeAttr.Value.Link) {
                    var values = docAttrs.GetValues(attrName);
                    if (values.Count == 0 && shadowAttrs != null) {
                        values = shadowAttrs.GetValues(attrName);
                    }
                    if (values.Count == 0) continue;
                    jsonw.WritePropertyName(attrName);
                    jsonw.WriteStartArray();
                    foreach (var v in values) {
                        jsonw.WriteValue(v);
                    }
                    jsonw.WriteEndArray();
                //Atribut je polem poddokumentů, řešeno rekurzí
                } else if (schemeAttr.Value.Container && schemeAttr.Value.Link) {
                    var values = docAttrs.GetValues(attrName);
                    if (values.Count == 0 && shadowAttrs != null) {
                        values = shadowAttrs.GetValues(attrName);
                    }
                    if (values.Count == 0) continue;
                    jsonw.WritePropertyName(attrName);
                    jsonw.WriteStartArray();
                    foreach (var v in values) {
                        if (v.IndexOf('@') != 0) continue;
                        jsonw.WriteStartObject();
                        HandleProperties(schemeAttr.Value.Child, int.Parse(v.Substring(1)), jsonw, date);
                        jsonw.WriteEndObject();
                    }
                    jsonw.WriteEndArray();
                }
            }
        }
    }
}
