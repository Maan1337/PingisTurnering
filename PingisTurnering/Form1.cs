namespace PingisTurnering
{
    public partial class Form1 : Form
    {
        private int _noOfPlayers = 14;
        private List<string> _initialPlayerNames = new();
        private List<Round> _rounds = new();
        private List<Player> _players = new();

        public Form1()
        {
            InitializeComponent();

            using var setup = new PlayerSetupForm(_noOfPlayers, _initialPlayerNames);
            var dr = setup.ShowDialog(this);
            if (dr == DialogResult.OK)
            {
                _noOfPlayers = setup.NumberOfPlayers;
                _initialPlayerNames = new List<string>((IEnumerable<string>)setup.PlayerNames ?? Array.Empty<string>());
            }

            StartTournament();
        }

        private void StartTournament()
        {
            CreatePlayers();
            CreateStartRound();
            CreateGfx();
        }

        private void CreateGfx()
        {
            for (var i = Controls.Count - 1; i >= 0; i--)
            {
                var c = Controls[i];
                if (c?.Tag is string s && s == "GeneratedGfx")
                {
                    AutoScroll = true;
                    AutoScrollMinSize = new Size(1920, 1024);
                    Controls.RemoveAt(i);
                }
            }

            if (_rounds.Count == 0) return;

            var round = _rounds[0];
            ClientSize = new Size(1920, 1024);

            var leftMargin = 10;
            var topMargin = 10;
            var panelWidth = 420;
            var pointsWidth = 60;
            var rowHeight = 30;
            var panelPadding = 30;
            var panelVerticalSpacing = 32;
            var columnSpacing = 32;

            var panelHeight = panelPadding * 2 + rowHeight * 2 + 6;
            int maxRowsPerColumn = 4;
            int matchCount = round.Matches.Count;
            int columns = (int)Math.Ceiling(matchCount / (double)maxRowsPerColumn);

            for (var mIndex = 0; mIndex < matchCount; mIndex++)
            {
                var match = round.Matches[mIndex];
                int column = mIndex / maxRowsPerColumn;
                int row = mIndex % maxRowsPerColumn;
                int x = leftMargin + column * (panelWidth + columnSpacing);
                int y = topMargin + row * (panelHeight + panelVerticalSpacing);

                var panel = new Panel
                {
                    Location = new Point(x, y),
                    Size = new Size(panelWidth, panelHeight),
                    BorderStyle = BorderStyle.FixedSingle,
                    Tag = "GeneratedGfx"
                };

                for (var playerRow = 0; playerRow < 2; playerRow++)
                {
                    var rowY = panelPadding + playerRow * (rowHeight + 4);

                    var nameBox = new TextBox
                    {
                        Location = new Point(6, rowY),
                        Size = new Size(panelWidth - pointsWidth - 18, rowHeight - 6),
                        Tag = "GeneratedGfx",
                        Text = playerRow == 0 ? (match.Player1?.Name ?? string.Empty) : (match.Player2?.Name ?? string.Empty)
                    };

                    var capturedMatch = match;
                    var capturedPlayerRow = playerRow;

                    nameBox.TextChanged += (s, e) =>
                    {
                        if (s is TextBox tb)
                        {
                            if (capturedPlayerRow == 0)
                            {
                                if (capturedMatch.Player1 != null)
                                    capturedMatch.Player1.Name = tb.Text;
                            }
                            else
                            {
                                if (capturedMatch.Player2 != null)
                                    capturedMatch.Player2.Name = tb.Text;
                            }
                        }
                    };

                    var pointsBox = new TextBox
                    {
                        Location = new Point(panelWidth - pointsWidth - 6, rowY),
                        Size = new Size(pointsWidth, rowHeight - 6),
                        Tag = "GeneratedGfx",
                        Text = playerRow == 0 ? match.PointsPlayer1.ToString() : match.PointsPlayer2.ToString(),
                        TextAlign = HorizontalAlignment.Center
                    };

                    pointsBox.TextChanged += (s, e) =>
                    {
                        if (s is TextBox tb)
                        {
                            if (int.TryParse(tb.Text, out var val))
                            {
                                if (capturedPlayerRow == 0)
                                    capturedMatch.PointsPlayer1 = val;
                                else
                                    capturedMatch.PointsPlayer2 = val;
                            }
                        }
                    };

                    panel.Controls.Add(nameBox);
                    panel.Controls.Add(pointsBox);
                }

                var matchLabel = new Label
                {
                    Text = $"Match {mIndex + 1}",
                    AutoSize = true,
                    Location = new Point(8, panelHeight - 28),
                    Tag = "GeneratedGfx"
                };
                panel.Controls.Add(matchLabel);

                Controls.Add(panel);
            }

            var leaderboardWidth = 360;
            var rightMargin = 200;
            var minimumGapAfterPanels = 40;

            int rightmostPanelX = leftMargin + (columns) * (panelWidth + columnSpacing) - columnSpacing;
            int tentativeLeaderboardX = rightmostPanelX + minimumGapAfterPanels;
            int maxLeaderboardX = ClientSize.Width - leaderboardWidth - rightMargin;
            int leaderboardX = tentativeLeaderboardX > maxLeaderboardX ? maxLeaderboardX : tentativeLeaderboardX;
            int leaderboardTop = topMargin;

            var headerFont = new Font("Segoe UI", 18, FontStyle.Bold);
            var itemFont = new Font("Segoe UI", 16, FontStyle.Bold);

            var headerLabel = new Label
            {
                Text = "Leaderboard",
                Location = new Point(leaderboardX, leaderboardTop),
                Font = headerFont,
                AutoSize = true,
                Tag = "GeneratedGfx"
            };
            Controls.Add(headerLabel);

            var sorted = _players
                .OrderByDescending(p => p.TotalPoints)
                .ThenBy(p => p.Name)
                .ToList();

            var itemsTop = leaderboardTop + headerLabel.Height + 8;
            var nameColumnWidth = leaderboardWidth - 80;
            var pointsColumnWidth = 70;
            int leaderboardRowSpacing = rowHeight + 9;

            for (var i = 0; i < sorted.Count; i++)
            {
                var p = sorted[i];
                var y = itemsTop + i * leaderboardRowSpacing;

                var nameLabel = new Label
                {
                    Text = p.Name,
                    Location = new Point(leaderboardX, y),
                    Size = new Size(nameColumnWidth, rowHeight),
                    Font = itemFont,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Tag = "GeneratedGfx",
                    AutoEllipsis = true
                };

                var pointsLabel = new Label
                {
                    Text = p.TotalPoints.ToString(),
                    Location = new Point(leaderboardX + leaderboardWidth - pointsColumnWidth - 6, y),
                    Size = new Size(pointsColumnWidth, rowHeight),
                    Font = itemFont,
                    TextAlign = ContentAlignment.MiddleRight,
                    Tag = "GeneratedGfx"
                };

                Controls.Add(nameLabel);
                Controls.Add(pointsLabel);
            }

            var newRoundButton = new Button
            {
                Text = "New round",
                Tag = "GeneratedGfx",
                Size = new Size(120, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };

            newRoundButton.Location = new Point(
                ClientSize.Width - newRoundButton.Width - leftMargin,
                ClientSize.Height - newRoundButton.Height - topMargin
            );

            newRoundButton.Click += (s, e) =>
            {
                CreateNextRound();
                CreateGfx();
            };

            Controls.Add(newRoundButton);
        }

        private void CreateNextRound()
        {
            if (_rounds.Count == 0) return;

            var prevRound = _rounds[0];

            foreach (var match in prevRound.Matches)
            {
                if (match.Player1 != null)
                    match.Player1.TotalPoints += match.PointsPlayer1;
                if (match.Player2 != null)
                    match.Player2.TotalPoints += match.PointsPlayer2;
            }

            var participants = new List<Player>(_players);
            if (participants.Count % 2 == 1)
                participants.Add(new Player("BYE", 0));

            var playedPairs = new HashSet<string>();
            foreach (var round in _rounds)
            {
                foreach (var m in round.Matches)
                {
                    if (m.Player1 == null || m.Player2 == null) continue;
                    var a = m.Player1.Id;
                    var b = m.Player2.Id;
                    var key = a < b ? $"{a}:{b}" : $"{b}:{a}";
                    playedPairs.Add(key);
                }
            }

            bool TryGeneratePairings(List<Player> list, HashSet<string> played, out List<Match> result)
            {
                result = null!;
                var rnd = new Random();
                const int maxAttempts = 1000;

                for (var attempt = 0; attempt < maxAttempts; attempt++)
                {
                    for (var i = list.Count - 1; i > 0; i--)
                    {
                        var j = rnd.Next(i + 1);
                        (list[i], list[j]) = (list[j], list[i]);
                    }

                    var ok = true;
                    var matches = new List<Match>();

                    for (var i = 0; i < list.Count; i += 2)
                    {
                        var p1 = list[i];
                        var p2 = list[i + 1];
                        var key = p1.Id < p2.Id ? $"{p1.Id}:{p2.Id}" : $"{p2.Id}:{p1.Id}";

                        if (!played.Contains(key))
                        {
                            matches.Add(new Match(p1, p2));
                            continue;
                        }

                        var found = false;
                        for (var k = i + 2; k < list.Count; k++)
                        {
                            var candidate = list[k];
                            var candidateKey = p1.Id < candidate.Id ? $"{p1.Id}:{candidate.Id}" : $"{candidate.Id}:{p1.Id}";
                            if (!played.Contains(candidateKey))
                            {
                                (list[i + 1], list[k]) = (list[k], list[i + 1]);
                                matches.Add(new Match(p1, list[i + 1]));
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            ok = false;
                            break;
                        }
                    }

                    if (ok)
                    {
                        result = matches;
                        return true;
                    }
                }

                return false;
            }

            var working = new List<Player>(participants);
            if (!TryGeneratePairings(working, playedPairs, out var nextMatches))
            {
                nextMatches = new List<Match>();
                for (var i = 0; i < participants.Count; i += 2)
                    nextMatches.Add(new Match(participants[i], participants[i + 1]));
            }

            var nextRound = new Round(nextMatches);
            _rounds.Insert(0, nextRound);
        }

        private void CreateStartRound()
        {
            var players = new List<Player>(_players);
            var rnd = new Random();
            for (var i = players.Count - 1; i > 0; i--)
            {
                var j = rnd.Next(i + 1);
                (players[i], players[j]) = (players[j], players[i]);
            }

            if (players.Count % 2 == 1)
                players.Add(new Player("BYE", 0));

            var matches = new List<Match>();
            for (var i = 0; i < players.Count; i += 2)
                matches.Add(new Match(players[i], players[i + 1]));

            _rounds.Add(new Round(matches));
        }

        private void CreatePlayers()
        {
            _players.Clear();

            if (_initialPlayerNames != null && _initialPlayerNames.Count > 0)
            {
                var names = _initialPlayerNames
                    .Select(n => n?.Trim() ?? string.Empty)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Take(_noOfPlayers)
                    .ToList();

                for (var i = 0; i < _noOfPlayers; i++)
                {
                    var name = i < names.Count ? names[i] : (i + 1).ToString();
                    _players.Add(new Player(name, 0));
                }
            }
            else
            {
                for (var i = 0; i < _noOfPlayers; i++)
                    _players.Add(new Player((i + 1).ToString(), 0));
            }
        }
    }

    public class Player
    {
        private static int _nextId = 0;
        public int Id { get; }
        public string Name { get; set; }
        public int Rating { get; set; }
        public int TotalPoints { get; set; }

        public Player(string name, int rating)
        {
            Id = System.Threading.Interlocked.Increment(ref _nextId);
            Name = name;
            Rating = rating;
            TotalPoints = 0;
        }
    }

    public class Match
    {
        public Player Player1 { get; set; }
        public Player Player2 { get; set; }

        public int PointsPlayer1 { get; set; }

        public int PointsPlayer2 { get; set; }
        public Match(Player player1, Player player2)
        {
            Player1 = player1;
            Player2 = player2;
            PointsPlayer1 = 0;
            PointsPlayer2 = 0;
        }

        public Player GetWinner()
        {
            return PointsPlayer1 > PointsPlayer2 ? Player1 : Player2;
        }
    }

    public class Round
    {
        public List<Match> Matches { get; set; }
        public Round(List<Match> matches) => Matches = matches;
    }
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
                // Ensure the form does NOT close after validation failure.
                _btnOk.DialogResult = DialogResult.None;
                this.DialogResult = DialogResult.None;

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

            // Set DialogResult only on success to close the form.
            this.DialogResult = DialogResult.OK;
            Close();
        }
    }
}
