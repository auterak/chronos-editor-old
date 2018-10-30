using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ChronosEditor {
    internal static class GuiHandler {
        /// <summary>
        /// Metoda pro generování formulářů
        /// </summary>
        /// <param name="grid">Umístění formuláře</param>
        /// <param name="path">Cesta</param>
        /// <param name="root">Příznak kořenového dokumentu</param>
        /// <param name="subAttrId">Identifikátor poddokumentu</param>
        /// <param name="newDoc">Příznak generování formuláře pro nový poddokument</param>
        public static void GenerateElements(this Grid grid, string path, bool root = true, int subAttrId = -1, bool newDoc = false) {
            var db = (ChronosLib)Application.Current.Properties["dbHandler"];
            var choppedPath = path.Split('/').ToList();
            //Parsování cesty bez odkazů na poddokumenty
            var noLinksPath = (from chunk in choppedPath
                where chunk.IndexOf("@", StringComparison.Ordinal) == -1
                select chunk).ToList();
            var scheme = string.Join("/", noLinksPath).GetScheme();
            var links = (Dictionary<string, string>) Application.Current.Properties["links"];
            var children = new List<string>();
            DataTable attrs = null;

            if (subAttrId != -1) attrs = db.ScanDocs(subAttrId, DateTime.Now);

            var x = root ? 125 : 10;
            var y = 5;

            //Vygenerování pole pro název poddokumentu
            if (!root) {
                var id = attrs?.GetValues("_id") ?? new List<string>();
                if (id.Count == 0 || newDoc) {
                    var label = new Label {
                        Content = "Název",
                        Margin = new Thickness(x, y - 3, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top
                    };

                    grid.Children.Add(label);

                    var textBox = new TextBox {
                        Name = "tb_id",
                        Tag = "_id",
                        Width = 440,
                        Margin = new Thickness(x + 85, y, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top,
                    };

                    grid.Children.Add(textBox);

                    y += (5 + textBox.MinLines * 16);
                }
            }

            try {
                //Procházení atributů ze schématu
                foreach (var attribute in scheme.Attributes.Values) {
                    var name = attribute.Name.SplitJoinExcLast();
                    //Pokud je atribut odkazem na poddokument, tak se přeskakuje
                    if (attribute.Child == null) {
                        var content = "";
                        var items = new List<string>();
                        //Pokud je atribut odkazem na atribut, získájí se hodnoty z odkazu
                        if (attribute.Link) {
                            if (links.Keys.Contains(attribute.Name)) {
                                var link = links[attribute.Name];
                                var temp = link.GetContent(noLinksPath, noLinksPath.Count - 1);
                                if (link.IndexOf("->", StringComparison.Ordinal) != -1 && temp.Count > 0) {
                                    content = temp[0];
                                } else {
                                    items = temp;
                                }
                            }
                        }

                        //Hlavička pro vyplňované pole
                        var labelContent = name.FirstCharToUpper();
                        labelContent = labelContent.Replace('_', ' ');
                        var ending = labelContent.Length > 11 ? "..." : "";
                        var label = new Label {
                            Content = labelContent.Substring(0, labelContent.Length < 11 ? labelContent.Length : 11) + ending,
                            Margin = new Thickness(x, y - 3, 0, 0),
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Top,
                            ToolTip = labelContent
                        };

                        grid.Children.Add(label);

                        //Zjištění, zda se bude generovat TextBox nebo ComboBox
                        var tb = true;
                        if (attribute.Link) {
                            if (links.Keys.Contains(attribute.Name)) {
                                var link = links[attribute.Name];
                                if (link.IndexOf("=>", StringComparison.Ordinal) != -1) {
                                    tb = false;
                                }
                            }
                        }

                        //Generování TextBoxu
                        if (tb) {
                            //Základní vlastnosti
                            var textBox = new TextBox {
                                Name = "tb" + new string(name.Where(c => !char.IsPunctuation(c)).ToArray()),
                                Tag = name,
                                Width = 440,
                                Margin = new Thickness(x + 85, y, 0, 0),
                                HorizontalAlignment = HorizontalAlignment.Left,
                                VerticalAlignment = VerticalAlignment.Top,
                                Text = content,
                            };

                            //Nastavení pro explicitní vyjádření počtu řádků
                            var brIndex = attribute.Name.LastIndexOf("(", StringComparison.Ordinal);
                            if (brIndex != -1) {
                                if (int.TryParse(attribute.Name.Substring(brIndex + 1, (attribute.Name.LastIndexOf(")", StringComparison.Ordinal) - brIndex) - 1), out var lines)) {
                                    textBox.AcceptsTab = true;
                                    textBox.MinLines = lines;
                                    textBox.TextWrapping = TextWrapping.Wrap;
                                    textBox.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                                } else {
                                    brIndex = -1;
                                }
                            }

                            //Nastavení pro pole skalárů
                            if (attribute.Container) {
                                textBox.MinLines = 5;
                                textBox.ToolTip = "Každou položku vložte na nový řádek";
                            }

                            //Společné nastavení pro pole a počet řádků
                            if (brIndex != -1 || attribute.Container) {
                                textBox.AcceptsReturn = true;
                                textBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                                textBox.Height = textBox.MinLines * 16;
                                textBox.MaxHeight = textBox.MinLines * 16;
                            }

                            //Doplnění existujících hodnot do formuláře
                            if (!attribute.Container && attrs != null) {
                                var tempAttr = attrs.GetValues(name);
                                if (tempAttr.Count == 1) {
                                    textBox.Text = tempAttr.Last();
                                    textBox.IsEnabled = false;
                                }
                            }

                            grid.Children.Add(textBox);
                            y += (5 + textBox.MinLines * 16);
                        } else {
                            //Základní vlastnosti
                            var comboBox = new ComboBox {
                                Name = "cb" + new string(name.Where(c => !char.IsPunctuation(c)).ToArray()),
                                Tag = name,
                                Width = 440,
                                Height = 21,
                                Margin = new Thickness(x + 85, y, 0, 0),
                                HorizontalAlignment = HorizontalAlignment.Left,
                                VerticalAlignment = VerticalAlignment.Top,
                                IsEditable = true,
                                ItemsSource = items
                            };

                            //Doplnění existující hodnot do formuláře
                            if (!attribute.Container && attrs != null) {
                                var tempAttr = attrs.GetValues(name);
                                if (tempAttr.Count == 1) {
                                    comboBox.Text = tempAttr.Last();
                                    comboBox.IsEnabled = false;
                                }
                            }

                            grid.Children.Add(comboBox);
                            y += 23;
                        }
                    } else {
                        children.Add(attribute.Name);
                    }
                }

                //Tlačítko pro zpracování formuláře
                var button = new Button {
                    Name = "btnAdd",
                    Content = "Přidat",
                    Width = 525,
                    Height = 21,
                    Tag = subAttrId,
                    Margin = new Thickness(x, y, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top
                };
                y += 23;
                button.Click += AddButtonClick;
                grid.Children.Add(button);

                //Vytvoření tlačítek pro poddokumenty, pokud se jedná o kořenový formulář
                if (children.Count <= 0 || !root) return;
                grid.Children.Add(new Separator {
                    Width = 525,
                    Height = 21,
                    Margin = new Thickness(x, y, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                });
                y += 23;
                foreach (var ch in children) {
                    var btn = new Button {
                        Name = "btn" + ch,
                        Content = ch,
                        Tag = path + "/" + ch,
                        Width = 525,
                        Height = 21,
                        Margin = new Thickness(x, y, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    y += 23;
                    btn.Click += OpenSubAttrWindow;
                    grid.Children.Add(btn);
                }
            } catch (Exception ex) {
                MessageBox.Show("Chyba pri generovani: " + ex.Message);
            }
        }


        /// <summary>
        /// Metoda pro obsluhu tlačítka pro zpracování formuláře
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void AddButtonClick(object sender, RoutedEventArgs e) {
            var rootId = (int)Application.Current.Properties["docId"];
            var docId = rootId;

            //Získání správného identifikátoru dokumentu
            if (sender is Button btn) {
                var temp = int.Parse(btn.Tag.ToString());
                docId = temp == -1 ? docId : temp;
            }
            
            var scheme = (Document)Application.Current.Properties["scheme"];

            //Zjištění aktivního okna
            var w = Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive);
            if (w != null) {
                Grid grid = null;
                //Získání gridu s formulářem
                if (w.Content is ScrollViewer scrollViewer) {
                    grid = (Grid)scrollViewer.Content;
                } else {
                    var mainGrid = (Grid) w.Content;
                    foreach (var mainGridChild in mainGrid.Children) {
                        if (mainGridChild is ScrollViewer sv) {
                            grid = (Grid) sv.Content;
                        }
                    }
                }

                if (w.Tag != null) {
                    try {
                        //Získání cesty k poddokumentu a zavolání metody pro zpracování 
                        var choppedPath = w.Tag.ToString().Split('/').ToList();
                        var noLinksPath = (from chunk in choppedPath
                            where chunk.IndexOf("@", StringComparison.Ordinal) == -1
                            select chunk).ToList();
                        var name = choppedPath.Last();
                        var schemeAttr = (from a in string.Join("/", noLinksPath.Take(noLinksPath.Count - 1)).GetScheme().Attributes where a.Key.SplitJoinExcLast().Equals(name) select a.Value).Take(1).ToList()[0];
                        var container = schemeAttr.Container;
                        scheme = string.Join("/", noLinksPath).GetScheme();
                        grid.HandleElements(scheme, docId, w.Title.Split('-').Last() == "Úprava", container, name, schemeAttr.Value);
                    } catch(Exception ex) {
                        MessageBox.Show("Atribut nelze přidat kvůli následující chybě: " + ex.Message, "Atribut nepřidán", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                } else {
                    grid.HandleElements(scheme, docId);
                }

                if (grid != null && (!bool.Parse(grid.Tag.ToString()) || w.Name == "Main")) return;
                w.Tag = grid.Tag;
            }

            w.Close();
        }

        /// <summary>
        /// Metoda pro zpracování formuláře
        /// </summary>
        /// <param name="grid">Umístění formuláře</param>
        /// <param name="scheme">Schéma dokumentu</param>
        /// <param name="docId">Identifikátor dokumentu</param>
        /// <param name="edit">Příznak upravování dokumentu</param>
        /// <param name="container">Příznak zda je poddokument v poli</param>
        /// <param name="attrName">Název atributu</param>
        /// <param name="schemeLink">Odkaz na schéma</param>
        private static void HandleElements(this Grid grid, Document scheme, int docId, bool edit = true, bool container = false, string attrName = "", string schemeLink = "") {
            var db = (ChronosLib) Application.Current.Properties["dbHandler"];
            var login = Application.Current.Properties["login"].ToString();
            var pwd = Application.Current.Properties["pwd"].ToString().DecryptPassword();
            DataTable docAttrs = null;
            var tempDocId = docId;
            var success = true;

            //Získání existujících hodnot atributů
            try {
                if (edit) {
                    docAttrs = db.ScanDocs(tempDocId, DateTime.Now);
                } else {    
                    tempDocId = db.InsertDoc(login, pwd);
                    if (container) {
                        db.InsertAttribute(docId, attrName, "@" + tempDocId, true, login, pwd);
                    } else {
                        db.SetAttribute(docId, attrName, "@" + tempDocId, true, login, pwd);
                    }

                    docAttrs = db.ScanDocs(tempDocId, DateTime.Now);
                }
            } catch (Exception ex) {
                MessageBox.Show("Během zpracovávání došlo k chybě: " + ex.Message,
                    "Chyba při zpracování", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var counter = 0;

            //Zpracování formuláře
            foreach (var child in grid.Children) {
                switch (child) {
                    //Zpracování TextBoxu
                    case TextBox textBox: {
                        //Přeskočení prázdných polí a zneplatněných polí při editaci
                        if (textBox.Text == "") continue;
                        if (!textBox.IsEnabled && edit) continue;
                        var name = textBox.Tag.ToString();
                        //Pokud jde o vyplnění názvu, vyplní se i schéma
                        if (name == "_id") {
                            db.SetAttribute(tempDocId, name, textBox.Text, false, login, pwd);
                            db.SetAttribute(tempDocId, "_scheme", schemeLink, true, login, pwd);
                            counter++;
                            textBox.Clear();
                            continue;
                        }
                        var schemeAttr =
                            (from a in scheme.Attributes where a.Key.SplitJoinExcLast().Equals(name) select a.Value)
                            .Take(1).ToList()[0];
                        //Testování, zda je možné hodnotu atributu vložit a případné vložení
                        var docAttrCount = docAttrs.GetValues(name).Count;
                        try {
                            if (docAttrCount == 0 && !schemeAttr.Container) {
                                db.SetAttribute(tempDocId, name, textBox.Text, false, login, pwd);
                                counter++;
                            } else if (docAttrCount >= 0 && schemeAttr.Container) {
                                for (var i = 0; i < textBox.LineCount; i++) {
                                    db.InsertAttribute(tempDocId, name,
                                        Regex.Replace(textBox.GetLineText(i), @"\r\n?|\n", ""), false, login,
                                        pwd);
                                    counter++;
                                }
                            } else if (docAttrCount > 0 && !schemeAttr.Container) {
                                MessageBox.Show("Atribut \"" + name + "\" již v databázi existuje",
                                    "Atribut existuje", MessageBoxButton.OK, MessageBoxImage.Warning);
                                success = false;
                            }

                            if (textBox.IsEnabled) textBox.Clear();
                        } catch (Exception ex) {
                            success = false;
                            MessageBox.Show(
                                "Atribut \"" + name + "\" nebyl přidán kvůli následující chybě: " + ex.Message,
                                "Atribut nepřidán", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }

                        break;
                    }
                    //Zpracování ComboBoxu, až na menší odlišnosti viz TextBox
                    case ComboBox cb: {
                        if (cb.Text == "") continue;
                        if (!cb.IsEnabled && edit) continue;
                        var name = cb.Tag.ToString();
                        var schemeAttr =
                            (from a in scheme.Attributes where a.Key.SplitJoinExcLast().Equals(name) select a.Value)
                            .Take(1).ToList()[0];
                        var docAttrCount = docAttrs.GetValues(name).Count;
                        try {
                            if (docAttrCount == 0 && !schemeAttr.Container) {
                                db.SetAttribute(tempDocId, name, cb.Text, false, login, pwd);
                                counter++;
                            } else if (docAttrCount >= 0 && schemeAttr.Container) {
                                db.InsertAttribute(tempDocId, name, cb.Text, false, login, pwd);
                                counter++;
                            } else if (docAttrCount > 0 && !schemeAttr.Container) {
                                MessageBox.Show("Atribut \"" + name + "\" již v databázi existuje",
                                    "Atribut existuje", MessageBoxButton.OK, MessageBoxImage.Warning);
                                success = false;
                            }

                            if (cb.IsEnabled) cb.Text = "";
                        } catch (Exception ex) {
                            MessageBox.Show(
                                "Atribut \"" + name + "\" nebyl přidán kvůli následující chybě: " + ex.Message,
                                "Atribut nepřidán", MessageBoxButton.OK, MessageBoxImage.Warning);
                            success = false;
                        }

                        break;
                    }
                }
            }

            grid.Tag = success;
            if (success) {
                MessageBox.Show("Bylo přidáno " + counter + " záznamů");
            } else {
                MessageBox.Show("Některá data nebyla přidána", "Data nepřidána", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Metoda pro obsluhu tlačítka pro otevření okna pro poddokument
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OpenSubAttrWindow(object sender, RoutedEventArgs e) {
            if (sender is Button btn && btn.Tag.ToString().CanAdd()) {
                var newAttr = new NewAttr(btn.Tag.ToString(), btn.Content.ToString());
                newAttr.ShowDialog();
            } else {
                MessageBox.Show("Tento atribut již nelze přidat", "Dosažen limit atributů", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Metoda pro vyvolání dialogu pro export dokumentu
        /// </summary>
        /// <param name="type">Typ dokumentu</param>
        /// <param name="date">Časová značka</param>
        public static void OpenSaveDialog(string type, DateTime date) {
            var docId = (int)Application.Current.Properties["docId"];
            var scheme = (Document)Application.Current.Properties["scheme"];

            //Dialog pro výběr souboru
            var saveFileDialog = new SaveFileDialog {
                Filter = string.Format("{0} files (*.{1})|*.{1}", type.ToUpper(), type)
            };
            if (saveFileDialog.ShowDialog() != true) return;
            try {
                //Podle typu se zavolá odpovídající metoda pro generování souboru
                switch(type) {
                    case "xml":
                        XmlHandler.CreateXmlFile(scheme, docId, saveFileDialog.FileName, date);
                        break;
                    case "json":
                        JsonHandler.CreateJsonFile(scheme, docId, saveFileDialog.FileName, date);
                        break;
                }
                MessageBox.Show("Dokument úspěšně uložen do " + saveFileDialog.FileName, "Uložení úspěšné");
            } catch (Exception ex) {
                MessageBox.Show("Uložení dokumentu do souboru se nezdařilo: " + ex.Message, "Uložení se nezdařilo", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Metoda pro generování stromu dokumentu
        /// </summary>
        /// <param name="docId">Identifikátor dokumentu</param>
        /// <param name="scheme">Schéma dokumentu</param>
        /// <param name="date">Časová značka</param>
        /// <param name="name">Název atributu</param>
        /// <param name="deletionPath">Cesta pro smazání atributu</param>
        /// <returns>TreeViewItem - část stromu</returns>
        public static TreeViewItem GenerateTree(this int docId, Document scheme,  DateTime date, string name = "N/a", string deletionPath = "") {
            var db = (ChronosLib)Application.Current.Properties["dbHandler"];
            DataTable attrs;
            DataTable shadowAttrs = null;
            //Získání atributů
            try {
                attrs = db.ScanDocs(docId, date);
            } catch (Exception ex) {
                MessageBox.Show("Během zpracovávání došlo k chybě: " + ex.Message,
                    "Chyba při zpracování", MessageBoxButton.OK, MessageBoxImage.Error);
                return new TreeViewItem();
            }

            var tag = deletionPath;

            var nameList = attrs.GetValues("_id");

            //Nastavení názvu dokumentu na kořen
            if (nameList.Count > 0 && deletionPath == "") {
                name = nameList.First();
                tag = name;

                shadowAttrs = attrs.GetShadowAttrs();
            }

            //Vytvoření kořene
            var root = new TreeViewItem {
                Header = name,
                Tag = tag
            };

            //Procházení atributů podle schématu
            foreach (var attr in scheme.Attributes) {
                var tempDp = deletionPath;
                var attrName = attr.Value.Name.SplitJoinExcLast();
                //Řešení atributů, které nejsou v poli
                if (!attr.Value.Container) {
                    var values = attrs.GetValues(attrName);
                    if (values.Count == 0 && shadowAttrs != null) {
                        values = shadowAttrs.GetValues(attrName);
                        deletionPath = "shadow";
                    }
                    foreach (var v in values) {
                        //Atribut není odkaz na poddokument
                        if (v.IndexOf('@') != 0) {
                            var ending = v.Length > 20 ? "..." : "";
                            root.Items.Add(new TreeViewItem {
                                Header = attrName + " - " + v.Substring(0, v.Length < 20 ? v.Length : 20) + ending,
                                ToolTip = v,
                                Tag = deletionPath+(deletionPath == "" ? "" : "/")+attr.Value.Name
                            });
                        //Atribut je odkazem na poddokument - rekurze
                        } else {
                            var item = int.Parse(v.Substring(1)).GenerateTree(attr.Value.Child, date, attr.Value.Name, deletionPath + (deletionPath == "" ? "" : "/") + attr.Value.Name + "/" + v);
                            if (item != null) {
                                root.Items.Add(item);
                            }
                        }
                    }
                //Řešení atributů v poli (nejsou poddokumenty)
                } else if (attr.Value.Container && !attr.Value.Link) {
                    //Vytvoření kořene pro atribut
                    var item = new TreeViewItem {
                        Header = attr.Value.Name,
                        Tag = deletionPath + (deletionPath == "" ? "" : "/") + attr.Value.Name
                    };
                    var values = attrs.GetValues(attrName);
                    if (values.Count == 0 && shadowAttrs != null) {
                        values = shadowAttrs.GetValues(attrName);
                        deletionPath = "shadow";
                    }
                    //Vytvoření "poddstromu" z hodnot v poli
                    foreach (var v in values) {
                        item.Items.Add(new TreeViewItem {
                            Header = v,
                            Tag = deletionPath + (deletionPath == "" ? "" : "/") + attr.Value.Name
                        });
                    }
                    if (values.Count > 0) {
                        root.Items.Add(item);
                    }
                //Řešení atributů v poli (jsou poddokumenty)
                } else if (attr.Value.Container && attr.Value.Link) {
                    var values = attrs.GetValues(attrName);
                    if (values.Count == 0 && shadowAttrs != null) {
                        values = shadowAttrs.GetValues(attrName);
                        deletionPath = "shadow";
                    }
                    //Každý poddokument má vlastní podstrom - řešeno rekurzí
                    foreach (var v in values) {
                        if (v.IndexOf('@') != 0) continue;
                        var item = int.Parse(v.Substring(1)).GenerateTree(attr.Value.Child, date, attr.Value.Name, deletionPath + (deletionPath == "" ? "" : "/") + attr.Value.Name + "/" + v);
                        item.Tag = deletionPath + (deletionPath == "" ? "" : "/") + attr.Value.Name + "/" + v;
                        root.Items.Add(item);
                    }
                }

                deletionPath = tempDp;
            }

            return root;
        }

        /// <summary>
        /// Metoda pro mazání atributů ze stromu
        /// </summary>
        /// <param name="selectedItem">Vybraný prvek stromu</param>
        /// <param name="docId">Identifikátor dokumentu</param>
        /// <param name="scheme">Schéma dokuemntu</param>
        /// <param name="choppedPath">Cesta k atributu</param>
        public static void DeleteFromTree(this TreeViewItem selectedItem, int docId, Document scheme, List<string> choppedPath) {
            var db = (ChronosLib)Application.Current.Properties["dbHandler"];
            var login = Application.Current.Properties["login"].ToString();
            var pwd = Application.Current.Properties["pwd"].ToString().DecryptPassword();
            var docName = db.GetName((int) Application.Current.Properties["docId"], login, pwd);

            //Kořenový dokument nelze mazat
            if (choppedPath.Count == 1 && choppedPath.Last() == docName) {
                MessageBox.Show("Kořenový dokument nelze odstranit", "Atribut nelze odstranit", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            //Ze stínového dokumentu nelze mazat
            if (choppedPath.First() == "shadow") {
                MessageBox.Show("Atribut stínového dokumentu nelze odstranit", "Atribut nelze odstranit", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            //V cestě je jeden prvek = konec cesty
            if (choppedPath.Count == 1) {
                try {
                    var name = choppedPath.First();
                    var schemeAttr = (from a in scheme.Attributes where a.Key.Equals(name) select a.Value).Take(1).ToList()[0];
                    //Mazaný atribut je v poli
                    if (schemeAttr.Container) {
                        var value = selectedItem.Header.ToString();
                        //K mazání označen prvek pole
                        if (name != value) {
                            db.RemoveAttribute(docId, name, value, login, pwd);
                        //K mazání označeno celé pole
                        } else {
                            db.ResetAttribute(docId, name, login, pwd);
                        } 
                    //Mazaný atribut není v poli
                    } else {
                        db.ResetAttribute(docId, name, login, pwd);
                    }
                } catch (Exception ex) {
                    MessageBox.Show("Při odstraňování došlo k chybě: " + ex.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            //V cestě je několik prvků
            } else {
                var name = choppedPath.First();
                var schemeAttr = (from a in scheme.Attributes where a.Key.Equals(name) select a.Value).Take(1).ToList()[0];
                if (!schemeAttr.Link) return;
                //Získání odkazu pro pokračování v poddokumentu
                var link = choppedPath[1];
                //Je-li odkaz zárověň posledním prvkem v cestě, nastává mazání celého poddokumentu
                if (link == choppedPath.Last()) {
                    try {
                        if (schemeAttr.Container) {
                            db.RemoveAttribute(docId, name, link, login, pwd);
                        } else {
                            db.ResetAttribute(docId, name, login, pwd);
                        }
                    } catch (Exception ex) {
                        MessageBox.Show(ex.Message);
                    }
                //Je-li odkaz druhý od konce, maže se atribut odkazovaného dokumentu
                } else if (link == choppedPath[choppedPath.Count-2]) {
                    var attrName = choppedPath.Last().SplitJoinExcLast();
                    var tempDocId = int.Parse(link.Substring(1));
                    var subAttrScheme = (from a in schemeAttr.Child.Attributes where a.Key.Equals(attrName) select a.Value).Take(1).ToList()[0];
                    try {
                        if (subAttrScheme.Container) {
                            var value = selectedItem.Header.ToString();
                            if (attrName != value) {
                                db.RemoveAttribute(tempDocId, attrName, value, login, pwd);
                            } else {
                                db.ResetAttribute(tempDocId, attrName, login, pwd);
                            }
                        } else {
                            db.ResetAttribute(tempDocId, attrName, login, pwd);
                        }
                    } catch (Exception ex) {
                        MessageBox.Show("Při odstraňování došlo k chybě: " + ex.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                //Rekurze v případě nesplnění předchozích podmínek
                } else {
                    selectedItem.DeleteFromTree(int.Parse(link.Substring(1)), schemeAttr.Child, choppedPath.GetRange(2, choppedPath.Count-2));
                }
            }
        }

        /// <summary>
        /// Metoda pro přidání atributů přes strom dokumentu
        /// </summary>
        /// <param name="selectedItem">Vybraný prvek stromu</param>
        /// <param name="docId">Identifikátor dokumentu</param>
        /// <param name="scheme">Schéma dokumentu</param>
        /// <param name="choppedPath">Cesta k atributu</param>
        /// <returns></returns>
        public static bool AddToDoc(this TreeViewItem selectedItem, int docId, Document scheme, List<string> choppedPath) {
            //Stínovému dokumentu nelze přidávat atributy
            if (choppedPath.First() == "shadow") {
                MessageBox.Show("Nelze přidávat atributy do stínového dokumentu", "Atribut nelze přidat", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            var login = Application.Current.Properties["login"].ToString();
            var pwd = Application.Current.Properties["pwd"].ToString().DecryptPassword();
            var db = (ChronosLib)Application.Current.Properties["dbHandler"];

            //Parsování cesty bez odkazů pro získání schématu
            var pathForScheme = new List<string> { db.GetName(docId, login, pwd) };
            var possibleAttrsList = new Dictionary<string, Tuple<int, string, string>>();
            foreach (var chunk in choppedPath) {
                if (chunk.First() != '@') pathForScheme.Add(chunk.SplitJoinExcLast());
            }
            var selectedScheme = string.Join("/", pathForScheme).GetScheme();
            //Pokud atribut nemá schéma, nemá poddatributy k přidání
            if (selectedScheme == null) {
                MessageBox.Show("Tomuto atributu nelze přidat podatribut", "Nelze přidat podatribut", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            var name = db.GetName(docId, login, pwd);
            //Získání možných atributů k přidání
            foreach (var a in selectedScheme.Attributes) {
                if (a.Value.Child == null) continue;
                if (choppedPath.Count == 1 && choppedPath.First() == name) {
                    possibleAttrsList[a.Value.Name] = (new Tuple<int, string, string>(docId, a.Value.Name, string.Join("/", choppedPath)));
                } else {
                    possibleAttrsList[a.Value.Name] = (new Tuple<int, string, string>(
                        int.Parse(choppedPath.Last().Substring(1)), a.Value.Name,
                        string.Join("/", choppedPath)));
                }
            }

            //Pokud nemá vhodné kandidáty na přidání
            if (possibleAttrsList.Count == 0) {
                MessageBox.Show("Tento atribut nemá vhodné podatributy pro přidání", "Nelze přidat podatribut", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            //Vyvolání okna pro výběr atributu k přidání
            var attrList = new AttrList(possibleAttrsList);
            attrList.ShowDialog();
            return attrList.Success;
        }

        /// <summary>
        /// Pomocná metoda pro hledání vybraného prvku stromu dokumentu
        /// </summary>
        /// <param name="source"></param>
        /// <returns>Vybraný prvek</returns>
        public static TreeViewItem VisualUpwardSearch(DependencyObject source) {
            while (source != null && !(source is TreeViewItem))
                source = VisualTreeHelper.GetParent(source);

            return source as TreeViewItem;
        }

        /// <summary>
        /// Metoda pro získání cesty k logu pro spash screen
        /// </summary>
        /// <returns>Cesta k obrázku</returns>
        public static string GetLogoFile() {
            var files = new DirectoryInfo(".").GetFiles("logo.png", SearchOption.AllDirectories);
            return files.Length > 0 ? files[0].FullName : null;
        }

        /// <summary>
        /// Metoda pro zneplatnění tlačítek formuláře
        /// </summary>
        /// <param name="grid">Umístění tlačítek</param>
        public static void DisableButtons(this Grid grid) {
            foreach (var gridChild in grid.Children) {
                if (gridChild is Button btn) {
                    btn.IsEnabled = false;
                }
            }
        }

        /// <summary>
        /// Metoda pro zplatnění tlačítek formuláře
        /// </summary>
        /// <param name="grid">Umístění tlačítek</param>
        public static void EnableButtons(this Grid grid) {
            foreach (var gridChild in grid.Children) {
                if (gridChild is Button btn) {
                    btn.IsEnabled = true;
                }
            }
        }
    }
}
