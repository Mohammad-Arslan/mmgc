-- Script to manually create nurse profile for existing user
-- Run this in SQLPad or SQL Server Management Studio

-- First, get the UserId for the nurse user
DECLARE @UserId NVARCHAR(450);
SELECT @UserId = Id FROM AspNetUsers WHERE Email = 'miwum@mailinator.com';

IF @UserId IS NOT NULL
BEGIN
    -- Check if nurse profile already exists
    IF NOT EXISTS (SELECT 1 FROM Nurses WHERE UserId = @UserId)
    BEGIN
        -- Create nurse profile
        INSERT INTO Nurses (FirstName, LastName, Email, ContactNumber, Department, IsActive, CreatedDate, UserId)
        SELECT 
            FirstName,
            LastName,
            Email,
            PhoneNumber,
            'Nursing' AS Department,
            1 AS IsActive,
            GETDATE() AS CreatedDate,
            Id AS UserId
        FROM AspNetUsers
        WHERE Id = @UserId;
        
        PRINT 'Nurse profile created successfully!';
        SELECT * FROM Nurses WHERE UserId = @UserId;
    END
    ELSE
    BEGIN
        PRINT 'Nurse profile already exists for this user.';
        SELECT * FROM Nurses WHERE UserId = @UserId;
    END
END
ELSE
BEGIN
    PRINT 'User not found. Please check the email address.';
END

-- To create profiles for ALL nurse users without profiles:
/*
INSERT INTO Nurses (FirstName, LastName, Email, ContactNumber, Department, IsActive, CreatedDate, UserId)
SELECT 
    u.FirstName,
    u.LastName,
    u.Email,
    u.PhoneNumber,
    'Nursing' AS Department,
    1 AS IsActive,
    GETDATE() AS CreatedDate,
    u.Id AS UserId
FROM AspNetUsers u
INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE r.Name = 'Nurse'
  AND NOT EXISTS (SELECT 1 FROM Nurses n WHERE n.UserId = u.Id);
*/
