DECLARE @UserId NVARCHAR(450);
SELECT TOP 1 @UserId = Id FROM AspNetUsers WHERE Email = 'testclient@gmail.com';
IF NOT EXISTS (SELECT 1 FROM HealthProfiles WHERE UserId = @UserId)
BEGIN
    INSERT INTO HealthProfiles (UserId, Weight, Height, BMI, DateOfBirth, MedicalNotes)
    VALUES (@UserId, 72.0, 175.0, 23.5, '2000-01-01', 'No notes');
END
INSERT INTO HealthRecords (ClientId, Weight, Height, BMI, CaloriesBurned, RecordDate)
VALUES 
(@UserId, 74.0, 175.0, 24.2, 300, '2026-08-09'),
(@UserId, 73.0, 175.0, 23.8, 320, '2026-09-09'),
(@UserId, 72.0, 175.0, 23.5, 350, '2026-10-09');