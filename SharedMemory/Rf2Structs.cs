using System.Runtime.InteropServices;

namespace LeMansUltimateCoPilot.SharedMemory
{
    // Official rF2 Telemetry Data Structures from rF2SharedMemoryMapPlugin
    // Source: https://github.com/TheIronWolfModding/rF2SharedMemoryMapPlugin/blob/master/Include/rF2State.h

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct rF2Vec3
    {
        public double x, y, z;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct rF2Wheel
    {
        public double mSuspensionDeflection;
        public double mRideHeight;
        public double mSuspForce;
        public double mBrakeTemp;
        public double mBrakePressure;
        public double mRotation;
        public double mLateralPatchVel;
        public double mLongitudinalPatchVel;
        public double mLateralGroundVel;
        public double mLongitudinalGroundVel;
        public double mCamber;
        public double mLateralForce;
        public double mLongitudinalForce;
        public double mTireLoad;
        public double mGripFract;
        public double mPressure;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public double[] mTemperature;
        public double mWear;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] mTerrainName;
        public byte mSurfaceType;
        public byte mFlat;
        public byte mDetached;
        public byte mStaticUndeflectedRadius;
        public double mVerticalTireDeflection;
        public double mWheelYLocation;
        public double mToe;
        public double mTireCarcassTemperature;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public double[] mTireInnerLayerTemperature;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
        public byte[] mExpansion;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct rF2VehicleTelemetry
    {
        public int mID;
        public double mDeltaTime;
        public double mElapsedTime;
        public int mLapNumber;
        public double mLapStartET;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] mVehicleName;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] mTrackName;

        public rF2Vec3 mPos;
        public rF2Vec3 mLocalVel;
        public rF2Vec3 mLocalAccel;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public rF2Vec3[] mOri;
        public rF2Vec3 mLocalRot;
        public rF2Vec3 mLocalRotAccel;

        public int mGear;                    // -1=reverse, 0=neutral, 1+=forward
        public double mEngineRPM;
        public double mEngineWaterTemp;
        public double mEngineOilTemp;
        public double mClutchRPM;

        public double mUnfilteredThrottle;   // 0.0-1.0
        public double mUnfilteredBrake;      // 0.0-1.0
        public double mUnfilteredSteering;   // -1.0 to 1.0
        public double mUnfilteredClutch;     // 0.0-1.0

        public double mFilteredThrottle;
        public double mFilteredBrake;
        public double mFilteredSteering;
        public double mFilteredClutch;

        public double mSteeringShaftTorque;
        public double mFront3rdDeflection;
        public double mRear3rdDeflection;

        public double mFrontWingHeight;
        public double mFrontRideHeight;
        public double mRearRideHeight;
        public double mDrag;
        public double mFrontDownforce;
        public double mRearDownforce;

        public double mFuel;
        public double mEngineMaxRPM;
        public byte mScheduledStops;
        public byte mOverheating;
        public byte mDetached;
        public byte mHeadlights;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] mDentSeverity;
        public double mLastImpactET;
        public double mLastImpactMagnitude;
        public rF2Vec3 mLastImpactPos;

        public double mEngineTorque;
        public int mCurrentSector;
        public byte mSpeedLimiter;
        public byte mMaxGears;
        public byte mFrontTireCompoundIndex;
        public byte mRearTireCompoundIndex;
        public double mFuelCapacity;
        public byte mFrontFlapActivated;
        public byte mRearFlapActivated;
        public byte mRearFlapLegalStatus;
        public byte mIgnitionStarter;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)]
        public byte[] mFrontTireCompoundName;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)]
        public byte[] mRearTireCompoundName;

        public byte mSpeedLimiterAvailable;
        public byte mAntiStallActivated;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] mUnused;

        public float mVisualSteeringWheelRange;
        public double mRearBrakeBias;
        public double mTurboBoostPressure;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] mPhysicsToGraphicsOffset;
        public float mPhysicalSteeringWheelRange;

        public double mBatteryChargeFraction;
        public double mElectricBoostMotorTorque;
        public double mElectricBoostMotorRPM;
        public double mElectricBoostMotorTemperature;
        public double mElectricBoostWaterTemperature;
        public byte mElectricBoostMotorState;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 111)]
        public byte[] mExpansion;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public rF2Wheel[] mWheels;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct rF2Telemetry
    {
        public uint mVersionUpdateBegin;
        public uint mVersionUpdateEnd;
        public int mBytesUpdatedHint;
        public int mNumVehicles;
        public rF2VehicleTelemetry mVehicles;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct rF2ScoringInfo
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] mTrackName;
        public int mSession;
        public double mCurrentET;
        public double mEndET;
        public int mMaxLaps;
        public double mLapDist;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] pointer1;
        public int mNumVehicles;
        public byte mGamePhase;
        public sbyte mYellowFlagState;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public sbyte[] mSectorFlag;
        public byte mStartLight;
        public byte mNumRedLights;
        public byte mInRealtime;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] mPlayerName;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] mPlrFileName;
        public double mDarkCloud;
        public double mRaining;
        public double mAmbientTemp;
        public double mTrackTemp;
        public rF2Vec3 mWind;
        public double mMinPathWetness;
        public double mMaxPathWetness;
        public byte mGameMode;
        public byte mIsPasswordProtected;
        public ushort mServerPort;
        public uint mServerPublicIP;
        public int mMaxPlayers;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] mServerName;
        public float mStartET;
        public double mAvgPathWetness;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 200)]
        public byte[] mExpansion;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] pointer2;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct rF2VehicleScoring
    {
        public int mID;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] mDriverName;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] mVehicleName;
        public short mTotalLaps;
        public sbyte mSector;
        public sbyte mFinishStatus;
        public double mLapDist;              // current lap distance travelled
        public double mPathLateral;
        public double mTrackEdge;
        public double mBestSector1;
        public double mBestSector2;
        public double mBestLapTime;
        public double mLastSector1;
        public double mLastSector2;
        public double mLastLapTime;
        public double mCurSector1;
        public double mCurSector2;
        public short mNumPitstops;
        public short mNumPenalties;
        public byte mIsPlayer;               // 1 if this is the player's vehicle
        public sbyte mControl;               // 0=local player
        public byte mInPits;
        public byte mPlace;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] mVehicleClass;
        public double mTimeBehindNext;
        public int mLapsBehindNext;
        public double mTimeBehindLeader;
        public int mLapsBehindLeader;
        public double mLapStartET;
        public rF2Vec3 mPos;
        public rF2Vec3 mLocalVel;
        public rF2Vec3 mLocalAccel;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public rF2Vec3[] mOri;
        public rF2Vec3 mLocalRot;
        public rF2Vec3 mLocalRotAccel;
        public byte mHeadlights;
        public byte mPitState;
        public byte mServerScored;
        public byte mIndividualPhase;
        public int mQualification;
        public double mTimeIntoLap;
        public double mEstimatedLapTime;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
        public byte[] mPitGroup;
        public byte mFlag;
        public byte mUnderYellow;
        public byte mCountLapFlag;
        public byte mInGarageStall;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] mUpgradePack;
        public float mPitLapDist;
        public float mBestLapSector1;
        public float mBestLapSector2;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 48)]
        public byte[] mExpansion;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct rF2Scoring
    {
        public int mBytesUpdatedHint;
        public rF2ScoringInfo mScoringInfo;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public rF2VehicleScoring[] mVehicles;
    }
}
