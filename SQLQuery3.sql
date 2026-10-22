DECLARE @ClientId NVARCHAR(450);
SELECT TOP 1 @ClientId = Id FROM AspNetUsers WHERE FullName = 'client two' OR UserName LIKE '%client two%';

INSERT INTO HealthRecords (ClientId, Weight, Height, BMI, CaloriesBurned, RecordDate, HeartRate, BloodPressure)
VALUES 
-- May 2026 (Weight: 53.5 kg, BMI: 19.89)
(@ClientId, 53.5, 164.0, 19.89, 250, '2026-05-15', 82, '118/78'),

-- June 2026 (Weight: 52.0 kg, BMI: 19.33)
(@ClientId, 52.0, 164.0, 19.33, 300, '2026-06-15', 80, '120/80'),

-- July 2026 (Weight: 50.5 kg, BMI: 18.78)
(@ClientId, 50.5, 164.0, 18.78, 280, '2026-07-15', 79, '115/75'),

-- August 2026 (Weight: 49.0 kg, BMI: 18.22)
(@ClientId, 49.0, 164.0, 18.22, 350, '2026-08-15', 78, '112/72'),

-- September 2026 (Weight: 48.0 kg, BMI: 17.85)
(@ClientId, 48.0, 164.0, 17.85, 320, '2026-09-15', 76, '110/70');