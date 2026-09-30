using KrishiSahayAI.Models;
using Microsoft.Data.Sqlite;

namespace KrishiSahayAI.Data
{
    public class AppDb
    {
        private readonly string _connectionString;

        public AppDb(IConfiguration configuration)
        {
            var databasePath =
                configuration["Database:Path"]
                ?? "Data/krishisahay.db";

            var directory = Path.GetDirectoryName(databasePath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _connectionString =
                $"Data Source={databasePath}";
        }


        // =========================================================
        // DATABASE INITIALIZATION
        // =========================================================

        public void Initialize()
        {
            using var connection =
                new SqliteConnection(_connectionString);

            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText = @"
CREATE TABLE IF NOT EXISTS FarmProfiles
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId TEXT NOT NULL,
    FarmerName TEXT NOT NULL,
    Location TEXT,
    LandSize REAL,
    LandUnit TEXT,
    SoilType TEXT,
    WaterSource TEXT,
    FarmingMethod TEXT,
    Crop TEXT,
    ExperienceLevel TEXT,
    Budget REAL,
    CreatedAt TEXT
);

CREATE TABLE IF NOT EXISTS FarmCrops
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId TEXT NOT NULL,
    FarmId INTEGER NOT NULL,
    CropName TEXT NOT NULL,
    Variety TEXT,
    Season TEXT,
    Area REAL NOT NULL DEFAULT 0,
    AreaUnit TEXT NOT NULL DEFAULT 'Acre',
    SowingDate TEXT,
    Notes TEXT,
    CreatedAt TEXT NOT NULL,
    FOREIGN KEY (FarmId) REFERENCES FarmProfiles(Id)
);

CREATE TABLE IF NOT EXISTS FarmActivities
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FarmProfileId INTEGER,
    Title TEXT,
    Description TEXT,
    ActivityDate TEXT,
    IsCompleted INTEGER,
    Category TEXT
);

CREATE TABLE IF NOT EXISTS LearningLessonCompletions
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FarmProfileId INTEGER NOT NULL,
    TopicId INTEGER NOT NULL,
    LessonId INTEGER NOT NULL,
    CompletedAt TEXT NOT NULL,

    UNIQUE
    (
        FarmProfileId,
        TopicId,
        LessonId
    )
);

CREATE TABLE IF NOT EXISTS CropObservations
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FarmProfileId INTEGER,
    CropName TEXT,
    ImagePath TEXT,
    Observation TEXT,
    AiResult TEXT,
    CreatedAt TEXT
);

CREATE TABLE IF NOT EXISTS SoilAssessments
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FarmProfileId INTEGER NOT NULL,
    SoilKnowledge TEXT,
    SoilTexture TEXT,
    SoilColour TEXT,
    Drainage TEXT,
    PreviousCrop TEXT,
    SoilTestStatus TEXT,
    Result TEXT,
    CreatedAt TEXT NOT NULL,
    FOREIGN KEY (FarmProfileId) REFERENCES FarmProfiles(Id)
);

CREATE TABLE IF NOT EXISTS WaterAssessments
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FarmProfileId INTEGER NOT NULL,
    WaterSource TEXT,
    WaterAvailability TEXT,
    IrrigationMethod TEXT,
    IrrigationFrequency TEXT,
    WaterQualityKnowledge TEXT,
    DrainageCondition TEXT,
    RecentWaterProblem TEXT,
    Result TEXT,
    CreatedAt TEXT NOT NULL,
    FOREIGN KEY (FarmProfileId) REFERENCES FarmProfiles(Id)
);
";

            command.ExecuteNonQuery();


            // =====================================================
            // ONE-TIME CROP DOCTOR HISTORY CORRECTION
            // Moves the old Wheat analysis from Groundnut to Wheat.
            // This is intentionally specific to the old test record.
            // =====================================================

            using var correctionCommand =
                connection.CreateCommand();

            correctionCommand.CommandText = @"
