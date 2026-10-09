using Steamworks;
using Steamworks.Data;

namespace Fade.MonoGame.Steam;

/// <summary>
/// What the leaderboard commands keep between frames.
///
/// Everything that Steam does with a leaderboard takes a while: finding it, sending a
/// score, fetching scores. A Fade program cannot wait on any of it, so a command only
/// starts the work, and the program asks on later frames how it went. Each of the three
/// kinds of work has a state, one of the four numbers below.
///
/// A leaderboard is known to the program by an id that it picks, the way a sprite is.
/// </summary>
internal static class SteamLeaderboards
{
    public const int StateNone = 0;
    public const int StateWorking = 1;
    public const int StateDone = 2;
    public const int StateFailed = 3;

    /// <summary>The most extra numbers that Steam keeps with one score.</summary>
    public const int MaxDetails = 64;

    public const int KindGlobal = 0;
    public const int KindFriends = 1;
    public const int KindNearMe = 2;

    // Steam does not always answer. Fetching scores also waits for the names of the players
    // to arrive, which has no end of its own. After this long, the work has failed.
    const int TimeoutMs = 20000;

    sealed class Row
    {
        public int Rank;
        public int Score;
        public string Name = "";
        public string UserId = "";
        public bool IsMe;
        public bool Checked;
        public int[] Details = Array.Empty<int>();
    }

    sealed class Board
    {
        // the name that Steam knows it by, with the prefix of this build on it
        public string Name = "";

        // which detail of a score on this board holds its check. -1 is none of them.
        public int CheckIndex = -1;
        public Task<Leaderboard?> Find = Task.FromResult<Leaderboard?>(null);
        public int FindState;
        public int Size;

        // A newer request makes the answer to an older one worthless. Each request takes
        // the next generation, and an answer that is not for the latest one is dropped.
        public int SubmitGeneration;
        public int SubmitState;
        public bool SubmitImproved;
        public int SubmitRank;

        public int ScoresGeneration;
        public int ScoresState;
        public Row[] Rows = Array.Empty<Row>();
    }

    // Answers can arrive on another thread: fetching scores waits on a timer in between.
    // So everything that the commands read is read and written under this.
    static readonly object Gate = new();
    static readonly Dictionary<int, Board> Boards = new();

    static readonly int[] StagedDetails = new int[MaxDetails];
    static int _stagedCount;

    public static void Reset()
    {
        lock (Gate)
        {
            Boards.Clear();
            _stagedCount = 0;
        }
    }

    public static int FreeId()
    {
        lock (Gate)
        {
            var id = 1;
            while (Boards.ContainsKey(id)) id++;
            return id;
        }
    }

    public static void Reserve(int id)
    {
        lock (Gate)
        {
            if (!Boards.ContainsKey(id)) Boards[id] = new Board();
        }
    }

    public static void Find(int id, string name, bool create, bool lowestFirst)
    {
        name ??= "";
        var board = new Board { Name = SteamSystem.LeaderboardPrefix + name };
        lock (Gate)
        {
            // finding a leaderboard again keeps what was said about its check
            if (Boards.TryGetValue(id, out var before)) board.CheckIndex = before.CheckIndex;
            Boards[id] = board;
        }

        if (!SteamSystem.Available || name.Length == 0)
        {
            board.FindState = StateFailed;
            return;
        }

        board.FindState = StateWorking;
        board.Find = RunFind(board, create, lowestFirst);
    }

