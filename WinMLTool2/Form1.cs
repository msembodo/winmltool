using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using nwitsml;

namespace WinMLTool2
{
    public partial class Form1 : Form
    {     
        static Capabilities clientCapabilities;
        static WitsmlServer svr;
        static List<WitsmlWell> wells;
        static WitsmlWell selectedWell;
        static List<WitsmlWellbore> wellbores;
        static WitsmlWellbore selectedWellbore;
        static List<WitsmlLog> logs;
        static WitsmlLog selectedLog;
        static List<WitsmlLogCurve> curves;
        static int step;
        static WitsmlLogCurve iv;
        private int numberOfRows;

        private bool CertValidation(Object sender, X509Certificate certificate, X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            bool result = true;
            return result;
        }

        public Form1()
        {
            InitializeComponent();

            WitsmlServer.UOM = UomBase.Imperial;

            clientCapabilities = new Capabilities(WitsmlVersion.VERSION_1_3_1,
                "Martyono Sembodo",
                "martyono.sembodo@gmail.com",
                "0818139678",
                "WinMLTool",
                "WITSML utility",
                "MS",
                "1.0.1");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Form source = (Form)sender;
            source.Text = "WinMLTool";

            LoadlogdataToolStripMenuItem.Enabled = false;
            toCSVToolStripMenuItem.Enabled = false;
            unitToolStripMenuItem.Enabled = false;

            toolStripStatusLabelWellPath.Text = "Provide URL, username, and password";

            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CertValidation);

            ReloadHistoryFromFile("history.txt");
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            listViewWellbore.Columns.Clear();
            listViewWellbore.Items.Clear();
            listViewDetail.Columns.Clear();
            listViewDetail.Items.Clear();
            listViewLogData.Columns.Clear();
            listViewLogData.Items.Clear();
            listViewLogData.GridLines = false;
            LoadlogdataToolStripMenuItem.Enabled = false;
            toCSVToolStripMenuItem.Enabled = false;
            toolStripStatusLabelWellPath.Text = String.Empty;

            step = 0;

            Cursor.Current = Cursors.WaitCursor;

            try
            {
                svr = new WitsmlServer(comboBoxURL.Text, textBoxUsername.Text, textBoxPassword.Text,
                    WitsmlVersion.VERSION_1_3_1, clientCapabilities);

                wells = svr.get<WitsmlWell>(new WitsmlQuery());

                CreateWellColumns();
                ShowItemsOfWells();

                toolStripStatusLabelWellPath.Text = "Double click on well to see wellbores";

                if (!comboBoxURL.Items.Contains(comboBoxURL.Text))
                    comboBoxURL.Items.Add(comboBoxURL.Text);

                SaveHistoryInFile("history.txt");
            }

