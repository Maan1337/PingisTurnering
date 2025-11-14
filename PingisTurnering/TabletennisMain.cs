using static PingisTurnering.TabletennisMain;

namespace PingisTurnering
{
    public partial class TabletennisMain : Form
    {
        private int _noOfPlayers = 14;
        private List<string> _initialPlayerNames = new();
        private List<Round> _rounds = new();
        private List<Player> _players = new();


        // Elimination fields
        private bool _showingElimination = false;
        private List<EliminationBracket> _eliminationBrackets = new();
        private Dictionary<Match, (TextBox name1, TextBox name2, TextBox pts1, TextBox pts2)> _eliminationControls = new();

        public TabletennisMain()
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
            _showingElimination = false;
            _eliminationBrackets.Clear();
            _eliminationControls.Clear();
            CreatePlayers();
            CreateStartRound();
            CreateGfx();
        }
        private void ClearGeneratedGfx()
        {
            for (var i = Controls.Count - 1; i >= 0; i--)
            {
                var c = Controls[i];
                if (c?.Tag is string s && s == "GeneratedGfx")
                    Controls.RemoveAt(i);
            }
        }

        // Pseudocode:
        // In CreateGfx leaderboard loop:
        // - While iterating sorted players, detect index 16 (17th player, zero-based).
        // - Before rendering that player's labels, insert a horizontal separator line control.
        // - Only add the separator if there are more than 16 players (otherwise no line needed).
        // - Separator: Panel (or Label) with small height, full leaderboard width, dark gray color, tagged as GeneratedGfx.
        // - Position: same X as leaderboard, Y just above the 17th player's row (use y - 4 for slight spacing).

