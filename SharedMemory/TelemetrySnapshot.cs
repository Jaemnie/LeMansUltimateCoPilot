namespace LeMansUltimateCoPilot.SharedMemory
{
    /// <summary>
    /// Lightweight telemetry snapshot - only the 9 fields needed for Apex Follower.
    /// Struct for zero-allocation, lock-free volatile sharing between threads.
    /// </summary>
    public readonly struct TelemetrySnapshot
    {
        public double LapDistance { get; init; }  // mLapDist from scoring (metres)
        public float Throttle { get; init; }      // mUnfilteredThrottle 0.0-1.0
        public float Brake { get; init; }         // mUnfilteredBrake 0.0-1.0
        public float Steering { get; init; }      // mUnfilteredSteering -1.0 to 1.0
        public int Gear { get; init; }            // -1=reverse, 0=neutral, 1+=forward
        public float RPM { get; init; }
        public float Speed { get; init; }         // km/h
        public double LapTime { get; init; }      // elapsed time in current lap (seconds)
        public int LapNumber { get; init; }

        // Used for track/vehicle auto-detection
        public string TrackName { get; init; }
        public string VehicleName { get; init; }

        public static readonly TelemetrySnapshot Empty = new TelemetrySnapshot
        {
            LapDistance = 0,
            Throttle = 0,
            Brake = 0,
            Steering = 0,
            Gear = 0,
            RPM = 0,
            Speed = 0,
            LapTime = 0,
            LapNumber = 0,
            TrackName = "",
            VehicleName = ""
        };
    }
}
