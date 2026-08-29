using System.Collections.Generic;
using UnityEngine;
using MetaCricket.Core;

namespace MetaCricket.CareerMode
{
    /// <summary>
    /// ScriptableObject defining a fictional cricket team: team name,
    /// logo color, home venue, difficulty rating, and player roster names.
    /// </summary>
    [CreateAssetMenu(fileName = "NewTeam", menuName = "MetaCricket/Career Mode/Team Data")]
    public class TeamData : ScriptableObject
    {
        [Header("Team Identity")]
        [Tooltip("Team name.")]
        public string TeamName;

        [Tooltip("Short team name/abbreviation.")]
        public string ShortName;

        [Tooltip("Primary team color.")]
        public Color PrimaryColor = Color.blue;

        [Tooltip("Secondary team color.")]
        public Color SecondaryColor = Color.white;

        [Header("Team Attributes")]
        [Tooltip("Home venue for this team.")]
        public string HomeVenue;

        [Tooltip("Overall difficulty rating of facing this team (1-10).")]
        [Range(1, 10)]
        public int DifficultyRating = 5;

        [Tooltip("Team strength description.")]
        [TextArea(2, 3)]
        public string TeamDescription;

        [Header("Roster")]
        [Tooltip("Player names in the team roster.")]
        public List<string> PlayerRoster;

        [Tooltip("Star player name.")]
        public string StarPlayer;

        [Header("Bowling Attack")]
        [Tooltip("Primary bowling types this team uses.")]
        public List<BallType> BowlingStrengths;

        [Tooltip("Average bowling speed of pace bowlers (kph).")]
        [Range(70f, 150f)]
        public float AveragePaceSpeed = 130f;

        /// <summary>
        /// Get a bowler name from the roster.
        /// </summary>
        public string GetBowlerName(int index)
        {
            if (PlayerRoster == null || PlayerRoster.Count == 0) return "Bowler";
            // Bowlers typically at positions 7-11
            int bowlerIndex = Mathf.Clamp(6 + index, 0, PlayerRoster.Count - 1);
            return PlayerRoster[bowlerIndex];
        }

        /// <summary>
        /// Get a batsman name from the roster.
        /// </summary>
        public string GetBatsmanName(int index)
        {
            if (PlayerRoster == null || PlayerRoster.Count == 0) return "Batsman";
            int batsmanIndex = Mathf.Clamp(index, 0, PlayerRoster.Count - 1);
            return PlayerRoster[batsmanIndex];
        }

        /// <summary>
        /// Get the difficulty level based on team rating.
        /// </summary>
        public DifficultyLevel GetDifficultyLevel()
        {
            if (DifficultyRating <= 3) return DifficultyLevel.Easy;
            if (DifficultyRating <= 5) return DifficultyLevel.Medium;
            if (DifficultyRating <= 8) return DifficultyLevel.Hard;
            return DifficultyLevel.Legend;
        }