UPDATE CropObservations
SET CropName = 'Wheat'
WHERE FarmProfileId = 3
AND CropName = 'Groundnut'
AND Observation = 'leaves turning yellow'
AND AiResult LIKE '%wheat%';
";

            correctionCommand.ExecuteNonQuery();
        }


        // =========================================================
        // FARM PROFILE
        // =========================================================

        public int SaveFarm(FarmProfile farm)
        {
            using var connection =
                new SqliteConnection(_connectionString);

            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText = @"
INSERT INTO FarmProfiles
(
    UserId,
    FarmerName,
    Location,
    LandSize,
    LandUnit,
    SoilType,
    WaterSource,
    FarmingMethod,
    Crop,
    ExperienceLevel,
    Budget,
    CreatedAt
)
VALUES
(
    $UserId,
    $FarmerName,
    $Location,
    $LandSize,
    $LandUnit,
    $SoilType,
    $WaterSource,
    $FarmingMethod,
    $Crop,
    $ExperienceLevel,
    $Budget,
    $CreatedAt
);

SELECT last_insert_rowid();
";

            command.Parameters.AddWithValue(
                "$UserId",
                farm.UserId);

            command.Parameters.AddWithValue(
                "$FarmerName",
                farm.FarmerName);

            command.Parameters.AddWithValue(
                "$Location",
                farm.Location);

            command.Parameters.AddWithValue(
                "$LandSize",
                farm.LandSize);

            command.Parameters.AddWithValue(
                "$LandUnit",
                farm.LandUnit);

            command.Parameters.AddWithValue(
                "$SoilType",
                farm.SoilType);

            command.Parameters.AddWithValue(
                "$WaterSource",
                farm.WaterSource);

            command.Parameters.AddWithValue(
                "$FarmingMethod",
                farm.FarmingMethod);

            command.Parameters.AddWithValue(
                "$Crop",
                farm.Crop);

            command.Parameters.AddWithValue(
                "$ExperienceLevel",
                farm.ExperienceLevel);

            command.Parameters.AddWithValue(
                "$Budget",
                farm.Budget);

            command.Parameters.AddWithValue(
                "$CreatedAt",
                farm.CreatedAt.ToString("O"));

            return Convert.ToInt32(
                command.ExecuteScalar());
        }
        public int SaveFarmCrop(FarmCrop crop)
        {
            using var connection =
                new SqliteConnection(_connectionString);

            connection.Open();

            using var command =
                connection.CreateCommand();

            command.CommandText = @"
INSERT INTO FarmCrops
(
    UserId,
    FarmId,
    CropName,
    Variety,
    Season,
    Area,
    AreaUnit,
    SowingDate,
    Notes,
    CreatedAt
)
VALUES
(
    $UserId,
    $FarmId,
    $CropName,
    $Variety,
    $Season,
    $Area,
    $AreaUnit,
    $SowingDate,
    $Notes,
    $CreatedAt
);

SELECT last_insert_rowid();
";

            command.Parameters.AddWithValue(
                "$UserId",
                crop.UserId);

            command.Parameters.AddWithValue(
                "$FarmId",
                crop.FarmId);

            command.Parameters.AddWithValue(
                "$CropName",
                crop.CropName);

            command.Parameters.AddWithValue(
                "$Variety",
                crop.Variety ?? "");

            command.Parameters.AddWithValue(
                "$Season",
                crop.Season ?? "");

            command.Parameters.AddWithValue(
                "$Area",
                crop.Area);

            command.Parameters.AddWithValue(
                "$AreaUnit",
                crop.AreaUnit ?? "Acre");

            command.Parameters.AddWithValue(
                "$SowingDate",
                crop.SowingDate?.ToString("O")
                ?? (object)DBNull.Value);

            command.Parameters.AddWithValue(
                "$Notes",
                crop.Notes ?? "");

            command.Parameters.AddWithValue(
                "$CreatedAt",
                crop.CreatedAt.ToString("O"));

            return Convert.ToInt32(
                command.ExecuteScalar());
        }

        public List<FarmCrop> GetFarmCrops(
    int farmId,
    string userId)
        {
            var crops = new List<FarmCrop>();

            using var connection =
                new SqliteConnection(_connectionString);

            connection.Open();

            using var command =
                connection.CreateCommand();

            command.CommandText = @"
SELECT *
FROM FarmCrops
WHERE FarmId = $FarmId
AND UserId = $UserId
ORDER BY Id DESC;
";

            command.Parameters.AddWithValue(
                "$FarmId",
                farmId);

            command.Parameters.AddWithValue(
                "$UserId",
                userId);

            using var reader =
                command.ExecuteReader();

            while (reader.Read())
            {
                DateTime? sowingDate = null;

                var sowingDateValue =
                    reader["SowingDate"]?.ToString();

                if (!string.IsNullOrWhiteSpace(
                        sowingDateValue))
                {
                    if (DateTime.TryParse(
                            sowingDateValue,
                            out var parsedDate))
                    {
                        sowingDate = parsedDate;
                    }
                }

                crops.Add(new FarmCrop
                {
                    Id = reader.GetInt32(
                        reader.GetOrdinal("Id")),

                    UserId =
                        reader["UserId"]?.ToString() ?? "",

                    FarmId =
                        Convert.ToInt32(
                            reader["FarmId"]),

                    CropName =
                        reader["CropName"]?.ToString() ?? "",

                    Variety =
                        reader["Variety"]?.ToString() ?? "",

                    Season =
                        reader["Season"]?.ToString() ?? "",

                    Area =
                        Convert.ToDouble(
                            reader["Area"]),

                    AreaUnit =
                        reader["AreaUnit"]?.ToString()
                        ?? "Acre",

                    SowingDate =
                        sowingDate,

                    Notes =
                        reader["Notes"]?.ToString() ?? "",

                    CreatedAt =
                        DateTime.Parse(
                            reader["CreatedAt"]?.ToString()
                            ?? DateTime.UtcNow.ToString("O"))
                });
            }

            return crops;
        }
        // =========================================================
        // GET SINGLE FARM CROP
        // =========================================================

        public FarmCrop? GetFarmCrop(
            int cropId,
            int farmId,
            string userId)
        {
            using var connection =
                new SqliteConnection(_connectionString);

            connection.Open();

            using var command =
                connection.CreateCommand();

            command.CommandText = @"
SELECT *
FROM FarmCrops
WHERE Id = $Id
AND FarmId = $FarmId
AND UserId = $UserId;
";

            command.Parameters.AddWithValue(
                "$Id",
                cropId);

            command.Parameters.AddWithValue(
                "$FarmId",
                farmId);

            command.Parameters.AddWithValue(
                "$UserId",
                userId);

            using var reader =
                command.ExecuteReader();

            if (!reader.Read())
            {
                return null;
            }

            DateTime? sowingDate = null;

            var sowingDateValue =
                reader["SowingDate"]?.ToString();

            if (!string.IsNullOrWhiteSpace(
                sowingDateValue))
            {
                if (DateTime.TryParse(
                    sowingDateValue,
                    out var parsedDate))
                {
                    sowingDate = parsedDate;
                }
            }

            return new FarmCrop
            {
                Id =
                    reader.GetInt32(
                        reader.GetOrdinal("Id")),

                UserId =
                    reader["UserId"]?.ToString() ?? "",

                FarmId =
                    Convert.ToInt32(
                        reader["FarmId"]),

                CropName =
                    reader["CropName"]?.ToString() ?? "",

                Variety =
                    reader["Variety"]?.ToString() ?? "",

                Season =
                    reader["Season"]?.ToString() ?? "",

                Area =
                    Convert.ToDouble(
                        reader["Area"]),

                AreaUnit =
                    reader["AreaUnit"]?.ToString()
                    ?? "Acre",

                SowingDate =
                    sowingDate,

                Notes =
                    reader["Notes"]?.ToString() ?? "",

                CreatedAt =
                    DateTime.Parse(
                        reader["CreatedAt"]?.ToString()
                        ?? DateTime.UtcNow.ToString("O"))
            };
        }


        // =========================================================
        // UPDATE FARM CROP
        // =========================================================

        public bool UpdateFarmCrop(
            FarmCrop crop)
        {
            using var connection =
                new SqliteConnection(_connectionString);

            connection.Open();

            using var command =
                connection.CreateCommand();

            command.CommandText = @"
UPDATE FarmCrops
SET
    CropName = $CropName,
    Variety = $Variety,
    Season = $Season,
    Area = $Area,
    AreaUnit = $AreaUnit,
    SowingDate = $SowingDate,
    Notes = $Notes
WHERE Id = $Id
AND FarmId = $FarmId
AND UserId = $UserId;
";

            command.Parameters.AddWithValue(
                "$Id",
                crop.Id);

            command.Parameters.AddWithValue(
                "$FarmId",
                crop.FarmId);

            command.Parameters.AddWithValue(
                "$UserId",
                crop.UserId);

            command.Parameters.AddWithValue(
                "$CropName",
                crop.CropName);

            command.Parameters.AddWithValue(
                "$Variety",
                crop.Variety ?? "");

            command.Parameters.AddWithValue(
                "$Season",
                crop.Season ?? "");

            command.Parameters.AddWithValue(
                "$Area",
                crop.Area);

            command.Parameters.AddWithValue(
                "$AreaUnit",
                crop.AreaUnit ?? "Acre");

            command.Parameters.AddWithValue(
                "$SowingDate",
                crop.SowingDate?.ToString("O")
                ?? (object)DBNull.Value);

            command.Parameters.AddWithValue(
                "$Notes",
                crop.Notes ?? "");

            return command.ExecuteNonQuery() > 0;
        }


        // =========================================================
        // DELETE FARM CROP
        // =========================================================

        public bool DeleteFarmCrop(
            int cropId,
            int farmId,
            string userId)
        {
            using var connection =
                new SqliteConnection(_connectionString);

            connection.Open();

            using var command =
                connection.CreateCommand();

            command.CommandText = @"
DELETE FROM FarmCrops
WHERE Id = $Id
AND FarmId = $FarmId
AND UserId = $UserId;
";

            command.Parameters.AddWithValue(
                "$Id",
                cropId);

            command.Parameters.AddWithValue(
                "$FarmId",
                farmId);

            command.Parameters.AddWithValue(
                "$UserId",
                userId);

            return command.ExecuteNonQuery() > 0;
        }
        public FarmProfile? GetFarm(int id)
        {
            using var connection =
                new SqliteConnection(_connectionString);

            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText = @"
SELECT *
FROM FarmProfiles
WHERE Id = $Id;
";

            command.Parameters.AddWithValue(
                "$Id",
                id);

            using var reader =
                command.ExecuteReader();

            if (!reader.Read())
            {
                return null;
            }

            return new FarmProfile
            {
                Id = reader.GetInt32(
                    reader.GetOrdinal("Id")),

                UserId =
                    reader["UserId"]?.ToString() ?? "",

                FarmerName =
                    reader["FarmerName"]?.ToString() ?? "",

                Location =
                    reader["Location"]?.ToString() ?? "",

                LandSize =
                    Convert.ToDouble(reader["LandSize"]),

                LandUnit =
                    reader["LandUnit"]?.ToString() ?? "",

                SoilType =
                    reader["SoilType"]?.ToString() ?? "",

                WaterSource =
                    reader["WaterSource"]?.ToString() ?? "",

                FarmingMethod =
                    reader["FarmingMethod"]?.ToString() ?? "",

                Crop =
                    reader["Crop"]?.ToString() ?? "",

                ExperienceLevel =
                    reader["ExperienceLevel"]?.ToString() ?? "",

                Budget =
                    Convert.ToDecimal(reader["Budget"]),

                CreatedAt =
                    DateTime.Parse(
                        reader["CreatedAt"]?.ToString()
                        ?? DateTime.UtcNow.ToString("O"))
            };
        }

        public FarmProfile? GetFarm(int id, string userId)
        {
            using var connection =
                new SqliteConnection(_connectionString);

            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText = @"
SELECT *
FROM FarmProfiles
WHERE Id = $Id
AND UserId = $UserId;
";

            command.Parameters.AddWithValue(
                "$Id",
                id);

            command.Parameters.AddWithValue(
                "$UserId",
                userId);

            using var reader =
                command.ExecuteReader();

            if (!reader.Read())
            {
                return null;
            }

            return new FarmProfile
            {
                Id = reader.GetInt32(
                    reader.GetOrdinal("Id")),

                UserId =
                    reader["UserId"]?.ToString() ?? "",

                FarmerName =
                    reader["FarmerName"]?.ToString() ?? "",

                Location =
                    reader["Location"]?.ToString() ?? "",

                LandSize =
                    Convert.ToDouble(reader["LandSize"]),

                LandUnit =
                    reader["LandUnit"]?.ToString() ?? "",

                SoilType =
                    reader["SoilType"]?.ToString() ?? "",

                WaterSource =
                    reader["WaterSource"]?.ToString() ?? "",

                FarmingMethod =
                    reader["FarmingMethod"]?.ToString() ?? "",

                Crop =
                    reader["Crop"]?.ToString() ?? "",

                ExperienceLevel =
                    reader["ExperienceLevel"]?.ToString() ?? "",

                Budget =
                    Convert.ToDecimal(reader["Budget"]),

                CreatedAt =
                    DateTime.Parse(
                        reader["CreatedAt"]?.ToString()
                        ?? DateTime.UtcNow.ToString("O"))
            };
        }

        public List<FarmProfile> GetFarmsByUserId(string userId)
        {
            var farms = new List<FarmProfile>();

            using var connection =
                new SqliteConnection(_connectionString);

            connection.Open();

            using var command =
                connection.CreateCommand();

            command.CommandText = @"
SELECT *
FROM FarmProfiles
WHERE UserId = $UserId
ORDER BY Id DESC;
";

            command.Parameters.AddWithValue(
                "$UserId",
                userId);

            using var reader =
                command.ExecuteReader();

            while (reader.Read())
            {
                farms.Add(new FarmProfile
                {
                    Id = reader.GetInt32(
                        reader.GetOrdinal("Id")),

                    UserId =
                        reader["UserId"]?.ToString() ?? "",

                    FarmerName =
                        reader["FarmerName"]?.ToString() ?? "",

                    Location =
                        reader["Location"]?.ToString() ?? "",

                    LandSize =
                        Convert.ToDouble(reader["LandSize"]),

                    LandUnit =
                        reader["LandUnit"]?.ToString() ?? "",

                    SoilType =
                        reader["SoilType"]?.ToString() ?? "",

                    WaterSource =
                        reader["WaterSource"]?.ToString() ?? "",

                    FarmingMethod =
                        reader["FarmingMethod"]?.ToString() ?? "",

                    Crop =
                        reader["Crop"]?.ToString() ?? "",

                    ExperienceLevel =
                        reader["ExperienceLevel"]?.ToString() ?? "",

                    Budget =
                        Convert.ToDecimal(reader["Budget"]),

                    CreatedAt =
                        DateTime.Parse(
                            reader["CreatedAt"]?.ToString()
                            ?? DateTime.UtcNow.ToString("O"))
                });
            }

            return farms;
        }


        public FarmProfile? GetLatestFarm()
        {
            using var connection =
                new SqliteConnection(_connectionString);

            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText = @"
SELECT *
FROM FarmProfiles
ORDER BY Id DESC
LIMIT 1;
";

            using var reader =
                command.ExecuteReader();

            if (!reader.Read())
            {
                return null;
            }

            return new FarmProfile
            {
                Id = reader.GetInt32(
                    reader.GetOrdinal("Id")),

                UserId =
                    reader["UserId"]?.ToString() ?? "",

                FarmerName =
                    reader["FarmerName"]?.ToString() ?? "",

                Location =
                    reader["Location"]?.ToString() ?? "",

                LandSize =
                    Convert.ToDouble(reader["LandSize"]),

                LandUnit =
                    reader["LandUnit"]?.ToString() ?? "",

                SoilType =
                    reader["SoilType"]?.ToString() ?? "",

                WaterSource =
                    reader["WaterSource"]?.ToString() ?? "",

                FarmingMethod =
                    reader["FarmingMethod"]?.ToString() ?? "",

                Crop =
                    reader["Crop"]?.ToString() ?? "",

                ExperienceLevel =
                    reader["ExperienceLevel"]?.ToString() ?? "",

                Budget =
                    Convert.ToDecimal(reader["Budget"]),

                CreatedAt =
                    DateTime.Parse(
                        reader["CreatedAt"]?.ToString()
                        ?? DateTime.UtcNow.ToString("O"))
            };
        }


        // =========================================================
        // FARM ACTIVITIES
        // =========================================================

        public void AddActivity(FarmActivity activity)
        {
            using var connection =
                new SqliteConnection(_connectionString);

            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText = @"
INSERT INTO FarmActivities
(
    FarmProfileId,
    Title,
    Description,
    ActivityDate,
    IsCompleted,
    Category
)
VALUES
(
    $FarmProfileId,
    $Title,
    $Description,
    $ActivityDate,
    $IsCompleted,
    $Category
);
";

            command.Parameters.AddWithValue(
                "$FarmProfileId",
                activity.FarmProfileId);

            command.Parameters.AddWithValue(
                "$Title",
                activity.Title);

            command.Parameters.AddWithValue(
                "$Description",
                activity.Description);

            command.Parameters.AddWithValue(
                "$ActivityDate",
                activity.ActivityDate.ToString("O"));

            command.Parameters.AddWithValue(
                "$IsCompleted",
                activity.IsCompleted ? 1 : 0);

            command.Parameters.AddWithValue(
                "$Category",
                activity.Category);

            command.ExecuteNonQuery();
        }


        public List<FarmActivity> GetActivities(int farmId)
        {
            var activities =
                new List<FarmActivity>();

            using var connection =
                new SqliteConnection(_connectionString);

            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText = @"
SELECT *
FROM FarmActivities
WHERE FarmProfileId = $FarmProfileId
ORDER BY ActivityDate ASC;
";

            command.Parameters.AddWithValue(
                "$FarmProfileId",
                farmId);

            using var reader =
                command.ExecuteReader();

            while (reader.Read())
            {
                activities.Add(new FarmActivity
                {
                    Id = reader.GetInt32(
                        reader.GetOrdinal("Id")),

                    FarmProfileId =
                        reader.GetInt32(
                            reader.GetOrdinal("FarmProfileId")),

                    Title =
                        reader["Title"]?.ToString() ?? "",

                    Description =
                        reader["Description"]?.ToString() ?? "",

                    ActivityDate =
                        DateTime.Parse(
                            reader["ActivityDate"]?.ToString()
                            ?? DateTime.UtcNow.ToString("O")),

                    IsCompleted =
                        Convert.ToInt32(
                            reader["IsCompleted"]) == 1,

                    Category =
                        reader["Category"]?.ToString() ?? ""
                });
            }

            return activities;
        }

        // =========================================================
        // LEARNING LESSON COMPLETION
        // =========================================================

        public void MarkLessonCompleted(
            int farmId,
            int topicId,
            int lessonId)
        {
            using var connection =
                new SqliteConnection(_connectionString);

            connection.Open();

            using var command =
                connection.CreateCommand();

            command.CommandText = @"
INSERT OR IGNORE INTO LearningLessonCompletions
(
    FarmProfileId,
    TopicId,
    LessonId,
    CompletedAt
)
VALUES
(
    $FarmProfileId,
    $TopicId,
    $LessonId,
    $CompletedAt
);
";

            command.Parameters.AddWithValue(
                "$FarmProfileId",
                farmId);

            command.Parameters.AddWithValue(
                "$TopicId",
                topicId);

            command.Parameters.AddWithValue(
                "$LessonId",
                lessonId);

            command.Parameters.AddWithValue(
                "$CompletedAt",
                DateTime.UtcNow.ToString("O"));

            command.ExecuteNonQuery();
        }


        public List<int> GetCompletedLessonIds(
            int farmId,
            int topicId)
        {
            var completedLessonIds =
                new List<int>();

            using var connection =
                new SqliteConnection(_connectionString);

            connection.Open();

            using var command =
                connection.CreateCommand();

            command.CommandText = @"
SELECT LessonId
FROM LearningLessonCompletions
WHERE FarmProfileId = $FarmProfileId
AND TopicId = $TopicId
ORDER BY LessonId;
";

            command.Parameters.AddWithValue(
                "$FarmProfileId",
                farmId);

            command.Parameters.AddWithValue(
                "$TopicId",
                topicId);

            using var reader =
                command.ExecuteReader();

            while (reader.Read())
            {
                completedLessonIds.Add(
                    reader.GetInt32(
                        reader.GetOrdinal("LessonId")));
            }

            return completedLessonIds;
        }
        // =========================================================
        // UPDATE FARM ACTIVITY COMPLETION
        // =========================================================

        public bool UpdateActivityCompletion(
            int activityId,
            int farmId,
            bool isCompleted)
        {
            using var connection =
                new SqliteConnection(_connectionString);

            connection.Open();

            using var command =
                connection.CreateCommand();

            command.CommandText = @"
UPDATE FarmActivities
SET IsCompleted = $IsCompleted
WHERE Id = $Id
AND FarmProfileId = $FarmProfileId;
";

            command.Parameters.AddWithValue(
                "$IsCompleted",
                isCompleted ? 1 : 0);

            command.Parameters.AddWithValue(
                "$Id",
                activityId);

            command.Parameters.AddWithValue(
                "$FarmProfileId",
                farmId);

            return command.ExecuteNonQuery() > 0;
        }

        // =========================================================
        // CROP OBSERVATIONS
        // =========================================================

        public int SaveCropObservation(
            int farmProfileId,
            string cropName,
            string imagePath,
            string observation,
            string aiResult)
        {
            using var connection =
                new SqliteConnection(_connectionString);

            connection.Open();

            using var command =
                connection.CreateCommand();

            command.CommandText = @"
INSERT INTO CropObservations
(
    FarmProfileId,
    CropName,
    ImagePath,
    Observation,
    AiResult,
    CreatedAt
)
VALUES
(
    $FarmProfileId,
    $CropName,
    $ImagePath,
    $Observation,
    $AiResult,
    $CreatedAt
);

SELECT last_insert_rowid();
";

            command.Parameters.AddWithValue(
                "$FarmProfileId",
                farmProfileId);

            command.Parameters.AddWithValue(
                "$CropName",
                cropName ?? "");

            command.Parameters.AddWithValue(
                "$ImagePath",
                imagePath ?? "");

            command.Parameters.AddWithValue(
                "$Observation",
                observation ?? "");

            command.Parameters.AddWithValue(
                "$AiResult",
                aiResult ?? "");

            command.Parameters.AddWithValue(
                "$CreatedAt",
                DateTime.UtcNow.ToString("O"));

            return Convert.ToInt32(
                command.ExecuteScalar());
        }


        public List<CropObservation> GetCropObservations(
            int farmProfileId,
            string cropName)
        {
            var observations =
                new List<CropObservation>();

            using var connection =
                new SqliteConnection(_connectionString);

            connection.Open();

            using var command =
                connection.CreateCommand();

            command.CommandText = @"
SELECT *
FROM CropObservations
WHERE FarmProfileId = $FarmProfileId
AND CropName = $CropName COLLATE NOCASE
ORDER BY Id DESC;
";

            command.Parameters.AddWithValue(
                "$FarmProfileId",
                farmProfileId);

            command.Parameters.AddWithValue(
                "$CropName",
                cropName ?? "");

            using var reader =
                command.ExecuteReader();

            while (reader.Read())
            {
                observations.Add(new CropObservation
                {
                    Id = reader.GetInt32(
                        reader.GetOrdinal("Id")),

                    FarmProfileId =
                        reader.GetInt32(
                            reader.GetOrdinal("FarmProfileId")),

                    CropName =
                        reader["CropName"]?.ToString() ?? "",

                    ImagePath =
                        reader["ImagePath"]?.ToString() ?? "",

                    Observation =
                        reader["Observation"]?.ToString() ?? "",

                    AiResult =
                        reader["AiResult"]?.ToString() ?? "",

                    CreatedAt =
                        DateTime.Parse(
                            reader["CreatedAt"]?.ToString()
                            ?? DateTime.UtcNow.ToString("O"))
                });
            }

            return observations;
        }


        // =========================================================
        // SOIL ASSESSMENT
        // =========================================================

        public int SaveSoilAssessment(
            SoilAssessment assessment)
        {
            using var connection =
                new SqliteConnection(_connectionString);

            connection.Open();

            using var command =
                connection.CreateCommand();

            command.CommandText = @"
INSERT INTO SoilAssessments
(
    FarmProfileId,
    SoilKnowledge,
    SoilTexture,
    SoilColour,
    Drainage,
    PreviousCrop,
    SoilTestStatus,
    Result,
    CreatedAt
)
VALUES
(
    $FarmProfileId,
    $SoilKnowledge,
    $SoilTexture,
    $SoilColour,
    $Drainage,
    $PreviousCrop,
    $SoilTestStatus,
    $Result,
    $CreatedAt
);

SELECT last_insert_rowid();
";

            command.Parameters.AddWithValue(
                "$FarmProfileId",
                assessment.FarmProfileId);

            command.Parameters.AddWithValue(
                "$SoilKnowledge",
                assessment.SoilKnowledge);

            command.Parameters.AddWithValue(
                "$SoilTexture",
                assessment.SoilTexture);

            command.Parameters.AddWithValue(
                "$SoilColour",
                assessment.SoilColour);

            command.Parameters.AddWithValue(
                "$Drainage",
                assessment.Drainage);

            command.Parameters.AddWithValue(
                "$PreviousCrop",
                assessment.PreviousCrop);

            command.Parameters.AddWithValue(
                "$SoilTestStatus",
                assessment.SoilTestStatus);

            command.Parameters.AddWithValue(
                "$Result",
                assessment.Result);

            command.Parameters.AddWithValue(
                "$CreatedAt",
                assessment.CreatedAt.ToString("O"));

            return Convert.ToInt32(
                command.ExecuteScalar());
        }


        public SoilAssessment? GetSoilAssessment(
            int farmProfileId)
        {
            using var connection =
                new SqliteConnection(_connectionString);

            connection.Open();

            using var command =
                connection.CreateCommand();

            command.CommandText = @"
SELECT *
FROM SoilAssessments
WHERE FarmProfileId = $FarmProfileId
ORDER BY Id DESC
LIMIT 1;
";

            command.Parameters.AddWithValue(
                "$FarmProfileId",
                farmProfileId);

            using var reader =
                command.ExecuteReader();

            if (!reader.Read())
            {
                return null;
            }

            return new SoilAssessment
            {
                Id = reader.GetInt32(
                    reader.GetOrdinal("Id")),

                FarmProfileId =
                    reader.GetInt32(
                        reader.GetOrdinal("FarmProfileId")),

                SoilKnowledge =
                    reader["SoilKnowledge"]?.ToString() ?? "",

                SoilTexture =
                    reader["SoilTexture"]?.ToString() ?? "",

                SoilColour =
                    reader["SoilColour"]?.ToString() ?? "",

                Drainage =
                    reader["Drainage"]?.ToString() ?? "",

                PreviousCrop =
                    reader["PreviousCrop"]?.ToString() ?? "",

                SoilTestStatus =
                    reader["SoilTestStatus"]?.ToString() ?? "",

                Result =
                    reader["Result"]?.ToString() ?? "",

                CreatedAt =
                    DateTime.Parse(
                        reader["CreatedAt"]?.ToString()
                        ?? DateTime.UtcNow.ToString("O"))
            };
        }


        // =========================================================
        // WATER ASSESSMENT
        // =========================================================

        public int SaveWaterAssessment(
            WaterAssessment assessment)
        {
            using var connection =
                new SqliteConnection(_connectionString);

            connection.Open();

            using var command =
                connection.CreateCommand();

            command.CommandText = @"
INSERT INTO WaterAssessments
(
    FarmProfileId,
    WaterSource,
    WaterAvailability,
    IrrigationMethod,
    IrrigationFrequency,
    WaterQualityKnowledge,
    DrainageCondition,
    RecentWaterProblem,
    Result,
    CreatedAt
)
VALUES
(
    $FarmProfileId,
    $WaterSource,
    $WaterAvailability,
    $IrrigationMethod,
    $IrrigationFrequency,
    $WaterQualityKnowledge,
    $DrainageCondition,
    $RecentWaterProblem,
    $Result,
    $CreatedAt
);

SELECT last_insert_rowid();
";

            command.Parameters.AddWithValue(
                "$FarmProfileId",
                assessment.FarmProfileId);

            command.Parameters.AddWithValue(
                "$WaterSource",
                assessment.WaterSource ?? "");

            command.Parameters.AddWithValue(
                "$WaterAvailability",
                assessment.WaterAvailability ?? "");

            command.Parameters.AddWithValue(
                "$IrrigationMethod",
                assessment.IrrigationMethod ?? "");

            command.Parameters.AddWithValue(
                "$IrrigationFrequency",
                assessment.IrrigationFrequency ?? "");

            command.Parameters.AddWithValue(
                "$WaterQualityKnowledge",
                assessment.WaterQualityKnowledge ?? "");

            command.Parameters.AddWithValue(
                "$DrainageCondition",
                assessment.DrainageCondition ?? "");

            command.Parameters.AddWithValue(
                "$RecentWaterProblem",
                assessment.RecentWaterProblem ?? "");

            command.Parameters.AddWithValue(
                "$Result",
                assessment.Result ?? "");

            command.Parameters.AddWithValue(
                "$CreatedAt",
                assessment.CreatedAt.ToString("O"));

            return Convert.ToInt32(
                command.ExecuteScalar());
        }


        public WaterAssessment? GetWaterAssessment(
            int farmProfileId)
        {
            using var connection =
                new SqliteConnection(_connectionString);

            connection.Open();

            using var command =
                connection.CreateCommand();

            command.CommandText = @"
SELECT *
FROM WaterAssessments
WHERE FarmProfileId = $FarmProfileId
ORDER BY Id DESC
LIMIT 1;
";

            command.Parameters.AddWithValue(
                "$FarmProfileId",
                farmProfileId);

            using var reader =
                command.ExecuteReader();

            if (!reader.Read())
            {
                return null;
            }

            return new WaterAssessment
            {
                Id = reader.GetInt32(
                    reader.GetOrdinal("Id")),

                FarmProfileId =
                    reader.GetInt32(
                        reader.GetOrdinal("FarmProfileId")),

                WaterSource =
                    reader["WaterSource"]?.ToString() ?? "",

                WaterAvailability =
                    reader["WaterAvailability"]?.ToString() ?? "",

                IrrigationMethod =
                    reader["IrrigationMethod"]?.ToString() ?? "",

                IrrigationFrequency =
                    reader["IrrigationFrequency"]?.ToString() ?? "",

                WaterQualityKnowledge =
                    reader["WaterQualityKnowledge"]?.ToString() ?? "",

                DrainageCondition =
                    reader["DrainageCondition"]?.ToString() ?? "",

                RecentWaterProblem =
                    reader["RecentWaterProblem"]?.ToString() ?? "",

                Result =
                    reader["Result"]?.ToString() ?? "",

                CreatedAt =
                    DateTime.Parse(
                        reader["CreatedAt"]?.ToString()
                        ?? DateTime.UtcNow.ToString("O"))
            };
        }

        // =========================================================
        // DELETE FARM
        // Deletes the farm and all data belonging to it.
        // UserId check prevents deleting another user's farm.
        // =========================================================

        public bool DeleteFarm(
            int farmId,
            string userId)
        {
            using var connection =
                new SqliteConnection(_connectionString);

            connection.Open();

            using var transaction =
                connection.BeginTransaction();

            try
            {
                // -------------------------------------------------
                // FIRST VERIFY THAT THIS FARM BELONGS TO THE USER
                // -------------------------------------------------

                using (var checkCommand =
                    connection.CreateCommand())
                {
                    checkCommand.Transaction = transaction;

                    checkCommand.CommandText = @"
SELECT COUNT(*)
FROM FarmProfiles
WHERE Id = $FarmId
AND UserId = $UserId;
";

                    checkCommand.Parameters.AddWithValue(
                        "$FarmId",
                        farmId);

                    checkCommand.Parameters.AddWithValue(
                        "$UserId",
                        userId);

                    var exists =
                        Convert.ToInt32(
                            checkCommand.ExecuteScalar());

                    if (exists == 0)
                    {
                        transaction.Rollback();

                        return false;
                    }
                }


                // -------------------------------------------------
                // DELETE FARM CROPS
                // -------------------------------------------------

                using (var command =
                    connection.CreateCommand())
                {
                    command.Transaction = transaction;

                    command.CommandText = @"
DELETE FROM FarmCrops
WHERE FarmId = $FarmId
AND UserId = $UserId;
";

                    command.Parameters.AddWithValue(
                        "$FarmId",
                        farmId);

                    command.Parameters.AddWithValue(
                        "$UserId",
                        userId);

                    command.ExecuteNonQuery();
                }


                // -------------------------------------------------
                // DELETE FARM ACTIVITIES
                // -------------------------------------------------

                using (var command =
                    connection.CreateCommand())
                {
                    command.Transaction = transaction;

                    command.CommandText = @"
DELETE FROM FarmActivities
WHERE FarmProfileId = $FarmId;
";

                    command.Parameters.AddWithValue(
                        "$FarmId",
                        farmId);

                    command.ExecuteNonQuery();
                }


                // -------------------------------------------------
                // DELETE LEARNING PROGRESS
                // -------------------------------------------------

                using (var command =
                    connection.CreateCommand())
                {
                    command.Transaction = transaction;

                    command.CommandText = @"
DELETE FROM LearningLessonCompletions
WHERE FarmProfileId = $FarmId;
";

                    command.Parameters.AddWithValue(
                        "$FarmId",
                        farmId);

                    command.ExecuteNonQuery();
                }


                // -------------------------------------------------
                // DELETE CROP OBSERVATIONS
                // -------------------------------------------------

                using (var command =
                    connection.CreateCommand())
                {
                    command.Transaction = transaction;

                    command.CommandText = @"
DELETE FROM CropObservations
WHERE FarmProfileId = $FarmId;
";

                    command.Parameters.AddWithValue(
                        "$FarmId",
                        farmId);

                    command.ExecuteNonQuery();
                }


                // -------------------------------------------------
                // DELETE SOIL ASSESSMENTS
                // -------------------------------------------------

                using (var command =
                    connection.CreateCommand())
                {
                    command.Transaction = transaction;

                    command.CommandText = @"
DELETE FROM SoilAssessments
WHERE FarmProfileId = $FarmId;
";

                    command.Parameters.AddWithValue(
                        "$FarmId",
                        farmId);

                    command.ExecuteNonQuery();
                }


                // -------------------------------------------------
                // DELETE WATER ASSESSMENTS
                // -------------------------------------------------

                using (var command =
                    connection.CreateCommand())
                {
                    command.Transaction = transaction;

                    command.CommandText = @"
DELETE FROM WaterAssessments
WHERE FarmProfileId = $FarmId;
";

                    command.Parameters.AddWithValue(
                        "$FarmId",
                        farmId);

                    command.ExecuteNonQuery();
                }


                // -------------------------------------------------
                // FINALLY DELETE THE FARM
                // -------------------------------------------------

                int deletedRows;

                using (var command =
                    connection.CreateCommand())
                {
                    command.Transaction = transaction;

                    command.CommandText = @"
DELETE FROM FarmProfiles
WHERE Id = $FarmId
AND UserId = $UserId;
";

                    command.Parameters.AddWithValue(
                        "$FarmId",
                        farmId);

                    command.Parameters.AddWithValue(
                        "$UserId",
                        userId);

                    deletedRows =
                        command.ExecuteNonQuery();
                }


                // -------------------------------------------------
                // COMMIT
                // -------------------------------------------------

                if (deletedRows > 0)
                {
                    transaction.Commit();

                    return true;
                }


                transaction.Rollback();

                return false;
            }
            catch
            {
                transaction.Rollback();

                throw;
            }
        }
    }
}