DECLARE @UserId NVARCHAR(450);
SELECT TOP 1 @UserId = Id FROM AspNetUsers WHERE Email = 'clienttwo@gmail.com';

INSERT INTO HealthProfiles (UserId, Weight, Height, BMI, DateOfBirth, MedicalNotes)
VALUES (@UserId, 47.0, 164.0, 17.47, DATEADD(year, -25, GETDATE()), 'Female client, Age 25');

INSERT INTO HealthRecords (ClientId, Weight, Height, BMI, CaloriesBurned, RecordDate, HeartRate, BloodPressure)
VALUES (@UserId, 47.0, 164.0, 17.47, 200, GETDATE(), 78, '110/70');