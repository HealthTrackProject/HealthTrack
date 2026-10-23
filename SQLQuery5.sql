DECLARE @ClientId NVARCHAR(450);
DECLARE @Trainer1Id NVARCHAR(450);
DECLARE @Trainer2Id NVARCHAR(450);

SELECT TOP 1 @ClientId = Id FROM AspNetUsers WHERE Email = 'clienttwo@gmail.com'; 
SELECT TOP 1 @Trainer1Id = Id FROM AspNetUsers WHERE Email = 'trainer1@gmail.com';
SELECT TOP 1 @Trainer2Id = Id FROM AspNetUsers WHERE Email = 'trainer2@gmail.com';

DECLARE @TrainerRoleId NVARCHAR(450);
SELECT TOP 1 @TrainerRoleId = Id FROM AspNetRoles WHERE Name = 'Trainer';

IF NOT EXISTS (SELECT 1 FROM AspNetUserRoles WHERE UserId = @Trainer1Id AND RoleId = @TrainerRoleId)
    INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES (@Trainer1Id, @TrainerRoleId);

IF NOT EXISTS (SELECT 1 FROM AspNetUserRoles WHERE UserId = @Trainer2Id AND RoleId = @TrainerRoleId)
    INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES (@Trainer2Id, @TrainerRoleId);

IF NOT EXISTS (SELECT 1 FROM HealthProfiles WHERE UserId = @Trainer1Id)
    INSERT INTO HealthProfiles (UserId, Weight, Height, BMI, DateOfBirth, MedicalNotes)
    VALUES (@Trainer1Id, 75.0, 180.0, 23.1, '1990-01-01', 'Certified Personal Trainer');

IF NOT EXISTS (SELECT 1 FROM HealthProfiles WHERE UserId = @Trainer2Id)
    INSERT INTO HealthProfiles (UserId, Weight, Height, BMI, DateOfBirth, MedicalNotes)
    VALUES (@Trainer2Id, 65.0, 165.0, 23.8, '1992-05-15', 'Yoga and Nutrition Expert');

INSERT INTO Appointments (ClientId, TrainerId, AppointmentDate, Reason, Status)
VALUES 
-- Upcoming Appointments (ඉදිරියට)
(@ClientId, @Trainer1Id, DATEADD(day, 2, GETDATE()), 'Initial fitness assessment', 'Approved'),
(@ClientId, @Trainer2Id, DATEADD(day, 5, GETDATE()), 'Diet plan discussion', 'Pending'),

-- Past Appointments (අතීතය)
(@ClientId, @Trainer1Id, DATEADD(day, -10, GETDATE()), 'Follow-up workout session', 'Approved'),
(@ClientId, @Trainer2Id, DATEADD(day, -20, GETDATE()), 'Monthly progress review', 'Cancelled'),
(@ClientId, @Trainer1Id, DATEADD(day, -30, GETDATE()), 'Body composition analysis', 'Approved');