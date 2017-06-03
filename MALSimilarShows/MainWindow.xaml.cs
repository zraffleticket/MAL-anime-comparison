using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

namespace MALSimilarShows {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
        }

        private string getPage(String url) {
            WebClient web = new WebClient();
            Stream stream = web.OpenRead(url);
            string output;
            using (StreamReader reader = new StreamReader(stream)) {

                output = reader.ReadToEnd();
            }
            return output;
        }

        /// <summary>
        /// Builds tables of staff and their positions in a supplied URL
        /// </summary>
        /// <param name="url"></param>
        /// <returns>
        /// Tables of staff positions.
        /// [0] - voice actors
        /// [1] - staff
        /// [2] - Licensors, studios, and other info
        /// </returns>
        public DataTable[] getEachTable(string url) {
            url = fixUrl(url);

            string staffText = getPage(url);
            string vaText = staffText;
            string infoText = vaText;
            // title of the series is in the first span itemprop="name'. Screw doing the XML, I can do this myself with far less overhead
            string title = staffText.Substring(staffText.IndexOf("<span itemprop=\"name\">") + 22);
            title = title.Substring(0, title.IndexOf("</"));

            // setup xml reader
            XmlDocument doc = new XmlDocument();
            XmlNode tempElement;

            // tables for output
            DataTable[] showdata = new DataTable[3]; // separate to pair them better in the final join
            showdata[0] = new DataTable(); // voice actors
            showdata[1] = new DataTable(); // staff
            showdata[2] = new DataTable(); // information

            DataRow tempRow;

            // table's title is the name of the series - will reuse in join
            showdata[0].TableName = title;

            // set up columns so we can reference them later
            showdata[0].Columns.Add("Name", typeof(string));
            showdata[0].Columns.Add("Position", typeof(string));

            // ... and do it again for the staff
            // table's title is the name of the series - will reuse in join
            showdata[1].TableName = title;

            // set up columns so we can reference them later
            showdata[1].Columns.Add("Name", typeof(string));
            showdata[1].Columns.Add("Position", typeof(string));

            // ... and do it again for information
            // table's title is the name of the series - will reuse in join
            showdata[2].TableName = title;

            // set up columns so we can reference them later
            showdata[2].Columns.Add("Name", typeof(string));
            showdata[2].Columns.Add("Position", typeof(string));

            #region Voice Actors
            // voice actor range is from the h2 for Voice actors</h2> to the h2 for staff.
            vaText = vaText.Substring(vaText.IndexOf("tors</h2>") + 9);
            vaText = vaText.Substring(0, vaText.IndexOf("Staff\n</h2>"));

            // add root elements, and end properly
            vaText = "<root>" + vaText;
            vaText = vaText + "</h2></root>";

            // Regex:   <a href=\"/dbchanges\.php\?.+a>
            //
            // Literal: <a href=
            // doublequote:     \"
            // Literal:           /dbchanges
            // dot . :                      \.
            // Literal:                       php
            // question mark ? :                 \?
            // any character, 1 to many times:     .+
            // Literal:                              a>

            // fix errors before they happen
            vaText = Regex.Replace(vaText, "<img.+>", ""); // c# was shit and fixed itself. Great.
            vaText = Regex.Replace(vaText, "<br>", "<br />"); // this one does work properly
            vaText = Regex.Replace(vaText, "&nbsp", ""); // & gets in the way, but there are a lot of these in the text so let's get rid of these first, then the rest of the &'s
            vaText = Regex.Replace(vaText, "<a href=\"/dbchanges\\.php\\?.+a>", ""); // fuck this one in particular... this link breaks everything because of the ? I think... see above for explanation
            vaText = Regex.Replace(vaText, "&", "and"); // could be in titles, if there are any other ampersands we'll handle it separately

            // fire this up
            doc.LoadXml(vaText);

            // for each table element (character info)
            string character;
            foreach(XmlElement table in doc.FirstChild.ChildNodes) {

                try {
                    // get the name of the character
                    character = table.FirstChild.ChildNodes[1].FirstChild.InnerText;

                    // get in the table with the voice actors in them
                    tempElement = table.FirstChild.ChildNodes[2].FirstChild;
                        
                    foreach(XmlNode x in tempElement) {
                        if(x.FirstChild.ChildNodes[2].InnerText.Equals("Japanese")) {
                            tempRow = showdata[0].NewRow();
                            tempRow[0] = x.FirstChild.FirstChild.InnerText ; // Voice Actor
                            tempRow[1] = "Voice Actor (" + character + ")";  // Character 
                            showdata[0].Rows.Add(tempRow);
                        }
                    }

                } catch(Exception ex) {
                    // some rows get discarded, design decision here
                }
            }

            // showdata[0] is done

            #endregion

            #region Staff
            // Extract the staff positions
            staffText = staffText.Substring(staffText.IndexOf("Staff\n</h2>") + 11);
            staffText = staffText.Substring(0, staffText.IndexOf("</table></div>  <div class=\"mauto") + 8);
            staffText = "<root>" + staffText + "</root>";

            // filter
            staffText = Regex.Replace(staffText, "<img.+>", ""); // 
            staffText = Regex.Replace(staffText, "<br>", "<br />"); // this one does work properly
            staffText = Regex.Replace(staffText, "&nbsp", ""); // & gets in the way, but there are a lot of these in the text so let's get rid of these first, then the rest of the &'s
            staffText = Regex.Replace(staffText, "&", "and"); // could be in titles, if there are any other ampersands we'll handle it separately

            string[] test = new string[1];
            test[0] = staffText;
            File.WriteAllLines("test.txt", test);

            // make a new document for staff
            doc.LoadXml(staffText);

            // for each table in the doc
            foreach(XmlElement table in doc.FirstChild.ChildNodes) {
                try {
                    tempRow = showdata[1].NewRow();
                    tempRow[0] = table.FirstChild.ChildNodes[1].FirstChild.InnerText; // person
                    tempRow[1] = table.FirstChild.ChildNodes[1].LastChild.InnerText;  // position
                    showdata[1].Rows.Add(tempRow);
                } catch(Exception ex) {
                    // some rows get discarded, design decision here
                }
            }

            // showdata[1] done..

            #endregion

            #region Information
            /* 
            // mark beginning and end
            infoText = infoText.Substring(infoText.IndexOf("Information</h2>") + 16);
            infoText = infoText.Substring(0, infoText.IndexOf("<h2>Statistics"));

            // add root element
            infoText = "<root>" + infoText;
            infoText += "</root>";


                

            // regexes will be hell.
            // make a new document for staff
            doc.LoadXml(infoText);

            var temp = doc.ChildNodes[1];

            // root if first child, childnodes are DIV tags

            int category = 0;

            // Type
            tempRow = showdata[2].NewRow();
            tempRow[0] = Regex.Replace(doc.FirstChild.ChildNodes[category].FirstChild.InnerText.Trim(), ":", "");   // Type
            tempRow[1] = doc.FirstChild.ChildNodes[category].LastChild.InnerText.Trim();  // Result
            showdata[2].Rows.Add(tempRow);

            // Episodes
            category = 1;
            tempRow = showdata[2].NewRow();
            tempRow[0] = Regex.Replace(doc.FirstChild.ChildNodes[category].FirstChild.InnerText.Trim(), ":", "");   // Episodes
            tempRow[1] = doc.FirstChild.ChildNodes[category].LastChild.InnerText.Trim();  // Result
            showdata[2].Rows.Add(tempRow);

            // status = 2
            // aired = 3

            // premiered
            category = 4;
            tempRow = showdata[2].NewRow();
            tempRow[0] = Regex.Replace(doc.FirstChild.ChildNodes[category].FirstChild.InnerText.Trim(), ":", "");   // premiered
            tempRow[1] = doc.FirstChild.ChildNodes[category].LastChild.InnerText.Trim();  // Result
            showdata[2].Rows.Add(tempRow);

            // broadcast = 5
               
            // Producers
            category = 6;
            tempRow = showdata[2].NewRow();
            tempRow[0] = Regex.Replace(doc.FirstChild.ChildNodes[category].FirstChild.InnerText.Trim(), ":", "");   // Producers
            tempRow[1] = "";
            for(int i = 1; i < doc.FirstChild.ChildNodes[category].ChildNodes.Count; i++) {
                tempRow[1] += doc.FirstChild.ChildNodes[category].ChildNodes[i].InnerText.Trim();  // Result
            }
            tempRow[1] = Regex.Replace(tempRow[1].ToString(), ",+", ", ");
            showdata[2].Rows.Add(tempRow);

            // Licensors
            category = 7;
            tempRow = showdata[2].NewRow();
            tempRow[0] = Regex.Replace(doc.FirstChild.ChildNodes[category].FirstChild.InnerText.Trim(), ":", "");   // Licensors
            tempRow[1] = "";
            for(int i = 1; i < doc.FirstChild.ChildNodes[category].ChildNodes.Count; i++) {
                tempRow[1] += doc.FirstChild.ChildNodes[category].ChildNodes[i].InnerText.Trim();  // Result
            }
            tempRow[1] = Regex.Replace(tempRow[1].ToString(), ",+", ", ");
            showdata[2].Rows.Add(tempRow);

            // Studios
            category = 8;
            tempRow = showdata[2].NewRow();
            tempRow[0] = Regex.Replace(doc.FirstChild.ChildNodes[category].FirstChild.InnerText.Trim(), ":", "");   // Sudios
            tempRow[1] = "";
            for(int i = 1; i < doc.FirstChild.ChildNodes[category].ChildNodes.Count; i++) {
                tempRow[1] += doc.FirstChild.ChildNodes[category].ChildNodes[i].InnerText.Trim();  // Result
            }
            tempRow[1] = Regex.Replace(tempRow[1].ToString(), ",+", ", ");
            showdata[2].Rows.Add(tempRow);

            // Source
            category = 9;
            tempRow = showdata[2].NewRow();
            tempRow[0] = Regex.Replace(doc.FirstChild.ChildNodes[category].FirstChild.InnerText.Trim(), ":", "");   // Source
            tempRow[1] = doc.FirstChild.ChildNodes[category].LastChild.InnerText.Trim();  // Result
            showdata[2].Rows.Add(tempRow);

            // Genres
            category = 10;
            tempRow = showdata[2].NewRow();
            tempRow[0] = Regex.Replace(doc.FirstChild.ChildNodes[category].FirstChild.InnerText.Trim(), ":", "");   // Genres
            tempRow[1] = "";
            for(int i = 1; i < doc.FirstChild.ChildNodes[category].ChildNodes.Count; i++) {
                tempRow[1] += doc.FirstChild.ChildNodes[category].ChildNodes[i].InnerText.Trim();  // Result
            }
            tempRow[1] = Regex.Replace(tempRow[1].ToString(), ",+", ", ");
            showdata[2].Rows.Add(tempRow);

            // Duration
            category = 11;
            tempRow = showdata[2].NewRow();
            tempRow[0] = Regex.Replace(doc.FirstChild.ChildNodes[category].FirstChild.InnerText.Trim(), ":", "");   // Duration
            tempRow[1] = doc.FirstChild.ChildNodes[category].LastChild.InnerText.Trim();  // Result
            showdata[2].Rows.Add(tempRow);

            // Rating
            category = 12;
            tempRow = showdata[2].NewRow();
            tempRow[0] = Regex.Replace(doc.FirstChild.ChildNodes[category].FirstChild.InnerText.Trim(), ":", "");   // Rating
            tempRow[1] = doc.FirstChild.ChildNodes[category].LastChild.InnerText.Trim();  // Result
            showdata[2].Rows.Add(tempRow);
            //test.Text = infoText;
            */
            #endregion
                
            return showdata;
            
        }

        private void doOne() {
            string url = txtOne.Text;
            DataTable result = getShowInfo(url);
            dgTbl.DataContext = result.DefaultView;
        }

        private string fixUrl(string url) {
            if (!url.EndsWith("/characters")) {
                // remove query if any
                if (url.Contains('?')) {
                    url = url.Substring(0, url.IndexOf('?'));
                }
                url += "/characters";
            }
            return url;
        }

        private DataTable getShowInfo(string url) {
            url = fixUrl(url);
            string staffText = getPage(url);
            string title = staffText.Substring(staffText.IndexOf("<span itemprop=\"name\">") + 22);
            title = title.Substring(0, title.IndexOf("</"));

            // Extract the staff positions
            staffText = staffText.Substring(staffText.IndexOf("Staff\n</h2>") + 11);
            staffText = staffText.Substring(0, staffText.IndexOf("</table></div>  <div class=\"mauto") + 8);
            staffText = "<root>" + staffText + "</root>";

            // filter
            staffText = Regex.Replace(staffText, "<img.+>", ""); // 
            staffText = Regex.Replace(staffText, "<br>", "<br />"); // this one does work properly
            staffText = Regex.Replace(staffText, "&nbsp", ""); // & gets in the way, but there are a lot of these in the text so let's get rid of these first, then the rest of the &'s
            staffText = Regex.Replace(staffText, "&", "and"); // could be in titles, if there are any other ampersands we'll handle it separately

            // Positions I care about
            List<string> positions = new List<string>();
            positions.Add("Director");
            positions.Add("Script");
            positions.Add("Series Composition");
            positions.Add("Original Creator");
            positions.Add("Assistant Director");
            positions.Add("Episode Director");

            // make a new document for staff
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(staffText);

            List<Person> people = new List<Person>();

            // for each table in the doc (each person)
            foreach (XmlElement table in doc.FirstChild.ChildNodes) {
                try {
                    // for each position each person holds,
                    foreach(string position in table.FirstChild.ChildNodes[1].LastChild.InnerText.Split(',')) {
                        // if current position is in the whitelist
                        if(positions.Contains(position.Trim())){
                            // get their info     // table      tr         td[1]          a         
                            people.Add(new Person(table.FirstChild.ChildNodes[1].FirstChild.Attributes["href"].InnerText, // url 
                                                  table.FirstChild.ChildNodes[1].FirstChild.InnerText, // name
                                                  position)); // position
                        }
                    }
                } catch (Exception ex) {
                    // some rows get discarded, design decision here
                }
            }

            // start making output
            DataTable output = new DataTable();
            output.TableName = title + "'s Staff's Other Notable Positions";
            output.Columns.Add("Name", typeof(string));
            output.Columns.Add("Position", typeof(string));
            output.Columns.Add("Show", typeof(string));

            DataRow tempRow;

            string page;
            // for each person, retrieve their URL data, and grab any other entries they had the same role in

            

            foreach(Person person in people) {
                page = getPage(person.Url);

                // Extract the staff positions
                page = page.Substring(page.IndexOf("Positions</div>") + 15);
                page = page.Substring(0, page.IndexOf("</table>") + 8);
                page = "<root>" + page + "</root>";

                // filter
                page = Regex.Replace(page, "<img.+>", "</a></div></td>"); // 
                page = Regex.Replace(page, "<br>", "<br />"); // this one does work properly
                page = Regex.Replace(page, "&nbsp", ""); // & gets in the way, but there are a lot of these in the text so let's get rid of these first, then the rest of the &'s
                page = Regex.Replace(page, "&", "and"); // could be in titles, if there are any other ampersands we'll handle it separately
                page = Regex.Replace(page, "</small>", ""); // remove small ends
                page = Regex.Replace(page, "<\\/div><\\/td>\\s+<\\/tr>", "</small></div></td></tr>"); // fix episode counts

                // Load in the page
                doc.LoadXml(page);

                // for each entry the person has
                foreach(XmlElement tr in doc.FirstChild.FirstChild.ChildNodes) {
                    // see if they had the same position in it
                    //                         tr         td           div         small
                    //MessageBox.Show(tr.ChildNodes[1].ChildNodes[1].ChildNodes[1].InnerText + "\n" + person.Position);
                    if (person.Position.Trim().Equals(tr.ChildNodes[1].ChildNodes[1].ChildNodes[1].InnerText.Trim())) {
                        // if so, add to output
                        tempRow = output.NewRow();
                        tempRow[0] = person.Name.Trim();
                        tempRow[1] = person.Position.Trim();  
                        //           tr         td             a
                        tempRow[2] = tr.ChildNodes[1].FirstChild.InnerText.Trim();
                        output.Rows.Add(tempRow);
                    }
                }
                 /*
                string[] test = new string[1];
                test[0] = page;
                File.WriteAllLines("test.txt", test);
                // */
            }
            

            return output;

        }

        /// <summary>
        /// Launches when the button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e) {
            try {
                if(chkOne.IsChecked == true) {
                    if (textGood(txtOne.Text)) {
                        doOne();
                    } else {
                        MessageBox.Show("Error: Invalid link", "Error");
                    }
                } else {
                    if(textGood(txtTwo.Text) && textGood(txtOne.Text))
                        doStuff();
                    else
                        MessageBox.Show("Error: Invalid link(s)", "Error");
                }

                
            } catch (DuplicateNameException ex) {
                MessageBox.Show("You cannot search the same show against itself", "Error");
            } catch (Exception ex) {
                // if the operation fails anywhere else...
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
            }
        }

        /// <summary>
        /// handles the main processing of the program, joins tables together, and returns the output.
        /// </summary>
        private void doStuff() {
            // get the URLs
            string url1 = txtOne.Text;
            string url2 = txtTwo.Text;

            // and the tables from them
            DataTable[] dt1 = getEachTable(url1);
            DataTable[] dt2 = getEachTable(url2);

            // inner join voice actors together
            var vares = from p in dt1[0].AsEnumerable()
                        join q in dt2[0].AsEnumerable()
                        on p.Field<string>("Name") equals q.Field<string>("Name")
                        select new {
                            Name = p.Field<string>("Name"),
                            Show_1_Position = p.Field<string>("Position"),
                            Show_2_Position = q.Field<string>("Position")
                        };
            // select name, a.position, b.posiiton
            // from dt1 a join dt2 b
            // on a.name = b.name


            // inner join staff together
            var staffres = from p in dt1[1].AsEnumerable()
                           join q in dt2[1].AsEnumerable()
                           on p.Field<string>("Name") equals q.Field<string>("Name")
                           select new {
                               Name = p.Field<string>("Name"),
                               Show_1_Position = p.Field<string>("Position"),
                               Show_2_Position = q.Field<string>("Position")
                           };

            // inner join staff together
            var infores = from p in dt1[2].AsEnumerable()
                          join q in dt2[2].AsEnumerable()
                          on p.Field<string>("Name") equals q.Field<string>("Name")
                          select new {
                              Name = p.Field<string>("Name"),
                              Show_1_Position = p.Field<string>("Position"),
                              Show_2_Position = q.Field<string>("Position")
                          };



            // prep final table for display
            DataTable result = new DataTable();
            result.Columns.Add("Name", typeof(string));
            result.Columns.Add(dt1[0].TableName, typeof(string));
            result.Columns.Add(dt2[0].TableName, typeof(string));

            DataRow row;

            // why on earth is this the proper way to do this microsoft? God damn...
            // remake into data row for voice actors
            foreach(var item in vares) {
                row = result.NewRow();
                row[0] = item.Name;
                row[1] = item.Show_1_Position;
                row[2] = item.Show_2_Position;
                result.Rows.Add(row);
            }

            // and do it again but for staff
            foreach(var item in staffres) {
                row = result.NewRow();
                row[0] = item.Name;
                row[1] = item.Show_1_Position;
                row[2] = item.Show_2_Position;
                result.Rows.Add(row);
            }

            // and do it again but for info
            foreach(var item in infores) {
                row = result.NewRow();
                row[0] = item.Name;
                row[1] = item.Show_1_Position;
                row[2] = item.Show_2_Position;
                result.Rows.Add(row);
            }

            // show it
            dgTbl.DataContext = result.DefaultView;
        }

        /// <summary>
        /// Verifies if the URL provided is in the proper format
        /// </summary>
        /// <param name="txt"></param>
        /// <returns></returns>
        private bool textGood(string txt) {

            // Regex:   https://myanimelist.net/anime/\d+/.+
            //
            //
            // Literal: https://myanimelist.net/anime/
            // any decimal 1 to many times:           \d+
            // Literal:                                  /
            // any character 1 to many times:             .+

            return Regex.IsMatch(txt, "https://myanimelist.net/anime/\\d+/.+");
        }

        /// <summary>
        /// Responsive check as to whether or not the URL supplied in the first text box is in an acceptable format, and shows the user whether or not it is
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtOne_TextChanged(object sender, RoutedEventArgs e) {
            if (chkOne.IsChecked == true) {
                if (textGood(txtOne.Text)) {
                    tbGood1.Text = "\u221A";
                    tbGood1.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FF00"));
                } else {
                    tbGood1.Text = "X";
                    tbGood1.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0000"));
                }
                if (textGood(txtOne.Text)) {
                    btnGo.IsEnabled = true;
                } else {
                    btnGo.IsEnabled = false;
                }
            } else {
                if (textGood(txtOne.Text)) {
                    tbGood1.Text = "\u221A";
                    tbGood1.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FF00"));
                } else {
                    tbGood1.Text = "X";
                    tbGood1.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0000"));
                }

                if (textGood(txtTwo.Text) && textGood(txtOne.Text)) {
                    btnGo.IsEnabled = true;
                } else {
                    btnGo.IsEnabled = false;
                }
            }
        }

        /// <summary>
        /// Responsive check as to whether the URL supplied in the second text box is in an acceptable format, and shows the user whether or not it is
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtTwo_TextChanged(object sender, RoutedEventArgs e) {
            tbGood2.Text = "X";
            txtTwo.IsEnabled = true;
            if(textGood(txtTwo.Text)) {
                tbGood2.Text = "\u221A";
                tbGood2.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FF00"));
            } else {
                tbGood2.Text = "X";
                tbGood2.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0000"));
            }

            if(textGood(txtTwo.Text) && textGood(txtOne.Text)) {
                btnGo.IsEnabled = true;
            } else {
                btnGo.IsEnabled = false;
            }
        }

        private void chkOne_Checked(object sender, RoutedEventArgs e) {
            if((sender as CheckBox).IsChecked == true) {
                tbGood2.Text = "";
                txtTwo.IsEnabled = false;
            } else {
                txtTwo_TextChanged(null, null);
            }
        }
    }
}