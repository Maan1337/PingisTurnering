// Plan (pseudocode):
// 1. Extract PlayerSetupForm from TabletennisMain.cs into its own file to reduce clutter.
// 2. Preserve namespace PingisTurnering and all existing behavior.
// 3. Keep constructor, validation logic, private controls, public properties exactly the same.
// 4. Ensure required using directives are present (WinForms + collections + LINQ).
// 5. No functional changes; purely structural refactor.
// 6. After adding this file, remove the PlayerSetupForm class definition from TabletennisMain.cs.
//
// Usage after refactor:
// TabletennisMain will still instantiate: new PlayerSetupForm(_noOfPlayers, _initialPlayerNames);
// No further changes required.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PingisTurnering
{
    // Simple runtime-built form to configure number of players and provide a list of names (one per line).
    public class PlayerSetupForm : Form
    {
        private readonly NumericUpDown _nudPlayers;
        private readonly TextBox _txtNames;
        private readonly Button _btnOk;
        private readonly Button _btnCancel;

        public int NumberOfPlayers { get; private set; }
        public List<string> PlayerNames { get; private set; } = new();

        public PlayerSetupForm(int initialNumberOfPlayers = 14, IEnumerable<string>? initialNames = null)
        {
            Text = "Player Setup";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(480, 420);
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;

            var lblCount = new Label
            {
                Text = "Number of players:",
                Location = new Point(12, 14),
                AutoSize = true
            };
            Controls.Add(lblCount);

            _nudPlayers = new NumericUpDown
            {
                Location = new Point(150, 10),
                Size = new Size(80, 24),
                Minimum = 2,
                Maximum = 256,
                Value = Math.Max(2, Math.Min(256, initialNumberOfPlayers))
            };
            Controls.Add(_nudPlayers);

            var lblNames = new Label
            {
                Text = "Player names (one per line):",
                Location = new Point(12, 48),
                AutoSize = true
            };
            Controls.Add(lblNames);

            _txtNames = new TextBox
            {
                Location = new Point(12, 72),
                Size = new Size(ClientSize.Width - 24, 280),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                AcceptsReturn = true,
                WordWrap = false
            };
            Controls.Add(_txtNames);

            if (initialNames != null)
                _txtNames.Lines = initialNames.ToArray();
            else
            {
                var lines = new List<string>();
                for (var i = 0; i < initialNumberOfPlayers; i++)
                    lines.Add((i + 1).ToString());
                _txtNames.Lines = lines.ToArray();
            }

            _btnOk = new Button
            {
                Text = "OK",
                // IMPORTANT: Do NOT set DialogResult here to allow validation to keep form open.
                Size = new Size(100, 30),
                Location = new Point(ClientSize.Width - 220, ClientSize.Height - 44)
            };
            _btnOk.Click += BtnOk_Click;
            Controls.Add(_btnOk);

            _btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Size = new Size(100, 30),
                Location = new Point(ClientSize.Width - 110, ClientSize.Height - 44)
            };
            Controls.Add(_btnCancel);

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            var rawLines = _txtNames.Lines ?? Array.Empty<string>();
            var names = rawLines
                .Select(l => l?.Trim() ?? string.Empty)
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

            var requestedPlayers = (int)_nudPlayers.Value;

            if (names.Count != requestedPlayers)
            {
                _btnOk.DialogResult = DialogResult.None;
                DialogResult = DialogResult.None;

                MessageBox.Show(
                    this,
                    $"The number of names entered ({names.Count}) must match the number of players ({requestedPlayers}).\nAdjust the player count or the list of names before continuing.",
                    "Invalid player configuration",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return;
            }

            NumberOfPlayers = requestedPlayers;
            PlayerNames = names;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
