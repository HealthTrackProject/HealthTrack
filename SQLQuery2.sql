DECLARE @ClientId NVARCHAR(450);
SELECT TOP 1 @ClientId = Id FROM AspNetUsers WHERE Email = 'clienttwo@gmail.com';

INSERT INTO Notifications (UserId, Title, Message, CreatedAt, IsRead)
VALUES 
(@ClientId, 'Workout Reminder', 'Don''t forget your Chest & Arms workout today!', DATEADD(minute, -15, GETDATE()), 0),
(@ClientId, 'Appointment Confirmed', 'Your session with Trainer Mike is confirmed for tomorrow at 10 AM.', DATEADD(hour, -3, GETDATE()), 0),
(@ClientId, 'Diet Plan Updated', 'Your trainer has updated your meal plan.', DATEADD(day, -1, GETDATE()), 0),
(@ClientId, 'Welcome to HealthTrack', 'Start your healthy journey by logging your first workout.', DATEADD(day, -5, GETDATE()), 1);