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
using System.Security.Cryptography;

namespace Azimuth
{
    public partial class CustomMusic : Form
    {
        public string SelectedFolder;
        private string m_x3root;

        public CustomMusic()
        {
            InitializeComponent();
            btnOk.DialogResult = DialogResult.None;
        }

        public CustomMusic(string existingFolder, string x3root)
        {
            InitializeComponent();
            btnOk.DialogResult = DialogResult.None;
            lblFolderName.Text = existingFolder;
            m_x3root = x3root;
        }


        private void btnSelectFolder_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                if (Directory.Exists(folderBrowserDialog1.SelectedPath))
                {
                    lblFolderName.Text = folderBrowserDialog1.SelectedPath;
                }
                else
                {
                    lblFolderName.Text = "None";
                }
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            string folder = lblFolderName.Text;

            if (!Directory.Exists(folder))
            {
                MessageBox.Show("Custom Music folder does not exist.");
                return;
            }

            try
            {
                string[] sources = Directory.GetFiles(folder, "*.mp3");

                if (sources.Length < 10)
                {
                    MessageBox.Show(
                        "You need at least 10 MP3 files in the custom music folder.");
                    return;
                }

                string destinationFolder = Path.Combine(m_x3root, "soundtrack");
                Directory.CreateDirectory(destinationFolder);

                // Index existing MP3s by numeric ID, not literal filename.
                Dictionary<int, List<string>> existingById =
                    new Dictionary<int, List<string>>();

                foreach (string existing in Directory.GetFiles(destinationFolder, "*.mp3"))
                {
                    if (!int.TryParse(
                        Path.GetFileNameWithoutExtension(existing), out int id))
                        continue;

                    if (!existingById.TryGetValue(id, out List<string> matches))
                    {
                        matches = new List<string>();
                        existingById.Add(id, matches);
                    }

                    matches.Add(existing);
                }

                // Validate everything before copying.
                Dictionary<int, string> sourceById = new Dictionary<int, string>();
                List<KeyValuePair<string, string>> filesToCopy =
                    new List<KeyValuePair<string, string>>();

                foreach (string source in sources)
                {
                    string originalName = Path.GetFileName(source);
                    string numericName = X3Utils.Transmute(
                        Path.GetFileNameWithoutExtension(source));

                    if (!X3Utils.IsSectorMusicTrack(numericName))
                    {
                        throw new InvalidOperationException(
                            $"'{originalName}' maps to reserved or invalid soundtrack ID " +
                            $"'{numericName}'. Only ID 12 and IDs 15+ are permitted.");
                    }

                    int trackId = int.Parse(numericName);

                    // Detect collisions within the selected custom folder.
                    if (sourceById.TryGetValue(trackId, out string previous))
                    {
                        throw new InvalidOperationException(
                            $"'{Path.GetFileName(previous)}' and '{originalName}' both " +
                            $"map to soundtrack ID {trackId}. Rename or remove one file.");
                    }

                    sourceById.Add(trackId, source);
                    string destination = Path.Combine(destinationFolder, numericName + ".mp3");

                    // Detect collisions with existing game music.
                    if (existingById.TryGetValue(trackId, out List<string> matches))
                    {
                        // Reusing the exact same previously imported file is safe.
                        // Numeric aliases or different contents are not.
                        if (matches.Count != 1 || !string.Equals(matches[0], destination, StringComparison.OrdinalIgnoreCase) || !FilesAreIdentical(source, matches[0]))
                        {
                            throw new InvalidOperationException(
                                $"'{originalName}' conflicts with an existing MP3 " +
                                $"for soundtrack ID {trackId}. Existing game audio " +
                                "was not changed.");
                        }

                        continue;
                    }

                    filesToCopy.Add(
                        new KeyValuePair<string, string>(source, destination));
                }

                if (sourceById.Count < 10)
                {
                    MessageBox.Show("Custom Music requires at least 10 unique, usable tracks.");
                    return;
                }

                // Do not overwrite existing files.
                foreach (var file in filesToCopy)
                    File.Copy(file.Key, file.Value, false);

                SelectedFolder = folder;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Custom music import rejected: " + ex.Message,
                    "Custom Music",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private static bool FilesAreIdentical(string first, string second)
        {
            if (new FileInfo(first).Length != new FileInfo(second).Length)
                return false;

            using (var hash = SHA256.Create())
            using (var firstStream = File.OpenRead(first))
            using (var secondStream = File.OpenRead(second))
            {
                byte[] firstHash = hash.ComputeHash(firstStream);
                byte[] secondHash = hash.ComputeHash(secondStream);

                return firstHash.SequenceEqual(secondHash);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
