using UnityEngine;
using MetaCricket.Core;
using MetaCricket.MatchEngine;

namespace MetaCricket.CareerMode
{
    /// <summary>
    /// ScriptableObject defining a cricket stadium/venue: stadium name,
    /// capacity, pitch type, visual theme, and crowd intensity.
    /// Includes iconic venues like Lords, Mohali, MCG, plus gully/local grounds.
    /// </summary>
    [CreateAssetMenu(fileName = "NewStadium", menuName = "MetaCricket/Career Mode/Stadium Data")]
    public class StadiumData : ScriptableObject
    {
        [Header("Stadium Identity")]
        [Tooltip("Full name of the stadium.")]
        public string StadiumName;

        [Tooltip("City/Location of the stadium.")]
        public string City;

        [Tooltip("Country of the stadium.")]
        public string Country;

        [Header("Stadium Properties")]
        [Tooltip("Seating capacity.")]
        public int Capacity;

        [Tooltip("Pitch type at this venue.")]
        public PitchType PitchCondition = PitchType.Balanced;

        [Tooltip("Boundary distance in meters.")]
        [Range(50f, 90f)]
        public float BoundaryDistance = 65f;

        [Tooltip("Whether the ground is an enclosed stadium.")]
        public bool IsEnclosed = true;

        [Header("Visuals & Atmosphere")]
        [Tooltip("Visual theme identifier for the stadium.")]
        public StadiumTheme Theme = StadiumTheme.Modern;

        [Tooltip("Crowd intensity level (0 = empty, 1 = full house).")]
        [Range(0f, 1f)]
        public float CrowdIntensity = 0.7f;

        [Tooltip("Primary ambient color for lighting.")]
        public Color AmbientColor = new Color(0.9f, 0.9f, 1f);

        [Tooltip("Whether this is a day/night capable venue.")]
        public bool HasFloodlights = true;

        [Header("Gameplay Effects")]
        [Tooltip("Dew factor at night (affects ball grip). 0 = none, 1 = heavy dew.")]
        [Range(0f, 1f)]
        public float DewFactor = 0.3f;

        [Tooltip("Wind speed affecting ball flight (m/s).")]
        [Range(0f, 10f)]
        public float WindSpeed = 2f;

        [Tooltip("Altitude affecting ball carry (meters above sea level).")]
        public float Altitude = 100f;

        /// <summary>
        /// Get a bounce multiplier based on pitch type.
        /// </summary>
        public float GetBounceMultiplier()
        {
            switch (PitchCondition)
            {
                case PitchType.Fast: return 1.3f;
                case PitchType.Green: return 1.1f;
                case PitchType.Balanced: return 1.0f;
                case PitchType.Spin: return 0.85f;
                case PitchType.Dead: return 0.7f;
                default: return 1.0f;
            }
        }

        /// <summary>
        /// Get a spin multiplier based on pitch type.
        /// </summary>
        public float GetSpinMultiplier()
        {
            switch (PitchCondition)
            {
                case PitchType.Spin: return 1.4f;
                case PitchType.Dead: return 1.2f;
                case PitchType.Balanced: return 1.0f;
                case PitchType.Green: return 0.8f;
                case PitchType.Fast: return 0.7f;
                default: return 1.0f;
            }
        }

        /// <summary>
        /// Get the seam movement multiplier based on pitch type.
        /// </summary>
        public float GetSeamMultiplier()
        {
            switch (PitchCondition)
            {
                case PitchType.Green: return 1.5f;
                case PitchType.Fast: return 1.1f;
                case PitchType.Balanced: return 1.0f;
                case PitchType.Spin: return 0.7f;
                case PitchType.Dead: return 0.6f;
                default: return 1.0f;
            }
        }

        /// <summary>
        /// Create default stadium data for all game venues.
        /// </summary>
        public static StadiumData[] CreateDefaultStadiums()
        {
            return new StadiumData[]
            {
                // Gully/Local grounds
                CreateStadium("Street Ground", "Local", "India", 0, PitchType.Dead,
                    50f, false, StadiumTheme.Gully, 0.2f, false, 0f),
                CreateStadium("Local Park", "Local", "India", 50, PitchType.Balanced,
                    55f, false, StadiumTheme.Gully, 0.3f, false, 0f),
                CreateStadium("Community Ground", "Local", "India", 200, PitchType.Balanced,
                    58f, false, StadiumTheme.Local, 0.4f, true, 0f),

                // District/State
                CreateStadium("District Stadium", "City", "India", 5000, PitchType.Balanced,
                    62f, false, StadiumTheme.Local, 0.5f, true, 0.1f),
                CreateStadium("State Stadium", "City", "India", 15000, PitchType.Balanced,
                    65f, true, StadiumTheme.Modern, 0.6f, true, 0.2f),

                // Iconic Indian venues
                CreateStadium("Wankhede", "Mumbai", "India", 33000, PitchType.Balanced,
                    70f, true, StadiumTheme.Modern, 0.9f, true, 0.4f),
                CreateStadium("Eden Gardens", "Kolkata", "India", 68000, PitchType.Balanced,
                    68f, true, StadiumTheme.Heritage, 0.95f, true, 0.3f),
                CreateStadium("Chinnaswamy", "Bangalore", "India", 40000, PitchType.Fast,
                    60f, true, StadiumTheme.Modern, 0.85f, true, 0.2f),
                CreateStadium("Mohali", "Chandigarh", "India", 26000, PitchType.Fast,
                    67f, true, StadiumTheme.Modern, 0.8f, true, 0.2f),

                // International venues
                CreateStadium("Lords", "London", "England", 30000, PitchType.Green,
                    69f, true, StadiumTheme.Heritage, 0.85f, true, 0.1f),
                CreateStadium("MCG", "Melbourne", "Australia", 100024, PitchType.Fast,
                    75f, true, StadiumTheme.Modern, 0.95f, true, 0.1f)
            };
        }

        private static StadiumData CreateStadium(string name, string city, string country,
            int capacity, PitchType pitch, float boundary, bool enclosed, StadiumTheme theme,
            float crowd, bool floodlights, float dew)
        {
            StadiumData stadium = CreateInstance<StadiumData>();
            stadium.StadiumName = name;
            stadium.City = city;
            stadium.Country = country;
            stadium.Capacity = capacity;
            stadium.PitchCondition = pitch;
            stadium.BoundaryDistance = boundary;
            stadium.IsEnclosed = enclosed;
            stadium.Theme = theme;
            stadium.CrowdIntensity = crowd;
            stadium.HasFloodlights = floodlights;
            stadium.DewFactor = dew;
            stadium.WindSpeed = Random.Range(1f, 5f);
            stadium.Altitude = city == "Bangalore" ? 920f : city == "Melbourne" ? 31f : 100f;
            return stadium;
        }
    }

    /// <summary>
    /// Visual themes for stadiums.
    /// </summary>
    public enum StadiumTheme
    {
        Gully,      // Street cricket look
        Local,      // Small local ground
        Modern,     // Modern international stadium
        Heritage    // Classic/historic ground (Lords, Eden Gardens)
    }
}