        /// <summary>
        /// Create default fictional team data for all IPL-style teams.
        /// </summary>
        public static TeamData[] CreateDefaultTeams()
        {
            return new TeamData[]
            {
                CreateTeam("Bharat Warriors", "BW", "Wankhede", Color.blue, 8,
                    new List<string> {
                        "R. Sharma", "V. Kohli", "S. Patel", "A. Rahul",
                        "H. Pandya", "R. Jadeja", "D. Chahal", "M. Siraj",
                        "J. Bumrah", "S. Thakur", "A. Patel"
                    },
                    new List<BallType> { BallType.Pace, BallType.Swing, BallType.LegSpin }),

                CreateTeam("Mumbai Mavericks", "MM", "Wankhede", new Color(0.1f, 0.1f, 0.7f), 9,
                    new List<string> {
                        "I. Kishan", "R. Sharma", "S. Yadav", "T. David",
                        "K. Pollard", "H. Pandya", "K. Ahmed", "R. Chahar",
                        "J. Bumrah", "P. Krishna", "D. Sams"
                    },
                    new List<BallType> { BallType.Pace, BallType.Bouncer, BallType.Yorker }),

                CreateTeam("Chennai Kings", "CK", "Chinnaswamy", new Color(1f, 0.84f, 0f), 8,
                    new List<string> {
                        "R. Gaikwad", "D. Conway", "M. Ali", "A. Rayudu",
                        "R. Jadeja", "M. Dhoni", "D. Pretorius", "D. Chahar",
                        "S. Thakur", "T. Deshpande", "M. Theekshana"
                    },
                    new List<BallType> { BallType.OffSpin, BallType.LegSpin, BallType.SlowerBall }),

                CreateTeam("Delhi Dynamos", "DD", "Eden Gardens", new Color(0.1f, 0.3f, 0.8f), 7,
                    new List<string> {
                        "D. Warner", "P. Shaw", "M. Labuschagne", "R. Pant",
                        "L. Livingstone", "A. Patel", "S. Thakur", "K. Rabada",
                        "A. Nortje", "C. Sakariya", "P. Krishna"
                    },
                    new List<BallType> { BallType.Pace, BallType.Bouncer, BallType.Swing }),

                CreateTeam("Kolkata Knights", "KK", "Eden Gardens", new Color(0.4f, 0.1f, 0.6f), 7,
                    new List<string> {
                        "V. Iyer", "A. Hales", "S. Iyer", "N. Rana",
                        "A. Russell", "S. Narine", "P. Cummins", "U. Malik",
                        "T. Southee", "V. Chakravarthy", "S. Mavi"
                    },
                    new List<BallType> { BallType.OffSpin, BallType.Googly, BallType.Pace }),

                CreateTeam("Bangalore Bulls", "BB", "Chinnaswamy", Color.red, 8,
                    new List<string> {
                        "F. du Plessis", "V. Kohli", "G. Maxwell", "D. Karthik",
                        "S. Ahmed", "W. Hasaranga", "H. Patel", "J. Hazlewood",
                        "M. Siraj", "A. Patel", "S. Bhatt"
                    },
                    new List<BallType> { BallType.Pace, BallType.LegSpin, BallType.Seam }),

                CreateTeam("Hyderabad Hawks", "HH", "State Stadium", new Color(1f, 0.5f, 0f), 6,
                    new List<string> {
                        "A. Sharma", "K. Williamson", "R. Tripathi", "A. Markram",
                        "N. Pooran", "W. Sundar", "S. Kumar", "B. Kumar",
                        "T. Natarajan", "U. Malik", "S. Kaul"
                    },
                    new List<BallType> { BallType.Swing, BallType.Yorker, BallType.OffSpin }),

                CreateTeam("Punjab Panthers", "PP", "Mohali", Color.red, 6,
                    new List<string> {
                        "S. Dhawan", "J. Bairstow", "L. Livingstone", "M. Agarwal",
                        "J. Holder", "S. Curran", "R. Chahar", "K. Rabada",
                        "A. Singh", "R. Bawa", "H. Brar"
                    },
                    new List<BallType> { BallType.Pace, BallType.Swing, BallType.SlowerBall })
            };
        }

        private static TeamData CreateTeam(string name, string shortName, string venue,
            Color color, int difficulty, List<string> roster, List<BallType> bowlingStrengths)
        {
            TeamData team = CreateInstance<TeamData>();
            team.TeamName = name;
            team.ShortName = shortName;
            team.HomeVenue = venue;
            team.PrimaryColor = color;
            team.SecondaryColor = Color.white;
            team.DifficultyRating = difficulty;
            team.PlayerRoster = roster;
            team.StarPlayer = roster.Count > 1 ? roster[1] : roster[0];
            team.BowlingStrengths = bowlingStrengths;
            team.AveragePaceSpeed = 130f + (difficulty - 5) * 3f;
            return team;
        }
    }
}