    static async Task<Leaderboard?> RunFind(Board board, bool create, bool lowestFirst)
    {
        try
        {
            var sort = lowestFirst ? LeaderboardSort.Ascending : LeaderboardSort.Descending;
            var found = await WithTimeout(create
                ? SteamUserStats.FindOrCreateLeaderboardAsync(board.Name, sort, LeaderboardDisplay.Numeric)
                : SteamUserStats.FindLeaderboardAsync(board.Name));

            lock (Gate)
            {
                board.FindState = found.HasValue ? StateDone : StateFailed;
                if (found.HasValue) board.Size = found.Value.EntryCount;
            }

            if (!found.HasValue) Console.WriteLine($"[steam] there is no leaderboard named \"{board.Name}\"");
            return found;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[steam] could not find the leaderboard \"{board.Name}\": {ex.Message}");
            lock (Gate) board.FindState = StateFailed;
            return null;
        }
    }

    public static void ClearDetails()
    {
        lock (Gate) _stagedCount = 0;
    }

    public static void SetDetail(int index, int value)
    {
        if (index < 0 || index >= MaxDetails)
        {
            Console.WriteLine($"[steam] a score has details 0 to {MaxDetails - 1}. there is no detail {index}.");
            return;
        }

        lock (Gate)
        {
            // the ones that were skipped over are sent as 0, and not as whatever was left there
            for (var i = _stagedCount; i < index; i++) StagedDetails[i] = 0;
            StagedDetails[index] = value;
            if (index >= _stagedCount) _stagedCount = index + 1;
        }
    }

    public static void SetBits(int bitIndex, int bitCount, int value)
    {
        if (bitCount < 1 || bitCount > 32 || bitIndex < 0 || bitIndex + bitCount > MaxDetails * 32)
        {
            Console.WriteLine($"[steam] cannot set {bitCount} bits at bit {bitIndex}. a score has bits 0 to {MaxDetails * 32 - 1}, and 1 to 32 of them are set at a time.");
            return;
        }

        lock (Gate)
        {
            var last = (bitIndex + bitCount - 1) / 32;
            for (var i = _stagedCount; i <= last; i++) StagedDetails[i] = 0;
            if (last >= _stagedCount) _stagedCount = last + 1;

            for (var i = 0; i < bitCount; i++)
            {
                var at = bitIndex + i;
                var mask = 1 << (at % 32);
                if (((value >> i) & 1) != 0) StagedDetails[at / 32] |= mask;
                else StagedDetails[at / 32] &= ~mask;
            }
        }
    }

    static int ReadBits(int[] details, int bitIndex, int bitCount)
    {
        if (bitCount < 1 || bitCount > 32 || bitIndex < 0) return 0;

        // bits past the end of what the score came with read as 0
        var value = 0;
        for (var i = 0; i < bitCount; i++)
        {
            var at = bitIndex + i;
            if (at / 32 >= details.Length) break;
            if (((details[at / 32] >> (at % 32)) & 1) != 0) value |= 1 << i;
        }
        return value;
    }

    public static void SetCheck(int id, int detailIndex)
    {
        if (detailIndex < -1 || detailIndex >= MaxDetails)
        {
            Console.WriteLine($"[steam] the check of a score goes in a detail from 0 to {MaxDetails - 1}, and not in {detailIndex}.");
            return;
        }

        lock (Gate)
        {
            if (!Boards.TryGetValue(id, out var board)) Boards[id] = board = new Board();
            board.CheckIndex = detailIndex;
        }
    }

    // The check of a score: a keyed hash over everything that says what the score is and whose
    // it is. The name of the leaderboard is in it so that a score cannot be moved to another
    // board, and the player is in it so that it cannot be copied onto another account. The
    // detail that holds the check counts as 0.
    static int ComputeCheck(string boardName, ulong steamId, int score, int[] details, int checkIndex)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(System.Text.Encoding.UTF8.GetBytes(boardName));
            writer.Write((byte)0);
            writer.Write(steamId);
            writer.Write(score);
            writer.Write(details.Length);
            for (var i = 0; i < details.Length; i++) writer.Write(i == checkIndex ? 0 : details[i]);
        }

        using var hmac = new System.Security.Cryptography.HMACSHA256(SteamSystem.ScoreKey);
        return BitConverter.ToInt32(hmac.ComputeHash(stream.ToArray()), 0);
    }

    public static void Submit(int id, int score, bool replace)
    {
        if (!TryGet(id, "submit a score to", out var board)) return;

        int[] details;
        int generation;
        lock (Gate)
        {
            details = StagedDetails.AsSpan(0, _stagedCount).ToArray();
            if (board.CheckIndex >= 0 && SteamSystem.Available)
            {
                if (details.Length <= board.CheckIndex) Array.Resize(ref details, board.CheckIndex + 1);
                details[board.CheckIndex] = ComputeCheck(board.Name, SteamClient.SteamId.Value, score, details, board.CheckIndex);
            }
            generation = ++board.SubmitGeneration;
            board.SubmitState = StateWorking;
            board.SubmitImproved = false;
            board.SubmitRank = 0;
        }

        _ = RunSubmit(board, generation, score, details, replace);
    }

    static async Task RunSubmit(Board board, int generation, int score, int[] details, bool replace)
    {
        LeaderboardUpdate? update = null;
        try
        {
            var found = await board.Find;
            if (found.HasValue)
            {
                update = await WithTimeout(replace
                    ? found.Value.ReplaceScore(score, details)
                    : found.Value.SubmitScoreAsync(score, details));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[steam] could not submit a score to \"{board.Name}\": {ex.Message}");
        }

        lock (Gate)
        {
            if (generation != board.SubmitGeneration) return;
            board.SubmitState = update.HasValue ? StateDone : StateFailed;
            if (update.HasValue)
            {
                board.SubmitImproved = update.Value.Changed;
                board.SubmitRank = update.Value.NewGlobalRank;
            }
        }
    }

    public static void Load(int id, int kind, int a, int b)
    {
        if (!TryGet(id, "load scores from", out var board)) return;

        int generation;
        lock (Gate)
        {
            generation = ++board.ScoresGeneration;
            board.ScoresState = StateWorking;
            board.Rows = Array.Empty<Row>();
        }

        _ = RunLoad(board, generation, kind, a, b);
    }

    static async Task RunLoad(Board board, int generation, int kind, int a, int b)
    {
        Row[]? rows = null;
        var size = 0;
        try
        {
            var found = await board.Find;
            if (found.HasValue)
            {
                var leaderboard = found.Value;
                LeaderboardEntry[]? entries;
                switch (kind)
                {
                    case KindFriends:
                        entries = await WithTimeout(leaderboard.GetScoresFromFriendsAsync());
                        break;
                    case KindNearMe:
                        entries = await WithTimeout(leaderboard.GetScoresAroundUserAsync(-Math.Max(a, 0), Math.Max(b, 0)));
                        break;
                    default:
                        entries = b <= 0
                            ? null
                            : await WithTimeout(leaderboard.GetScoresAsync(b, Math.Max(a, 1)));
                        break;
                }

                int checkIndex;
                lock (Gate) checkIndex = board.CheckIndex;

                // No entries comes back as nothing at all. That is an empty table, not a failure.
                rows = (entries ?? Array.Empty<LeaderboardEntry>())
                    .Where(entry => entry.User.Id.Value != 0)
                    .Select(entry =>
                    {
                        var details = entry.Details ?? Array.Empty<int>();
                        return new Row
                        {
                            Rank = entry.GlobalRank,
                            Score = entry.Score,
                            Name = entry.User.Name ?? "",
                            UserId = entry.User.Id.Value.ToString(),
                            IsMe = entry.User.IsMe,
                            Details = details,
                            Checked = checkIndex >= 0 && checkIndex < details.Length &&
                                      details[checkIndex] == ComputeCheck(board.Name, entry.User.Id.Value, entry.Score, details, checkIndex)
                        };
                    })
                    .ToArray();
                size = leaderboard.EntryCount;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[steam] could not load scores from \"{board.Name}\": {ex.Message}");
            rows = null;
        }

        lock (Gate)
        {
            if (generation != board.ScoresGeneration) return;
            board.ScoresState = rows != null ? StateDone : StateFailed;
            board.Rows = rows ?? Array.Empty<Row>();
            if (rows != null) board.Size = size;
        }
    }

    public static int FindState(int id) => Read(id, board => board.FindState);
    public static int Size(int id) => Read(id, board => board.Size);
    public static int SubmitState(int id) => Read(id, board => board.SubmitState);
    public static int SubmitImproved(int id) => Read(id, board => board.SubmitImproved ? 1 : 0);
    public static int SubmitRank(int id) => Read(id, board => board.SubmitRank);
    public static int ScoresState(int id) => Read(id, board => board.ScoresState);
    public static int RowCount(int id) => Read(id, board => board.Rows.Length);

    public static int RowRank(int id, int row) => ReadRow(id, row, 0, r => r.Rank);
    public static int RowScore(int id, int row) => ReadRow(id, row, 0, r => r.Score);
    public static string RowName(int id, int row) => ReadRow(id, row, "", r => r.Name);
    public static string RowUserId(int id, int row) => ReadRow(id, row, "", r => r.UserId);
    public static int RowIsMe(int id, int row) => ReadRow(id, row, 0, r => r.IsMe ? 1 : 0);
    public static int RowDetailCount(int id, int row) => ReadRow(id, row, 0, r => r.Details.Length);
    public static int RowChecked(int id, int row) => ReadRow(id, row, 0, r => r.Checked ? 1 : 0);
    public static int RowBits(int id, int row, int bitIndex, int bitCount) => ReadRow(id, row, 0, r => ReadBits(r.Details, bitIndex, bitCount));

    // Only the letters, digits and marks that every font has, so that a name can be drawn by a
    // font that does not know every alphabet. Anything else is left out, and a name with
    // nothing left is a question mark.
    public static string RowPlainName(int id, int row, int maxLetters) => ReadRow(id, row, "", r =>
    {
        var plain = new string(r.Name.Where(c => c >= 32 && c < 127).ToArray()).Trim();
        if (plain.Length == 0) plain = "?";
        if (maxLetters > 0 && plain.Length > maxLetters) plain = plain.Substring(0, maxLetters).TrimEnd();
        return plain;
    });

    public static int RowDetail(int id, int row, int index) =>
        ReadRow(id, row, 0, r => index >= 0 && index < r.Details.Length ? r.Details[index] : 0);

    static bool TryGet(int id, string doing, out Board board)
    {
        lock (Gate)
        {
            if (Boards.TryGetValue(id, out board!)) return true;
        }

        Console.WriteLine($"[steam] cannot {doing} leaderboard {id}. no leaderboard has that id yet.");
        return false;
    }

    static T Read<T>(int id, Func<Board, T> read) where T : struct
    {
        lock (Gate)
        {
            return Boards.TryGetValue(id, out var board) ? read(board) : default;
        }
    }

    static T ReadRow<T>(int id, int row, T missing, Func<Row, T> read)
    {
        lock (Gate)
        {
            if (!Boards.TryGetValue(id, out var board)) return missing;
            if (row < 0 || row >= board.Rows.Length) return missing;
            return read(board.Rows[row]);
        }
    }

    static async Task<T> WithTimeout<T>(Task<T> work)
    {
        var first = await Task.WhenAny(work, Task.Delay(TimeoutMs));
        if (first != work) throw new TimeoutException("Steam did not answer.");
        return await work;
    }
}