        private void CreateGfx()
        {
            if (_showingElimination)
            {
                ShowEliminationBrackets();
                return;
            }

            ClearGeneratedGfx();

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
                        if (s is TextBox tb && int.TryParse(tb.Text, out var val))
                        {
                            if (capturedPlayerRow == 0)
                                capturedMatch.PointsPlayer1 = val;
                            else
                                capturedMatch.PointsPlayer2 = val;
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
            var itemFont = new Font("Segoe UI", 10, FontStyle.Bold);

            var headerLabel = new Label
            {
                Text = "Aktuell ställning",
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
            bool separatorAdded = false;

            for (var i = 0; i < sorted.Count; i++)
            {
                var p = sorted[i];
                var y = itemsTop + i * leaderboardRowSpacing;

                // Insert horizontal separator before player in position 17 (index 16).
                if (!separatorAdded && i == 16)
                {
                    var sep = new Panel
                    {
                        Location = new Point(leaderboardX, y - 4),
                        Size = new Size(leaderboardWidth, 2),
                        BackColor = Color.DarkGray,
                        Tag = "GeneratedGfx"
                    };
                    Controls.Add(sep);
                    separatorAdded = true;
                }

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
                Text = "Ny runda",
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

            var endGameButton = new Button
            {
                Text = "Slutspel",
                Tag = "GeneratedGfx",
                Size = new Size(120, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };

            var endGameButtonX = leftMargin;
            var endGameButtonY = ClientSize.Height - endGameButton.Height - topMargin;
            endGameButton.Location = new Point(endGameButtonX, endGameButtonY);

            endGameButton.Click += (s, e) =>
            {
                BuildEliminationPhase();
                ShowEliminationBrackets();
            };

            Controls.Add(endGameButton);
        }

        private void BuildEliminationPhase()
        {
            _showingElimination = true;
            _eliminationBrackets.Clear();
            _eliminationControls.Clear();

            // Sort players by total points (descending) then name.
            var ordered = _players
                .OrderByDescending(p => p.TotalPoints)
                .ThenBy(p => p.Name)
                .ToList();

            // Need 16 players (two trees of 8). Pad with BYE if fewer.
            while (ordered.Count < 16)
                ordered.Add(new Player("BYE", 0));

            // Split into two groups of 8.
            var leftGroup = ordered.Take(8).ToList();
            var rightGroup = ordered.Skip(8).Take(8).ToList();

            var leftBracket = BuildBracket(leftGroup, "A");
            var rightBracket = BuildBracket(rightGroup, "B");

            AutoAdvanceByes(leftBracket);
            AutoAdvanceByes(rightBracket);

            _eliminationBrackets.Add(leftBracket);
            _eliminationBrackets.Add(rightBracket);

            // Perform initial propagation after BYE auto-advances.
            UpdateEliminationAdvancements(leftBracket);
            UpdateEliminationAdvancements(rightBracket);
        }

        private EliminationBracket BuildBracket(List<Player> players, string name)
        {
            // Quarterfinals: pair sequential (0-1,2-3,4-5,6-7)
            var qMatches = new List<Match>();
            for (int i = 0; i < 8; i += 2)
                qMatches.Add(new Match(players[i], players[i + 1]));

            // Semifinals placeholders
            var sMatches = new List<Match>
            {
                new Match(new Player("",0), new Player("",0)),
                new Match(new Player("",0), new Player("",0))
            };

            // Final placeholder
            var fMatches = new List<Match>
            {
                new Match(new Player("",0), new Player("",0))
            };

            var rounds = new List<List<Match>> { qMatches, sMatches, fMatches };

            var advancement = new Dictionary<Match, (Match target, int slot)>
            {
                { qMatches[0], (sMatches[0], 0) },
                { qMatches[1], (sMatches[0], 1) },
                { qMatches[2], (sMatches[1], 0) },
                { qMatches[3], (sMatches[1], 1) },
                { sMatches[0], (fMatches[0], 0) },
                { sMatches[1], (fMatches[0], 1) }
            };

            return new EliminationBracket(_players, name, rounds, advancement);
        }

        private void AutoAdvanceByes(EliminationBracket bracket)
        {
            foreach (var round in bracket.Rounds)
            {
                foreach (var match in round)
                {
                    bool p1Bye = match.Player1.Name == "BYE";
                    bool p2Bye = match.Player2.Name == "BYE";
                    if (p1Bye ^ p2Bye)
                    {
                        if (p1Bye)
                        {
                            match.PointsPlayer1 = 0;
                            match.PointsPlayer2 = 1;
                        }
                        else
                        {
                            match.PointsPlayer1 = 1;
                            match.PointsPlayer2 = 0;
                        }
                    }
                }
            }
        }

        private void ShowEliminationBrackets()
        {
            ClearGeneratedGfx();
            ClientSize = new Size(1920, 1024);

            var treeWidth = 860;
            var treeSpacing = 80;//40;
            var startX = 20;
            var startY = 20;

            var matchPanelWidth = 240;
            var matchPanelHeight = 110; //70;
            var roundHorizontalSpacing = 250; //200;
            var verticalSpacingQuarter = 70;//30;
            var verticalSpacingSemi = 140;
            var verticalSpacingFinal = 300;

            var headerFont = new Font("Segoe UI", 20, FontStyle.Bold);
            var labelFont = new Font("Segoe UI", 10, FontStyle.Bold);
            var nameFont = new Font("Segoe UI", 10, FontStyle.Regular);

            for (int bIndex = 0; bIndex < _eliminationBrackets.Count; bIndex++)
            {
                var bracket = _eliminationBrackets[bIndex];
                int baseX = startX + bIndex * (treeWidth + treeSpacing);

                var header = new Label
                {
                    Text = $"{bracket.Name} Bracket",
                    Font = headerFont,
                    Location = new Point(baseX, startY),
                    AutoSize = true,
                    Tag = "GeneratedGfx"
                };
                Controls.Add(header);

                // Render rounds: 0=Quarter(4), 1=Semi(2), 2=Final(1)
                for (int r = 0; r < bracket.Rounds.Count; r++)
                {
                    var roundMatches = bracket.Rounds[r];
                    int roundX = baseX + r * roundHorizontalSpacing;
                    int roundY;
                    int spacing;

                    if (r == 0)
                    {
                        roundY = startY + 60;
                        spacing = verticalSpacingQuarter;
                    }
                    else if (r == 1)
                    {
                        roundY = startY + 60 + (matchPanelHeight + verticalSpacingQuarter) - 10;
                        spacing = verticalSpacingSemi;
                    }
                    else
                    {
                        roundY = startY + 60 + (matchPanelHeight + verticalSpacingQuarter) + verticalSpacingSemi - 40;
                        spacing = verticalSpacingFinal;
                    }

                    for (int m = 0; m < roundMatches.Count; m++)
                    {
                        var match = roundMatches[m];
                        int panelY;

                        if (r == 0)
                        {
                            panelY = roundY + m * (matchPanelHeight + spacing);
                        }
                        else if (r == 1)
                        {
                            // Position semis roughly between their source quarter matches
                            if (m == 0)
                                panelY = startY + 60 + (matchPanelHeight / 2);
                            else
                                panelY = startY + 60 + (matchPanelHeight / 2) + (matchPanelHeight + verticalSpacingQuarter) * 2;
                        }
                        else
                        {
                            // Final center
                            panelY = startY + 60 + (matchPanelHeight + verticalSpacingQuarter) + (matchPanelHeight / 2) + verticalSpacingSemi / 2;
                        }

                        var panel = new Panel
                        {
                            Location = new Point(roundX, panelY),
                            Size = new Size(matchPanelWidth, matchPanelHeight),
                            BorderStyle = BorderStyle.FixedSingle,
                            Tag = "GeneratedGfx"
                        };

                        // Two rows for players
                        for (int pr = 0; pr < 2; pr++)
                        {
                            int rowY = 6 + pr * 28;

                            var nameBox = new TextBox
                            {
                                Location = new Point(6, rowY),
                                Size = new Size(matchPanelWidth - 80, 22),
                                Tag = "GeneratedGfx",
                                Font = nameFont,
                                Text = pr == 0 ? (match.Player1?.Name ?? "") : (match.Player2?.Name ?? "")
                            };

                            // No manual name editing for elimination propagation (lock names after assigned)
                            nameBox.ReadOnly = true;

                            var ptsBox = new TextBox
                            {
                                Location = new Point(matchPanelWidth - 66, rowY),
                                Size = new Size(54, 22),
                                Tag = "GeneratedGfx",
                                TextAlign = HorizontalAlignment.Center,
                                Font = nameFont,
                                Text = pr == 0 ? match.PointsPlayer1.ToString() : match.PointsPlayer2.ToString()
                            };

                            var capturedMatch = match;
                            var capturedRow = pr;

                            ptsBox.TextChanged += (s, e) =>
                            {
                                if (s is TextBox tb && int.TryParse(tb.Text, out var val))
                                {
                                    if (capturedRow == 0)
                                        capturedMatch.PointsPlayer1 = val;
                                    else
                                        capturedMatch.PointsPlayer2 = val;

                                    // Update advancements for this bracket
                                    UpdateEliminationAdvancements(bracket);
                                }
                            };

                            panel.Controls.Add(nameBox);
                            panel.Controls.Add(ptsBox);

                            // Store references for later UI updates
                            if (!_eliminationControls.ContainsKey(match))
                            {
                                _eliminationControls[match] = (null!, null!, null!, null!);
                            }

                            var tuple = _eliminationControls[match];
                            if (pr == 0)
                            {
                                tuple.name1 = nameBox;
                                tuple.pts1 = ptsBox;
                            }
                            else
                            {
                                tuple.name2 = nameBox;
                                tuple.pts2 = ptsBox;
                            }
                            _eliminationControls[match] = tuple;
                        }

                        var label = new Label
                        {
                            AutoSize = true,
                            Font = labelFont,
                            Tag = "GeneratedGfx",
                            Location = new Point(6, matchPanelHeight - 28), //18),
                            Text = r switch
                            {
                                0 => $"QF {m + 1}",
                                1 => $"SF {m + 1}",
                                2 => "Final",
                                _ => "Match"
                            }
                        };
                        panel.Controls.Add(label);
                        Controls.Add(panel);
                    }
                }
            }

            // Button to return to round-robin view (optional)
            var backButton = new Button
            {
                Text = "Tillbaka",
                Size = new Size(100, 30),
                Location = new Point(ClientSize.Width - 120, ClientSize.Height - 50),
                Tag = "GeneratedGfx"
            };
            backButton.Click += (s, e) =>
            {
                _showingElimination = false;
                CreateGfx();
            };
            Controls.Add(backButton);
        }

        private void UpdateEliminationAdvancements(EliminationBracket bracket)
        {
            // Propagate winners through advancement map
            foreach (var kvp in bracket.AdvancementMap)
            {
                var src = kvp.Key;
                var (target, slot) = kvp.Value;

                var winner = GetMatchWinnerConsideringByes(src);
                if (winner == null) continue;

                if (slot == 0)
                    target.Player1 = winner;
                else
                    target.Player2 = winner;

                // Update UI text boxes for target match if available
                if (_eliminationControls.TryGetValue(target, out var tuple))
                {
                    if (tuple.name1 != null)
                        tuple.name1.Text = target.Player1?.Name ?? "";
                    if (tuple.name2 != null)
                        tuple.name2.Text = target.Player2?.Name ?? "";
                }
            }

            // Refresh points text boxes (in case of changes)
            foreach (var kvp in _eliminationControls)
            {
                var match = kvp.Key;
                var (n1, n2, p1, p2) = kvp.Value;
                if (n1 != null) n1.Text = match.Player1?.Name ?? "";
                if (n2 != null) n2.Text = match.Player2?.Name ?? "";
                if (p1 != null) p1.Text = match.PointsPlayer1.ToString();
                if (p2 != null) p2.Text = match.PointsPlayer2.ToString();
            }
        }

        private Player? GetMatchWinnerConsideringByes(Match match)
        {
            bool p1Bye = match.Player1?.Name == "BYE";
            bool p2Bye = match.Player2?.Name == "BYE";

            if (p1Bye && !p2Bye) return match.Player2;
            if (p2Bye && !p1Bye) return match.Player1;

            if (match.Player1 == null || match.Player2 == null) return null;

            if (match.PointsPlayer1 == match.PointsPlayer2) return null; // tie -> cannot decide yet

            return match.GetWinner();
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

    public class EliminationBracket
    {
        public string Name { get; }
        public List<Player> Players { get; } = new();
        public List<List<Match>> Rounds { get; }
        public Dictionary<Match, (Match target, int slot)> AdvancementMap { get; }

        public EliminationBracket(
            List<Player> players,
            string name,
            List<List<Match>> rounds,
            Dictionary<Match, (Match target, int slot)> advancement)
        {
            Name = name;
            Rounds = rounds;
            AdvancementMap = advancement;
            Players = players;
        }
    }
}
