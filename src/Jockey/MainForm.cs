using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Jockey
{
    public partial class MainForm : Form
    {
        private string _rootFolder;
        private string _appBaseDir;

        public MainForm()
        {
            InitializeComponent();
        }

        private void setStyle( ScintillaNET.Scintilla scintilla )
        {
            // Reset the styles
            scintilla.StyleResetDefault();
            scintilla.Styles[ScintillaNET.Style.Default].Font = "Consolas";
            scintilla.Styles[ScintillaNET.Style.Default].Size = 10;
            scintilla.StyleClearAll();
            
            // Show line numbers
            scintilla.Margins[0].Width = 20;

            // Enable folding
            scintilla.SetProperty("fold", "1");
            scintilla.SetProperty("fold.compact", "1");
            scintilla.SetProperty("fold.html", "1");

            // Use Margin 2 for fold markers
            scintilla.Margins[2].Type = ScintillaNET.MarginType.Symbol;
            scintilla.Margins[2].Mask = ScintillaNET.Marker.MaskFolders;
            scintilla.Margins[2].Sensitive = true;
            scintilla.Margins[2].Width = 20;

            // Reset folder markers
            for(int i = ScintillaNET.Marker.FolderEnd; i <= ScintillaNET.Marker.FolderOpen; i++)
            {
                scintilla.Markers[i].SetForeColor(SystemColors.ControlLightLight);
                scintilla.Markers[i].SetBackColor(SystemColors.ControlDark);
            }

            // Style the folder markers
            scintilla.Markers[ScintillaNET.Marker.Folder].Symbol = ScintillaNET.MarkerSymbol.BoxPlus;
            scintilla.Markers[ScintillaNET.Marker.Folder].SetBackColor(SystemColors.ControlText);
            scintilla.Markers[ScintillaNET.Marker.FolderOpen].Symbol = ScintillaNET.MarkerSymbol.BoxMinus;
            scintilla.Markers[ScintillaNET.Marker.FolderEnd].Symbol = ScintillaNET.MarkerSymbol.BoxPlusConnected;
            scintilla.Markers[ScintillaNET.Marker.FolderEnd].SetBackColor(SystemColors.ControlText);
            scintilla.Markers[ScintillaNET.Marker.FolderMidTail].Symbol = ScintillaNET.MarkerSymbol.TCorner;
            scintilla.Markers[ScintillaNET.Marker.FolderOpenMid].Symbol = ScintillaNET.MarkerSymbol.BoxMinusConnected;
            scintilla.Markers[ScintillaNET.Marker.FolderSub].Symbol = ScintillaNET.MarkerSymbol.VLine;
            scintilla.Markers[ScintillaNET.Marker.FolderTail].Symbol = ScintillaNET.MarkerSymbol.LCorner;

            // Enable automatic folding
            scintilla.AutomaticFold = ScintillaNET.AutomaticFold.Show | ScintillaNET.AutomaticFold.Click | ScintillaNET.AutomaticFold.Change;

            // Set the Styles
            scintilla.StyleResetDefault();
            // I like fixed font for XML
            scintilla.Styles[ScintillaNET.Style.Default].Font = "Courier";
            scintilla.Styles[ScintillaNET.Style.Default].Size = 10;
            scintilla.StyleClearAll();
            scintilla.Styles[ScintillaNET.Style.Xml.Attribute].ForeColor = Color.Red;
            scintilla.Styles[ScintillaNET.Style.Xml.Entity].ForeColor = Color.Red;
            scintilla.Styles[ScintillaNET.Style.Xml.Comment].ForeColor = Color.Green;
            scintilla.Styles[ScintillaNET.Style.Xml.Tag].ForeColor = Color.Blue;
            scintilla.Styles[ScintillaNET.Style.Xml.TagEnd].ForeColor = Color.Blue;
            scintilla.Styles[ScintillaNET.Style.Xml.DoubleString].ForeColor = Color.DeepPink;
            scintilla.Styles[ScintillaNET.Style.Xml.SingleString].ForeColor = Color.DeepPink;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            setStyle(_text);
            _text.Lexer = ScintillaNET.Lexer.Html;
            _appBaseDir = AppDomain.CurrentDomain.BaseDirectory;
            string sf = _appBaseDir + "lastfolder.txt";
            if(File.Exists(sf))
                _rootFolder = File.ReadAllText(sf);
        }

        private void _text_KeyDown(object sender, KeyEventArgs evt)
        {
            if(evt.KeyCode == Keys.F5 || evt.KeyCode == Keys.F6 || evt.KeyCode == Keys.F7)
            {
                // check vincity of selection
                var selections = _text.Selections;
                if(selections == null || selections.Count == 0)
                    return;
                var selection = selections[0];
                int s = selection.Start;
                int e = selection.End;
                var curText = _text.SelectedText;
                if(s > 0)
                {
                    // check if we have " before
                    var ch = _text.GetCharAt(s - 1);
                    if(ch == '"') //if preceding char is " then expand selection
                        s--;
                }
                while(e < _text.TextLength - 1)
                {
                    var ch = _text.GetCharAt(e);
                    if(ch == '.' || ch == ':') //if next char is special then expand selection
                    {
                        e++;
                        curText = curText + (char)ch;
                    }
                    else if(ch == '"')
                        e++;
                    else
                        break;
                }
                selection.Start = s;
                selection.End = e;
                string symbol = (evt.KeyCode == Keys.F5 ? "S" : "R");

                string newText = (evt.KeyCode == Keys.F5 || evt.KeyCode == Keys.F6 ? "@" : "") + symbol + "(\"" + curText + "\")";
                _text.DeleteRange(s, e - s);
                _text.InsertText(s, newText);
                
                evt.Handled = true;
            }
            else if(evt.KeyCode == Keys.F8)
            {
                if(_files.SelectedNode != null)
                {
                    if(_files.SelectedNode.Tag != null && _autoSaveFiles.Checked)
                    {
                        var fp = _files.SelectedNode.Tag as string;
                        if(File.Exists(fp))
                        {
                            File.WriteAllText(fp, _text.Text, Encoding.UTF8);
                        }
                    }
                    var nextNode = _files.SelectedNode.NextVisibleNode;
                    while(nextNode != null)
                    {
                        if(nextNode.Tag != null)
                        {
                            _files.SelectedNode = nextNode;
                            break;
                        }
                        nextNode = nextNode.NextVisibleNode;
                    }
                }
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(_files.SelectedNode != null && _files.SelectedNode != null)
            {
                var fp = _files.SelectedNode.Tag as string;
                if(File.Exists(fp))
                {
                    File.WriteAllText(fp, _text.Text, Encoding.UTF8);
                }
            }
        }

        private void openRootDirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            if(_rootFolder != null)
                dlg.SelectedPath = _rootFolder;
            if(dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _rootFolder = dlg.SelectedPath;
                // clear treeview
                _files.Nodes.Clear();
                // go over all dirs and get cshtml files
                var allFiles = Directory.EnumerateFiles(_rootFolder, "*.cshtml", SearchOption.AllDirectories);
                foreach(var filePath in allFiles)
                {
                    var relativeFilePath = filePath.Substring(_rootFolder.Length + 1);
                    addNode(filePath, relativeFilePath);
                }
                _files.ExpandAll();
                File.WriteAllText(_appBaseDir + "lastfolder.txt", _rootFolder);
            }
        }

        private void addNode(string filePath, string relativeFilePath)
        {
            // split relative file path by separator
            var elems = relativeFilePath.Split(Path.DirectorySeparatorChar);
            // check if treeview has node already
            TreeNodeCollection curNodes = _files.Nodes;
            for(int i = 0; i < elems.Length; ++i)
            {
                var nodeName = elems[i];
                var nodes = curNodes.Find(nodeName, false);
                if(nodes != null && nodes.Length == 1)
                {
                    curNodes = nodes[0].Nodes;
                }
                else
                {
                    // create new node
                    var newNode = curNodes.Add(nodeName, nodeName);
                    if(i == elems.Length - 1)
                        newNode.Tag = filePath; // we are at the end
                    curNodes = newNode.Nodes;
                }
            }
        }


        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _text.Undo();
        }

        private void _files_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if(e.Node == null)
                return;

            var fp = e.Node.Tag as string;
            if(File.Exists(fp))
            {
                _text.Text = File.ReadAllText(fp, Encoding.UTF8);
            }
        }
    }
}