            catch (WebException)
            {
                MessageBox.Show("Username and password do not match. Please check your URL and credentials again.",
                    "Login error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

            catch (UriFormatException)
            {
                MessageBox.Show("URI format could not be determined. Please input complete URL; for example: http://serveraddress/wmls/wmls.asmx",
                    "URL not valid", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        private void CreateWellColumns()
        {
            listViewWellbore.View = View.Details;

            ColumnHeader wellHeader = new ColumnHeader();
            wellHeader.Text = "Wells";
            wellHeader.Width = 100;
            listViewWellbore.Columns.Add(wellHeader);

            ColumnHeader wellIdHeader = new ColumnHeader();
            wellIdHeader.Text = "Well ID";
            wellIdHeader.Width = 150;
            listViewWellbore.Columns.Add(wellIdHeader);
        }

        private void ShowItemsOfWells()
        {
            listViewWellbore.Items.Clear();
            listViewWellbore.BeginUpdate();

            foreach (var well in wells)
            {
                ListViewItem item = new ListViewItem();
                item.Text = well.getName();

                ListViewItem.ListViewSubItem subitem = new ListViewItem.ListViewSubItem();
                subitem.Text = well.getId();
                item.SubItems.Add(subitem);

                listViewWellbore.Items.Add(item);
            }

            listViewWellbore.EndUpdate();
        }

        private void listViewWellbore_DoubleClick(object sender, EventArgs e)
        {
            if (step == 0)
            {
                Cursor.Current = Cursors.WaitCursor;
                try
                {
                    CreateWellDetailColumns();
                    ShowDetailsOfWell();

                    wellbores = svr.get<WitsmlWellbore>(new WitsmlQuery(), selectedWell);

                    CreateWellboreColumns();
                    ShowItemsOfWellbores();
                }
                finally
                {
                    Cursor.Current = Cursors.Default;
                }

                step++;
                toolStripStatusLabelWellPath.Text = "Double click on wellbore to see logs";
            }
            else if (step == 1)
            {
                Cursor.Current = Cursors.WaitCursor;
                try
                {
                    CreateWellboreDetailColumns();
                    ShowDetailsOfWellbore();

                    logs = svr.get<WitsmlLog>(new WitsmlQuery(), selectedWellbore);

                    CreateLogColumns();
                    ShowItemsOfLogs();
                }
                finally
                {
                    Cursor.Current = Cursors.Default;
                }

                step++;
                toolStripStatusLabelWellPath.Text = "Double click on log to see parameters";
            }

            else if (step == 2)
            {
                Cursor.Current = Cursors.WaitCursor;
                try
                {
                    CreateLogDetailColumns();
                    ShowDetailsOfLog();

                    curves = selectedLog.getCurves();
                    iv = selectedLog.getIndexCurve();
                    numberOfRows = iv.getNValues();

                    CreateCurveColumns();
                    ShowItemsOfCurves();
                }
                finally
                {
                    Cursor.Current = Cursors.Default;
                }

                step++;
                toolStripStatusLabelWellPath.Text = "'File > Load data' to view";
            }
        }

        private void CreateWellDetailColumns()
        {
            listViewDetail.View = View.Details;
            listViewDetail.Columns.Clear();

            ColumnHeader wellDetailProperty = new ColumnHeader();
            wellDetailProperty.Text = "Well details";
            wellDetailProperty.Width = 100;
            listViewDetail.Columns.Add(wellDetailProperty);

            ColumnHeader wellDetailValue = new ColumnHeader();
            wellDetailValue.Text = "";
            wellDetailValue.Width = 250;
            listViewDetail.Columns.Add(wellDetailValue);
        }

        private void ShowDetailsOfWell()
        {
            listViewDetail.Items.Clear();
            listViewDetail.BeginUpdate();

            foreach (var well in wells)
            {
                if (well.getId() == listViewWellbore.SelectedItems[0].SubItems[1].Text)
                {
                    selectedWell = well;

                    ListViewItem item = new ListViewItem();
                    item.Text = "Name";

                    ListViewItem.ListViewSubItem subitem = new ListViewItem.ListViewSubItem();
                    subitem.Text = well.getName();
                    item.SubItems.Add(subitem);

                    listViewDetail.Items.Add(item);

                    item = new ListViewItem();
                    item.Text = "Country";

                    subitem = new ListViewItem.ListViewSubItem();
                    subitem.Text = well.getCountry();
                    item.SubItems.Add(subitem);

                    listViewDetail.Items.Add(item);

                    item = new ListViewItem();
                    item.Text = "Field";

                    subitem = new ListViewItem.ListViewSubItem();
                    subitem.Text = well.getField();
                    item.SubItems.Add(subitem);

                    listViewDetail.Items.Add(item);

                    item = new ListViewItem();
                    item.Text = "County";

                    subitem = new ListViewItem.ListViewSubItem();
                    subitem.Text = well.getCounty();
                    item.SubItems.Add(subitem);

                    listViewDetail.Items.Add(item);

                    item = new ListViewItem();
                    item.Text = "State";

                    subitem = new ListViewItem.ListViewSubItem();
                    subitem.Text = well.getState();
                    item.SubItems.Add(subitem);

                    listViewDetail.Items.Add(item);

                    item = new ListViewItem();
                    item.Text = "Time zone";

                    subitem = new ListViewItem.ListViewSubItem();
                    subitem.Text = well.getTimeZone();
                    item.SubItems.Add(subitem);

                    listViewDetail.Items.Add(item);

                    item = new ListViewItem();
                    item.Text = "Operator";

                    subitem = new ListViewItem.ListViewSubItem();
                    subitem.Text = well.getOperator();
                    item.SubItems.Add(subitem);

                    listViewDetail.Items.Add(item);

                    item = new ListViewItem();
                    item.Text = "Ground elevation";

                    subitem = new ListViewItem.ListViewSubItem();
                    subitem.Text = well.getGroundElevation().ToString();
                    item.SubItems.Add(subitem);

                    listViewDetail.Items.Add(item);

                    item = new ListViewItem();
                    item.Text = "Water depth";

                    subitem = new ListViewItem.ListViewSubItem();
                    subitem.Text = well.getWaterDepth().ToString();
                    item.SubItems.Add(subitem);

                    listViewDetail.Items.Add(item);

                    item = new ListViewItem();
                    item.Text = "Time of spud";

                    subitem = new ListViewItem.ListViewSubItem();
                    subitem.Text = well.getSpudTime().ToString();
                    item.SubItems.Add(subitem);

                    listViewDetail.Items.Add(item);
                }
            }

            listViewDetail.EndUpdate();
        }

        private void CreateWellboreColumns()
        {
            listViewWellbore.Columns.Clear();
            listViewWellbore.View = View.Details;

            ColumnHeader wellboreHeader = new ColumnHeader();
            wellboreHeader.Text = "Wellbores";
            wellboreHeader.Width = 100;
            listViewWellbore.Columns.Add(wellboreHeader);

            ColumnHeader wellboreIdHeader = new ColumnHeader();
            wellboreIdHeader.Text = "Wellbore ID";
            wellboreIdHeader.Width = 150;
            listViewWellbore.Columns.Add(wellboreIdHeader);
        }

        private void ShowItemsOfWellbores()
        {
            listViewWellbore.Items.Clear();
            listViewWellbore.BeginUpdate();

            foreach (var wellbore in wellbores)
            {
                ListViewItem item = new ListViewItem();
                item.Text = wellbore.getName();

                ListViewItem.ListViewSubItem subitem = new ListViewItem.ListViewSubItem();
                subitem.Text = wellbore.getId();
                item.SubItems.Add(subitem);

                listViewWellbore.Items.Add(item);
            }

            listViewWellbore.EndUpdate();
        }

        private void CreateWellboreDetailColumns()
        {
            listViewDetail.View = View.Details;
            listViewDetail.Columns.Clear();

            ColumnHeader wellboreDetailProperty = new ColumnHeader();
            wellboreDetailProperty.Text = "Wellbore details";
            wellboreDetailProperty.Width = 100;
            listViewDetail.Columns.Add(wellboreDetailProperty);

            ColumnHeader wellboreDetailValue = new ColumnHeader();
            wellboreDetailValue.Text = "";
            wellboreDetailValue.Width = 150;
            listViewDetail.Columns.Add(wellboreDetailValue);
        }

        private void ShowDetailsOfWellbore()
        {
            listViewDetail.Items.Clear();
            listViewDetail.BeginUpdate();

            foreach (var wellbore in wellbores)
            {
                if (wellbore.getId() == listViewWellbore.SelectedItems[0].SubItems[1].Text)
                {
                    selectedWellbore = wellbore;

                    ListViewItem item = new ListViewItem();
                    item.Text = "Name";

                    ListViewItem.ListViewSubItem subitem = new ListViewItem.ListViewSubItem();
                    subitem.Text = wellbore.getName();
                    item.SubItems.Add(subitem);

                    listViewDetail.Items.Add(item);

                    item = new ListViewItem();
                    item.Text = "Kickoff time";

                    subitem = new ListViewItem.ListViewSubItem();
                    subitem.Text = wellbore.getKickoffTime().ToString();
                    item.SubItems.Add(subitem);

                    listViewDetail.Items.Add(item);
                }
            }

            listViewDetail.EndUpdate();
        }

        private void CreateLogColumns()
        {
            listViewWellbore.Columns.Clear();
            listViewWellbore.View = View.Details;

            ColumnHeader logHeader = new ColumnHeader();
            logHeader.Text = "Logs";
            logHeader.Width = 150;
            listViewWellbore.Columns.Add(logHeader);

            ColumnHeader logIdHeader = new ColumnHeader();
            logIdHeader.Text = "Log ID";
            logIdHeader.Width = 250;
            listViewWellbore.Columns.Add(logIdHeader);
        }

        private void ShowItemsOfLogs()
        {
            listViewWellbore.Items.Clear();
            listViewWellbore.BeginUpdate();

            foreach (var log in logs)
            {
                ListViewItem item = new ListViewItem();
                item.Text = log.getName();

                ListViewItem.ListViewSubItem subitem = new ListViewItem.ListViewSubItem();
                subitem.Text = log.getId();
                item.SubItems.Add(subitem);

                listViewWellbore.Items.Add(item);
            }

            listViewWellbore.EndUpdate();
        }

        private void CreateLogDetailColumns()
        {
            listViewDetail.View = View.Details;
            listViewDetail.Columns.Clear();

            ColumnHeader logDetailProperty = new ColumnHeader();
            logDetailProperty.Text = "Log details";
            logDetailProperty.Width = 150;
            listViewDetail.Columns.Add(logDetailProperty);

            ColumnHeader logDetailValue = new ColumnHeader();
            logDetailValue.Text = "";
            logDetailValue.Width = 150;
            listViewDetail.Columns.Add(logDetailValue);
        }

        private void ShowDetailsOfLog()
        {
            listViewDetail.Items.Clear();
            listViewDetail.BeginUpdate();

            foreach (var log in logs)
            {
                if (log.getId() == listViewWellbore.SelectedItems[0].SubItems[1].Text)
                {
                    selectedLog = log;

                    ListViewItem item = new ListViewItem();
                    item.Text = "Name";

                    ListViewItem.ListViewSubItem subitem = new ListViewItem.ListViewSubItem();
                    subitem.Text = log.getName();
                    item.SubItems.Add(subitem);

                    listViewDetail.Items.Add(item);

                    item = new ListViewItem();
                    item.Text = "Well";

                    subitem = new ListViewItem.ListViewSubItem();
                    subitem.Text = log.getParent().getParent().getName();
                    item.SubItems.Add(subitem);

                    listViewDetail.Items.Add(item);

                    item = new ListViewItem();
                    item.Text = "Wellbore";

                    subitem = new ListViewItem.ListViewSubItem();
                    subitem.Text = log.getParent().getName();
                    item.SubItems.Add(subitem);

                    listViewDetail.Items.Add(item);

                    item = new ListViewItem();
                    item.Text = "Service company";

                    subitem = new ListViewItem.ListViewSubItem();
                    subitem.Text = log.getServiceCompany();
                    item.SubItems.Add(subitem);

                    listViewDetail.Items.Add(item);

                    item = new ListViewItem();
                    item.Text = "Index type";

                    subitem = new ListViewItem.ListViewSubItem();
                    subitem.Text = log.getIndexType();
                    item.SubItems.Add(subitem);

                    listViewDetail.Items.Add(item);

                    item = new ListViewItem();
                    item.Text = "Independent variable";

                    subitem = new ListViewItem.ListViewSubItem();
                    subitem.Text = log.getIndexCurve().getNameAndUnit();
                    item.SubItems.Add(subitem);

                    listViewDetail.Items.Add(item);

                    item = new ListViewItem();
                    item.Text = "Start";

                    subitem = new ListViewItem.ListViewSubItem();
                    subitem.Text = log.getStartIndex().ToString();
                    item.SubItems.Add(subitem);

                    listViewDetail.Items.Add(item);

                    item = new ListViewItem();
                    item.Text = "End";

                    subitem = new ListViewItem.ListViewSubItem();
                    subitem.Text = log.getEndIndex().ToString();
                    item.SubItems.Add(subitem);

                    listViewDetail.Items.Add(item);
                }
            }

            listViewDetail.EndUpdate();
        }

        private void listViewWellbore_Click(object sender, EventArgs e)
        {
            // for preview when item is selected.
            if (step == 0)
            {
                Cursor.Current = Cursors.WaitCursor;
                try
                {
                    CreateWellDetailColumns();
                    ShowDetailsOfWell();
                }
                finally
                {
                    Cursor.Current = Cursors.Default;
                }
            }

            else if (step == 1)
            {
                Cursor.Current = Cursors.WaitCursor;
                try
                {
                    CreateWellboreDetailColumns();
                    ShowDetailsOfWellbore();
                }
                finally
                {
                    Cursor.Current = Cursors.Default;
                }
            }

            else if (step == 2)
            {
                Cursor.Current = Cursors.WaitCursor;
                try
                {
                    CreateLogDetailColumns();
                    ShowDetailsOfLog();
                }
                finally
                {
                    Cursor.Current = Cursors.Default;
                }
            }

            else if (step == 3)
            {
                Cursor.Current = Cursors.WaitCursor;
                try
                {
                    CreateCurveDetailColumns();
                    ShowDetailsOfCurve();
                }
                finally
                {
                    Cursor.Current = Cursors.Default;
                }
            }
        }

        private void CreateCurveColumns()
        {
            listViewWellbore.Columns.Clear();
            listViewWellbore.View = View.Details;

            ColumnHeader curveHeader = new ColumnHeader();
            curveHeader.Text = "Curves";
            curveHeader.Width = 100;
            listViewWellbore.Columns.Add(curveHeader);

            ColumnHeader quantityHeader = new ColumnHeader();
            quantityHeader.Text = "Quantity";
            quantityHeader.Width = 100;
            listViewWellbore.Columns.Add(quantityHeader);

            ColumnHeader unitHeader = new ColumnHeader();
            unitHeader.Text = "Unit";
            unitHeader.Width = 60;
            listViewWellbore.Columns.Add(unitHeader);

            ColumnHeader curveIdHeader = new ColumnHeader();
            curveIdHeader.Text = "Curve ID";
            curveIdHeader.Width = 40;
            listViewWellbore.Columns.Add(curveIdHeader);
        }

        private void ShowItemsOfCurves()
        {
            listViewWellbore.Items.Clear();
            listViewWellbore.BeginUpdate();

            foreach (var curve in curves)
            {
                ListViewItem item = new ListViewItem();
                item.Text = curve.getName();

                ListViewItem.ListViewSubItem subitem = new ListViewItem.ListViewSubItem();
                subitem.Text = curve.getQuantity();
                item.SubItems.Add(subitem);

                subitem = new ListViewItem.ListViewSubItem();
                subitem.Text = curve.getUnit();
                item.SubItems.Add(subitem);

                subitem = new ListViewItem.ListViewSubItem();
                subitem.Text = curve.getId();
                item.SubItems.Add(subitem);

                listViewWellbore.Items.Add(item);
            }

            listViewWellbore.EndUpdate();

            LoadlogdataToolStripMenuItem.Enabled = true;
        }

        private void CreateCurveDetailColumns()
        {
            listViewDetail.View = View.Details;
            listViewDetail.Columns.Clear();

            ColumnHeader curveDetailProperty = new ColumnHeader();
            curveDetailProperty.Text = "Curve details";
            curveDetailProperty.Width = 150;
            listViewDetail.Columns.Add(curveDetailProperty);

            ColumnHeader curveDetailValue = new ColumnHeader();
            curveDetailValue.Text = "";
            curveDetailValue.Width = 150;
            listViewDetail.Columns.Add(curveDetailValue);
        }

        private void ShowDetailsOfCurve()
        {
            listViewDetail.Items.Clear();
            listViewDetail.BeginUpdate();

            foreach (var curve in curves)
            {
                if (curve.getId() == listViewWellbore.SelectedItems[0].SubItems[3].Text)
                {
                    ListViewItem item = new ListViewItem();
                    item.Text = "Name / Mnemonic";

                    ListViewItem.ListViewSubItem subitem = new ListViewItem.ListViewSubItem();
                    subitem.Text = curve.getName() + " / " + curve.getMnemonic();
                    item.SubItems.Add(subitem);

                    listViewDetail.Items.Add(item);

                    item = new ListViewItem();
                    item.Text = "Quantity";

                    subitem = new ListViewItem.ListViewSubItem();
                    subitem.Text = curve.getQuantity();
                    item.SubItems.Add(subitem);

                    listViewDetail.Items.Add(item);

                    item = new ListViewItem();
                    item.Text = "Unit";

                    subitem = new ListViewItem.ListViewSubItem();
                    subitem.Text = curve.getUnit();
                    item.SubItems.Add(subitem);

                    listViewDetail.Items.Add(item);

                    item = new ListViewItem();
                    item.Text = "Minimum";

                    subitem = new ListViewItem.ListViewSubItem();
                    subitem.Text = curve.getStartIndex().ToString();
                    item.SubItems.Add(subitem);

                    listViewDetail.Items.Add(item);

                    item = new ListViewItem();
                    item.Text = "Maximum";

                    subitem = new ListViewItem.ListViewSubItem();
                    subitem.Text = curve.getEndIndex().ToString();
                    item.SubItems.Add(subitem);

                    listViewDetail.Items.Add(item);
                }
            }

            listViewDetail.EndUpdate();
        }

        private void LoadlogdataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;

            try
            {
                CreateLogDataColumns();
                ShowLogData();

                LoadlogdataToolStripMenuItem.Enabled = false;
                toCSVToolStripMenuItem.Enabled = true;
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }

            toolStripStatusLabelWellPath.Text = "'File > Save > to .csv' to save";
        }

        private void CreateLogDataColumns()
        {
            listViewLogData.View = View.Details;
            listViewLogData.Columns.Clear();

            ColumnHeader colHeader;

            foreach (var curve in curves)
            {
                colHeader = new ColumnHeader();
                colHeader.Text = curve.getMnemonic() + " (" + curve.getUnit() + ")";
                colHeader.Width = 100;
                listViewLogData.Columns.Add(colHeader);
            }
        }

        private void ShowLogData()
        {
            listViewLogData.Items.Clear();
            listViewLogData.GridLines = true;

            listViewLogData.BeginUpdate();

            string[] cell = new string[selectedLog.getNCurves()];
            ListViewItem item;

            for (int row = 0; row < numberOfRows; row++)
            {
                for (int col = 0; col < selectedLog.getNCurves(); col++)
                {
                    if (curves[col].getValue(row) == null)
                        cell[col] = "";
                    else
                        cell[col] = curves[col].getValue(row).ToString();
                }
                
                item = new ListViewItem(cell);
                listViewLogData.Items.Add(item);
            }

            listViewLogData.EndUpdate();
            
        }

        private void toCSVToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialogToCsv.Filter = "Comma Separated Value|.csv";
            saveFileDialogToCsv.FileName = String.Empty;
            saveFileDialogToCsv.DefaultExt = ".csv";

            DialogResult result = saveFileDialogToCsv.ShowDialog();

            if (result == DialogResult.OK)
            {
                try
                {
                    FileStream fs = new FileStream(saveFileDialogToCsv.FileName, FileMode.Create);
                    StreamWriter outFile = new StreamWriter(fs);

                    using (outFile)
                    {
                        foreach (var curve in curves)
                            outFile.Write("{0},", curve.getMnemonic());

                        outFile.WriteLine();

                        foreach (var curve in curves)
                            outFile.Write("{0},", curve.getUnit());

                        outFile.WriteLine();

                        for (int row = 0; row < numberOfRows; row++)
                        {
                            for (int col = 0; col < selectedLog.getNCurves(); col++)
                                outFile.Write("{0},", curves[col].getValue(row));

                            outFile.WriteLine();
                        }
                    }
                }

                catch (IOException)
                {
                    MessageBox.Show("Another file with same name is currently open.", "File save error",
                        MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox about = new AboutBox();
            about.StartPosition = FormStartPosition.CenterScreen;
            about.Show();
        }

        private void SaveHistoryInFile(string historyFileName)
        {
            try
            {
                using (StreamWriter comboboxsw = new StreamWriter(historyFileName))
                {
                    foreach (var cfgitem in comboBoxURL.Items)
                    {
                        comboboxsw.WriteLine(cfgitem);
                    }
                }
            }

            catch (Exception)
            {
                // handle exception; if any.
            }
        }

        private void ReloadHistoryFromFile(string historyFileName)
        {
            try
            {
                using (StreamReader comboboxsr = new StreamReader(historyFileName))
                {
                    while (!comboboxsr.EndOfStream)
                    {
                        string itemread = comboboxsr.ReadLine();
                        comboBoxURL.Items.Add(itemread);
                    }
                }
            }

            catch (Exception)
            {
                // handle exception; if any.
            }
        }
    }
}
