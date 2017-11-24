﻿using Jamiras.Components;
using Jamiras.DataModels;
using Jamiras.Services;
using Jamiras.ViewModels;
using Jamiras.ViewModels.Fields;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace RATools.ViewModels
{
    public class GameStatsViewModel : DialogViewModelBase
    {
        public GameStatsViewModel()
            : this(ServiceRepository.Instance.FindService<IFileSystemService>(),
                   ServiceRepository.Instance.FindService<IHttpRequestService>(),
                   ServiceRepository.Instance.FindService<IBackgroundWorkerService>())
        {
        }

        public GameStatsViewModel(IFileSystemService fileSystemService, IHttpRequestService httpRequestService, IBackgroundWorkerService backgroundWorkerService)
        {
            _fileSystemService = fileSystemService;
            _httpRequestService = httpRequestService;
            _backgroundWorkerService = backgroundWorkerService;

            Progress = new ProgressFieldViewModel { Label = String.Empty };
            DialogTitle = "Game Stats";
            CanClose = true;
        }

        private readonly IFileSystemService _fileSystemService;
        private readonly IHttpRequestService _httpRequestService;
        private readonly IBackgroundWorkerService _backgroundWorkerService;

        public ProgressFieldViewModel Progress { get; private set; }

        public static readonly ModelProperty GameIdProperty = ModelProperty.Register(typeof(GameStatsViewModel), "GameId", typeof(int), 0, OnGameIdChanged);

        public int GameId
        {
            get { return (int)GetValue(GameIdProperty); }
            set { SetValue(GameIdProperty, value); }
        }

        private static void OnGameIdChanged(object sender, ModelPropertyChangedEventArgs e)
        {
            var vm = (GameStatsViewModel)sender;
            vm.Progress.Label = "Fetching Game " + vm.GameId;
            vm.Progress.IsEnabled = true;
            vm._backgroundWorkerService.RunAsync(vm.LoadGame);
        }

        [DebuggerDisplay("{Title} ({Id})")]
        public class AchievementStats
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public int Points { get; set; }
            public int EarnedBy { get; set; }
            public int EarnedHardcoreBy { get; set; }
        }

        [DebuggerDisplay("{User} ({PointsEarned} points)")]
        public class UserStats : IComparer<UserStats>
        {
            public UserStats()
            {
                Achievements = new TinyDictionary<int, DateTime>();
            }

            public string User { get; set; }
            public int PointsEarned { get; set; }
            public TimeSpan RealTime { get; set; }
            public TimeSpan GameTime { get; set; }
            public TinyDictionary<int, DateTime> Achievements { get; private set; }

            public string Summary
            {
                get
                {
                    var builder = new StringBuilder();
                    builder.AppendFormat("{0}h{1:D2}m", (int)GameTime.TotalHours, GameTime.Minutes);
                    if (RealTime.TotalDays > 1.0)
                        builder.AppendFormat(" over {0} days", (int)Math.Ceiling(RealTime.TotalDays));

                    return builder.ToString();
                }
            }

            int IComparer<UserStats>.Compare(UserStats x, UserStats y)
            {
                return String.Compare(x.User, y.User);
            }
        }

        public static readonly ModelProperty AchievementsProperty = ModelProperty.Register(typeof(GameStatsViewModel), "Achievements", typeof(IEnumerable<AchievementStats>), new AchievementStats[0]);

        public IEnumerable<AchievementStats> Achievements
        {
            get { return (IEnumerable<AchievementStats>)GetValue(AchievementsProperty); }
            private set { SetValue(AchievementsProperty, value); }
        }

        public static readonly ModelProperty HardcoreUserCountProperty = ModelProperty.Register(typeof(GameStatsViewModel), "HardcoreUserCount", typeof(int), 0);

        public int HardcoreUserCount
        {
            get { return (int)GetValue(HardcoreUserCountProperty); }
            private set { SetValue(HardcoreUserCountProperty, value); }
        }

        public static readonly ModelProperty NonHardcoreUserCountProperty = ModelProperty.Register(typeof(GameStatsViewModel), "NonHardcoreUserCount", typeof(int), 0);

        public int NonHardcoreUserCount
        {
            get { return (int)GetValue(NonHardcoreUserCountProperty); }
            private set { SetValue(NonHardcoreUserCountProperty, value); }
        }

        public static readonly ModelProperty MedianHardcoreUserScoreProperty = ModelProperty.Register(typeof(GameStatsViewModel), "MedianHardcoreUserScore", typeof(int), 0);

        public int MedianHardcoreUserScore
        {
            get { return (int)GetValue(MedianHardcoreUserScoreProperty); }
            private set { SetValue(MedianHardcoreUserScoreProperty, value); }
        }

        public static readonly ModelProperty HardcoreMasteredUserCountProperty = ModelProperty.Register(typeof(GameStatsViewModel), "HardcoreMasteredUserCount", typeof(int), 0);

        public int HardcoreMasteredUserCount
        {
            get { return (int)GetValue(HardcoreMasteredUserCountProperty); }
            private set { SetValue(HardcoreMasteredUserCountProperty, value); }
        }

        public static readonly ModelProperty MedianTimeToMasterProperty = ModelProperty.Register(typeof(GameStatsViewModel), "MedianTimeToMaster", typeof(string), "n/a");

        public string MedianTimeToMaster
        {
            get { return (string)GetValue(MedianTimeToMasterProperty); }
            private set { SetValue(MedianTimeToMasterProperty, value); }
        }

        public static readonly ModelProperty TopUsersProperty = ModelProperty.Register(typeof(GameStatsViewModel), "TopUsers", typeof(IEnumerable<UserStats>), new UserStats[0]);

        public IEnumerable<UserStats> TopUsers
        {
            get { return (IEnumerable<UserStats>)GetValue(TopUsersProperty); }
            private set { SetValue(TopUsersProperty, value); }
        }

        private void LoadGame()
        {
            var gamePage = GetGamePage();
            if (gamePage == null)
                return;

            var tokenizer = Tokenizer.CreateTokenizer(gamePage);
            tokenizer.ReadTo("<title>");
            if (tokenizer.Match("<title>"))
            {
                var title = tokenizer.ReadTo("</title>");
                title = title.SubToken(24);
                DialogTitle = "Game Stats - " +title.ToString();
            }

            var allStats = new List<AchievementStats>();
            do
            {
                tokenizer.ReadTo("<div class='achievemententry'>");
                if (tokenizer.NextChar == '\0')
                    break;

                AchievementStats stats = new AchievementStats();

                tokenizer.ReadTo("won by ");
                tokenizer.Advance(7);
                var winners = tokenizer.ReadNumber();
                stats.EarnedBy = Int32.Parse(winners.ToString());

                tokenizer.ReadTo("(");
                tokenizer.Advance();
                var hardcoreWinners = tokenizer.ReadNumber();
                stats.EarnedHardcoreBy = Int32.Parse(hardcoreWinners.ToString());

                tokenizer.ReadTo("<a href='/Achievement/");
                if (tokenizer.Match("<a href='/Achievement/"))
                {
                    var achievementId = tokenizer.ReadTo("'>");
                    stats.Id = Int32.Parse(achievementId.ToString());
                    tokenizer.Advance(2);

                    var achievementTitle = tokenizer.ReadTo("</a>");
                    Token achievementPoints = Token.Empty;
                    for (int i = achievementTitle.Length - 1; i >= 0; i--)
                    {
                        if (achievementTitle[i] == '(')
                        {
                            achievementPoints = achievementTitle.SubToken(i + 1, achievementTitle.Length - i - 2);
                            achievementTitle = achievementTitle.SubToken(0, i);
                            break;
                        }
                    }

                    stats.Title = achievementTitle.ToString();
                    stats.Points = Int32.Parse(achievementPoints.ToString());
                }

                allStats.Add(stats);
            } while (true);

            Progress.Label = "Fetching user stats";
            Progress.Reset(allStats.Count);
            
            allStats.Sort((l, r) =>
            {
                var diff = r.EarnedHardcoreBy - l.EarnedHardcoreBy;
                if (diff == 0)
                    diff = String.Compare(l.Title, r.Title, StringComparison.OrdinalIgnoreCase);

                return diff;
            });

            Achievements = allStats;

            var nonHardcoreUsers = new List<string>();
            var userStats = new List<UserStats>();
            foreach (var achievement in allStats)
            {
                var achievementPage = GetAchievementPage(achievement.Id);
                if (achievementPage != null)
                {
                    tokenizer = Tokenizer.CreateTokenizer(achievementPage);
                    tokenizer.ReadTo("<h3>Winners</h3>");

                    do
                    {
                        tokenizer.ReadTo("<a href='/User/");
                        if (tokenizer.NextChar == '\0')
                            break;

                        tokenizer.ReadTo("'>");
                        tokenizer.Advance(2);
                        var user = tokenizer.ReadTo("</a>");
                        if (user.StartsWith("<img"))
                            continue;

                        var mid = tokenizer.ReadTo("<small>");
                        if (mid.Contains("Hardcore!"))
                        {
                            tokenizer.Advance(7);
                            var when = tokenizer.ReadTo("</small>");
                            var date = DateTime.Parse(when.ToString());
                            date = DateTime.SpecifyKind(date, DateTimeKind.Utc);

                            var stats = new UserStats { User = user.ToString() };
                            var index = userStats.BinarySearch(stats, stats);
                            if (index < 0)
                                userStats.Insert(~index, stats);
                            else
                                stats = userStats[index];

                            stats.Achievements[achievement.Id] = date;
                            stats.PointsEarned += achievement.Points;
                        }
                        else
                        {
                            if (!nonHardcoreUsers.Contains(user.ToString()))
                                nonHardcoreUsers.Add(user.ToString());
                        }

                    } while (true);
                }

                Progress.Current++;
            }

            Progress.Label = "Analyzing data";

            var idleTime = TimeSpan.FromHours(4);
            foreach (var user in userStats)
            {
                var times = new List<DateTime>(user.Achievements.Count);
                foreach (var achievement in user.Achievements)
                    times.Add(achievement.Value);

                times.Sort((l, r) => (int)((l - r).TotalSeconds));

                user.RealTime = times[times.Count - 1] - times[0];

                int start = 0, end = 0;
                while (end < times.Count)
                {
                    if (end + 1 == times.Count || (times[end + 1] - times[end]) >= idleTime)
                    {
                        user.GameTime += times[end] - times[start];
                        start = end + 1;
                    }

                    end++;
                }
            }

            userStats.Sort((l, r) => 
            {
                var diff = r.PointsEarned - l.PointsEarned;
                if (diff == 0)
                    diff = (int)((l.GameTime - r.GameTime).TotalSeconds);

                return diff;
            });

            HardcoreUserCount = userStats.Count;
            NonHardcoreUserCount = nonHardcoreUsers.Count;
            MedianHardcoreUserScore = userStats.Count > 0 ? userStats[userStats.Count / 2].PointsEarned : 0;

            int masteredCount = 0;
            while (masteredCount < userStats.Count && userStats[masteredCount].PointsEarned == 400)
                ++masteredCount;
            HardcoreMasteredUserCount = masteredCount;
            var timeToMaster = masteredCount > 0 ? userStats[masteredCount / 2].Summary : "n/a";
            var space = timeToMaster.IndexOf(' ');
            if (space > 0)
                timeToMaster = timeToMaster.Substring(0, space);
            MedianTimeToMaster = timeToMaster;

            if (userStats.Count > 100)
                userStats.RemoveRange(100, userStats.Count - 100);

            TopUsers = userStats;

            Progress.Label = String.Empty;
        }

        private string GetGamePage()
        {
            var filename = Path.Combine(Path.GetTempPath(), String.Format("raGame{0}.html", GameId));
            if (!_fileSystemService.FileExists(filename))                
            {
                var url = String.Format("http://retroachievements.org/Game/{0}", GameId);
                var request = new HttpRequest(url);
                var response = _httpRequestService.Request(request);
                if (response.Status != System.Net.HttpStatusCode.OK)
                    return null;

                using (var outputStream = _fileSystemService.CreateFile(filename))
                {
                    byte[] buffer = new byte[4096];
                    using (var stream = response.GetResponseStream())
                    {
                        int bytesRead;
                        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                            outputStream.Write(buffer, 0, bytesRead);
                    }
                }
            }

            using (var stream = new StreamReader(_fileSystemService.OpenFile(filename, OpenFileMode.Read)))
            {
                return stream.ReadToEnd();
            }
        }

        private string GetAchievementPage(int achievementId)
        {
            var filename = Path.Combine(Path.GetTempPath(), String.Format("raAch{0}.html", achievementId));
            if (!_fileSystemService.FileExists(filename))
            {
                var url = String.Format("http://retroachievements.org/Achievement/{0}", achievementId);
                var request = new HttpRequest(url);
                var response = _httpRequestService.Request(request);
                if (response.Status != System.Net.HttpStatusCode.OK)
                    return null;

                using (var outputStream = _fileSystemService.CreateFile(filename))
                {
                    byte[] buffer = new byte[4096];
                    using (var stream = response.GetResponseStream())
                    {
                        int bytesRead;
                        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                            outputStream.Write(buffer, 0, bytesRead);
                    }
                }
            }

            using (var stream = new StreamReader(_fileSystemService.OpenFile(filename, OpenFileMode.Read)))
            {
                return stream.ReadToEnd();
            }
        }
    }
}
