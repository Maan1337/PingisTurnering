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

            // Show setup dialog before starting tournament
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

        // Plan (pseudocode):
        // 1. Keep existing layout computation for left side (matches).
        // 2. Choose a desired leaderboard width and a small right margin.
        // 3. Compute leaderboardX as right-aligned: ClientSize.Width - leaderboardWidth - rightMargin.
        // 4. Ensure leaderboard does not overlap match panels: if computed leaderboardX is too small,
        //    move it to at least (leftMargin + panelWidth + minimumGap).
        // 5. Use leaderboardX in the rest of the leaderboard code so the leaderboard appears further right
        //    (and will be flush to the right edge unless that would overlap the match panels).
        private void CreateGfx()
        {
            // Remove previously generated controls (identified by Tag) so we don't duplicate on repeated calls
            for (var i = Controls.Count - 1; i >= 0; i--)
            {
                var c = Controls[i];
                if (c?.Tag is string s && s == "GeneratedGfx")
                {
                    Controls.RemoveAt(i);
                }
            }

            if (_rounds.Count == 0) return;

            var round = _rounds[0];

            // Set the form to the requested size (client area)
            ClientSize = new Size(1920, 1200);

            // Layout configuration
            var leftMargin = 10;
            var topMargin = 10;
            var panelWidth = 420;        // keep horizontal usage small
            var pointsWidth = 60;
            var rowHeight = 30;
            var panelPadding = 30; //6
            var panelVerticalSpacing = 32; //12

            var currentY = topMargin;

            for (var mIndex = 0; mIndex < round.Matches.Count; mIndex++)
            {
                var match = round.Matches[mIndex];

                // Panel height for two rows + padding
                var panelHeight = panelPadding * 2 + rowHeight * 2 + 6;

                var panel = new Panel
                {
                    Location = new Point(leftMargin, currentY),
                    Size = new Size(panelWidth, panelHeight),
                    BorderStyle = BorderStyle.FixedSingle,
                    Tag = "GeneratedGfx"
                };

                // Create two rows (player 1 and player 2)
                for (var playerRow = 0; playerRow < 2; playerRow++)
                {
                    var rowY = panelPadding + playerRow * (rowHeight + 4);

                    // Name textbox (takes most of the panel width)
                    var nameBox = new TextBox
                    {
                        Location = new Point(6, rowY),
                        Size = new Size(panelWidth - pointsWidth - 18, rowHeight - 6),
                        Tag = "GeneratedGfx",
                        Text = playerRow == 0 ? (match.Player1?.Name ?? string.Empty) : (match.Player2?.Name ?? string.Empty)
                    };

                    // Capture variables for closure so handlers reference the correct match/playerRow
                    var capturedMatch = match;
                    var capturedPlayerRow = playerRow;

                    // Update the underlying Player.Name when the user edits the text
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

                    // Points textbox (small, to the right of the name)
                    var pointsBox = new TextBox
                    {
                        Location = new Point(panelWidth - pointsWidth - 6, rowY),
                        Size = new Size(pointsWidth, rowHeight - 6),
                        Tag = "GeneratedGfx",
                        Text = playerRow == 0 ? match.PointsPlayer1.ToString() : match.PointsPlayer2.ToString(),
                        TextAlign = HorizontalAlignment.Center
                    };

                    // Update Match points when the user edits the points textbox.
                    // Only update when the text parses to an int; ignore invalid/transient input.
                    pointsBox.TextChanged += (s, e) =>
                    {
                        if (s is TextBox tb)
                        {
                            if (int.TryParse(tb.Text, out var val))
                            {
                                if (capturedPlayerRow == 0)
                                {
                                    capturedMatch.PointsPlayer1 = val;
                                }
                                else
                                {
                                    capturedMatch.PointsPlayer2 = val;
                                }
                            }
                            // If parsing fails, do not modify existing stored points.
                        }
                    };

                    panel.Controls.Add(nameBox);
                    panel.Controls.Add(pointsBox);
                }

                // Optional: add a small label showing "Match X" to clarify grouping (keeps horizontal space minimal)
                var matchLabel = new Label
                {
                    Text = $"Match {mIndex + 1}",
                    AutoSize = true,
                    Location = new Point(8, panelHeight - 28), //-18
                    Tag = "GeneratedGfx"
                };
                panel.Controls.Add(matchLabel);

                Controls.Add(panel);

                currentY += panelHeight + panelVerticalSpacing;
            }

            // Leaderboard area on the right side
            // Use right-alignment but ensure it doesn't overlap the left match panels.
            var leaderboardWidth = 360;
            var rightMargin = 200; // margin from the right edge
            var minimumGapAfterPanels = 40; // minimum gap between panels and leaderboard

            var tentativeLeaderboardX = ClientSize.Width - leaderboardWidth - rightMargin;
            var minLeaderboardX = leftMargin + panelWidth + minimumGapAfterPanels;
            var leaderboardX = tentativeLeaderboardX < minLeaderboardX ? minLeaderboardX : tentativeLeaderboardX;
            var leaderboardTop = topMargin;

            // Fonts for header and items
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

            // Prepare sorted list of players by TotalPoints descending, then name
            var sorted = _players
                .OrderByDescending(p => p.TotalPoints)
                .ThenBy(p => p.Name)
                .ToList();

            // Start listing directly under the header
            var itemsTop = leaderboardTop + headerLabel.Height + 8;
            var nameColumnWidth = leaderboardWidth - 80;
            var pointsColumnWidth = 70;

            for (var i = 0; i < sorted.Count; i++)
            {
                var p = sorted[i];
                var y = itemsTop + i * (rowHeight + 4);

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

            // Add "New round" button at the bottom-left corner
            var newRoundButton = new Button
            {
                Text = "New round",
                Tag = "GeneratedGfx",
                Size = new Size(120, 30),
                Location = new Point(leftMargin, ClientSize.Height - 78)
            };

            newRoundButton.Click += (s, e) =>
            {
                CreateNextRound();
                CreateGfx();
            };

            Controls.Add(newRoundButton);
        }

        private void CreateNextRound()
        {
            // If there are no rounds, nothing to advance from
            if (_rounds.Count == 0) return;

            var prevRound = _rounds[0];

            // Accumulate points from the previous round into each Player.TotalPoints
            foreach (var match in prevRound.Matches)
            {
                if (match.Player1 != null)
                {
                    match.Player1.TotalPoints += match.PointsPlayer1;
                }
                if (match.Player2 != null)
                {
                    match.Player2.TotalPoints += match.PointsPlayer2;
                }
            }

            // Use the master player list so every player participates in the next round
            var participants = new List<Player>(_players);

            // If odd number of participants, add a BYE so pairing is complete
            if (participants.Count % 2 == 1)
            {
                participants.Add(new Player("BYE", 0));
            }

            // Build set of previously played unordered pairs (minId:maxId)
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

            // Try to generate pairings that avoid repeats
            bool TryGeneratePairings(List<Player> list, HashSet<string> played, out List<Match> result)
            {
                result = null!;
                var rnd = new Random();
                const int maxAttempts = 1000;

                for (var attempt = 0; attempt < maxAttempts; attempt++)
                {
                    // Shuffle in-place (Fisher-Yates)
                    for (var i = list.Count - 1; i > 0; i--)
                    {
                        var j = rnd.Next(i + 1);
                        var tmp = list[i];
                        list[i] = list[j];
                        list[j] = tmp;
                    }

                    var ok = true;
                    var matches = new List<Match>();

                    // Greedy pairing with local swaps when encountering a repeat
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

                        // Try to find a later partner that hasn't played p1
                        var found = false;
                        for (var k = i + 2; k < list.Count; k++)
                        {
                            var candidate = list[k];
                            var candidateKey = p1.Id < candidate.Id ? $"{p1.Id}:{candidate.Id}" : $"{candidate.Id}:{p1.Id}";
                            if (!played.Contains(candidateKey))
                            {
                                // Swap candidate into position i+1
                                var tmp = list[i + 1];
                                list[i + 1] = list[k];
                                list[k] = tmp;

                                // Accept the new pair
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

                // Failed to find conflict-free pairings within attempts
                return false;
            }

            // Make a working copy for the pairing attempts
            var working = new List<Player>(participants);
            if (!TryGeneratePairings(working, playedPairs, out var nextMatches))
            {
                // Fallback: sequential pairing if we couldn't avoid repeats
                nextMatches = new List<Match>();
                for (var i = 0; i < participants.Count; i += 2)
                {
                    var p1 = participants[i];
                    var p2 = participants[i + 1];
                    nextMatches.Add(new Match(p1, p2));
                }
            }

            var nextRound = new Round(nextMatches);

            // Insert at front so CreateGfx continues to display index 0 as "current"
            _rounds.Insert(0, nextRound);
        }

        // Plan (pseudocode):
        // 1. Make a local copy of the _players list so we don't modify the original ordering.
        // 2. Shuffle the local copy using Fisher-Yates to produce random pairings.
        // 3. If the number of players is odd, append a dummy "BYE" player so everyone is in a match.
        //    - The BYE player ensures every real player is placed into exactly one match.
        // 4. Iterate the shuffled list two-by-two and create Match objects for each adjacent pair.
        // 5. Create a Round from the list of matches and add it to _rounds.
        // 6. (Invariant) No Player instance from the original list will appear in more than one match
        //    because the shuffle + linear pairing uses each element exactly once.
        private void CreateStartRound()
        {
            // Copy players to avoid mutating the original list
            var players = new List<Player>(_players);

            // Shuffle using Fisher-Yates
            var rnd = new Random();
            for (var i = players.Count - 1; i > 0; i--)
            {
                var j = rnd.Next(i + 1);
                var tmp = players[i];
                players[i] = players[j];
                players[j] = tmp;
            }

            // If odd number of players, add a BYE player so everyone is in a match
            if (players.Count % 2 == 1)
            {
                players.Add(new Player("BYE", 0));
            }

            // Pair sequentially to create matches
            var matches = new List<Match>();
            for (var i = 0; i < players.Count; i += 2)
            {
                var p1 = players[i];
                var p2 = players[i + 1];
                matches.Add(new Match(p1, p2));
            }

            // Create and store the round
            var round = new Round(matches);
            _rounds.Add(round);
        }

        private void CreatePlayers()
        {
            _players.Clear();

            // Use provided initial names if any were entered in the setup dialog.
            if (_initialPlayerNames != null && _initialPlayerNames.Count > 0)
            {
                // Take up to _noOfPlayers names, trim whitespace, ignore empty lines.
                var names = _initialPlayerNames
                    .Select(n => n?.Trim() ?? string.Empty)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Take(_noOfPlayers)
                    .ToList();

                // If fewer names than requested count, fill remaining with numbered defaults.
                for (var i = 0; i < _noOfPlayers; i++)
                {
                    string name;
                    if (i < names.Count)
                    {
                        name = names[i];
                    }
                    else
                    {
                        name = (i + 1).ToString();
                    }
                    _players.Add(new Player(name, 0));
                }
            }
            else
            {
                // Original behavior: create numbered players 1..N
                for (var i = 0; i < _noOfPlayers; i++)
                {
                    _players.Add(new Player((i + 1).ToString(), 0));
                }
            }
        }
    }

    public class Player
    {
        private static int _nextId = 0;

        public int Id { get; }
        public string Name { get; set; }
        public int Rating { get; set; }

        // Accumulated points across rounds
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
        public Round(List<Match> matches)
        {
            Matches = matches;
        }
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

            // Prefill names if provided, otherwise fill with default numbered names
            if (initialNames != null)
            {
                _txtNames.Lines = initialNames.ToArray();
            }
            else
            {
                var lines = new List<string>();
                for (var i = 0; i < initialNumberOfPlayers; i++)
                {
                    lines.Add((i + 1).ToString());
                }
                _txtNames.Lines = lines.ToArray();
            }

            _btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
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
            // Read and trim names
            var rawLines = _txtNames.Lines ?? Array.Empty<string>();
            var names = rawLines.Select(l => l?.Trim() ?? string.Empty)
                                .Where(s => !string.IsNullOrEmpty(s))
                                .ToList();

            NumberOfPlayers = (int)_nudPlayers.Value;

            // If user provided more names than the chosen number, trim extras.
            if (names.Count > NumberOfPlayers)
            {
                names = names.Take(NumberOfPlayers).ToList();
            }

            PlayerNames = names;

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
